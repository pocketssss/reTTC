using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;

namespace CaptBot;

internal static class SiteNet
{
	private const string Address = "195.62.49.142";

	private const int WaitMs = 4000;

	private const int RecheckMs = 120000;

	private const int Connections = 16;

	private static readonly Uri Site = new Uri("https://timetocapture.ru");

	private static readonly bool named = Site.Scheme == "https" && Site.HostNameType == UriHostNameType.Dns && !Site.IsLoopback;

	private static readonly object sync = new object();

	private static readonly Stopwatch clock = Stopwatch.StartNew();

	private static bool known;

	private static bool lost;

	private static bool checking;

	private static long nextMs;

	private static string answer = "";

	private static string bad = "";

	public static bool Direct
	{
		get
		{
			lock (sync)
			{
				return lost;
			}
		}
	}

	public static HttpWebRequest Create(string url)
	{
		Uri uri = new Uri(url);
		HttpWebRequest httpWebRequest = ((named && uri.Host == Site.Host && Lost(uri)) ? ByAddress(uri) : ((HttpWebRequest)WebRequest.Create(uri)));
		try
		{
			if (httpWebRequest.ServicePoint.ConnectionLimit < 16)
			{
				httpWebRequest.ServicePoint.ConnectionLimit = 16;
			}
		}
		catch
		{
		}
		return httpWebRequest;
	}

	private static HttpWebRequest ByAddress(Uri u)
	{
		UriBuilder uriBuilder = new UriBuilder(u);
		uriBuilder.Host = "195.62.49.142";
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(uriBuilder.Uri);
		httpWebRequest.Host = u.Host;
		return httpWebRequest;
	}

	public static byte[] Get(string url, int timeoutMs)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
		HttpWebRequest httpWebRequest = Create(url);
		httpWebRequest.UserAgent = "TTC/2.9.13";
		httpWebRequest.Timeout = timeoutMs;
		httpWebRequest.ReadWriteTimeout = timeoutMs;
		httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
		using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		using Stream stream = httpWebResponse.GetResponseStream();
		using MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		return memoryStream.ToArray();
	}

	private static bool Lost(Uri u)
	{
		if (Proxied(u))
		{
			return false;
		}
		lock (sync)
		{
			if (!known)
			{
				Check();
			}
			else if (!checking && clock.ElapsedMilliseconds >= nextMs)
			{
				checking = true;
				ThreadPool.QueueUserWorkItem(delegate
				{
					try
					{
						Check();
					}
					finally
					{
						lock (sync)
						{
							checking = false;
						}
					}
				});
			}
			return lost;
		}
	}

	private static bool Proxied(Uri u)
	{
		try
		{
			IWebProxy defaultWebProxy = WebRequest.DefaultWebProxy;
			if (defaultWebProxy == null)
			{
				return false;
			}
			Uri proxy = defaultWebProxy.GetProxy(u);
			return proxy != null && proxy.Host != u.Host;
		}
		catch
		{
			return false;
		}
	}

	private static void Check()
	{
		string text = "";
		try
		{
			IAsyncResult asyncResult = Dns.BeginGetHostAddresses(Site.Host, null, null);
			if (asyncResult.AsyncWaitHandle.WaitOne(4000))
			{
				text = Join(Dns.EndGetHostAddresses(asyncResult));
			}
		}
		catch
		{
		}
		lock (sync)
		{
			answer = text;
			known = true;
			lost = text.Length == 0 || text == bad;
			nextMs = clock.ElapsedMilliseconds + 120000;
		}
	}

	private static string Join(IPAddress[] a)
	{
		return string.Join(" ", (from x in a
			where !IPAddress.IsLoopback(x) && !x.Equals(IPAddress.Any) && !x.Equals(IPAddress.IPv6Any)
			select x.ToString()).OrderBy((string x) => x, StringComparer.Ordinal));
	}

	public static HttpWebRequest Retry(HttpWebRequest r, WebException ex)
	{
		if (!named || ex.Response != null || r.RequestUri.Host != Site.Host || Proxied(r.RequestUri))
		{
			return null;
		}
		lock (sync)
		{
			if (ex.Status != WebExceptionStatus.NameResolutionFailure && answer.Split(' ').Contains("195.62.49.142"))
			{
				return null;
			}
		}
		return ByAddress(r.RequestUri);
	}

	public static void Keep()
	{
		lock (sync)
		{
			if (!answer.Split(' ').Contains("195.62.49.142"))
			{
				bad = answer;
			}
			known = true;
			lost = true;
			nextMs = clock.ElapsedMilliseconds + 120000;
		}
	}

	public static string Why(Exception ex)
	{
		WebException ex2 = ex as WebException;
		if (Direct || (ex2 != null && ex2.Status == WebExceptionStatus.NameResolutionFailure))
		{
			return "Компьютер не находит адрес " + Site.Host + ", а напрямую сервер TTC не отвечает. Проверьте интернет. Если другие сайты открываются, включите VPN на весь компьютер: VPN в браузере на TTC не действует.";
		}
		return "Нет связи с сервером TTC (" + ex.Message.Trim().TrimEnd('.') + "). Проверьте интернет и попробуйте ещё раз.";
	}
}
