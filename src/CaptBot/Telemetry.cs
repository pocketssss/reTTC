using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class Telemetry
{
	private class Strike
	{
		public string Mode;

		public string Square;

		public int People;

		public DateTime Start = DateTime.UtcNow;

		public int Cur = -1;

		public int Retries;

		public int FillMs = -1;

		public string Last = "";

		public string Confirm;

		public readonly Dictionary<string, long> Marks = new Dictionary<string, long>();

		public readonly SortedDictionary<int, object[]> Steps = new SortedDictionary<int, object[]>();

		public long Ms()
		{
			return (long)(DateTime.UtcNow - Start).TotalMilliseconds;
		}
	}

	private const int MaxQueue = 300;

	private const int MaxBatch = 50;

	private const int MaxBatchChars = 60000;

	private const int MaxPerHour = 300;

	private const int MaxText = 400;

	private const int MaxStack = 3000;

	private const int MaxFileChars = 65536;

	private const int MergeSec = 600;

	private const string Path_ = "/api/v1/telemetry";

	private static readonly object sync = new object();

	private static readonly object flushLock = new object();

	private static readonly List<Dictionary<string, object>> queue = new List<Dictionary<string, object>>();

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

	private static readonly AutoResetEvent wake = new AutoResetEvent(initialState: false);

	private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public static volatile bool Disabled;

	public static volatile bool Quiet;

	private static Thread worker;

	private static int hourCount;

	private static int sessionCount;

	private static DateTime hourStart = DateTime.UtcNow;

	private static DateTime lastSendUtc = DateTime.UtcNow;

	private static DateTime nextTryUtc = DateTime.MinValue;

	private static DateTime firstErrorUtc = DateTime.MinValue;

	private static int failStreak;

	private static Exception lastEx;

	private static string lastExMsg;

	private static DateTime lastExAt;

	private static bool crashed;

	private static volatile string savedKey;

	private static readonly string[][] StepFails = new string[14][]
	{
		new string[2] { "Планшет не открылся", "Планшет" },
		new string[2] { "Приложение семьи не открылось", "Приложение семьи" },
		new string[2] { "Карта ещё не откликается", "Карта территорий" },
		new string[2] { "Карта: после клика подписи не прочитались", "Подписи квадратов" },
		new string[2] { "Карта: подписи квадратов не прочитались", "Подписи квадратов" },
		new string[2] { "Из игры: клик открыл зону", "Промах по квадрату" },
		new string[2] { "Карта: с первого клика форма не открылась", "Форма атаки" },
		new string[2] { "Форма не открылась", "Форма атаки" },
		new string[2] { "~форма не откроется", "Форма атаки" },
		new string[2] { "~не удалось её очистить", "Форма атаки" },
		new string[2] { "~не понял, какое число в поле", "Время капта" },
		new string[2] { "~числа в поле не складываются в круг", "Время капта" },
		new string[2] { "~поле не меняется от кнопки ›", "Время капта" },
		new string[2] { "~— меньше, чем нужно!", "Участники" }
	};

	private static Strike strike;

	private static string envSize;

	private static string envLayout;

	private static string envSig;

	private static string envSent;

	private static bool envCal;

	private static bool envBlack;

	private static DateTime envChangedUtc = DateTime.MinValue;

	private static readonly Regex KeyRe = new Regex("CAPT(?:-[0-9A-Z]{4}){4}", RegexOptions.IgnoreCase);

	private static readonly Regex MailRe = new Regex("[\\w.+-]+@[\\w-]+(?:\\.[\\w-]+)+");

	private static readonly Regex UserDirRe = new Regex("(?i)\\b([a-z]:\\\\(?:users|documents and settings)\\\\)[^\\\\/:*?\"<>|\\r\\n]+");

	private static readonly Regex TokenRe = new Regex("[A-Za-z0-9_\\-]{20,}\\.[A-Za-z0-9_\\-]{6,}\\.[A-Za-z0-9_\\-]{20,}|\\b[A-Za-z0-9+/_\\-]{48,}={0,2}");

	private static string QueueFile => Path.Combine(AppPaths.Data, "telemetry.queue");

	public static void Start(Dictionary<string, object> info)
	{
		if (Disabled || worker != null)
		{
			return;
		}
		try
		{
			savedKey = License.Normalize(License.SavedKey());
			LoadFile();
			Dictionary<string, object> dictionary = info ?? new Dictionary<string, object>();
			dictionary["os"] = Environment.OSVersion.VersionString;
			dictionary["x64"] = Environment.Is64BitOperatingSystem;
			dictionary["clr"] = Environment.Version.ToString();
			Record("start", null, "запуск", null, dictionary);
			Thread thread = new Thread(Loop);
			thread.IsBackground = true;
			thread.Priority = ThreadPriority.BelowNormal;
			thread.Name = "telemetry";
			worker = thread;
			worker.Start();
		}
		catch
		{
		}
	}

	public static void Save()
	{
		if (Disabled)
		{
			return;
		}
		try
		{
			List<Dictionary<string, object>> list;
			lock (sync)
			{
				list = queue.ToList();
			}
			if (list.Count == 0)
			{
				try
				{
					File.Delete(QueueFile);
					return;
				}
				catch
				{
					return;
				}
			}
			list = list.Where((Dictionary<string, object> e) => IsError(e)).Concat(list.Where((Dictionary<string, object> e) => !IsError(e))).Take(150)
				.ToList();
			string text = Json.Serialize(list);
			while (text.Length > 65536 && list.Count > 1)
			{
				list.RemoveAt(list.Count - 1);
				text = Json.Serialize(list);
			}
			File.WriteAllText(QueueFile, text, Encoding.UTF8);
		}
		catch
		{
		}
	}

	private static void LoadFile()
	{
		try
		{
			string queueFile = QueueFile;
			if (!File.Exists(queueFile))
			{
				return;
			}
			string text = ((new FileInfo(queueFile).Length <= 262144) ? File.ReadAllText(queueFile, Encoding.UTF8) : null);
			try
			{
				File.Delete(queueFile);
			}
			catch
			{
			}
			object[] array = ((text == null) ? null : (Json.DeserializeObject(text) as object[]));
			if (array == null)
			{
				return;
			}
			lock (sync)
			{
				foreach (object item in array.Take(300))
				{
					if (item is Dictionary<string, object> dictionary && dictionary.ContainsKey("kind"))
					{
						queue.Add(dictionary);
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void Caught(Exception ex, string where)
	{
		Ex(ex, where, "exception");
	}

	public static void Crash(Exception ex, string where)
	{
		if (Disabled || ex == null)
		{
			return;
		}
		try
		{
			Ex(ex, where, "crash");
			Save();
			if (crashed)
			{
				return;
			}
			crashed = true;
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					Flush(4000);
				}
				catch
				{
				}
			});
			thread.IsBackground = true;
			Thread thread2 = thread;
			thread2.Start();
			thread2.Join(4500);
			Save();
		}
		catch
		{
		}
	}

	private static void Ex(Exception ex, string where, string kind)
	{
		if (Disabled || ex == null)
		{
			return;
		}
		try
		{
			lock (sync)
			{
				if (object.ReferenceEquals(ex, lastEx))
				{
					return;
				}
				lastEx = ex;
				lastExMsg = ex.Message;
				lastExAt = DateTime.UtcNow;
			}
			Exception ex2 = ex;
			while ((ex2 is TargetInvocationException || ex2 is AggregateException) && ex2.InnerException != null)
			{
				ex2 = ex2.InnerException;
			}
			string text = TopFrame(ex2.StackTrace) ?? TopFrame(ex.StackTrace);
			string place = (where ?? "?") + ((text != null) ? (" · " + text) : "");
			Record(kind, place, ex2.GetType().Name + ": " + ex2.Message, ex.ToString(), null);
		}
		catch
		{
		}
	}

	private static string TopFrame(string stack)
	{
		if (string.IsNullOrEmpty(stack))
		{
			return null;
		}
		Match match = Regex.Match(stack, "(CaptBot\\.[\\w.`<>]+)\\(");
		if (!match.Success)
		{
			match = Regex.Match(stack, "\\s([\\w`<>]+\\.[\\w.`<>]+)\\(");
		}
		if (!match.Success)
		{
			return null;
		}
		return match.Groups[1].Value;
	}

	public static void FromLog(string m)
	{
		if (Disabled || string.IsNullOrEmpty(m))
		{
			return;
		}
		try
		{
			if (m.StartsWith("ОШИБКА", StringComparison.Ordinal) || m.StartsWith("Ошибка:", StringComparison.Ordinal))
			{
				string text = m.Substring((m.IndexOf(':') >= 0 && m.IndexOf(':') < 8) ? (m.IndexOf(':') + 1) : 0).Trim();
				lock (sync)
				{
					if (lastExMsg != null && (DateTime.UtcNow - lastExAt).TotalSeconds < 5.0 && text.Contains(lastExMsg))
					{
						return;
					}
				}
				Record("error", CurrentPlace(), text, null, null);
				return;
			}
			if (m.StartsWith("Внимание:", StringComparison.Ordinal))
			{
				Record("warn", CurrentPlace(), m, null, null);
				return;
			}
			string[][] stepFails = StepFails;
			foreach (string[] array in stepFails)
			{
				if (!((array[0][0] == '~') ? m.Contains(array[0].Substring(1)) : m.StartsWith(array[0], StringComparison.Ordinal)))
				{
					continue;
				}
				Record("step_fail", array[1], m, null, null);
				lock (sync)
				{
					if (strike != null)
					{
						strike.Retries++;
					}
					return;
				}
			}
			if (m.StartsWith("Калибровка под окно ", StringComparison.Ordinal) && !m.Contains("сброшена"))
			{
				Record("calib", "раскладка", m, null, EnvData());
				return;
			}
			if (m.StartsWith("Калибровка под окно ", StringComparison.Ordinal))
			{
				Record("calib", "сброс", m, null, EnvData());
				return;
			}
			if (m.StartsWith("Панель пинга найдена", StringComparison.Ordinal))
			{
				Record("calib", "панель пинга", m, null, EnvData());
				return;
			}
			lock (sync)
			{
				if (strike == null)
				{
					return;
				}
				if (m.StartsWith("✓ На ", StringComparison.Ordinal))
				{
					strike.Confirm = "accepted";
				}
				else if (m.StartsWith("✗ На ", StringComparison.Ordinal))
				{
					strike.Confirm = "lost";
				}
				else if (m.StartsWith("Подтверждения забива от игры", StringComparison.Ordinal))
				{
					strike.Confirm = "unconfirmed";
				}
				else if (m.StartsWith("Всё заполнено за ", StringComparison.Ordinal))
				{
					Match match = Regex.Match(m, "за (\\d+) мс");
					if (match.Success)
					{
						strike.FillMs = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void Begin(string mode, string square, int people)
	{
		if (Disabled)
		{
			return;
		}
		lock (sync)
		{
			Strike strike = new Strike();
			strike.Mode = mode;
			strike.Square = square;
			strike.People = people;
			Telemetry.strike = strike;
		}
	}

	public static void Step(int i, StepState st, string detail)
	{
		if (Disabled)
		{
			return;
		}
		try
		{
			lock (sync)
			{
				if (strike != null)
				{
					switch (st)
					{
					case StepState.Running:
						strike.Cur = i;
						strike.Steps[i] = new object[5]
						{
							i,
							StepName(i),
							"running",
							strike.Ms(),
							""
						};
						break;
					case StepState.Done:
					case StepState.Error:
					case StepState.Skipped:
						strike.Steps[i] = new object[5]
						{
							i,
							StepName(i),
							st.ToString().ToLowerInvariant(),
							strike.Ms(),
							Short(detail, 80)
						};
						break;
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void Status(string kind)
	{
		if (Disabled)
		{
			return;
		}
		Quiet = kind == "prep" || kind == "ready" || kind == "hold";
		try
		{
			lock (sync)
			{
				if (strike != null)
				{
					strike.Last = kind ?? "";
					if (!string.IsNullOrEmpty(kind) && !strike.Marks.ContainsKey(kind))
					{
						strike.Marks[kind] = strike.Ms();
					}
				}
			}
		}
		catch
		{
		}
	}

	public static void End()
	{
		Quiet = false;
		if (Disabled)
		{
			return;
		}
		try
		{
			Strike strike;
			lock (sync)
			{
				strike = Telemetry.strike;
				Telemetry.strike = null;
			}
			if (strike == null)
			{
				return;
			}
			string text = ((strike.Last == "done" || strike.Last == "testok") ? "ok" : ((strike.Last == "error") ? "fail" : ((strike.Last == "stop") ? "stopped" : "cancel")));
			if (text == "cancel" && strike.Mode == "calib" && strike.Steps.Count > 0 && strike.Steps.Values.All((object[] x) => (string)x[2] == "done"))
			{
				text = "ok";
			}
			string text2 = null;
			if (text == "fail")
			{
				object[] array = strike.Steps.Values.FirstOrDefault((object[] x) => (string)x[2] == "error" || (string)x[2] == "running");
				text2 = ((array != null) ? ((string)array[1]) : ((strike.Cur >= 0) ? StepName(strike.Cur) : "до начала шагов"));
			}
			Dictionary<string, object> dictionary = EnvData();
			dictionary["square"] = strike.Square;
			dictionary["people"] = strike.People;
			dictionary["retries"] = strike.Retries;
			if (strike.Confirm != null)
			{
				dictionary["confirm"] = strike.Confirm;
			}
			if (strike.FillMs >= 0)
			{
				dictionary["fill_ms"] = strike.FillMs;
			}
			dictionary["marks"] = ((IEnumerable<KeyValuePair<string, long>>)strike.Marks).ToDictionary((Func<KeyValuePair<string, long>, string>)((KeyValuePair<string, long> x) => x.Key), (Func<KeyValuePair<string, long>, object>)((KeyValuePair<string, long> x) => x.Value));
			dictionary["steps"] = strike.Steps.Values.ToArray();
			Dictionary<string, object> dictionary2 = Make((strike.Mode == "calib") ? "calib" : "strike", strike.Mode, text2, null, dictionary);
			dictionary2["res"] = text;
			dictionary2["dur"] = strike.Ms();
			Add(dictionary2, merge: false);
		}
		catch
		{
		}
	}

	private static string StepName(int i)
	{
		if (i < 0 || i >= Runner.StepNames.Length)
		{
			return "шаг " + (i + 1);
		}
		return Runner.StepNames[i];
	}

	private static string CurrentPlace()
	{
		lock (sync)
		{
			return (strike != null && strike.Cur >= 0) ? ("шаг: " + StepName(strike.Cur)) : ((strike != null) ? ("забив: " + strike.Mode) : "журнал");
		}
	}

	public static void Game(bool found, string size, string layout, bool calibrated, bool black)
	{
		if (Disabled || !found || string.IsNullOrEmpty(size))
		{
			return;
		}
		lock (sync)
		{
			envSize = size;
			envLayout = layout;
			envCal = calibrated;
			envBlack = black;
			string text = size + "|" + layout + "|" + calibrated + "|" + black;
			if (text != envSig)
			{
				envSig = text;
				envChangedUtc = DateTime.UtcNow;
			}
		}
	}

	private static Dictionary<string, object> EnvData()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		lock (sync)
		{
			if (envSize == null)
			{
				return dictionary;
			}
			dictionary["size"] = envSize;
			dictionary["layout"] = envLayout;
			dictionary["calibrated"] = envCal;
			if (envBlack)
			{
				dictionary["black"] = true;
			}
		}
		return dictionary;
	}

	private static void EnvTick()
	{
		lock (sync)
		{
			if (envSig == null || envSig == envSent || (DateTime.UtcNow - envChangedUtc).TotalSeconds < 15.0)
			{
				return;
			}
			envSent = envSig;
		}
		Dictionary<string, object> data = EnvData();
		Record("env", null, (envLayout ?? "раскладка не подобрана") + (envCal ? "" : ", без калибровки") + (envBlack ? ", снимок чёрный" : ""), null, data);
	}

	private static bool IsError(Dictionary<string, object> e)
	{
		e.TryGetValue("kind", out var value);
		string text = value as string;
		switch (text)
		{
		default:
			return text == "step_fail";
		case "crash":
		case "exception":
		case "error":
			return true;
		}
	}

	private static Dictionary<string, object> Make(string kind, string place, string text, string stack, Dictionary<string, object> data)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["at"] = (long)(Clock.UtcNow() - Epoch).TotalSeconds;
		dictionary["kind"] = kind;
		if (place != null)
		{
			dictionary["place"] = Short(Clean(place), 120);
		}
		if (text != null)
		{
			dictionary["text"] = Short(Clean(text), 400);
		}
		if (stack != null)
		{
			dictionary["stack"] = Short(Clean(stack), 3000);
		}
		if (data != null && data.Count > 0)
		{
			if (data.TryGetValue("size", out var value) && value is string)
			{
				dictionary["size"] = value;
			}
			dictionary["data"] = CleanData(data);
		}
		dictionary["n"] = 1;
		return dictionary;
	}

	private static void Record(string kind, string place, string text, string stack, Dictionary<string, object> data)
	{
		if (Disabled)
		{
			return;
		}
		try
		{
			Add(Make(kind, place, text, stack, data), kind != "strike" && kind != "calib" && kind != "start" && kind != "env");
		}
		catch
		{
		}
	}

	private static void Add(Dictionary<string, object> e, bool merge)
	{
		bool flag = IsError(e);
		lock (sync)
		{
			DateTime utcNow = DateTime.UtcNow;
			if (merge)
			{
				long num = (long)e["at"];
				int num2 = queue.Count - 1;
				while (num2 >= 0 && num2 >= queue.Count - 30)
				{
					Dictionary<string, object> dictionary = queue[num2];
					if (Eq(dictionary, e, "kind") && Eq(dictionary, e, "place") && Eq(dictionary, e, "text") && num - Convert.ToInt64(dictionary["at"], CultureInfo.InvariantCulture) < 600)
					{
						dictionary["n"] = Convert.ToInt32(dictionary["n"], CultureInfo.InvariantCulture) + 1;
						return;
					}
					num2--;
				}
			}
			if ((utcNow - hourStart).TotalHours >= 1.0)
			{
				hourStart = utcNow;
				hourCount = 0;
			}
			string text = (string)e["kind"];
			if (text != "crash" && (hourCount >= 300 || sessionCount >= 3000))
			{
				return;
			}
			hourCount++;
			sessionCount++;
			while (queue.Count >= 300)
			{
				int num3 = queue.FindIndex((Dictionary<string, object> x) => !IsError(x));
				queue.RemoveAt((num3 >= 0) ? num3 : 0);
			}
			queue.Add(e);
			if (flag && firstErrorUtc == DateTime.MinValue)
			{
				firstErrorUtc = utcNow;
			}
		}
		if (flag)
		{
			wake.Set();
		}
	}

	private static bool Eq(Dictionary<string, object> a, Dictionary<string, object> b, string k)
	{
		a.TryGetValue(k, out var value);
		b.TryGetValue(k, out var value2);
		return object.Equals(value as string, value2 as string);
	}

	private static void Loop()
	{
		while (true)
		{
			wake.WaitOne(5000);
			try
			{
				Tick();
			}
			catch
			{
			}
		}
	}

	private static void Tick()
	{
		EnvTick();
		DateTime utcNow = DateTime.UtcNow;
		if (!Quiet && !(utcNow < nextTryUtc))
		{
			int count;
			bool flag;
			lock (sync)
			{
				count = queue.Count;
				flag = firstErrorUtc != DateTime.MinValue && (utcNow - firstErrorUtc).TotalSeconds >= 10.0;
			}
			if (count != 0 && (count >= 40 || flag || (utcNow - lastSendUtc).TotalSeconds >= 60.0))
			{
				Flush(10000);
			}
		}
	}

	private static void Flush(int timeoutMs)
	{
		LicenseState current = License.Current;
		if (current == null || !current.Ok || !Monitor.TryEnter(flushLock, timeoutMs))
		{
			return;
		}
		try
		{
			FlushLocked(timeoutMs);
		}
		finally
		{
			Monitor.Exit(flushLock);
		}
	}

	private static void FlushLocked(int timeoutMs)
	{
		for (int i = 0; i < 6; i++)
		{
			if (Quiet && !crashed)
			{
				break;
			}
			List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
			lock (sync)
			{
				int num = 0;
				foreach (Dictionary<string, object> item in queue)
				{
					int length = Json.Serialize(item).Length;
					if (list.Count < 50 && (list.Count <= 0 || num + length <= 60000))
					{
						list.Add(item);
						num += length;
						continue;
					}
					break;
				}
			}
			if (list.Count == 0)
			{
				break;
			}
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			string text = License.SavedKey();
			savedKey = License.Normalize(text);
			dictionary["key"] = text;
			dictionary["hwid"] = License.Hwid();
			dictionary["ver"] = "2.9.13";
			dictionary["build"] = "user";
			dictionary["sent_at"] = (long)(Clock.UtcNow() - Epoch).TotalSeconds;
			dictionary["events"] = list.ToArray();
			bool answered;
			string text2 = Post(dictionary, timeoutMs, out answered);
			DateTime utcNow = DateTime.UtcNow;
			if (text2 != null && !answered)
			{
				failStreak++;
				nextTryUtc = utcNow.AddSeconds(Math.Min(900, 30 * (1 << Math.Min(5, failStreak - 1))));
				break;
			}
			lock (sync)
			{
				foreach (Dictionary<string, object> item2 in list)
				{
					queue.Remove(item2);
				}
				if (!queue.Any(IsError))
				{
					firstErrorUtc = DateTime.MinValue;
				}
			}
			failStreak = 0;
			lastSendUtc = utcNow;
			if (text2 != null)
			{
				nextTryUtc = utcNow.AddMinutes(10.0);
				break;
			}
		}
	}

	private static string Post(Dictionary<string, object> body, int timeoutMs, out bool answered)
	{
		answered = false;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest httpWebRequest = SiteNet.Create("https://timetocapture.ru/api/v1/telemetry");
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json; charset=utf-8";
			httpWebRequest.UserAgent = "TTC/2.9.13";
			httpWebRequest.Timeout = timeoutMs;
			httpWebRequest.ReadWriteTimeout = timeoutMs;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
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
				int statusCode = (int)httpWebResponse2.StatusCode;
				using (StreamReader streamReader2 = new StreamReader(httpWebResponse2.GetResponseStream(), Encoding.UTF8))
				{
					input = streamReader2.ReadToEnd();
				}
				if (statusCode == 429 || statusCode >= 500)
				{
					return "сервер занят (" + statusCode + ")";
				}
			}
			answered = true;
			Dictionary<string, object> dictionary = Json.DeserializeObject(input) as Dictionary<string, object>;
			object value = null;
			dictionary?.TryGetValue("ok", out value);
			if (value is bool && (bool)value)
			{
				return null;
			}
			object value2 = null;
			dictionary?.TryGetValue("error", out value2);
			return (value2 as string) ?? "сервер отказал";
		}
		catch (Exception ex2)
		{
			return ex2.Message;
		}
	}

	public static string Clean(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return s;
		}
		try
		{
			string text = savedKey;
			if (text != null && text.Length >= 8)
			{
				s = s.Replace(text, "CAPT-…" + text.Substring(text.Length - 4));
			}
			s = KeyRe.Replace(s, (Match m) => "CAPT-…" + m.Value.Substring(m.Value.Length - 4).ToUpperInvariant());
			s = MailRe.Replace(s, "…@…");
			s = UserDirRe.Replace(s, "$1…");
			s = TokenRe.Replace(s, "…");
			string userName = Environment.UserName;
			string machineName = Environment.MachineName;
			if (!string.IsNullOrEmpty(userName) && userName.Length >= 3)
			{
				s = Regex.Replace(s, Regex.Escape(userName), "…", RegexOptions.IgnoreCase);
			}
			if (!string.IsNullOrEmpty(machineName) && machineName.Length >= 3)
			{
				s = Regex.Replace(s, Regex.Escape(machineName), "…", RegexOptions.IgnoreCase);
			}
		}
		catch
		{
		}
		return s;
	}

	private static object CleanData(object v)
	{
		if (v is string s)
		{
			return Short(Clean(s), 200);
		}
		if (v is Dictionary<string, object> source)
		{
			return source.ToDictionary((KeyValuePair<string, object> x) => x.Key, (KeyValuePair<string, object> x) => CleanData(x.Value));
		}
		if (v is object[] source2)
		{
			return source2.Select(CleanData).ToArray();
		}
		return v;
	}

	private static string Short(string s, int max)
	{
		if (s == null)
		{
			return null;
		}
		s = s.Trim();
		if (s.Length > max)
		{
			return s.Substring(0, max - 1) + "…";
		}
		return s;
	}
}
