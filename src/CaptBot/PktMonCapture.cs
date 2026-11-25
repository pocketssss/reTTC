using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CaptBot;

internal static class PktMonCapture
{
	private const string Session = "TTC-PktMon";

	public static readonly Guid Provider = new Guid("4d4f80d9-c8bd-4d73-bb5b-19c90402c5ac");

	public static string Error;

	public static long Events;

	public static long Packets;

	public static Action<byte[], int, DateTime> OnPacket = delegate
	{
	};

	private static readonly object Sync = new object();

	private static ulong session;

	private static ulong trace;

	private static IntPtr logfileMem;

	private static Etw.RecordCallback cb;

	private static Thread thread;

	private static string ports = "";

	private static readonly Dictionary<long, long> recent = new Dictionary<long, long>();

	public static bool Running { get; private set; }

	private static bool Pktmon(string args, out string output)
	{
		output = "";
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo("pktmon.exe", args);
			processStartInfo.UseShellExecute = false;
			processStartInfo.CreateNoWindow = true;
			processStartInfo.RedirectStandardOutput = true;
			processStartInfo.RedirectStandardError = true;
			ProcessStartInfo startInfo = processStartInfo;
			using Process process = Process.Start(startInfo);
			output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
			if (!process.WaitForExit(10000))
			{
				try
				{
					process.Kill();
				}
				catch
				{
				}
				return false;
			}
			return process.ExitCode == 0;
		}
		catch (Exception ex)
		{
			output = ex.Message;
			return false;
		}
	}

	public static bool Start(ICollection<int> udpPorts)
	{
		lock (Sync)
		{
			List<int> list = new List<int>(udpPorts);
			list.Sort();
			string text = string.Join(",", list);
			if (Running && text == ports)
			{
				return true;
			}
			if (Running)
			{
				StopLocked();
			}
			Error = null;
			if (list.Count == 0)
			{
				Error = "нет портов игры";
				return false;
			}
			Pktmon("stop", out var output);
			Pktmon("filter remove", out output);
			foreach (int item in list)
			{
				if (!Pktmon("filter add TTC" + item + " -t UDP -p " + item, out output))
				{
					Error = "pktmon не принял фильтр: " + Short(output);
					Cleanup();
					return false;
				}
			}
			if (!Pktmon("start --capture --comp nics --pkt-size 0 -m memory -s 16", out output))
			{
				Error = "pktmon не запустился: " + Short(output);
				Cleanup();
				return false;
			}
			ulong handle;
			uint num = Etw.StartSession("TTC-PktMon", Provider, 4, 48uL, out handle);
			if (num != 0)
			{
				Error = "сеанс ETW: ошибка " + num;
				Etw.StopSession("TTC-PktMon");
				Cleanup();
				return false;
			}
			session = handle;
			cb = OnRecord;
			trace = Etw.Open(null, "TTC-PktMon", cb, out logfileMem);
			if (!Etw.Valid(trace))
			{
				Error = "сеанс ETW не открылся: " + Marshal.GetLastWin32Error();
				Etw.StopSession("TTC-PktMon");
				Cleanup();
				return false;
			}
			ulong t = trace;
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					Etw.Process(t);
				}
				catch
				{
				}
			});
			thread.IsBackground = true;
			thread.Name = "PktMon.read";
			PktMonCapture.thread = thread;
			PktMonCapture.thread.Start();
			ports = text;
			Running = true;
			return true;
		}
	}

	public static void Stop()
	{
		lock (Sync)
		{
			StopLocked();
		}
	}

	public static void CleanupStale()
	{
		lock (Sync)
		{
			if (!Running)
			{
				if (Pktmon("filter list", out var output) && output.Contains("TTC"))
				{
					Cleanup();
				}
				Etw.StopSession("TTC-PktMon");
			}
		}
	}

	private static void StopLocked()
	{
		if (Running)
		{
			Running = false;
			ports = "";
			try
			{
				Etw.Close(trace);
			}
			catch
			{
			}
			try
			{
				Etw.StopSession("TTC-PktMon");
			}
			catch
			{
			}
			Cleanup();
			if (logfileMem != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(logfileMem);
				logfileMem = IntPtr.Zero;
			}
		}
	}

	private static void Cleanup()
	{
		Pktmon("stop", out var output);
		Pktmon("filter remove", out output);
	}

	private static string Short(string s)
	{
		s = (s ?? "").Trim().Replace("\r", " ").Replace("\n", " ");
		if (s.Length <= 120)
		{
			return s;
		}
		return s.Substring(0, 120) + "…";
	}

	private static void OnRecord(IntPtr r)
	{
		try
		{
			if (Etw.EventId(r) != 160 || Etw.Provider(r) != Provider)
			{
				return;
			}
			Interlocked.Increment(ref Events);
			byte[] array = Etw.UserData(r);
			int num = FindIPv4(array);
			if (num < 0)
			{
				return;
			}
			int num2 = (array[num + 2] << 8) | array[num + 3];
			if (num + num2 > array.Length)
			{
				num2 = array.Length - num;
			}
			long num3 = Etw.TimeStamp(r);
			long key = ((long)((array[num + 4] << 8) | array[num + 5]) << 48) ^ ((long)num2 << 32) ^ BitConverter.ToInt32(array, num + 12) ^ ((long)BitConverter.ToInt32(array, num + 16) << 16) ^ ((long)BitConverter.ToInt32(array, num + 20) << 8);
			lock (recent)
			{
				if (recent.TryGetValue(key, out var value) && Math.Abs(num3 - value) < 3000000)
				{
					return;
				}
				recent[key] = num3;
				if (recent.Count > 4096)
				{
					recent.Clear();
				}
			}
			byte[] array2 = new byte[num2];
			Buffer.BlockCopy(array, num, array2, 0, num2);
			Interlocked.Increment(ref Packets);
			DateTime arg = ((num3 > 0) ? DateTime.FromFileTimeUtc(num3) : DateTime.UtcNow);
			OnPacket(array2, num2, arg);
		}
		catch
		{
		}
	}

	public static int FindIPv4(byte[] d)
	{
		for (int i = 0; i + 28 <= d.Length; i++)
		{
			if (d[i] >> 4 != 4)
			{
				continue;
			}
			int num = (d[i] & 0xF) * 4;
			if (num < 20 || i + num + 8 > d.Length || d[i + 9] != 17)
			{
				continue;
			}
			int num2 = (d[i + 2] << 8) | d[i + 3];
			if (num2 >= num + 8 && i + num2 <= d.Length)
			{
				int num3 = 0;
				for (int j = 0; j < num; j += 2)
				{
					num3 += (d[i + j] << 8) | d[i + j + 1];
				}
				while (num3 >> 16 != 0)
				{
					num3 = (num3 & 0xFFFF) + (num3 >> 16);
				}
				bool flag = d[i + 10] == 0 && d[i + 11] == 0;
				if (num3 == 65535 || (flag && num == 20))
				{
					return i;
				}
			}
		}
		return -1;
	}
}
