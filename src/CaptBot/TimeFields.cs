using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace CaptBot;

internal class TimeFields
{
	public const double HoursX = 192.0;

	public const double MinsX = 392.5;

	public const double CellY = 218.5;

	public const double LeftX = 77.5;

	public const double CellW = 48.5;

	public const double CellH = 37.0;

	public Rectangle Hours;

	public Rectangle Mins;

	public double Left = -1.0;

	private static bool Cell(int c)
	{
		return Frame.Near(c, 855309, 3);
	}

	private static bool Line(int c)
	{
		int num = Math.Max(Frame.R(c), Math.Max(Frame.G(c), Frame.B(c)));
		int num2 = Math.Min(Frame.R(c), Math.Min(Frame.G(c), Frame.B(c)));
		if (num2 >= 32 && num <= 64)
		{
			return num - num2 <= 12;
		}
		return false;
	}

	private static bool Edge(Frame f, int x, int y, int dx)
	{
		if (!Line(f.At(x, y)) && !Line(f.At(x + dx, y)))
		{
			return Line(f.At(x + 2 * dx, y));
		}
		return true;
	}

	public static TimeFields Find(Frame f, Layout l)
	{
		int num = Math.Max(1, (int)Math.Round(l.X(90.0)));
		int num2 = Math.Min(f.W - 2, (int)Math.Round(l.X(640.0)));
		int num3 = Math.Max(0, (int)Math.Round(l.Y(186.0)));
		int num4 = Math.Min(f.H - 1, (int)Math.Round(l.Y(250.0)));
		double num5 = 48.5 * l.Sx;
		double num6 = 37.0 * l.Sy;
		int num7 = (int)Math.Floor(0.85 * num5 - 1.0);
		int num8 = (int)Math.Ceiling(1.5 * num5 + 1.0);
		List<int[]> list = new List<int[]>();
		for (int k = num3; k <= num4; k++)
		{
			int num9 = -1;
			int num10 = 0;
			int num11 = 0;
			for (int m = num; m <= num2; m++)
			{
				if (Cell(f.At(m, k)))
				{
					num11++;
					continue;
				}
				int num12 = m - num11;
				int num13 = num11;
				num11 = 0;
				if (num13 < num7 || num13 > num8 || !Edge(f, num12 - 1, k, -1) || !Edge(f, m, k, 1))
				{
					continue;
				}
				if (num9 < 0)
				{
					num9 = num12;
					num10 = num13;
					continue;
				}
				int num14 = num12 - num9;
				if ((double)num14 >= 150.0 * l.Sx && (double)num14 <= 330.0 * l.Sx)
				{
					list.Add(new int[5] { num9, num10, num12, num13, k });
				}
				break;
			}
		}
		if (list.Count < 4)
		{
			return null;
		}
		int[] c = new int[4];
		int j;
		for (j = 0; j < 4; j++)
		{
			c[j] = (from q in list
				select q[j] into q
				orderby q
				select q).ElementAt(list.Count / 2);
		}
		List<int[]> list2 = list.Where((int[] q) => Enumerable.Range(0, 4).All((int i) => Math.Abs(q[i] - c[i]) <= 1)).ToList();
		if (list2.Count < 4 || (double)list2.Count < (double)list.Count * 0.6)
		{
			return null;
		}
		int num15 = list2.Min((int[] q) => q[4]);
		int num16 = list2.Max((int[] q) => q[4]) + 1;
		if ((double)(num16 - num15) < 0.75 * num6 - 1.0 || (double)(num16 - num15) > 1.35 * num6 + 1.0)
		{
			return null;
		}
		TimeFields timeFields = new TimeFields();
		timeFields.Hours = new Rectangle(c[0], num15, c[1], num16 - num15);
		timeFields.Mins = new Rectangle(c[2], num15, c[3], num16 - num15);
		TimeFields timeFields2 = timeFields;
		int y = (num15 + num16) / 2;
		int num17 = Math.Max(1, (int)Math.Round(l.X(60.0)));
		int num18 = Math.Min(f.W - 3, (int)Math.Round(l.X(100.0)));
		for (int n = num17; n <= num18; n++)
		{
			if (Line(f.At(n, y)) && Frame.Near(f.At(n - 1, y), 1710618, 4))
			{
				int num19;
				for (num19 = n; num19 + 1 < num18 && Line(f.At(num19 + 1, y)); num19++)
				{
				}
				if (num19 - n <= 3 && Frame.Near(f.At(num19 + 1, y), 1250067, 4))
				{
					timeFields2.Left = (double)(n + num19 + 1) / 2.0;
					break;
				}
			}
		}
		return timeFields2;
	}

	public static PointF Off(Rectangle cell, Layout l, double bx)
	{
		return new PointF((float)((double)cell.X + (double)cell.Width / 2.0 - l.X(bx)), (float)((double)cell.Y + (double)cell.Height / 2.0 - l.Y(218.5)));
	}
}
