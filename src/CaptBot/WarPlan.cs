using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class WarPlan
{
	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 16777216
	};

	public static List<PlanFamily> Power(string server, out int capts, out int median, out string error)
	{
		capts = 0;
		median = 32;
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		Dictionary<string, object> dictionary2 = Post("/api/v1/war/power", dictionary, 20000, out error);
		if (dictionary2 == null)
		{
			return null;
		}
		median = I(G(dictionary2, "median_n"));
		if (G(dictionary2, "model") is Dictionary<string, object> d)
		{
			capts = I(G(d, "capts"));
		}
		List<PlanFamily> list = new List<PlanFamily>();
		object[] array = (G(dictionary2, "families") as object[]) ?? new object[0];
		foreach (object obj in array)
		{
			if (obj is Dictionary<string, object> d2)
			{
				list.Add(new PlanFamily
				{
					Name = S(G(d2, "name")),
					Tier = S(G(d2, "tier")),
					Id = (long)D(G(d2, "id")),
					DefN = D(G(d2, "def_n")),
					AtkN = D(G(d2, "atk_n")),
					DefW = I(G(d2, "def_w")),
					DefL = I(G(d2, "def_l")),
					AtkW = I(G(d2, "atk_w")),
					AtkL = I(G(d2, "atk_l")),
					Capts7d = I(G(d2, "capts_7d")),
					Capts30d = I(G(d2, "capts_30d")),
					Zones = I(G(d2, "zones")),
					Strength = I(G(d2, "strength")),
					DefSamples = I(G(d2, "def_samples")),
					AtkSamples = I(G(d2, "atk_samples")),
					Mansion = (G(d2, "mansion") is bool && (bool)G(d2, "mansion"))
				});
			}
		}
		return list;
	}

	public static PlanSuggest Suggest(string server, string family, int people, string square, long captAt, bool neutral, int timeoutMs, out string error)
	{
		return Suggest(server, family, people, square, captAt, neutral, null, timeoutMs, out error);
	}

	public static PlanSuggest Suggest(string server, string family, int people, string square, long captAt, bool neutral, IEnumerable<string> exclude, int timeoutMs, out string error)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["server"] = server;
		dictionary["family"] = family ?? "";
		dictionary["people"] = people;
		dictionary["square"] = square ?? "";
		dictionary["capt_at"] = captAt;
		dictionary["neutral"] = neutral;
		dictionary["limit"] = 6;
		if (exclude != null)
		{
			dictionary["exclude"] = new List<string>(exclude).ToArray();
		}
		Dictionary<string, object> dictionary2 = Post("/api/v1/war/suggest", dictionary, timeoutMs, out error);
		if (dictionary2 == null)
		{
			return null;
		}
		PlanSuggest planSuggest = new PlanSuggest();
		planSuggest.People = I(G(dictionary2, "people"));
		planSuggest.MedianN = I(G(dictionary2, "median_n"));
		planSuggest.Main = Opt(G(dictionary2, "main") as Dictionary<string, object>);
		PlanSuggest planSuggest2 = planSuggest;
		object[] array = (G(dictionary2, "options") as object[]) ?? new object[0];
		foreach (object obj in array)
		{
			PlanOption planOption = Opt(obj as Dictionary<string, object>);
			if (planOption != null)
			{
				planSuggest2.Options.Add(planOption);
			}
		}
		object[] array2 = (G(dictionary2, "notes") as object[]) ?? new object[0];
		foreach (object obj2 in array2)
		{
			planSuggest2.Notes.Add(Convert.ToString(obj2));
		}
		return planSuggest2;
	}

	public static bool PushPlan(Dictionary<string, object> plan, out string error)
	{
		Dictionary<string, object> dictionary = Base();
		dictionary["plan"] = plan;
		return Post("/api/v1/plan/state", dictionary, 15000, out error) != null;
	}

	private static PlanOption Opt(Dictionary<string, object> o)
	{
		if (o == null)
		{
			return null;
		}
		object obj = G(o, "their_n");
		PlanOption planOption = new PlanOption();
		planOption.Square = S(G(o, "square"));
		planOption.Family = S(G(o, "family"));
		planOption.Tier = S(G(o, "tier"));
		planOption.Reason = S(G(o, "reason"));
		planOption.Blocked = S(G(o, "blocked"));
		planOption.Type = S(G(o, "type"));
		planOption.FamilyId = (long)D(G(o, "family_id"));
		planOption.Chance = D(G(o, "chance"));
		planOption.TheirN = ((obj == null) ? (-1.0) : D(obj));
		planOption.Samples = I(G(o, "samples"));
		planOption.Confident = G(o, "confident") is bool && (bool)G(o, "confident");
		return planOption;
	}

	private static Dictionary<string, object> Base()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["key"] = License.SavedKey();
		dictionary["hwid"] = License.Hwid();
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
			HttpWebRequest httpWebRequest = SiteNet.Create("https://timetocapture.ru" + path);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json; charset=utf-8";
			httpWebRequest.UserAgent = "TTC/2.9.13";
			httpWebRequest.Timeout = timeoutMs;
			httpWebRequest.ReadWriteTimeout = timeoutMs;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
			httpWebRequest.ServicePoint.Expect100Continue = false;
			httpWebRequest.ServicePoint.UseNagleAlgorithm = false;
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

	private static int I(object v)
	{
		if (v == null)
		{
			return 0;
		}
		try
		{
			return Convert.ToInt32(Convert.ToDouble(v, CultureInfo.InvariantCulture));
		}
		catch
		{
			return 0;
		}
	}

	private static double D(object v)
	{
		if (v == null)
		{
			return 0.0;
		}
		try
		{
			return Convert.ToDouble(v, CultureInfo.InvariantCulture);
		}
		catch
		{
			return 0.0;
		}
	}
}
