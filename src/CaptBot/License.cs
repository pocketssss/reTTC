using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace CaptBot;

internal static class License
{
	private class Saved
	{
		public string Key { get; set; }

		public string D { get; set; }

		public string S { get; set; }

		public long T { get; set; }
	}

	public const string Version = "2.9.13";

	public static volatile LicenseState Current;

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

	private static string hwid;

	private static string exeHash;

	private static string FilePath => Path.Combine(AppPaths.Data, "license.dat");

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	private static extern bool GetVolumeInformation(string root, StringBuilder name, int nameLen, out uint serial, out uint maxLen, out uint flags, StringBuilder fs, int fsLen);

	public static string Hwid()
	{
		if (hwid != null)
		{
			return hwid;
		}
		string text = "";
		string text2 = "";
		uint serial = 0u;
		try
		{
			using RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
			using (RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography"))
			{
				if (registryKey2 != null)
				{
					text = (registryKey2.GetValue("MachineGuid") as string) ?? "";
				}
			}
			using RegistryKey registryKey3 = registryKey.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
			if (registryKey3 != null)
			{
				text2 = ((registryKey3.GetValue("ProcessorNameString") as string) ?? "").Trim();
			}
		}
		catch
		{
		}
		try
		{
			GetVolumeInformation(Path.GetPathRoot(Environment.SystemDirectory), null, 0, out serial, out var _, out var _, null, 0);
		}
		catch
		{
		}
		using (SHA256 sHA = SHA256.Create())
		{
			byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes("captbot|" + text + "|" + serial.ToString("X8") + "|" + text2));
			StringBuilder stringBuilder = new StringBuilder("H1");
			for (int i = 0; i < 16; i++)
			{
				stringBuilder.Append(array[i].ToString("X2"));
			}
			hwid = stringBuilder.ToString();
		}
		return hwid;
	}

