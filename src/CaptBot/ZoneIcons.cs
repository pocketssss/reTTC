using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace CaptBot;

internal static class ZoneIcons
{
	private static readonly Dictionary<int, string> D = new Dictionary<int, string>
	{
		{ 1, "M2.5,8 L8,3 L13.5,8 M4,7 V13 H12 V7 M7,13 V10 H9 V13" },
		{ 4, "M3,3 H13 V13 H3 Z M5.6,5.6 L5.61,5.61 M8,8 L8.01,8.01 M10.4,10.4 L10.41,10.41 M10.4,5.6 L10.41,5.61 M5.6,10.4 L5.61,10.41" },
		{ 5, "M6.3,3 H9.7 V6.3 H13 V9.7 H9.7 V13 H6.3 V9.7 H3 V6.3 H6.3 Z" },
		{ 6, "M2.5,6 L8,2.8 L13.5,6 Z M4,6.5 V11.5 M6.7,6.5 V11.5 M9.3,6.5 V11.5 M12,6.5 V11.5 M2.5,13 H13.5" },
		{ 7, "M3,3 H13 V13 H3 Z M6,3 V13 M8,3 V13 M10,3 V13" },
		{ 8, "M3,7 A4,4 0 1 1 11,7 A4,4 0 1 1 3,7 Z M10,10 L13.5,13.5" },
		{ 9, "M8,2.5 L9.6,6 L13.4,6.3 L10.5,8.8 L11.4,12.5 L8,10.5 L4.6,12.5 L5.5,8.8 L2.6,6.3 L6.4,6 Z" },
		{ 10, "M8,1.8 L13,3.8 V7.5 C13,10.8 10.8,13.2 8,14.2 C5.2,13.2 3,10.8 3,7.5 V3.8 Z M8,5.5 L8.9,7.4 L11,7.6 L9.4,9 L9.9,11 L8,9.9 L6.1,11 L6.6,9 L5,7.6 L7.1,7.4 Z" },
		{ 11, "M2.5,6 L5,3.5 L7,5.5 L4.5,8 Z M9,10.5 L11.5,8 L13.5,10 L11,12.5 Z M6,6.5 L9.5,10 M7.2,9 L5.5,10.7 M3.5,12.5 C4.5,13.5 6,13 6.8,12.2" },
		{ 15, "M3,13 C3,6 7,3 13,3 C13,9 10,13 3,13 Z M3,13 L9,7" },
		{ 16, "M2,4 L5.5,12 M5.5,4 L2,12 M7,4 L10.5,12 M10.5,4 L7,12 M12.5,4 L14,12 M15.5,4 L14,12" },
		{ 17, "M6,2.5 H10 M7,2.5 V6.5 L3.5,12.5 C3,13.3 3.5,13.8 4.3,13.8 H11.7 C12.5,13.8 13,13.3 12.5,12.5 L9,6.5 V2.5 M5,10 H11" },
		{ 18, "M4,14 V3 H13 M4,3 L7,6 M11,3 V7 M10,7 H12 V9 H10 Z M2.5,14 H6" },
		{ 24, "M8,1.8 L13,3.8 V7.5 C13,10.8 10.8,13.2 8,14.2 C5.2,13.2 3,10.8 3,7.5 V3.8 Z M8,4.5 V11.5 M5,8 H11" },
		{ 25, "M6,3 A2,2 0 0 1 10,3 V8 A2,2 0 0 1 6,8 Z M4,7.5 C4,10 6,11.5 8,11.5 C10,11.5 12,10 12,7.5 M8,11.5 V14 M6,14 H10" },
		{ 26, "M2.5,10.5 V8.2 L4.8,5.5 H11.2 L13.5,8.2 V10.5 Z M5,12.3 A1.1,1.1 0 1 1 5.01,12.3 M11,12.3 A1.1,1.1 0 1 1 11.01,12.3 M7,5.5 L9,8.2" },
		{ 100, "M2.5,6 H13.5 L12,12.5 H4 Z M5.5,6 L7,3 M10.5,6 L9,3" },
		{ 101, "M5,13 V7 C5,5 6,3.5 6.5,3.5 C7,3.5 8,5 8,7 V13 Z M10,13 V8 C10,6.5 10.7,5 11.2,5 C11.7,5 12.4,6.5 12.4,8 V13 Z" },
		{ 102, "M5,13 A1.8,1.8 0 1 1 5,9.4 A1.8,1.8 0 1 1 5,13 Z M11,13 A1.8,1.8 0 1 1 11,9.4 A1.8,1.8 0 1 1 11,13 Z M6.3,9.8 L11,3 M9.7,9.8 L5,3" },
		{ 103, "M5.5,3 L2.5,5 L4,7.5 L5,7 V13.5 H11 V7 L12,7.5 L13.5,5 L10.5,3 C10,4.2 9,5 8,5 C7,5 6,4.2 5.5,3 Z" },
		{ 104, "M10.5,2.5 A3,3 0 0 0 9.8,7.6 L3,13 L4.5,14 L10.6,8.6 A3,3 0 0 0 13.5,5.5 L11.6,6 L10.6,5 Z" },
		{ 105, "M3,14 V3.5 H9 V14 Z M2,14 H10 M4.5,6 H7.5 M9,6 H11 L12.5,7.5 V12 A1,1 0 0 1 10.5,12 V9.5 H9" },
		{ 106, "M3,13 L5,11 M4,12 L11,5 L13,7 L6,14 Z M11,5 L12.5,3.5 L14,5 L13,7" },
		{ 107, "M2.5,10.5 V8.2 L4.8,5.5 H11.2 L13.5,8.2 V10.5 Z M5,12.3 A1.1,1.1 0 1 1 5.01,12.3 M11,12.3 A1.1,1.1 0 1 1 11.01,12.3" },
		{ 108, "M8,2.5 C8,2.5 12,7 12,9.8 A4,4 0 0 1 4,9.8 C4,7 8,2.5 8,2.5 Z" },
		{ 109, "M2,10 H14 L12,13 H4 Z M5,10 V6 H11 V10 M8,6 V3" },
		{ 209, "M2.5,13.5 V7 L6,9 V7 L9.5,9 V3 H12.5 V13.5 Z M2.5,13.5 H13.5" },
		{ 300, "M2,13 L6,6 L9,10 L11,8 L14,13 Z" },
		{ 301, "M2.5,8 C2.5,4 13.5,4 13.5,8 Z M6.5,8 V12.5 C6.5,13.6 9.5,13.6 9.5,12.5 V8" },
		{ 302, "M2.5,8 C4.5,4.8 9,4.8 11.2,8 C9,11.2 4.5,11.2 2.5,8 Z M11.2,8 L14,5.6 V10.4 Z M5.3,7.5 L5.31,7.51" },
		{ 303, "M8,2 L12.5,9 H3.5 Z M8,9 V14 M6,14 H10" },
		{ 304, "M8,14 V5 M8,6 C6,6 5,4.5 5,3 C7,3 8,4.5 8,6 C8,4.5 9,3 11,3 C11,4.5 10,6 8,6 M8,9.5 C6,9.5 5,8 5,6.5 M8,9.5 C10,9.5 11,8 11,6.5 M8,13 C6,13 5,11.5 5,10 M8,13 C10,13 11,11.5 11,10" },
		{ 400, "M5,13 V7 C5,5 6,3.5 6.5,3.5 C7,3.5 8,5 8,7 V13 Z M10,9 H13 M11.5,7.5 V10.5" },
		{ 401, "M8,5.5 A2.5,2.5 0 1 1 7.99,5.5 M8,2.5 V4 M8,12 V13.5 M2.5,8 H4 M12,8 H13.5 M4.1,4.1 L5.2,5.2 M10.8,10.8 L11.9,11.9 M11.9,4.1 L10.8,5.2 M5.2,10.8 L4.1,11.9" },
		{ 402, "M6.3,3 H9.7 V6.3 H13 V9.7 H9.7 V13 H6.3 V9.7 H3 V6.3 H6.3 Z" },
		{ 702, "M8,2 C8.8,2 9,3 9,4 V6.5 L14,9.5 V11 L9,9.5 V12 L10.5,13.2 V14 L8,13.3 L5.5,14 V13.2 L7,12 V9.5 L2,11 V9.5 L7,6.5 V4 C7,3 7.2,2 8,2 Z" },
		{ 703, "M8,1.5 L13.8,12.2 L9.2,10.6 L8,14.2 L6.8,10.6 L2.2,12.2 Z M8,5.5 V9.5" },
		{ 800, "M3,13.5 V3 H13 V13.5 M2,13.5 H14 M10,8 V9" }
	};

