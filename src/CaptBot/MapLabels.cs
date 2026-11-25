using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CaptBot;

internal static class MapLabels
{
	private class Pt
	{
		public double C;

		public double R;

		public double X;

		public double Y;

		public string Code;
	}

	public class ToggleLook
	{
		public int Mean;

		public int Pill;

		public double PillShare;

		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "квадрат #{0:X6}, цвета плашек {1:0}%, плашки #{2:X6}", Mean, PillShare * 100.0, Pill);
		}

		public bool Same(ToggleLook o)
		{
			if (o != null && Frame.Near(Mean, o.Mean, 10) && Frame.Near(Pill, o.Pill, 10))
			{
				return Math.Abs(PillShare - o.PillShare) < 0.15;
			}
			return false;
		}
	}

	private const string CyrFrom = "АВСЕНКМОРТХЗЈІДбавсенкмортхзјід";

	private const string CyrTo = "ABCEHKMOPTX3JIA6ABCEHKMOPTX3JIA";

	public static string Normalize(string word)
	{
		if (string.IsNullOrEmpty(word))
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = word.Trim();
		foreach (char c in text)
		{
			char c2 = c;
			int num = "АВСЕНКМОРТХЗЈІДбавсенкмортхзјід".IndexOf(c2);
			if (num >= 0)
			{
				c2 = "ABCEHKMOPTX3JIA6ABCEHKMOPTX3JIA"[num];
			}
			c2 = char.ToUpperInvariant(c2);
			if (char.IsLetterOrDigit(c2))
			{
				stringBuilder.Append(c2);
			}
		}
		string text2 = stringBuilder.ToString();
		if (text2.Length < 2 || text2.Length > 4)
		{
			return null;
		}
		for (int j = 1; j <= 2; j++)
		{
			if (text2.Length - j < 1 || text2.Length - j > 2)
			{
				continue;
			}
			string text3 = Letters(text2.Substring(0, j));
			string text4 = Digits(text2.Substring(j));
			if (text3 != null && text4 != null)
			{
				string text5 = text3 + text4;
				if (Runner.ParseSquare(text5, out var _, out var _))
				{
					return text5;
				}
			}
		}
		return null;
	}

	private static string Letters(string s)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in s)
		{
			int num;
			switch (c)
			{
			default:
				num = c;
				break;
			case '4':
				num = 65;
				break;
			case '0':
			case 'O':
				num = 68;
				break;
			case '8':
				num = 66;
				break;
			case '6':
				num = 71;
				break;
			case '1':
			case 'L':
				num = 73;
				break;
			}
			char c2 = (char)num;
			if (c2 < 'A' || c2 > 'J')
			{
				return null;
			}
			stringBuilder.Append(c2);
		}
		return stringBuilder.ToString();
	}

	private static string Digits(string s)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in s)
		{
			int num;
			switch (c)
			{
			default:
				num = c;
				break;
			case 'G':
				num = 54;
				break;
			case 'B':
				num = 56;
				break;
			case 'S':
				num = 53;
				break;
			case 'Z':
				num = 50;
				break;
			case 'I':
			case 'J':
			case 'L':
				num = 49;
				break;
			case 'D':
			case 'O':
				num = 48;
				break;
			}
			char c2 = (char)num;
			if (c2 < '0' || c2 > '9')
			{
				return null;
			}
			stringBuilder.Append(c2);
		}
		return stringBuilder.ToString();
	}

	private static List<Pt> Points(IEnumerable<OcrLine> words)
	{
		List<Pt> list = new List<Pt>();
		foreach (OcrLine word in words)
		{
			string code = Normalize(word.Text);
			if (code == null)
			{
				continue;
			}
			Zone zone = Zones.ByCode(code);
			if (zone != null && !(zone.Label != code))
			{
				double x = (double)word.Box.X + (double)word.Box.Width / 2.0;
				double y = (double)word.Box.Y + (double)word.Box.Height / 2.0;
				if (!list.Any((Pt t) => t.Code == code && Math.Abs(t.X - x) < 10.0 && Math.Abs(t.Y - y) < 10.0))
				{
					list.Add(new Pt
					{
						Code = code,
						C = (double)(zone.C0 + zone.C1) / 2.0,
						R = (double)(zone.R0 + zone.R1) / 2.0,
						X = x,
						Y = y
					});
				}
			}
		}
		return list;
	}

	private static List<Pt> Inliers(List<Pt> pts, double ax, double ay, double p, out double err)
	{
		double num = Math.Max(3.0, 0.25 * p);
		List<Pt> list = new List<Pt>();
		err = 0.0;
		foreach (Pt pt in pts)
		{
			double num2 = pt.X - (ax + p * pt.C);
			double num3 = pt.Y - (ay + p * pt.R);
			if (Math.Abs(num2) < num && Math.Abs(num3) < num)
			{
				list.Add(pt);
				err += num2 * num2 + num3 * num3;
			}
		}
		return list;
	}

	private static MapGrid LeastSquares(List<Pt> inl)
	{
		int count = inl.Count;
		if (count < 2)
		{
			return null;
		}
		double num = inl.Average((Pt t) => t.C);
		double num2 = inl.Average((Pt t) => t.R);
		double num3 = inl.Average((Pt t) => t.X);
		double num4 = inl.Average((Pt t) => t.Y);
		double num5 = 0.0;
		double num6 = 0.0;
		foreach (Pt item in inl)
		{
			num5 += (item.C - num) * (item.X - num3) + (item.R - num2) * (item.Y - num4);
			num6 += (item.C - num) * (item.C - num) + (item.R - num2) * (item.R - num2);
		}
		if (num6 < 0.5)
		{
			return null;
		}
		double num7 = num5 / num6;
		if (num7 < 6.0 || num7 > 400.0)
		{
			return null;
		}
		MapGrid mapGrid = new MapGrid();
		mapGrid.P = num7;
		mapGrid.Ax = num3 - num7 * num;
		mapGrid.Ay = num4 - num7 * num2;
		mapGrid.Labels = count;
		return mapGrid;
	}

	public static MapGrid Fit(IEnumerable<OcrLine> words)
	{
		List<Pt> list = Points(words);
		if (list.Count < 3)
		{
			return null;
		}
		List<Pt> list2 = null;
		double num = double.MaxValue;
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				double num2 = list[j].C - list[i].C;
				double num3 = list[j].R - list[i].R;
				double num4 = num2 * num2 + num3 * num3;
				if (num4 < 0.9)
				{
					continue;
				}
				double num5 = ((list[j].X - list[i].X) * num2 + (list[j].Y - list[i].Y) * num3) / num4;
				if (!(num5 < 6.0) && !(num5 > 400.0))
				{
					double ax = (list[i].X - num5 * list[i].C + list[j].X - num5 * list[j].C) / 2.0;
					double ay = (list[i].Y - num5 * list[i].R + list[j].Y - num5 * list[j].R) / 2.0;
					double err;
					List<Pt> list3 = Inliers(list, ax, ay, num5, out err);
					if (list2 == null || list3.Count > list2.Count || (list3.Count == list2.Count && err < num))
					{
						list2 = list3;
						num = err;
					}
				}
			}
		}
		if (list2 == null || list2.Count < 3)
		{
			return null;
		}
		MapGrid mapGrid = null;
		for (int k = 0; k < 4; k++)
		{
			mapGrid = LeastSquares(list2);
			if (mapGrid == null)
			{
				return null;
			}
			double err2;
			List<Pt> list4 = Inliers(list, mapGrid.Ax, mapGrid.Ay, mapGrid.P, out err2);
			if (list4.Count < 3)
			{
				return null;
			}
			bool flag = list4.Count == list2.Count && list4.All(list2.Contains);
			list2 = list4;
			if (flag)
			{
				break;
			}
		}
		mapGrid = LeastSquares(list2);
		if (mapGrid == null)
		{
			return null;
		}
		double num6 = 0.0;
		foreach (Pt item in list2)
		{
			double num7 = item.X - (mapGrid.Ax + mapGrid.P * item.C);
			double num8 = item.Y - (mapGrid.Ay + mapGrid.P * item.R);
			num6 += num7 * num7 + num8 * num8;
		}
		mapGrid.Rms = Math.Sqrt(num6 / (double)(2 * list2.Count));
		mapGrid.Outliers = list.Count - list2.Count;
		return mapGrid;
	}

	public static Bitmap Prepare(Bitmap src, double scale, bool enhance)
	{
		Bitmap bitmap = (enhance ? Levels(src) : src);
		try
		{
			int width = Math.Max(1, (int)Math.Round((double)src.Width * scale));
			int height = Math.Max(1, (int)Math.Round((double)src.Height * scale));
			Bitmap bitmap2 = new Bitmap(width, height, PixelFormat.Format24bppRgb);
			using (Graphics graphics = Graphics.FromImage(bitmap2))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawImage(bitmap, new Rectangle(0, 0, width, height), new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);
			}
			return bitmap2;
		}
		finally
		{
			if (enhance)
			{
				bitmap.Dispose();
			}
		}
	}

	private static Bitmap Levels(Bitmap src)
	{
		Frame frame = Frame.From(src);
		for (int i = 0; i < frame.D.Length; i++)
		{
			int c = frame.D[i];
			int num = (Math.Min(Frame.R(c), Math.Min(Frame.G(c), Frame.B(c))) - 16) * 255 / 64;
			num = ((num >= 0) ? ((num > 255) ? 255 : num) : 0);
			frame.D[i] = (num << 16) | (num << 8) | num;
		}
		return frame.ToBitmap(new Rectangle(0, 0, frame.W, frame.H));
	}

	private static List<OcrLine> ReadWords(Bitmap src, double scale, bool enhance, bool latin)
	{
		List<OcrLine> list = new List<OcrLine>();
		using Bitmap bmp = Prepare(src, scale, enhance);
		foreach (OcrLine item in WinOcr.Recognize(bmp, words: true, latin))
		{
			list.Add(new OcrLine
			{
				Text = item.Text,
				Box = new RectangleF((float)((double)item.Box.X / scale), (float)((double)item.Box.Y / scale), (float)((double)item.Box.Width / scale), (float)((double)item.Box.Height / scale))
			});
		}
		return list;
	}

	public static MapGrid Read(Bitmap src, double scale, out int words)
	{
		List<OcrLine> list = ReadWords(src, Math.Max(1.0, Math.Min(2.0, 0.45 * scale)), enhance: true, latin: false);
		MapGrid mapGrid = Fit(list);
		if (mapGrid == null || mapGrid.Labels < 5)
		{
			list.AddRange(ReadWords(src, scale, enhance: false, latin: true));
			MapGrid mapGrid2 = Fit(list);
			if (mapGrid2 != null && (mapGrid == null || mapGrid2.Labels > mapGrid.Labels))
			{
				mapGrid = mapGrid2;
			}
		}
		words = list.Count;
		return mapGrid;
	}

	public static bool FindAttackButton(Frame f, Layout l, Point near, out Point center)
	{
		center = Point.Empty;
		int num = (int)Math.Round(280.0 * l.Sx);
		int num2 = (int)Math.Round(390.0 * l.Sx);
		int num3 = Math.Max(4, near.X - (int)(440.0 * l.Sx));
		int num4 = Math.Min(f.W - 5, near.X + (int)(440.0 * l.Sx));
		int num5 = Math.Max(4, near.Y - (int)(440.0 * l.Sy));
		int num6 = Math.Min(f.H - 5, near.Y + (int)(440.0 * l.Sy));
		for (int i = num5; i <= num6; i++)
		{
			int num7 = 0;
			for (int j = num3; j <= num4 + 1; j++)
			{
				if (j <= num4 && f.D[i * f.W + j] == 1842204)
				{
					num7++;
					continue;
				}
				if (num7 >= num && num7 <= num2)
				{
					int num8 = j - num7;
					int x = num8 + num7 / 2;
					if (f.At(x, i - 3) == 1052688 && f.At(num8 - 3, i + 2) == 1052688 && f.At(j + 2, i + 2) == 1052688)
					{
						int k;
						for (k = i; k + 1 < f.H && f.At(num8 + 2, k + 1) == 1842204; k++)
						{
						}
						int num9 = k - i + 1;
						if ((double)num9 >= 18.0 * l.Sy && (double)num9 <= 50.0 * l.Sy && f.At(x, k + 3) == 1052688)
						{
							center = new Point(x, (i + k) / 2);
							return true;
						}
					}
				}
				num7 = 0;
			}
		}
		return false;
	}

	public static int FindToggle(Frame f, Layout l, out Point center)
	{
		center = Point.Empty;
		if (!Screens.PillColor(Screens.On(f, l), out var pill))
		{
			return -1;
		}
		int pillLum = Frame.Lum(pill);
		bool flag = Frame.Near(pill, 394758, 4);
		if (flag)
		{
			pill = 394758;
		}
		int tol = (flag ? 1 : 10);
		int gapTol = (flag ? 1 : 12);
		Func<int, bool> func = (int c) => Frame.Near(c, pill, tol);
		Func<int, bool> func2 = (int c) => !Frame.Near(c, pill, gapTol);
		double num = Math.Min(l.Sx, l.Sy);
		double num2 = 18.7 * num;
		double num3 = l.Y(127.8);
		int num4 = (int)Math.Round(num3);
		int num5 = Math.Max(1, (int)Math.Round(l.X(420.0)));
		int num6 = Math.Min(f.W - 2, (int)Math.Round(l.X(960.0)));
		int num7 = (int)Math.Round(num3 - 0.71 * num2);
		int num8 = (int)Math.Round(num3 + 0.71 * num2);
		if (num6 - num5 < 10 || num7 < 1 || num8 >= f.H - 1)
		{
			return -1;
		}
		int num9 = 0;
		int num10 = (int)Math.Round(1.5 * num2);
		int num11 = -1;
		for (int i = num5; i <= num6; i++)
		{
			if (func(f.At(i, num7)) && func(f.At(i, num8)))
			{
				num9++;
				continue;
			}
			if (num9 >= num10)
			{
				num11 = i - 1;
			}
			num9 = 0;
		}
		if (num9 >= num10)
		{
			num11 = num6;
		}
		if (num11 < 0)
		{
			return -1;
		}
		double num12 = 0.0;
		int num13 = -1;
		int num14 = 0;
		int num15 = Math.Max(2, (int)Math.Round(0.3 * num2));
		for (int j = num11 + (int)Math.Round(0.6 * num2); j <= num11 + (int)Math.Round(1.7 * num2) && j + num15 < f.W; j++)
		{
			int num16 = 0;
			int num17 = 0;
			int num18 = 0;
			int num19 = 0;
			int num20 = 0;
			int num21 = 0;
			for (int k = num4 - num15; k <= num4 + num15; k++)
			{
				for (int m = j - num15; m <= j + num15; m++)
				{
					int num22 = f.At(m, k);
					num18++;
					if (flag ? Frame.Near(num22, 16776444, 40) : Contrast(num22, pillLum))
					{
						num16++;
						num19 += Frame.R(num22);
						num20 += Frame.G(num22);
						num21 += Frame.B(num22);
					}
					else if (func(num22))
					{
						num17++;
					}
				}
			}
			double num23 = (double)num16 / (double)num18;
			double num24 = (double)num17 / (double)num18;
			bool flag2 = flag || num16 == 0 || Bounded(f, j, num7, num8, (num19 / num16 << 16) | (num20 / num16 << 8) | (num21 / num16));
			if (num23 >= 0.45 && num23 > num12 && flag2)
			{
				num12 = num23;
				num13 = 1;
				num14 = j;
			}
			else if (num13 != 1 && num24 >= 0.85 && num24 > num12)
			{
				num12 = num24;
				num13 = 0;
				num14 = j;
			}
		}
		if (num13 < 0)
		{
			return -1;
		}
		int num25 = num11 + Math.Max(2, (int)Math.Round(0.25 * num2));
		if (num25 < num14 - num15 && !func2(f.At(num25, num4)) && num13 == 0)
		{
			return -1;
		}
		if (num13 == 0 && (!func2(f.At(num14, num7)) || !func2(f.At(num14, num8))))
		{
			return -1;
		}
		center = new Point(num14, num4);
		return num13;
	}

	public static ToggleLook Look(Frame f, Layout l, Point c)
	{
		if (!Screens.PillColor(Screens.On(f, l), out var color))
		{
			return null;
		}
		double num = 18.7 * Math.Min(l.Sx, l.Sy);
		int num2 = Math.Max(2, (int)Math.Round(0.3 * num));
		if (c.X - num2 < 0 || c.Y - num2 < 0 || c.X + num2 >= f.W || c.Y + num2 >= f.H)
		{
			return null;
		}
		long num3 = 0L;
		long num4 = 0L;
		long num5 = 0L;
		int num6 = 0;
		int num7 = 0;
		for (int i = c.Y - num2; i <= c.Y + num2; i++)
		{
			for (int j = c.X - num2; j <= c.X + num2; j++)
			{
				int c2 = f.At(j, i);
				num6++;
				num3 += Frame.R(c2);
				num4 += Frame.G(c2);
				num5 += Frame.B(c2);
				if (Screens.IsPill(c2, color))
				{
					num7++;
				}
			}
		}
		ToggleLook toggleLook = new ToggleLook();
		toggleLook.Mean = (int)((num3 / num6 << 16) | (num4 / num6 << 8) | (num5 / num6));
		toggleLook.Pill = color;
		toggleLook.PillShare = (double)num7 / (double)num6;
		return toggleLook;
	}

	private static bool Contrast(int c, int pillLum)
	{
		int num = Frame.Lum(c);
		int num2 = Math.Max(Frame.R(c), Math.Max(Frame.G(c), Frame.B(c))) - Math.Min(Frame.R(c), Math.Min(Frame.G(c), Frame.B(c)));
		if (num2 > 24 || Math.Abs(num - pillLum) < 96)
		{
			return false;
		}
		if (num < 216)
		{
			return num <= 16;
		}
		return true;
	}

	private static bool Bounded(Frame f, int cx, int yt, int yb, int fill)
	{
		if (!Frame.Near(f.At(cx, yt), fill, 3))
		{
			return !Frame.Near(f.At(cx, yb), fill, 3);
		}
		return false;
	}
}
