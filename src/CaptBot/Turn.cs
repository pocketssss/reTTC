using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class Turn
{
	public const int AfterMs = 1000;

	private const int LostSec = 45;

	private const int WaitFirst = 8;

	private const int WaitOther = 20;

	public static readonly bool Enabled;

	private static readonly object sync;

	private static readonly JavaScriptSerializer Json;

	private static Job job;

	private static string server;

	private static string phase;

	private static string known;

	private static long eta;

	private static List<Dictionary<string, object>> queue;

	private static string queueKey;

	private static bool alive;

	private static bool asked;

	private static int gen;

	private static long seq;

	private static HttpWebRequest inflight;

	private static volatile bool hold;

	private static volatile bool near;

	private static volatile bool mine;

	private static volatile bool on;

	private static long okTicks;

	private static readonly long SeqBase;

	public static bool Mine => mine;

	public static bool On => on;

	public static bool Held
	{
		get
		{
			if (hold)
			{
				return DateTime.UtcNow.Ticks - Interlocked.Read(ref okTicks) < 450000000;
			}
			return false;
		}
	}

	public static bool Near => near;

	static Turn()
	{
		Enabled = true;
		sync = new object();
		Json = new JavaScriptSerializer();
		server = "";
		phase = "off";
		known = "";
		queue = new List<Dictionary<string, object>>();
		queueKey = "";
		SeqBase = (long)(DateTime.UtcNow - LicenseState.Epoch).TotalMilliseconds;
		NetWatch.OwnFamilyChanged += delegate
		{
			lock (sync)
			{
				if (!alive)
				{
					return;
				}
			}
			Refresh();
		};
	}

	public static void Begin(Job j, string srv)
	{
		if (!Enabled)
		{
			return;
		}
		lock (sync)
		{
			job = j;
			server = srv ?? "";
			phase = "armed";
			eta = 0L;
			hold = (near = false);
			Changed();
		}
	}

	public static void End()
	{
		Dictionary<string, object> b;
		lock (sync)
		{
			if (job == null)
			{
				return;
			}
			b = Body("off");
			job = null;
			phase = "off";
			hold = (near = false);
			Changed();
		}
		Send(b, 2);
	}

	public static void Fired()
	{
		Dictionary<string, object> b;
		lock (sync)
		{
			if (job == null)
			{
				return;
			}
			b = Body("fired");
			job = null;
			phase = "off";
			hold = (near = false);
			Changed();
		}
		Send(b, 3);
	}

	public static void Waiting(bool w)
	{
		lock (sync)
		{
			if (job != null)
			{
				phase = (w ? "held" : "go");
				Changed();
			}
		}
	}

	public static void Eta(long unix)
	{
		lock (sync)
		{
			eta = unix;
		}
	}

	public static void SetQueue(IEnumerable<KeyValuePair<string, long>> items, string srv)
	{
		if (!Enabled)
		{
			return;
		}
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		foreach (KeyValuePair<string, long> item in items)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["square"] = item.Key;
			dictionary["fire_at"] = item.Value;
			list.Add(dictionary);
		}
		string text = string.Join(",", list.Select((Dictionary<string, object> d) => string.Concat(d["square"], "@", d["fire_at"])).ToArray());
		lock (sync)
		{
			if (!(text == queueKey))
			{
				queue = list;
				queueKey = text;
				if (job == null)
				{
					server = srv ?? "";
				}
				Changed();
			}
		}
	}

	public static void Refresh()
	{
		if (Enabled)
		{
			lock (sync)
			{
				Changed();
			}
		}
	}

	private static void Changed()
	{
		gen++;
		asked = false;
		known = "";
		HttpWebRequest r = inflight;
		inflight = null;
		if (r != null)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				try
				{
					r.Abort();
				}
				catch
				{
				}
			});
		}
		if (!alive && (job != null || queue.Count != 0 || mine))
		{
			alive = true;
			Thread thread = new Thread(Loop);
			thread.IsBackground = true;
			thread.Name = "TTC turn";
			Thread thread2 = thread;
			thread2.Start();
		}
	}

	private static Dictionary<string, object> Body(string ph)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		LicenseState current = License.Current;
		dictionary["key"] = ((current != null) ? current.Key : "");
		dictionary["hwid"] = License.Hwid();
		dictionary["ver"] = "2.9.13";
		dictionary["server"] = ((!string.IsNullOrEmpty(NetWatch.GameServer)) ? NetWatch.GameServer : server);
		bool flag = !string.IsNullOrEmpty(NetWatch.OwnFamily);
		dictionary["fam"] = (flag ? NetWatch.OwnFamily : ((Turn.job != null) ? (Turn.job.Family ?? "") : ""));
		dictionary["net"] = flag;
		dictionary["phase"] = ph;
		dictionary["seq"] = ++seq + SeqBase;
		Job job = Turn.job;
		if (job != null)
		{
			dictionary["square"] = job.Square;
			dictionary["prep"] = job.PrepSec;
			long num = (job.Watch ? eta : ((job.FireUtc == DateTime.MinValue) ? 0 : War.Unix(job.FireUtc)));
			if (num > 0 && (!job.Watch || num > War.NowUnix()))
			{
				dictionary["fire_at"] = num;
			}
		}
		if (queue.Count > 0)
		{
			dictionary["queue"] = queue;
		}
		return dictionary;
	}

	private static void Loop()
	{
		while (true)
		{
			Dictionary<string, object> dictionary;
			int num;
			int num2;
			lock (sync)
			{
				if (job == null && ((asked && !mine) || queue.Count == 0))
				{
					alive = false;
					mine = false;
					break;
				}
				dictionary = Body((job != null) ? phase : "off");
				num = (mine ? 8 : 20);
				dictionary["known"] = known;
				dictionary["wait"] = ((known.Length > 0) ? num : 0);
				num2 = gen;
			}
			string err;
			Dictionary<string, object> dictionary2 = Post(dictionary, (num + 12) * 1000, num2, out err);
			bool flag;
			lock (sync)
			{
				flag = num2 != gen;
				if (!flag && dictionary2 != null)
				{
					on = Is(dictionary2, "on");
					mine = Is(dictionary2, "mine");
					near = Is(dictionary2, "near");
					hold = Is(dictionary2, "hold") && !mine;
					known = ((dictionary2.TryGetValue("state", out var value) && value != null) ? Convert.ToString(value, CultureInfo.InvariantCulture) : "");
					asked = true;
					Interlocked.Exchange(ref okTicks, DateTime.UtcNow.Ticks);
				}
			}
			if (!flag && dictionary2 == null)
			{
				Pause(2500, num2);
			}
		}
	}

	private static void Pause(int ms, int g)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < ms)
		{
			lock (sync)
			{
				if (g != gen)
				{
					break;
				}
			}
			Thread.Sleep(50);
		}
	}

	private static void Send(Dictionary<string, object> b, int tries)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			for (int i = 0; i < tries; i++)
			{
				if (Post(b, 5000, -1, out var _) != null)
				{
					break;
				}
				Thread.Sleep(300);
			}
		});
	}

	private static bool Is(Dictionary<string, object> d, string k)
	{
		if (d.TryGetValue(k, out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	private static Dictionary<string, object> Post(Dictionary<string, object> body, int timeoutMs, int g, out string err)
	{
		err = null;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest httpWebRequest = SiteNet.Create("https://timetocapture.ru/api/v1/turn");
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json; charset=utf-8";
			httpWebRequest.UserAgent = "TTC/2.9.13";
			httpWebRequest.Timeout = timeoutMs;
			httpWebRequest.ReadWriteTimeout = timeoutMs;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
			if (httpWebRequest.ServicePoint.ConnectionLimit < 16)
			{
				httpWebRequest.ServicePoint.ConnectionLimit = 16;
			}
			if (g >= 0)
			{
				lock (sync)
				{
					if (g != gen)
					{
						return null;
					}
					inflight = httpWebRequest;
				}
			}
			byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(body));
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			string input;
			using (HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse())
			{
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
				input = streamReader.ReadToEnd();
			}
			return (Json.DeserializeObject(input) is Dictionary<string, object> dictionary && Is(dictionary, "ok")) ? dictionary : null;
		}
		catch (Exception ex)
		{
			err = ex.Message;
			return null;
		}
		finally
		{
			if (g >= 0)
			{
				lock (sync)
				{
					if (g == gen)
					{
						inflight = null;
					}
				}
			}
		}
	}
}
