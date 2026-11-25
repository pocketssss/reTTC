using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CaptBot;

internal static class Precise
{
	private static IntPtr timer = IntPtr.Zero;

	private static bool tried;

	private static readonly object Sync = new object();

	public static string Note = "";

	public static bool HighRes
	{
		get
		{
			Init();
			return timer != IntPtr.Zero;
		}
	}

	public static void Init()
	{
		lock (Sync)
		{
			if (tried)
			{
				return;
			}
			tried = true;
			try
			{
				Native.POWER_THROTTLING pOWER_THROTTLING = default(Native.POWER_THROTTLING);
				pOWER_THROTTLING.Version = 1u;
				pOWER_THROTTLING.ControlMask = 5u;
				pOWER_THROTTLING.StateMask = 0u;
				Native.POWER_THROTTLING info = pOWER_THROTTLING;
				bool flag = Native.SetProcessInformation(Native.GetCurrentProcess(), 4, ref info, Marshal.SizeOf(typeof(Native.POWER_THROTTLING)));
				if (!flag)
				{
					info.ControlMask = 1u;
					flag = Native.SetProcessInformation(Native.GetCurrentProcess(), 4, ref info, Marshal.SizeOf(typeof(Native.POWER_THROTTLING)));
				}
				Note = (flag ? "" : "троттлинг Windows не отключён");
			}
			catch
			{
				Note = "троттлинг Windows не отключён";
			}
			try
			{
				timer = Native.CreateWaitableTimerExW(IntPtr.Zero, null, 2u, 2031619u);
			}
			catch
			{
				timer = IntPtr.Zero;
			}
		}
	}

	public static void Sleep(double ms)
	{
		if (ms <= 0.0)
		{
			return;
		}
		Init();
		if (timer != IntPtr.Zero)
		{
			long due = -(long)(ms * 10000.0);
			if (Native.SetWaitableTimer(timer, ref due, 0, IntPtr.Zero, IntPtr.Zero, resume: false))
			{
				Native.WaitForSingleObject(timer, uint.MaxValue);
				return;
			}
		}
		Thread.Sleep(Math.Max(1, (int)ms));
	}
}
