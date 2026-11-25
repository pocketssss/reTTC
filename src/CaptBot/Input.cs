using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace CaptBot;

internal static class Input
{
	private static readonly int Size = Marshal.SizeOf(typeof(Native.INPUT));

	private static void Send(Native.INPUT i)
	{
		Native.SendInput(1u, new Native.INPUT[1] { i }, Size);
	}

	public static void Key(ushort scan, bool ext, bool up)
	{
		Native.INPUT i = default(Native.INPUT);
		i.type = 1u;
		i.u.ki.scan = scan;
		i.u.ki.flags = 8u | (ext ? 1u : 0u) | (up ? 2u : 0u);
		Send(i);
	}

	public static void Tap(ushort scan, bool ext, int hold)
	{
		Key(scan, ext, up: false);
		Thread.Sleep(hold);
		Key(scan, ext, up: true);
	}

	public static void MoveTo(int x, int y)
	{
		Native.SetCursorPos(x, y);
		int systemMetrics = Native.GetSystemMetrics(76);
		int systemMetrics2 = Native.GetSystemMetrics(77);
		int systemMetrics3 = Native.GetSystemMetrics(78);
		int systemMetrics4 = Native.GetSystemMetrics(79);
		Native.INPUT i = default(Native.INPUT);
		i.type = 0u;
		i.u.mi.dx = (int)Math.Round((double)(x - systemMetrics) * 65535.0 / (double)Math.Max(1, systemMetrics3 - 1));
		i.u.mi.dy = (int)Math.Round((double)(y - systemMetrics2) * 65535.0 / (double)Math.Max(1, systemMetrics4 - 1));
		i.u.mi.dwFlags = 49153u;
		Send(i);
	}

	public static void Down()
	{
		Native.INPUT i = default(Native.INPUT);
		i.type = 0u;
		i.u.mi.dwFlags = 2u;
		Send(i);
	}

	public static void Up()
	{
		Native.INPUT i = default(Native.INPUT);
		i.type = 0u;
		i.u.mi.dwFlags = 4u;
		Send(i);
	}

	public static void Click(int x, int y, int hold)
	{
		MoveTo(x, y);
		Thread.Sleep(8);
		Down();
		Thread.Sleep(hold);
		Up();
	}

	public static void Wheel(int x, int y, int notches, int pause)
	{
		MoveTo(x, y);
		Thread.Sleep(30);
		int num = Math.Abs(notches);
		uint mouseData = ((notches > 0) ? 120u : 4294967176u);
		for (int i = 0; i < num; i++)
		{
			Native.INPUT i2 = default(Native.INPUT);
			i2.type = 0u;
			i2.u.mi.dwFlags = 2048u;
			i2.u.mi.mouseData = mouseData;
			Send(i2);
			Thread.Sleep(pause);
		}
	}

	public static void Drag(int x1, int y1, int x2, int y2, int steps, int stepMs)
	{
		MoveTo(x1, y1);
		Thread.Sleep(30);
		Down();
		Thread.Sleep(40);
		for (int i = 1; i <= steps; i++)
		{
			MoveTo(x1 + (x2 - x1) * i / steps, y1 + (y2 - y1) * i / steps);
			Thread.Sleep(stepMs);
		}
		Thread.Sleep(40);
		Up();
	}
}
