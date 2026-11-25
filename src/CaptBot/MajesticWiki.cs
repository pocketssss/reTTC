using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class MajesticWiki
{
	private class Decoder
	{
		private readonly object[] flat;

		private readonly Dictionary<int, object> memo = new Dictionary<int, object>();

		public Decoder(object[] f)
		{
			flat = f;
		}

		public object At(object idx)
		{
			if (!(idx is int num))
			{
				return idx;
			}
			if (num < 0 || num >= flat.Length)
			{
				return null;
			}
			if (memo.TryGetValue(num, out var value))
			{
				return value;
			}
			object obj = flat[num];
			if (obj is object[] array)
			{
				if (array.Length > 0 && array[0] is string)
				{
					object obj2 = ((array[0].Equals("D") && array.Length > 1) ? array[1] : null);
					memo[num] = obj2;
					return obj2;
				}
				List<object> list = new List<object>();
				memo[num] = list;
				object[] array2 = array;
				foreach (object idx2 in array2)
				{
					list.Add(At(idx2));
				}
				return list;
			}
			if (obj is Dictionary<string, object> dictionary)
			{
				Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
				memo[num] = dictionary2;
				{
					foreach (KeyValuePair<string, object> item in dictionary)
					{
						if (item.Key.Length >= 2 && item.Key[0] == '_' && int.TryParse(item.Key.Substring(1), out var result) && result >= 0 && result < flat.Length && flat[result] is string key)
						{
							dictionary2[key] = At(item.Value);
						}
					}
					return dictionary2;
				}
			}
			memo[num] = obj;
			return obj;
		}
	}

	public const string Site = "https://wiki.majestic-rp.ru";

	public const string ServersUrl = "https://wiki.majestic-rp.ru/ru/servers.data?_routes=routes%2Fservers";

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 16777216
	};

	private static string CachePath => Path.Combine(AppPaths.Data, "servers.json");

	public static string CapturesUrl(string serverId)
	{
		return "https://wiki.majestic-rp.ru/ru/map/captures" + (string.IsNullOrEmpty(serverId) ? "" : ("?server=" + Uri.EscapeDataString(serverId.ToUpperInvariant())));
	}

	public static void OpenCaptures(string serverId)
	{
		AppPaths.OpenUrl(CapturesUrl(serverId));
	}

	public static List<ServerInfo> Load(out string error)
	{
		error = null;
		try
		{
			List<ServerInfo> list = Parse(Get("https://wiki.majestic-rp.ru/ru/servers.data?_routes=routes%2Fservers"));
			if (list.Count > 0)
			{
				SaveCache(list);
				return list;
			}
			error = "вики вернула пустой список серверов";
		}
		catch (Exception ex)
		{
			error = ex.Message;
		}
		List<ServerInfo> list2 = Cached();
		if (list2.Count <= 0)
		{
			return Builtin();
		}
		return list2;
	}

	private static string Get(string url)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
		httpWebRequest.Method = "GET";
		httpWebRequest.UserAgent = "TTC/2.9.13";
		httpWebRequest.Accept = "*/*";
		httpWebRequest.Timeout = 12000;
		httpWebRequest.ReadWriteTimeout = 12000;
		httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
		httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
		using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	public static List<ServerInfo> Parse(string body)
	{
		List<ServerInfo> list = new List<ServerInfo>();
		if (string.IsNullOrEmpty(body))
		{
			return list;
		}
		string input = body.Split('\n')[0];
		if (!(Json.DeserializeObject(input) is object[] array) || array.Length == 0)
		{
			throw new Exception("неожиданный ответ вики");
		}
		Dictionary<string, object> d = new Decoder(array).At(0) as Dictionary<string, object>;
		if (!(Dig(d, "routes/servers", "data", "servers") is IList list2))
		{
			throw new Exception("в ответе вики нет списка серверов");
		}
		foreach (object item in list2)
		{
			if (item is Dictionary<string, object> d2)
			{
				ServerInfo serverInfo = new ServerInfo();
				serverInfo.Id = Str(d2, "id");
				serverInfo.Name = Str(d2, "name");
				if (!string.IsNullOrEmpty(serverInfo.Id) && !string.IsNullOrEmpty(serverInfo.Name))
				{
					serverInfo.Players = (int)Num(d2, "players");
					serverInfo.Queued = (int)Num(d2, "queuedPlayers");
					serverInfo.Online = Bool(d2, "status");
					serverInfo.TechWorks = Bool(d2, "techWorks");
					serverInfo.RegOpened = !string.Equals(Str(d2, "isRegOpened"), "no", StringComparison.OrdinalIgnoreCase);
					serverInfo.Live = true;
					list.Add(serverInfo);
				}
			}
		}
		return (from x in list
			orderby x.Order, x.Name
			select x).ToList();
	}

	private static object Dig(Dictionary<string, object> d, params string[] keys)
	{
		object value = d;
		foreach (string key in keys)
		{
			if (!(value is Dictionary<string, object> dictionary) || !dictionary.TryGetValue(key, out value))
			{
				return null;
			}
		}
		return value;
	}

	private static string Str(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture);
	}

	private static double Num(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || value == null)
		{
			return 0.0;
		}
		try
		{
			return Convert.ToDouble(value, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0.0;
		}
	}

	private static bool Bool(Dictionary<string, object> d, string k)
	{
		if (d.TryGetValue(k, out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	private static void SaveCache(List<ServerInfo> l)
	{
		try
		{
			File.WriteAllText(CachePath, Json.Serialize(l), Encoding.UTF8);
		}
		catch
		{
		}
	}

	public static List<ServerInfo> Cached()
	{
		try
		{
			if (!File.Exists(CachePath))
			{
				return new List<ServerInfo>();
			}
			List<ServerInfo> list = Json.Deserialize<List<ServerInfo>>(File.ReadAllText(CachePath, Encoding.UTF8)) ?? new List<ServerInfo>();
			foreach (ServerInfo item in list)
			{
				item.Live = false;
			}
			return (from s in list
				where !string.IsNullOrEmpty(s.Id)
				orderby s.Order
				select s).ToList();
		}
		catch
		{
			return new List<ServerInfo>();
		}
	}

	public static List<ServerInfo> Builtin()
	{
		string[] array = new string[19]
		{
			"New York", "Detroit", "Chicago", "San Francisco", "Atlanta", "San Diego", "Los Angeles", "Miami", "Las Vegas", "Washington",
			"Dallas", "Boston", "Houston", "Seattle", "Phoenix", "Denver", "Portland", "Orlando", "Memphis"
		};
		List<ServerInfo> list = new List<ServerInfo>();
		for (int i = 0; i < array.Length; i++)
		{
			list.Add(new ServerInfo
			{
				Id = "ru" + (i + 1),
				Name = array[i]
			});
		}
		return list;
	}
}
