using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class Roster
{
	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 4194304
	};

	private static readonly object Sync = new object();

	private static RosterInfo last;

	public static volatile bool Armed;

	public static List<RosterResult> LastResults;

	public static DateTime LastResultsAt = DateTime.MinValue;

	public static RosterInfo Last
	{
		get
		{
			lock (Sync)
			{
				return last;
			}
		}
	}

	public static RosterInfo Pull(long ver, long fillId, int wait, out RosterFill fill, out string error)
	{
		fill = null;
		Dictionary<string, object> dictionary = Base();
		dictionary["roster_ver"] = ver;
		dictionary["fill_id"] = fillId;
		dictionary["wait"] = wait;
		dictionary["armed"] = Armed;
		Dictionary<string, object> dictionary2 = Post("/api/v1/roster/pull", dictionary, out error, (wait + 15) * 1000);
		if (dictionary2 == null)
		{
			return null;
		}
		RosterInfo rosterInfo = Parse(G(dictionary2, "roster") as Dictionary<string, object>);
		rosterInfo.Linked = G(dictionary2, "linked") is bool && (bool)G(dictionary2, "linked");
		if (G(dictionary2, "fill") is Dictionary<string, object> d)
		{
			fill = new RosterFill
			{
				Id = L(G(d, "id")),
				At = L(G(d, "at")),
				Square = S(G(d, "square"))
			};
		}
		lock (Sync)
		{
			last = rosterInfo;
			return rosterInfo;
		}
	}

	public static RosterInfo Fresh(RosterInfo fallback, int timeoutMs)
	{
		RosterInfo r = null;
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				r = Pull(-1L, long.MaxValue, 0, out var _, out var _);
			}
			catch
			{
			}
		});
		thread.IsBackground = true;
		Thread thread2 = thread;
		thread2.Start();
		thread2.Join(timeoutMs);
		return r ?? fallback;
	}

	public static void Report(long fillId, long ver, string status, string square, List<RosterResult> items, string error)
	{
		if (items != null)
		{
			LastResults = items;
			LastResultsAt = Clock.UtcNow();
		}
		Dictionary<string, object> b = Base();
		b["fill_id"] = fillId;
		b["roster_ver"] = ver;
		b["status"] = status;
		b["square"] = square ?? "";
		if (error != null)
		{
			b["error"] = error;
		}
		List<object> list = new List<object>();
		if (items != null)
		{
			foreach (RosterResult item in items)
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary["name"] = item.Entry.Name;
				dictionary["status"] = item.Status;
				if (item.Entry.Uid > 0)
				{
					dictionary["uid"] = item.Entry.Uid;
				}
				if (!string.IsNullOrEmpty(item.Entry.Ref))
				{
					dictionary["ref"] = item.Entry.Ref;
				}
				if (item.Online >= 0)
				{
					dictionary["online"] = item.Online == 1;
				}
				if (!string.IsNullOrEmpty(item.Note))
				{
					dictionary["note"] = item.Note;
				}
				list.Add(dictionary);
			}
		}
		b["items"] = list;
		Thread thread = new Thread((ThreadStart)delegate
		{
			for (int i = 0; i < 3; i++)
			{
				if (Post("/api/v1/roster/result", b, out var _, 15000) != null)
				{
					break;
				}
				Thread.Sleep(2000);
			}
		});
		thread.IsBackground = true;
		thread.Name = "TTC roster result";
		Thread thread2 = thread;
		thread2.Start();
	}

	public static string StatusText(string s)
	{
		return s switch
		{
			"added" => "добавлен", 
			"not_found" => "нет в списке семьи", 
			"ambiguous" => "несколько похожих", 
			"bad_name" => "имя не набрать", 
			"skipped" => "пропущен", 
			_ => "ошибка", 
		};
	}

	private static RosterInfo Parse(Dictionary<string, object> d)
	{
		RosterInfo rosterInfo = new RosterInfo();
		rosterInfo.ReceivedUtc = DateTime.UtcNow;
		RosterInfo rosterInfo2 = rosterInfo;
		if (d == null)
		{
			return rosterInfo2;
		}
		rosterInfo2.Ver = L(G(d, "ver"));
		rosterInfo2.Title = S(G(d, "title"));
		rosterInfo2.UpdatedAt = L(G(d, "updated_at"));
		rosterInfo2.ExpiresAt = L(G(d, "expires_at"));
		object[] array = (G(d, "entries") as object[]) ?? new object[0];
		foreach (object obj in array)
		{
			if (!(obj is Dictionary<string, object> d2) || string.IsNullOrEmpty(S(G(d2, "name"))))
			{
				continue;
			}
			List<string> list = null;
			object[] array2 = (G(d2, "alts") as object[]) ?? new object[0];
			foreach (object v in array2)
			{
				if (!string.IsNullOrEmpty(S(v)))
				{
					List<string> obj2 = list ?? new List<string>();
					list = obj2;
					obj2.Add(S(v));
				}
			}
			rosterInfo2.Entries.Add(new RosterEntry
			{
				Name = S(G(d2, "name")),
				Uid = L(G(d2, "uid")),
				Ref = S(G(d2, "ref")),
				Alts = ((list != null && list.Count > 1) ? list : null)
			});
		}
		return rosterInfo2;
	}

	private static Dictionary<string, object> Base()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["key"] = License.SavedKey();
		dictionary["hwid"] = License.Hwid();
		dictionary["ver"] = "2.9.13";
		return dictionary;
	}

	private static Dictionary<string, object> Post(string path, Dictionary<string, object> body, out string error, int timeoutMs)
	{
		error = null;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			if (ServicePointManager.DefaultConnectionLimit < 8)
			{
				ServicePointManager.DefaultConnectionLimit = 8;
			}
			HttpWebRequest httpWebRequest = SiteNet.Create("https://timetocapture.ru" + path);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json; charset=utf-8";
			httpWebRequest.UserAgent = "TTC/2.9.13";
			httpWebRequest.Timeout = timeoutMs;
			httpWebRequest.ReadWriteTimeout = timeoutMs;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
			httpWebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
			byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(body));
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
			if (!(G(dictionary, "ok") is bool) || !(bool)G(dictionary, "ok"))
			{
				error = S(G(dictionary, "error")) ?? "сервер отказал";
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

	private static object G(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value))
		{
			return null;
		}
		return value;
	}

	private static string S(object v)
	{
		if (v != null)
		{
			return Convert.ToString(v, CultureInfo.InvariantCulture);
		}
		return null;
	}

	private static long L(object v)
	{
		if (v == null)
		{
			return 0L;
		}
		try
		{
			return Convert.ToInt64(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0L;
		}
	}
}
