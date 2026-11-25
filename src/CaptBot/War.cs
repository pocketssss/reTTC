using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class War
{
	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 16777216
	};

	private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static long Unix(DateTime utc)
	{
		return (long)Math.Floor((utc - Epoch).TotalSeconds);
	}

	public static DateTime FromUnix(long s)
	{
		return Epoch.AddSeconds(s);
	}

	public static long NowUnix()
	{
		return Unix(Clock.UtcNow());
	}

	public static string Hm(long unix)
	{
		return Clock.Msk(FromUnix(unix)).ToString("HH:mm");
	}

	public static string Hms(long unix)
	{
		return Clock.Msk(FromUnix(unix)).ToString("HH:mm:ss");
	}

	public static string Window(long[] w)
	{
		if (w == null)
		{
			return "";
		}
		if (w[0] != w[1])
		{
			return Hm(w[0]) + "–" + Hm(w[1]);
		}
		return Hm(w[0]);
	}

	public static string Ago(long t, long now)
	{
		long num = Math.Max(0L, now - t);
		if (num < 90)
		{
			return "только что";
		}
		if (num < 3600)
		{
			return num / 60 + " мин назад";
		}
		if (num < 86400)
		{
			return num / 3600 + " ч назад";
		}
		return Clock.Msk(FromUnix(t)).ToString("dd.MM HH:mm");
	}

	public static WarState Load(string server, WarState prev, out string error)
	{
		return Load(server, prev, 0, out error);
	}

	public static WarState Load(string server, WarState prev, int wait, out string error)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		bool flag = prev != null && string.Equals(prev.Server, server, StringComparison.OrdinalIgnoreCase);
		if (flag && !string.IsNullOrEmpty(prev.OwnersVer))
		{
			dictionary["owners_since"] = prev.OwnersVer;
		}
		if (wait > 0 && flag && !string.IsNullOrEmpty(prev.StateVer))
		{
			dictionary["state_ver"] = prev.StateVer;
			dictionary["wait"] = wait;
		}
		Dictionary<string, object> dictionary2 = Post("/api/v1/war/state", dictionary, out error, (wait > 0) ? ((wait + 20) * 1000) : 15000);
		if (dictionary2 == null)
		{
			return null;
		}
		try
		{
			return Parse(dictionary2, flag ? prev : null);
		}
		catch (Exception ex)
		{
			error = "ответ сервера не разобран: " + ex.Message;
			return null;
		}
	}

	public static WarState LoadHistory(string server, int days, out string error)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["days"] = days;
		Dictionary<string, object> dictionary2 = Post("/api/v1/war/history", dictionary, out error);
		if (dictionary2 == null)
		{
			return null;
		}
		try
		{
			WarState warState = Parse(dictionary2, null);
			warState.HistorySource = S(G(dictionary2, "history_source"));
			warState.HistorySynced = L(G(dictionary2, "history_synced"));
			return warState;
		}
		catch (Exception ex)
		{
			error = "ответ сервера не разобран: " + ex.Message;
			return null;
		}
	}

	public static WarState FromJson(string text)
	{
		return Parse((Dictionary<string, object>)Json.DeserializeObject(text), null);
	}

	public static string Report(string server, string attacker, string square, DateTime captUtc, DateTime placedUtc, bool manual)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["attacker"] = attacker;
		dictionary["square"] = square;
		dictionary["start"] = Unix(captUtc);
		dictionary["kind"] = (manual ? "manual" : "app");
		if (placedUtc > DateTime.MinValue)
		{
			dictionary["placed"] = Unix(placedUtc);
		}
		if (Post("/api/v1/war/report", dictionary, out var error) != null)
		{
			return null;
		}
		return error;
	}

	public static string ReportSeen(string server, DateTime atUtc, int r0, int r1, List<int[]> marks)
	{
		return ReportSeen(server, atUtc, r0, r1, 1, 58, marks, null, null, mayEnd: true);
	}

	public static string ReportSeen(string server, DateTime atUtc, int r0, int r1, int c0, int c1, List<int[]> marks, string family, int[] limits, bool mayEnd)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["at"] = Unix(atUtc);
		dictionary["r0"] = r0;
		dictionary["r1"] = r1;
		dictionary["c0"] = c0;
		dictionary["c1"] = c1;
		if (!mayEnd)
		{
			dictionary["noend"] = true;
		}
		dictionary["zones"] = marks.Select((int[] m) => new object[2]
		{
			m[0],
			(m[1] == 2) ? "g" : "p"
		}).ToArray();
		if (limits != null && limits.Length == 4 && !string.IsNullOrEmpty(family))
		{
			Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
			dictionary2["family"] = family;
			dictionary2["atk"] = new int[2]
			{
				limits[0],
				limits[1]
			};
			dictionary2["def"] = new int[2]
			{
				limits[2],
				limits[3]
			};
			dictionary["limits"] = dictionary2;
		}
		if (Post("/api/v1/war/seen", dictionary, out var error) != null)
		{
			return null;
		}
		return error;
	}

	public static string ReportNet(string server, List<NetCapt> capts, List<NetOwner> owners, bool ownersFull, NetLimits limits, string family)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["at"] = NowUnix();
		if (owners != null && owners.Count > 0)
		{
			dictionary["owners"] = (from o in owners
				where !string.IsNullOrEmpty(o.Family)
				select new object[4] { o.Zone, o.FamilyId, o.Family, o.Since }).ToArray();
			dictionary["owners_full"] = ownersFull;
		}
		if (limits != null && !string.IsNullOrEmpty(family))
		{
			Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
			dictionary2["family"] = family;
			dictionary2["atk"] = new int[2] { limits.AtkLeft, limits.AtkMax };
			dictionary2["def"] = new int[2] { limits.DefLeft, limits.DefMax };
			dictionary["limits"] = dictionary2;
		}
		dictionary["capts"] = capts.Select(delegate(NetCapt c)
		{
			Dictionary<string, object> dictionary3 = new Dictionary<string, object>
			{
				["zone"] = c.Zone,
				["start"] = c.Start,
				["attacker"] = c.A,
				["attacker_id"] = c.AId
			};
			if (!string.IsNullOrEmpty(c.D))
			{
				dictionary3["defender"] = c.D;
			}
			if (c.DId != 0)
			{
				dictionary3["defender_id"] = c.DId;
			}
			if (c.End > 0)
			{
				dictionary3["end"] = c.End;
				dictionary3["winner"] = c.Winner;
			}
			if (c.PrevStart > 0)
			{
				dictionary3["prev_start"] = c.PrevStart;
			}
			return dictionary3;
		}).ToArray();
		if (Post("/api/v1/war/net", dictionary, out var error) != null)
		{
			return null;
		}
		return error;
	}

	public static FamilyStats LoadFamily(string server, string family, out string error)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["family"] = family;
		Dictionary<string, object> dictionary2 = Post("/api/v1/war/family", dictionary, out error);
		if (dictionary2 == null)
		{
			return null;
		}
		try
		{
			FamilyStats familyStats = new FamilyStats();
			familyStats.Family = S(G(dictionary2, "family"));
			familyStats.Zones = (int)L(G(dictionary2, "zones"));
			familyStats.HistoryFrom = L(G(dictionary2, "history_from"));
			FamilyStats familyStats2 = familyStats;
			if (G(dictionary2, "mansions") is object[] source)
			{
				familyStats2.Mansions = source.Select((object x) => S(x)).ToList();
			}
			Dictionary<string, object> d = G(dictionary2, "periods") as Dictionary<string, object>;
			string[] array = new string[3] { "day", "week", "month" };
			foreach (string text in array)
			{
				Dictionary<string, object> dictionary3 = G(d, text) as Dictionary<string, object>;
				familyStats2.Periods[text] = ((dictionary3 == null) ? new int[6] : new int[6]
				{
					(int)L(G(dictionary3, "atk")),
					(int)L(G(dictionary3, "atk_w")),
					(int)L(G(dictionary3, "atk_l")),
					(int)L(G(dictionary3, "def")),
					(int)L(G(dictionary3, "def_w")),
					(int)L(G(dictionary3, "def_l"))
				});
			}
			object[] array2 = (G(dictionary2, "rivals") as object[]) ?? new object[0];
			foreach (object obj in array2)
			{
				if (obj is Dictionary<string, object> d2)
				{
					familyStats2.Rivals.Add(new FamilyStats.Rival
					{
						Name = S(G(d2, "name")),
						N = (int)L(G(d2, "n")),
						W = (int)L(G(d2, "w")),
						Lo = (int)L(G(d2, "l")),
						Atk = (int)L(G(d2, "atk")),
						Def = (int)L(G(d2, "def"))
					});
				}
			}
			object[] array3 = (G(dictionary2, "hours") as object[]) ?? new object[0];
			foreach (object obj2 in array3)
			{
				if (obj2 is object[] array4 && array4.Length == 2)
				{
					familyStats2.Hours.Add(new int[2]
					{
						(int)L(array4[0]),
						(int)L(array4[1])
					});
				}
			}
			object[] array5 = (G(dictionary2, "recent") as object[]) ?? new object[0];
			foreach (object obj3 in array5)
			{
				if (obj3 is Dictionary<string, object> d3)
				{
					object obj4 = G(d3, "win");
					familyStats2.Recent.Add(new FamilyStats.Item
					{
						Start = L(G(d3, "start")),
						Attack = (S(G(d3, "role")) == "a"),
						Vs = S(G(d3, "vs")),
						Square = S(G(d3, "square")),
						Win = ((obj4 is bool) ? new bool?((bool)obj4) : null)
					});
				}
			}
			if (G(dictionary2, "ingame") is object[] array6 && array6.Length >= 5)
			{
				familyStats2.Ingame = new long[5]
				{
					L(array6[0]),
					L(array6[1]),
					L(array6[2]),
					L(array6[3]),
					L(array6[4])
				};
			}
			return familyStats2;
		}
		catch (Exception ex)
		{
			error = "ответ сервера не разобран: " + ex.Message;
			return null;
		}
	}

	public static string TelegramLink(string server, string family, out string error)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["family"] = family;
		Dictionary<string, object> dictionary2 = Post("/api/v1/war/tg-link", dictionary, out error);
		if (dictionary2 != null)
		{
			return S(G(dictionary2, "url"));
		}
		return null;
	}

	public static string End(string server, int captId, DateTime endUtc, string winner)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["id"] = captId;
		dictionary["end"] = Unix(endUtc);
		if (winner != null)
		{
			dictionary["winner"] = winner;
		}
		if (Post("/api/v1/war/end", dictionary, out var error) != null)
		{
			return null;
		}
		return error;
	}

	private static Dictionary<string, object> Base()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["key"] = License.SavedKey();
		dictionary["hwid"] = License.Hwid();
		dictionary["ver"] = "2.9.13";
		return dictionary;
	}

	private static Dictionary<string, object> Post(string path, Dictionary<string, object> body, out string error)
	{
		return Post(path, body, out error, 15000);
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

	private static bool B(object v)
	{
		if (v is bool)
		{
			return (bool)v;
		}
		return false;
	}

	private static long[] Pair(object v)
	{
		if (!(v is object[] array) || array.Length < 2)
		{
			return null;
		}
		return new long[2]
		{
			L(array[0]),
			L(array[1])
		};
	}

	private static WarState Parse(Dictionary<string, object> d, WarState prev)
	{
		WarState warState = new WarState();
		warState.ReceivedUtc = Clock.UtcNow();
		warState.Server = S(G(d, "server"));
		warState.Now = L(G(d, "now"));
		warState.DayStart = L(G(d, "day_start"));
		if (G(d, "rules") is Dictionary<string, object> d2)
		{
			warState.Attacks = (int)L(G(d2, "attacks"));
			warState.Defenses = (int)L(G(d2, "defenses"));
			warState.MansionDefenses = (int)L(G(d2, "mansion_defenses"));
			warState.PrepMin = (int)L(G(d2, "prep_min"));
			warState.FightMin = (int)L(G(d2, "fight_min"));
			warState.AtkCdMin = (int)L(G(d2, "attack_cd_min"));
			warState.DefCdMin = (int)L(G(d2, "defense_cd_min"));
			if (G(d2, "neutral_attack_cd_min") != null)
			{
				warState.NeutralAtkCdMin = (int)L(G(d2, "neutral_attack_cd_min"));
			}
		}
		if (G(d, "owners_info") is Dictionary<string, object> d3)
		{
			warState.OwnersSource = S(G(d3, "source"));
			warState.OwnersTitle = S(G(d3, "title"));
			warState.OwnersError = S(G(d3, "error"));
			warState.OwnersUpdated = L(G(d3, "updated_at"));
			warState.OwnersFetched = L(G(d3, "fetched_at"));
			warState.OwnersNetAt = L(G(d3, "net_at"));
		}
		warState.OwnersVer = S(G(d, "owners_ver"));
		warState.ServerIcon = S(G(d, "server_icon"));
		warState.StateVer = S(G(d, "state_ver"));
		object[] array = (G(d, "ingame") as object[]) ?? new object[0];
		foreach (object obj in array)
		{
			if (obj is object[] array2 && array2.Length >= 6)
			{
				warState.Ingame[Zones.Key(S(array2[0]))] = new long[5]
				{
					L(array2[1]),
					L(array2[2]),
					L(array2[3]),
					L(array2[4]),
					L(array2[5])
				};
			}
		}
		object[] array3 = (G(d, "families") as object[]) ?? new object[0];
		object[] array4 = array3;
		foreach (object obj2 in array4)
		{
			if (obj2 is Dictionary<string, object> d4)
			{
				WarFamily warFamily = new WarFamily();
				warFamily.Index = warState.Families.Count;
				warFamily.Name = S(G(d4, "name")) ?? "?";
				warFamily.Tag = S(G(d4, "tag"));
				warFamily.Color = S(G(d4, "color"));
				warFamily.Id = L(G(d4, "id"));
				warFamily.Zones = (int)L(G(d4, "zones"));
				warFamily.AtkUsed = (int)L(G(d4, "atk_used"));
				warFamily.AtkMax = (int)L(G(d4, "atk_max"));
				warFamily.DefUsed = (int)L(G(d4, "def_used"));
				warFamily.DefMax = (int)L(G(d4, "def_max"));
				warFamily.Mansion = B(G(d4, "mansion"));
				warFamily.MansionSoon = B(G(d4, "mansion_soon"));
				warFamily.AtkCd = Pair(G(d4, "atk_cd"));
				warFamily.DefCd = Pair(G(d4, "def_cd"));
				warFamily.Logo = S(G(d4, "logo"));
				WarFamily warFamily2 = warFamily;
				warFamily2.Key = Zones.Key(warFamily2.Name);
				if (G(d4, "in_capt") is Dictionary<string, object> d5)
				{
					warFamily2.InCaptEnd = Pair(G(d5, "end"));
					warFamily2.InCaptRole = S(G(d5, "role"));
				}
				warState.Families.Add(warFamily2);
			}
		}
		if (G(d, "owners") is object[] array5)
		{
			object[] array6 = array5;
			foreach (object obj3 in array6)
			{
				if (obj3 is object[] array7 && array7.Length >= 2)
				{
					int key = (int)L(array7[0]);
					int value = (int)L(array7[1]);
					warState.OwnerIdx[key] = value;
					if (array7.Length > 2 && array7[2] != null)
					{
						warState.Since[key] = L(array7[2]);
					}
				}
			}
		}
		else if (prev != null)
		{
			foreach (KeyValuePair<int, int> item in prev.OwnerIdx)
			{
				if (item.Value >= 0 && item.Value < prev.Families.Count)
				{
					WarFamily warFamily3 = warState.Family(prev.Families[item.Value].Name);
					if (warFamily3 != null)
					{
						warState.OwnerIdx[item.Key] = warFamily3.Index;
					}
				}
			}
			foreach (KeyValuePair<int, long> item2 in prev.Since)
			{
				warState.Since[item2.Key] = item2.Value;
			}
		}
		if (G(d, "live") is object[] array8)
		{
			object[] array9 = array8;
			foreach (object obj4 in array9)
			{
				if (obj4 is object[] array10 && array10.Length >= 4)
				{
					warState.Live.Add(new LiveCapt
					{
						ZoneId = (int)L(array10[0]),
						FirstSeen = L(array10[1]),
						LastSeen = L(array10[2]),
						EndedAt = L(array10[3]),
						Planned = (array10.Length > 4 && L(array10[4]) != 0),
						StartedAt = ((array10.Length > 5) ? L(array10[5]) : 0)
					});
				}
			}
		}
		object[] array11 = (G(d, "capts") as object[]) ?? new object[0];
		object[] array12 = array11;
		foreach (object obj5 in array12)
		{
			if (obj5 is Dictionary<string, object> d6)
			{
				long[] array13 = Pair(G(d6, "end")) ?? new long[2];
				object[] array14 = G(d6, "src") as object[];
				warState.Capts.Add(new WarCapt
				{
					Id = (int)L(G(d6, "id")),
					A = S(G(d6, "a")),
					D = S(G(d6, "d")),
					ZoneId = (int)L(G(d6, "zone")),
					Square = S(G(d6, "square")),
					Start = L(G(d6, "start")),
					EndLo = array13[0],
					EndHi = array13[1],
					ExactEnd = B(G(d6, "exact_end")),
					Placed = L(G(d6, "placed")),
					Winner = S(G(d6, "winner")),
					Src = ((array14 == null) ? "" : string.Join(",", array14.Select((object x) => S(x)).ToArray())),
					AId = L(G(d6, "a_id")),
					DId = L(G(d6, "d_id"))
				});
				if (G(d6, "extra") is Dictionary<string, object> d7)
				{
					WarCapt warCapt = warState.Capts[warState.Capts.Count - 1];
					warCapt.ANum = (int)L(G(d7, "a_n"));
					warCapt.DNum = (int)L(G(d7, "d_n"));
					warCapt.ADmg = L(G(d7, "a_dmg"));
					warCapt.DDmg = L(G(d7, "d_dmg"));
					warCapt.Place = S(G(d7, "place")) ?? S(G(d7, "zone_name"));
				}
			}
		}
		return warState;
	}
}
