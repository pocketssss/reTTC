using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CaptBot;

internal static class KbLayout
{
	private const uint WM_INPUTLANGCHANGEREQUEST = 80u;

	[DllImport("user32.dll")]
	private static extern uint GetWindowThreadProcessId(IntPtr h, out int pid);

	[DllImport("user32.dll")]
	private static extern IntPtr GetKeyboardLayout(uint thread);

	[DllImport("user32.dll")]
	private static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern IntPtr LoadKeyboardLayout(string id, uint flags);

	public static IntPtr Current(IntPtr wnd)
	{
		int pid;
		return GetKeyboardLayout(GetWindowThreadProcessId(wnd, out pid));
	}

	public static bool IsEnglish(IntPtr hkl)
	{
		return (hkl.ToInt64() & 0x3FF) == 9;
	}

	public static IntPtr English()
	{
		return LoadKeyboardLayout("00000409", 0u);
	}

	public static bool Switch(IntPtr wnd, IntPtr hkl, int waitMs)
	{
		if (hkl == IntPtr.Zero)
		{
			return false;
		}
		PostMessage(wnd, 80u, IntPtr.Zero, hkl);
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < waitMs)
		{
			if ((Current(wnd).ToInt64() & 0xFFFF) == (hkl.ToInt64() & 0xFFFF))
			{
				return true;
			}
			Thread.Sleep(15);
		}
		return (Current(wnd).ToInt64() & 0xFFFF) == (hkl.ToInt64() & 0xFFFF);
	}
}
