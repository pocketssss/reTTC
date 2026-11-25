using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class VzzOcr
{
	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 16777216
	};

	public static string Site = "https://timetocapture.ru";

	public static Func<string> KeyOf = License.SavedKey;

	public static Func<string> HwidOf = License.Hwid;

	public static bool Hello(out int ms, out string error)
	{
		DateTime utcNow = DateTime.UtcNow;
		Dictionary<string, object> dictionary = Post("/api/v1/vzz/hello", Base(), 20000, out error);
		ms = (int)(DateTime.UtcNow - utcNow).TotalMilliseconds;
		return dictionary != null;
	}

	public static bool Read(byte[] image, string msg, out string code, out double sure, out int ms, out string error)
	{
		code = "";
		sure = 0.0;
		Dictionary<string, object> dictionary = Base();
		dictionary["image"] = Convert.ToBase64String(image);
		dictionary["msg"] = msg ?? "";
		DateTime utcNow = DateTime.UtcNow;
		Dictionary<string, object> dictionary2 = Post("/api/v1/vzz/read", dictionary, 10000, out error);
		ms = (int)(DateTime.UtcNow - utcNow).TotalMilliseconds;
		if (dictionary2 == null)
		{
			return false;
		}
		code = ((dictionary2.TryGetValue("code", out var value) && value != null) ? Convert.ToString(value, CultureInfo.InvariantCulture) : "");
		sure = ((dictionary2.TryGetValue("sure", out var value2) && value2 != null) ? Convert.ToDouble(value2, CultureInfo.InvariantCulture) : 0.0);
		return true;
	}

	public static void Verdict(string msg, string code, int votes, int total)
	{
		if (!string.IsNullOrEmpty(msg))
		{
			Dictionary<string, object> b = Base();
			b["msg"] = msg;
			b["code"] = code;
			b["votes"] = votes;
			b["total"] = total;
			ThreadPool.QueueUserWorkItem(delegate
			{
				Post("/api/v1/vzz/verdict", b, 15000, out var _);
			});
		}
	}

	private static Dictionary<string, object> Base()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["key"] = KeyOf();
		dictionary["hwid"] = HwidOf();
		dictionary["ver"] = "2.9.13";
		return dictionary;
	}

	private static Dictionary<string, object> Post(string path, Dictionary<string, object> body, int timeoutMs, out string error)
	{
		error = null;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			if (ServicePointManager.DefaultConnectionLimit < 8)
			{
				ServicePointManager.DefaultConnectionLimit = 8;
			}
			HttpWebRequest httpWebRequest = SiteNet.Create(Site + path);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json; charset=utf-8";
			httpWebRequest.UserAgent = "TTC/2.9.13";
			httpWebRequest.Timeout = timeoutMs;
			httpWebRequest.ReadWriteTimeout = timeoutMs;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
			httpWebRequest.KeepAlive = true;
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.ServicePoint.UseNagleAlgorithm = false;
			byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(body));
			httpWebRequest.ContentLength = bytes.Length;
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			string input;
			try
			{
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
				input = streamReader.ReadToEnd();
			}
			catch (WebException ex)
			{
				if (!(ex.Response is HttpWebResponse httpWebResponse2))
				{
					throw;
				}
				using StreamReader streamReader2 = new StreamReader(httpWebResponse2.GetResponseStream(), Encoding.UTF8);
				input = streamReader2.ReadToEnd();
			}
			if (!(Json.DeserializeObject(input) is Dictionary<string, object> dictionary))
			{
				error = "пустой ответ сервера";
				return null;
			}
			if (!dictionary.TryGetValue("ok", out var value) || !(value is bool) || !(bool)value)
			{
				error = ((dictionary.TryGetValue("error", out var value2) && value2 != null) ? Convert.ToString(value2) : "сервер отказал");
				return null;
			}
			return dictionary;
		}
		catch (Exception ex2)
		{
			error = ex2.Message;
			return null;
		}
	}
}
