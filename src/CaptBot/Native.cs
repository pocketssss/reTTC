using System;
using System.Runtime.InteropServices;

namespace CaptBot;

internal static class Native
{
	public struct POINT
	{
		public int X;

		public int Y;
	}

	public struct RECT
	{
		public int L;

		public int T;

		public int R;

		public int B;
	}

	public struct MOUSEINPUT
	{
		public int dx;

		public int dy;

		public uint mouseData;

		public uint dwFlags;

		public uint time;

		public IntPtr extra;
	}

	public struct KEYBDINPUT
	{
		public ushort vk;

		public ushort scan;

		public uint flags;

		public uint time;

		public IntPtr extra;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct UNION
	{
		[FieldOffset(0)]
		public MOUSEINPUT mi;

		[FieldOffset(0)]
		public KEYBDINPUT ki;
	}

	public struct INPUT
	{
		public uint type;

		public UNION u;
	}

	public struct POWER_THROTTLING
	{
		public uint Version;

		public uint ControlMask;

		public uint StateMask;
	}

	[DllImport("user32.dll")]
	public static extern uint SendInput(uint n, INPUT[] i, int size);

	[DllImport("user32.dll")]
	public static extern bool SetCursorPos(int x, int y);

	[DllImport("user32.dll")]
	public static extern short GetAsyncKeyState(int vk);

	[DllImport("user32.dll")]
	public static extern bool GetClientRect(IntPtr h, out RECT r);

	[DllImport("user32.dll")]
	public static extern bool GetCursorPos(out POINT p);

	[DllImport("user32.dll")]
	public static extern bool ClientToScreen(IntPtr h, ref POINT p);

	[DllImport("user32.dll")]
	public static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr h);

	[DllImport("user32.dll")]
	public static extern bool ShowWindow(IntPtr h, int c);

	[DllImport("user32.dll")]
	public static extern bool IsIconic(IntPtr h);

	[DllImport("user32.dll")]
	public static extern bool IsWindow(IntPtr h);

	[DllImport("user32.dll")]
	public static extern int GetSystemMetrics(int i);

	[DllImport("kernel32.dll")]
	public static extern void GetSystemTimePreciseAsFileTime(out long ft);

	[DllImport("winmm.dll")]
	public static extern uint timeBeginPeriod(uint p);

	[DllImport("winmm.dll")]
	public static extern uint timeEndPeriod(uint p);

	[DllImport("kernel32.dll")]
	public static extern IntPtr OpenProcess(uint a, bool inh, int pid);

	[DllImport("advapi32.dll")]
	public static extern bool OpenProcessToken(IntPtr p, uint a, out IntPtr t);

	[DllImport("advapi32.dll")]
	public static extern bool GetTokenInformation(IntPtr t, int cls, out int info, int len, out int ret);

	[DllImport("kernel32.dll")]
	public static extern bool CloseHandle(IntPtr h);

	[DllImport("user32.dll")]
	public static extern IntPtr WindowFromPoint(POINT p);

	[DllImport("user32.dll")]
	public static extern IntPtr GetAncestor(IntPtr h, uint flags);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetProcessInformation(IntPtr p, int cls, ref POWER_THROTTLING info, int size);

	[DllImport("kernel32.dll")]
	public static extern IntPtr GetCurrentProcess();

	[DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
	public static extern IntPtr CreateWaitableTimerExW(IntPtr attr, string name, uint flags, uint access);

	[DllImport("kernel32.dll")]
	public static extern bool SetWaitableTimer(IntPtr h, ref long due, int period, IntPtr fn, IntPtr arg, bool resume);

	[DllImport("kernel32.dll")]
	public static extern uint WaitForSingleObject(IntPtr h, uint ms);
}
