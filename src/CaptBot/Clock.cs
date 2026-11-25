using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CaptBot;

internal static class Clock
{
	public static double OffsetMs;

	private static readonly string[] NtpHosts = new string[5] { "time.google.com", "time.windows.com", "pool.ntp.org", "ntp1.vniiftri.ru", "ntp.ix.ru" };

	public static DateTime Raw()
	{
		Native.GetSystemTimePreciseAsFileTime(out var ft);
		return DateTime.FromFileTimeUtc(ft);
	}

	public static DateTime UtcNow()
	{
		return Raw().AddTicks((long)(OffsetMs * 10000.0));
	}

	public static DateTime Msk(DateTime utc)
	{
		return utc.AddHours(3.0);
	}

	public static string Fmt(DateTime utc)
	{
		return Msk(utc).ToString("HH:mm:ss.fff");
	}

	public static bool ParseTime(string s, out TimeSpan t)
	{
		string[] formats = new string[10] { "h\\:mm", "hh\\:mm", "h\\:mm\\:ss", "hh\\:mm\\:ss", "h\\:mm\\:ss\\.f", "hh\\:mm\\:ss\\.f", "h\\:mm\\:ss\\.ff", "hh\\:mm\\:ss\\.ff", "h\\:mm\\:ss\\.fff", "hh\\:mm\\:ss\\.fff" };
		s = (s ?? "").Trim().Replace(',', '.');
		if (!TimeSpan.TryParseExact(s, formats, CultureInfo.InvariantCulture, out t))
		{
			return false;
		}
		return t.TotalHours < 24.0;
	}

	public static DateTime NextMskUtc(TimeSpan tod)
	{
		DateTime dateTime = Msk(UtcNow());
		DateTime dateTime2 = dateTime.Date + tod;
		if (dateTime2 <= dateTime)
		{
			dateTime2 = dateTime2.AddDays(1.0);
		}
		return DateTime.SpecifyKind(dateTime2.AddHours(-3.0), DateTimeKind.Utc);
	}

	private static DateTime NtpTs(byte[] d, int i)
	{
		ulong num = ((ulong)d[i] << 24) | ((ulong)d[i + 1] << 16) | ((ulong)d[i + 2] << 8) | d[i + 3];
		ulong num2 = ((ulong)d[i + 4] << 24) | ((ulong)d[i + 5] << 16) | ((ulong)d[i + 6] << 8) | d[i + 7];
		double num3 = (double)num * 1000.0 + (double)num2 * 1000.0 / 4294967296.0;
		return new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks((long)(num3 * 10000.0));
	}

	public static bool Sync(out double off, out double rtt, out string host)
	{
		string note;
		return Sync(out off, out rtt, out host, out note);
	}

	public static bool Sync(out double off, out double rtt, out string host, out string note)
	{
		Task<double[]>[] array = NtpHosts.Select((string h) => Task.Factory.StartNew(() => QueryHost(h, 5))).ToArray();
		try
		{
			Task.WaitAll(array, 12000);
		}
		catch
		{
		}
		int num = -1;
		List<double> list = new List<double>();
		for (int i = 0; i < array.Length; i++)
		{
			double[] array2 = ((array[i].Status == TaskStatus.RanToCompletion) ? array[i].Result : null);
			if (array2 != null)
			{
				list.Add(array2[0]);
				if (num < 0 || array2[1] < array[num].Result[1])
				{
					num = i;
				}
			}
		}
		if (num < 0)
		{
			off = 0.0;
			rtt = 0.0;
			host = null;
			note = "";
			return false;
		}
		off = array[num].Result[0];
		rtt = array[num].Result[1];
		host = NtpHosts[num];
		int count = list.Count;
		string arg = ((count % 10 >= 2 && count % 10 <= 4 && (count % 100 < 12 || count % 100 > 14)) ? "сервера" : "серверов");
		note = ((count > 1) ? $"ответили {count} {arg}, расходятся на {list.Max() - list.Min():0} мс" : "ответил только он");
		return true;
	}

	private static double[] QueryHost(string h, int n)
	{
		try
		{
			IPAddress address = Dns.GetHostAddresses(h).First((IPAddress a) => a.AddressFamily == AddressFamily.InterNetwork);
			double num = double.MaxValue;
			double num2 = 0.0;
			for (int i = 0; i < n; i++)
			{
				try
				{
					byte[] array = new byte[48];
					array[0] = 27;
					using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
					socket.ReceiveTimeout = 1500;
					socket.SendTimeout = 1500;
					socket.Connect(new IPEndPoint(address, 123));
					DateTime dateTime = Raw();
					socket.Send(array);
					int num3 = socket.Receive(array);
					DateTime dateTime2 = Raw();
					if (num3 < 48 || (array[0] & 7) != 4 || array[1] == 0 || array[1] > 15)
					{
						continue;
					}
					DateTime dateTime3 = NtpTs(array, 32);
					DateTime dateTime4 = NtpTs(array, 40);
					double num4 = (dateTime2 - dateTime).TotalMilliseconds - (dateTime4 - dateTime3).TotalMilliseconds;
					double num5 = ((dateTime3 - dateTime).TotalMilliseconds + (dateTime4 - dateTime2).TotalMilliseconds) / 2.0;
					if (num4 >= 0.0 && num4 < num)
					{
						num = num4;
						num2 = num5;
					}
					goto IL_0163;
				}
				catch (SocketException)
				{
					goto IL_0163;
				}
				IL_0163:
				Thread.Sleep(80);
			}
			return (num < double.MaxValue) ? new double[2] { num2, num } : null;
		}
		catch
		{
			return null;
		}
	}
}
