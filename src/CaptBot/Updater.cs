using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class Updater
{
	private const int MaxSize = 67108864;

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

	public static string ExePath => Process.GetCurrentProcess().MainModule.FileName;

	public static bool IsNewer(string a, string b)
	{
		if (Version.TryParse(Fix(a), out var result) && Version.TryParse(Fix(b), out var result2))
		{
			return result > result2;
		}
		if (!string.IsNullOrEmpty(a))
		{
			return a != b;
		}
		return false;
	}

	private static string Fix(string v)
	{
		v = (v ?? "").Trim();
		if (v.IndexOf('.') >= 0)
		{
			return v;
		}
		return v + ".0";
	}

	public static string Fetch(string baseUrl, out string newPath, out string version)
	{
		newPath = null;
		version = null;
		try
		{
			string text = Guid.NewGuid().ToString("N");
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["key"] = License.SavedKey();
			dictionary["hwid"] = License.Hwid();
			dictionary["nonce"] = text;
			dictionary["ver"] = "2.9.13";
			string json = Json.Serialize(dictionary);
			Dictionary<string, object> dictionary2 = Json.DeserializeObject(Encoding.UTF8.GetString(Post(baseUrl + "/api/v1/update/info", json, 1048576))) as Dictionary<string, object>;
			if (dictionary2 == null || !dictionary2.ContainsKey("d"))
			{
				return (dictionary2 != null && dictionary2.ContainsKey("error")) ? Convert.ToString(dictionary2["error"]) : "сервер не прислал сведения о сборке";
			}
			byte[] array = Convert.FromBase64String(Convert.ToString(dictionary2["d"]));
			byte[] sig = Convert.FromBase64String(Convert.ToString(dictionary2["s"]));
			if (!License.Verify(array, sig))
			{
				return "подпись сведений о сборке не сошлась — обновление отменено";
			}
			if (!(Json.DeserializeObject(Encoding.UTF8.GetString(array)) is Dictionary<string, object> dictionary3) || Str(dictionary3, "nonce") != text || Str(dictionary3, "hwid") != License.Hwid())
			{
				return "сведения о сборке не для этого запроса — обновление отменено";
			}
			version = Str(dictionary3, "version");
			string text2 = Str(dictionary3, "sha256").ToLowerInvariant();
			long num = Convert.ToInt64(dictionary3["size"]);
			if (num <= 0 || num > 67108864 || text2.Length != 64)
			{
				return "сервер прислал странные сведения о сборке";
			}
			byte[] array2 = Post(baseUrl + "/api/v1/update/download", json, 67108864);
			if (array2.Length != num)
			{
				return "файл скачался не полностью (" + array2.Length + " из " + num + " байт)";
			}
			string text3;
			using (SHA256 sHA = SHA256.Create())
			{
				text3 = BitConverter.ToString(sHA.ComputeHash(array2)).Replace("-", "").ToLowerInvariant();
			}
			if (text3 != text2)
			{
				return "файл не совпал с подписанной суммой — обновление отменено";
			}
			if (array2.Length < 2 || array2[0] != 77 || array2[1] != 90)
			{
				return "скачанный файл — не программа";
			}
			newPath = ExePath + ".new";
			File.WriteAllBytes(newPath, array2);
			return null;
		}
		catch (WebException ex)
		{
			if (ex.Response is HttpWebResponse httpWebResponse)
			{
				try
				{
					using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
					if (Json.DeserializeObject(streamReader.ReadToEnd()) is Dictionary<string, object> dictionary4 && dictionary4.ContainsKey("error"))
					{
						return Convert.ToString(dictionary4["error"]);
					}
				}
				catch
				{
				}
				return "сервер ответил " + (int)httpWebResponse.StatusCode;
			}
			return ex.Message;
		}
		catch (UnauthorizedAccessException)
		{
			return "нет прав на запись в папку программы — скачайте сборку в личном кабинете";
		}
		catch (Exception ex3)
		{
			return ex3.Message;
		}
	}

	public static string Swap(string exe, string newPath)
	{
		string text = exe + ".old";
		try
		{
			if (File.Exists(text))
			{
				File.Delete(text);
			}
		}
		catch
		{
			text = exe + "." + DateTime.Now.Ticks + ".old";
		}
		try
		{
			File.Move(exe, text);
		}
		catch (Exception ex)
		{
			return "не удалось заменить файл программы: " + ex.Message;
		}
		try
		{
			File.Move(newPath, exe);
		}
		catch (Exception ex2)
		{
			try
			{
				File.Move(text, exe);
			}
			catch
			{
			}
			return "не удалось поставить новую версию: " + ex2.Message;
		}
		return null;
	}

	public static void Rollback(string exe)
	{
		try
		{
			if (File.Exists(exe + ".old"))
			{
				File.Delete(exe);
				File.Move(exe + ".old", exe);
			}
		}
		catch
		{
		}
	}

	public static bool Start(string exe)
	{
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(exe);
			processStartInfo.UseShellExecute = false;
			processStartInfo.WorkingDirectory = Path.GetDirectoryName(exe);
			Process.Start(processStartInfo);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public static void Cleanup()
	{
		try
		{
			string exePath = ExePath;
			string directoryName = Path.GetDirectoryName(exePath);
			string fileName = Path.GetFileName(exePath);
			string[] files = Directory.GetFiles(directoryName, fileName + "*.old");
			foreach (string path in files)
			{
				try
				{
					File.Delete(path);
				}
				catch
				{
				}
			}
			if (File.Exists(exePath + ".new"))
			{
				File.Delete(exePath + ".new");
			}
		}
		catch
		{
		}
	}

	private static byte[] Post(string url, string json, int limit)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest httpWebRequest = SiteNet.Create(url);
		httpWebRequest.Method = "POST";
		httpWebRequest.ContentType = "application/json; charset=utf-8";
		httpWebRequest.UserAgent = "TTC/2.9.13";
		httpWebRequest.Timeout = 30000;
		httpWebRequest.ReadWriteTimeout = 60000;
		httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		httpWebRequest.ContentLength = bytes.Length;
		using (Stream stream = httpWebRequest.GetRequestStream())
		{
			stream.Write(bytes, 0, bytes.Length);
		}
		using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		using Stream stream2 = httpWebResponse.GetResponseStream();
		using MemoryStream memoryStream = new MemoryStream();
		byte[] array = new byte[81920];
		int count;
		while ((count = stream2.Read(array, 0, array.Length)) > 0)
		{
			memoryStream.Write(array, 0, count);
			if (memoryStream.Length > limit)
			{
				throw new Exception("ответ сервера слишком большой");
			}
		}
		return memoryStream.ToArray();
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture);
	}
}