	private static Saved Load()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				return Json.Deserialize<Saved>(File.ReadAllText(FilePath, Encoding.UTF8));
			}
		}
		catch
		{
		}
		return null;
	}

	private static void Save(Saved s)
	{
		try
		{
			File.WriteAllText(FilePath, Json.Serialize(s), Encoding.UTF8);
		}
		catch
		{
		}
	}

	public static string SavedKey()
	{
		Saved saved = Load();
		if (saved == null)
		{
			return "";
		}
		return saved.Key ?? "";
	}

	public static void Forget()
	{
		try
		{
			File.Delete(FilePath);
		}
		catch
		{
		}
		Current = null;
	}

	public static string Normalize(string key)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = (key ?? "").ToUpperInvariant();
		foreach (char c in text)
		{
			if (!char.IsWhiteSpace(c))
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}

	public static LicenseState Check(string key)
	{
		key = Normalize(key);
		if (string.IsNullOrEmpty("uZmdSu2TJuMMByTtBvSbllwLNj7ynJPEqRu31braqfG/vsrvF5ITHOwrg2DPFOB/kN2UszENrAlzNgQ9QmOeCgf9hn/y3AhEVrr2WpA/oAMc5bnMToZey55c49k32D2o5npCziXm3Sv7ox1RZ9IQxbd5pc1sQFrtXWwCKl94WCXxgitB4BsXAUjTkll5ucNtRDO1AfrZwhvIdSj7nXshNanK6UE/VECrK2r7alwbSBnIdiYrxy9MaoIcyt96No9yt3xTmUFsZd2UcRUm7m5cTcKKTthBeZOC+JGz9ZcOrwDFKJBBPSClMwvvpK8wcgUmRDXZ27ZCF5jXDgCOJYj8TQ==") || "https://timetocapture.ru/api/v1/license".Contains("example.com"))
		{
			LicenseState licenseState = new LicenseState();
			licenseState.Key = key;
			licenseState.Code = "network";
			licenseState.Message = "Программа не настроена: не задан сервер ключей (LicenseConfig.cs). Соберите её заново после настройки сервера.";
			return licenseState;
		}
		string text = Guid.NewGuid().ToString("N");
		string resp;
		try
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["key"] = key;
			dictionary["hwid"] = Hwid();
			dictionary["nonce"] = text;
			dictionary["ver"] = "2.9.13";
			dictionary["exe"] = ExeHash();
			dictionary["gs"] = NetWatch.GameServer ?? "";
			dictionary["fam"] = NetWatch.OwnFamily ?? "";
			resp = Post("https://timetocapture.ru/api/v1/license", Json.Serialize(dictionary));
		}
		catch (Exception ex)
		{
			LicenseState licenseState2 = FromCache(key);
			if (licenseState2 != null)
			{
				return licenseState2;
			}
			LicenseState licenseState3 = new LicenseState();
			licenseState3.Key = key;
			licenseState3.Code = "network";
			licenseState3.Message = SiteNet.Why(ex);
			return licenseState3;
		}
		string d;
		string s;
		LicenseState licenseState4 = Parse(resp, text, out d, out s);
		if (licenseState4.Code == "bad_signature")
		{
			LicenseState licenseState5 = FromCache(key);
			return licenseState5 ?? licenseState4;
		}
		Saved saved = Load();
		long t = Math.Max(saved?.T ?? 0, licenseState4.ServerTime);
		if (licenseState4.Ok)
		{
			Saved saved2 = new Saved();
			saved2.Key = key;
			saved2.D = d;
			saved2.S = s;
			saved2.T = t;
			Save(saved2);
		}
		else if (licenseState4.Definitive && saved != null && saved.Key == key)
		{
			Saved saved3 = new Saved();
			saved3.Key = key;
			saved3.T = t;
			Save(saved3);
		}
		return licenseState4;
	}

	private static LicenseState Parse(string resp, string nonce, out string d, out string s)
	{
		d = null;
		s = null;
		LicenseState licenseState = new LicenseState();
		try
		{
			Dictionary<string, object> d2 = Json.DeserializeObject(resp) as Dictionary<string, object>;
			d = Str(d2, "d");
			s = Str(d2, "s");
			byte[] array = Convert.FromBase64String(d);
			byte[] sig = Convert.FromBase64String(s);
			if (!Verify(array, sig))
			{
				throw new Exception("подпись");
			}
			Dictionary<string, object> dictionary = Json.DeserializeObject(Encoding.UTF8.GetString(array)) as Dictionary<string, object>;
			if (nonce != null && Str(dictionary, "nonce") != nonce)
			{
				throw new Exception("nonce");
			}
			if (Str(dictionary, "hwid") != Hwid())
			{
				throw new Exception("hwid");
			}
			Fill(licenseState, dictionary);
			return licenseState;
		}
		catch
		{
			licenseState.Ok = false;
			licenseState.Code = "bad_signature";
			licenseState.Message = "Ответ сервера лицензий не прошёл проверку. Обновите программу из личного кабинета.";
			return licenseState;
		}
	}

	private static void Fill(LicenseState st, Dictionary<string, object> p)
	{
		st.Ok = Bool(p, "ok");
		st.Code = Str(p, "code");
		st.Message = Str(p, "message");
		st.Key = Str(p, "key");
		st.Expires = Long(p, "expires");
		st.Lifetime = Bool(p, "lifetime");
		st.ServerTime = Long(p, "server_time");
		st.Latest = Str(p, "latest");
		st.GraceHours = (int)Long(p, "grace_hours");
		st.Role = Str(p, "role");
		object value;
		Dictionary<string, object> dictionary = ((p != null && p.TryGetValue("family", out value)) ? (value as Dictionary<string, object>) : null);
		st.FamilySeat = dictionary != null;
		if (dictionary != null)
		{
			st.Family = Str(dictionary, "name");
			st.FamilyServer = Str(dictionary, "server");
			st.FamilyNeed = Bool(dictionary, "need");
		}
	}

	private static LicenseState FromCache(string key)
	{
		Saved saved = Load();
		if (saved == null || saved.Key != key || string.IsNullOrEmpty(saved.D) || string.IsNullOrEmpty(saved.S))
		{
			return null;
		}
		try
		{
			byte[] array = Convert.FromBase64String(saved.D);
			if (!Verify(array, Convert.FromBase64String(saved.S)))
			{
				return null;
			}
			Dictionary<string, object> dictionary = Json.DeserializeObject(Encoding.UTF8.GetString(array)) as Dictionary<string, object>;
			if (Str(dictionary, "hwid") != Hwid() || Normalize(Str(dictionary, "key")) != key)
			{
				return null;
			}
			LicenseState licenseState = new LicenseState();
			Fill(licenseState, dictionary);
			if (!licenseState.Ok)
			{
				return null;
			}
			DateTime dateTime = Clock.UtcNow();
			if (dateTime - licenseState.ServerUtc > TimeSpan.FromHours(licenseState.GraceHours) || licenseState.ServerUtc - dateTime > TimeSpan.FromHours(1.0))
			{
				return null;
			}
			if (saved.T > 0 && dateTime < LicenseState.Epoch.AddSeconds(saved.T) - TimeSpan.FromMinutes(10.0))
			{
				return null;
			}
			if (!licenseState.Lifetime && licenseState.ExpiresUtc <= dateTime)
			{
				return null;
			}
			licenseState.Offline = true;
			licenseState.Message = "Сервер недоступен — работаю по последней проверке от " + licenseState.ServerUtc.AddHours(3.0).ToString("dd.MM HH:mm") + " МСК";
			return licenseState;
		}
		catch
		{
			return null;
		}
	}

	public static void Touch()
	{
		Saved saved = Load();
		if (saved != null && !string.IsNullOrEmpty(saved.Key))
		{
			long num = (long)(Clock.UtcNow() - LicenseState.Epoch).TotalSeconds;
			if (num > saved.T)
			{
				saved.T = num;
				Save(saved);
			}
		}
	}

	public static string ExeHash()
	{
		if (exeHash != null)
		{
			return exeHash;
		}
		try
		{
			using SHA256 sHA = SHA256.Create();
			using FileStream inputStream = File.OpenRead(Process.GetCurrentProcess().MainModule.FileName);
			exeHash = BitConverter.ToString(sHA.ComputeHash(inputStream)).Replace("-", "").ToLowerInvariant();
		}
		catch
		{
			exeHash = "";
		}
		return exeHash;
	}

	public static bool UsableNow()
	{
		LicenseState current = Current;
		if (current == null || !current.Ok)
		{
			return false;
		}
		if (current.Offline && Clock.UtcNow() - current.ServerUtc > TimeSpan.FromHours(current.GraceHours))
		{
			return false;
		}
		if (!current.Lifetime)
		{
			return current.ExpiresUtc > Clock.UtcNow();
		}
		return true;
	}

	internal static bool Verify(byte[] data, byte[] sig)
	{
		try
		{
			RSAParameters parameters = default(RSAParameters);
			parameters.Modulus = Convert.FromBase64String("uZmdSu2TJuMMByTtBvSbllwLNj7ynJPEqRu31braqfG/vsrvF5ITHOwrg2DPFOB/kN2UszENrAlzNgQ9QmOeCgf9hn/y3AhEVrr2WpA/oAMc5bnMToZey55c49k32D2o5npCziXm3Sv7ox1RZ9IQxbd5pc1sQFrtXWwCKl94WCXxgitB4BsXAUjTkll5ucNtRDO1AfrZwhvIdSj7nXshNanK6UE/VECrK2r7alwbSBnIdiYrxy9MaoIcyt96No9yt3xTmUFsZd2UcRUm7m5cTcKKTthBeZOC+JGz9ZcOrwDFKJBBPSClMwvvpK8wcgUmRDXZ27ZCF5jXDgCOJYj8TQ==");
			parameters.Exponent = Convert.FromBase64String("AQAB");
			using RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.PersistKeyInCsp = false;
			rSACryptoServiceProvider.ImportParameters(parameters);
			using SHA256 sHA = SHA256.Create();
			return rSACryptoServiceProvider.VerifyHash(sHA.ComputeHash(data), CryptoConfig.MapNameToOID("SHA256"), sig);
		}
		catch
		{
			return false;
		}
	}

	private static string Post(string url, string json)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest r = SiteNet.Create(url);
		try
		{
			return Send(r, json);
		}
		catch (WebException ex)
		{
			HttpWebRequest httpWebRequest = SiteNet.Retry(r, ex);
			if (httpWebRequest == null)
			{
				throw;
			}
			string result;
			try
			{
				result = Send(httpWebRequest, json);
			}
			catch (WebException)
			{
				throw ex;
			}
			SiteNet.Keep();
			return result;
		}
	}

	private static string Send(HttpWebRequest r, string json)
	{
		r.Method = "POST";
		r.ContentType = "application/json; charset=utf-8";
		r.UserAgent = "TTC/2.9.13";
		r.Timeout = 12000;
		r.ReadWriteTimeout = 12000;
		r.Proxy = WebRequest.DefaultWebProxy;
		byte[] bytes = Encoding.UTF8.GetBytes(json);
		r.ContentLength = bytes.Length;
		using (Stream stream = r.GetRequestStream())
		{
			stream.Write(bytes, 0, bytes.Length);
		}
		using HttpWebResponse httpWebResponse = (HttpWebResponse)r.GetResponse();
		using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture);
	}

	private static long Long(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return 0L;
		}
		try
		{
			return Convert.ToInt64(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0L;
		}
	}

	private static bool Bool(Dictionary<string, object> d, string k)
	{
		if (d != null && d.TryGetValue(k, out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	public static void OpenSite(string path)
	{
		AppPaths.OpenUrl("https://timetocapture.ru" + path);
	}
}