	private static readonly Dictionary<int, Geometry> G = new Dictionary<int, Geometry>();

	private static readonly Pen IconPen = Mk(Color.FromRgb(242, 179, 61), 1.8);

	private static readonly Pen Halo = Mk(Color.FromArgb(192, 21, 23, 27), 3.6);

	private static Pen Mk(Color c, double w)
	{
		Pen pen = new Pen(new SolidColorBrush(c), w);
		pen.LineJoin = PenLineJoin.Round;
		pen.StartLineCap = PenLineCap.Round;
		pen.EndLineCap = PenLineCap.Round;
		Pen pen2 = pen;
		pen2.Freeze();
		return pen2;
	}

	private static Geometry Geo(int sub)
	{
		if (G.TryGetValue(sub, out var value))
		{
			return value;
		}
		if (!D.TryGetValue(sub, out var value2))
		{
			return null;
		}
		value = Geometry.Parse(value2);
		value.Freeze();
		G[sub] = value;
		return value;
	}

	public static bool Has(int sub)
	{
		return D.ContainsKey(sub);
	}

	public static bool Draw(DrawingContext dc, Zone z, Rect r, double cellPx, bool corner)
	{
		Geometry geometry = Geo(z.Sub);
		if (geometry == null)
		{
			return false;
		}
		double num = Math.Min(r.Width, r.Height);
		double num2 = (corner ? Math.Max(8.0, Math.Min(17.0, num * 0.28)) : Math.Max(9.0, Math.Min(20.0, num * 0.6)));
		if (num < num2 + 2.0)
		{
			return false;
		}
		double offsetX = (corner ? (r.Right - num2 - 2.0) : (r.X + (r.Width - num2) / 2.0));
		double offsetY = (corner ? (r.Y + 2.0) : (r.Y + (r.Height - num2) / 2.0));
		double num3 = num2 / 16.0;
		dc.PushTransform(new MatrixTransform(num3, 0.0, 0.0, num3, offsetX, offsetY));
		dc.DrawGeometry(null, Halo, geometry);
		dc.DrawGeometry(null, IconPen, geometry);
		dc.Pop();
		return true;
	}
}
