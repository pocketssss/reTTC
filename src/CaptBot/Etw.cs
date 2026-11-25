using System;
using System.Runtime.InteropServices;

namespace CaptBot;

internal static class Etw
{
	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	public delegate void RecordCallback(IntPtr rec);

	private const ulong InvalidTrace = ulong.MaxValue;

	private const int LogfileSize = 448;

	private const int PropsSize = 120;

	private const uint ModeRealTime = 256u;

	private const uint ModeEventRecord = 268435456u;

	private const uint RealTimeLog = 256u;

	private const uint MsFlushTimer = 16u;

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
	private static extern uint StartTraceW(out ulong handle, string name, IntPtr props);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
	private static extern uint ControlTraceW(ulong handle, string name, IntPtr props, uint code);

	[DllImport("advapi32.dll")]
	private static extern uint EnableTraceEx2(ulong handle, ref Guid provider, uint code, byte level, ulong any, ulong all, uint timeout, IntPtr prm);

	[DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
	private static extern ulong OpenTraceW(IntPtr logfile);

	[DllImport("advapi32.dll")]
	private static extern uint ProcessTrace(ulong[] handles, uint count, IntPtr start, IntPtr end);

	[DllImport("advapi32.dll")]
	private static extern uint CloseTrace(ulong handle);

	public static long TimeStamp(IntPtr r)
	{
		return Marshal.ReadInt64(r, 16);
	}

	public static Guid Provider(IntPtr r)
	{
		byte[] array = new byte[16];
		Marshal.Copy(new IntPtr(r.ToInt64() + 24), array, 0, 16);
		return new Guid(array);
	}

	public static int EventId(IntPtr r)
	{
		return (ushort)Marshal.ReadInt16(r, 40);
	}

	public static byte[] UserData(IntPtr r)
	{
		int num = (ushort)Marshal.ReadInt16(r, 86);
		IntPtr intPtr = Marshal.ReadIntPtr(r, 96);
		byte[] array = new byte[num];
		if (num > 0 && intPtr != IntPtr.Zero)
		{
			Marshal.Copy(intPtr, array, 0, num);
		}
		return array;
	}

	public static ulong Open(string path, string session, RecordCallback cb, out IntPtr mem)
	{
		mem = Marshal.AllocHGlobal(448);
		for (int i = 0; i < 448; i++)
		{
			Marshal.WriteByte(mem, i, 0);
		}
		IntPtr val = Marshal.StringToHGlobalUni(path ?? session);
		Marshal.WriteIntPtr(mem, (path == null) ? 8 : 0, val);
		Marshal.WriteInt32(mem, 28, 0x10000000 | ((path == null) ? 256 : 0));
		Marshal.WriteIntPtr(mem, 424, Marshal.GetFunctionPointerForDelegate(cb));
		return OpenTraceW(mem);
	}

	public static bool Valid(ulong h)
	{
		if (h != ulong.MaxValue)
		{
			return h != 0;
		}
		return false;
	}

	public static uint Process(ulong h)
	{
		return ProcessTrace(new ulong[1] { h }, 1u, IntPtr.Zero, IntPtr.Zero);
	}

	public static void Close(ulong h)
	{
		if (Valid(h))
		{
			CloseTrace(h);
		}
	}

	public static uint ReadFile(string path, Action<IntPtr> onRecord)
	{
		RecordCallback recordCallback = delegate(IntPtr r)
		{
			try
			{
				onRecord(r);
			}
			catch
			{
			}
		};
		IntPtr mem;
		ulong h = Open(path, null, recordCallback, out mem);
		if (!Valid(h))
		{
			return (uint)Marshal.GetLastWin32Error();
		}
		uint result = Process(h);
		Close(h);
		GC.KeepAlive(recordCallback);
		return result;
	}

	private static IntPtr Props(string name)
	{
		int num = 120 + 2 * (name.Length + 1) + 64;
		IntPtr intPtr = Marshal.AllocHGlobal(num);
		for (int i = 0; i < num; i++)
		{
			Marshal.WriteByte(intPtr, i, 0);
		}
		Marshal.WriteInt32(intPtr, 0, num);
		Marshal.WriteInt32(intPtr, 40, 1);
		Marshal.WriteInt32(intPtr, 44, 131072);
		Marshal.WriteInt32(intPtr, 48, 16);
		Marshal.WriteInt32(intPtr, 52, 4);
		Marshal.WriteInt32(intPtr, 56, 64);
		Marshal.WriteInt32(intPtr, 64, 272);
		Marshal.WriteInt32(intPtr, 68, 15);
		Marshal.WriteInt32(intPtr, 116, 120);
		return intPtr;
	}

	public static void StopSession(string name)
	{
		IntPtr intPtr = Props(name);
		try
		{
			ControlTraceW(0uL, name, intPtr, 1u);
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}

	public static uint StartSession(string name, Guid provider, byte level, ulong keywords, out ulong handle)
	{
		handle = 0uL;
		IntPtr intPtr = Props(name);
		try
		{
			uint num = StartTraceW(out handle, name, intPtr);
			if (num == 183)
			{
				ControlTraceW(0uL, name, intPtr, 1u);
				Marshal.FreeHGlobal(intPtr);
				intPtr = Props(name);
				num = StartTraceW(out handle, name, intPtr);
			}
			if (num != 0)
			{
				return num;
			}
			return EnableTraceEx2(handle, ref provider, 1u, level, keywords, 0uL, 0u, IntPtr.Zero);
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
		}
	}
}
