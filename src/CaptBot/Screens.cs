using System;
using System.Collections.Generic;

namespace CaptBot;

internal static class Screens
{
	private const int PillTol = 10;

	private const int GapDiff = 12;

	private static readonly double[,] Bezel = new double[9, 2]
	{
		{ 640.0, 63.0 },
		{ 300.0, 68.0 },
		{ 980.0, 68.0 },
		{ 36.0, 500.0 },
		{ 42.0, 300.0 },
		{ 42.0, 860.0 },
		{ 1245.0, 300.0 },
		{ 1245.0, 700.0 },
		{ 640.0, 958.0 }
	};

	private static readonly double[,] FormHeader = new double[10, 2]
	{
		{ 88.0, 376.0 },
		{ 255.0, 376.0 },
		{ 360.0, 376.0 },
		{ 455.0, 376.0 },
		{ 600.0, 376.0 },
		{ 655.0, 376.0 },
		{ 830.0, 376.0 },
		{ 925.0, 376.0 },
		{ 1015.0, 376.0 },
		{ 1170.0, 376.0 }
	};

	private static readonly double[,] FormSearch = new double[4, 2]
	{
		{ 300.0, 338.0 },
		{ 500.0, 338.0 },
		{ 900.0, 338.0 },
		{ 1100.0, 338.0 }
	};

	private static readonly double[,] FormTime = new double[3, 2]
	{
		{ 110.0, 206.0 },
		{ 160.0, 218.0 },
		{ 300.0, 206.0 }
	};

	private static readonly double[,] FormBg = new double[3, 2]
	{
		{ 200.0, 190.0 },
		{ 250.0, 218.0 },
		{ 640.0, 240.0 }
	};

	private static readonly double[,] MapTop = new double[4, 2]
	{
		{ 100.0, 100.0 },
		{ 300.0, 100.0 },
		{ 640.0, 100.0 },
		{ 900.0, 100.0 }
	};

	private static readonly double[,] MapPills = new double[4, 2]
	{
		{ 86.0, 122.0 },
		{ 104.0, 138.0 },
		{ 128.0, 122.0 },
		{ 128.0, 136.0 }
	};

	public static bool TabletOpen(Func<double, double, int> s)
	{
		int num = 0;
		for (int i = 0; i < Bezel.GetLength(0); i++)
		{
			if (Frame.Near(s(Bezel[i, 0], Bezel[i, 1]), 1184274, 6))
			{
				num++;
			}
		}
		return num >= 6;
	}

	private static bool DarkPill(int pill)
	{
		return Frame.Near(pill, 394758, 4);
	}

	private static int Hits(Func<double, double, int> s, double[,] pts, int rgb, int tol)
	{
		int num = 0;
		for (int i = 0; i < pts.GetLength(0); i++)
		{
			if (Frame.Near(s(pts[i, 0], pts[i, 1]), rgb, tol))
			{
				num++;
			}
		}
		return num;
	}

	public static bool IsForm(Func<double, double, int> s)
	{
		if (Hits(s, FormHeader, 2171169, 4) >= 7 && Hits(s, FormSearch, 921102, 4) >= 3 && Hits(s, FormTime, 1250067, 3) >= 2)
		{
			return Hits(s, FormBg, 1710618, 3) >= 2;
		}
		return false;
	}

	public static bool PillColor(Func<double, double, int> s, out int color)
	{
		color = 0;
		int length = MapPills.GetLength(0);
		int[] array = new int[length];
		for (int i = 0; i < length; i++)
		{
			array[i] = s(MapPills[i, 0], MapPills[i, 1]);
		}
		for (int j = 0; j < length; j++)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			for (int k = 0; k < length; k++)
			{
				if (Frame.Near(array[k], array[j], 10))
				{
					num++;
					num2 += Frame.R(array[k]);
					num3 += Frame.G(array[k]);
					num4 += Frame.B(array[k]);
				}
			}
			if (num >= 3)
			{
				color = (num2 / num << 16) | (num3 / num << 8) | (num4 / num);
				return true;
			}
		}
		return false;
	}

	public static bool IsPill(int c, int pill)
	{
		return Frame.Near(c, pill, DarkPill(pill) ? 4 : 10);
	}

	public static bool NotPill(int c, int pill)
	{
		return !Frame.Near(c, pill, DarkPill(pill) ? 3 : 12);
	}

	public static bool IsMap(Func<double, double, int> s)
	{
		if (!PillColor(s, out var color))
		{
			return false;
		}
		if (LeftGap(s, color))
		{
			return true;
		}
		if (RightPills(s, color))
		{
			return true;
		}
		if (DarkPill(color))
		{
			return Hits(s, MapTop, 1447446, 2) >= 3;
		}
		return false;
	}

	private static bool LeftGap(Func<double, double, int> s, int pill)
	{
		int num = 0;
		int num2 = 0;
		double[] array = new double[3] { 120.0, 128.0, 136.0 };
		foreach (double arg in array)
		{
			double[] array2 = new double[4] { 115.0, 116.5, 118.0, 119.5 };
			foreach (double arg2 in array2)
			{
				num++;
				if (NotPill(s(arg2, arg), pill))
				{
					num2++;
				}
			}
		}
		return num2 >= num - 2;
	}

	private static bool RightPills(Func<double, double, int> s, int pill)
	{
		List<double[]> list = new List<double[]>();
		double num = 1166.0;
		bool flag = IsPill(s(1166.0, 116.5), pill);
		for (double num2 = 1165.5; num2 >= 1000.0; num2 -= 0.5)
		{
			bool flag2 = IsPill(s(num2, 116.5), pill);
			if (flag2 != flag)
			{
				list.Add(new double[3]
				{
					num2 + 0.5,
					num,
					flag ? 1.0 : 0.0
				});
				num = num2;
				flag = flag2;
			}
		}
		list.Add(new double[3]
		{
			1000.0,
			num,
			flag ? 1.0 : 0.0
		});
		int num3 = 1;
		while (num3 + 1 < list.Count)
		{
			if (list[num3][1] - list[num3][0] < 1.5 && list[num3 - 1][2] == list[num3 + 1][2])
			{
				list[num3 - 1][0] = list[num3 + 1][0];
				list.RemoveRange(num3, 2);
			}
			else
			{
				num3++;
			}
		}
		for (int i = 0; i + 2 < list.Count; i++)
		{
			double[] array = list[i];
			double[] array2 = list[i + 1];
			double[] array3 = list[i + 2];
			if (array[2] == 1.0 && array2[2] == 0.0 && array3[2] == 1.0 && !(array[1] < 1150.0) && !(array[1] - array[0] < 20.0))
			{
				double num4 = array2[1] - array2[0];
				double num5 = array3[1] - array3[0];
				if (!(num4 < 3.0) && !(num4 > 16.0) && !(num5 < 22.0) && !(num5 > 46.0))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool LogoNear(Func<double, double, int> s, double bx, double by)
	{
		for (int i = -6; i <= 6; i += 2)
		{
			for (int j = -6; j <= 6; j += 2)
			{
				int c = s(bx + (double)j, by + (double)i);
				if (Math.Max(Frame.R(c), Math.Max(Frame.G(c), Frame.B(c))) > 144)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static bool MenuBackground(Func<double, double, int> s)
	{
		if (Frame.Near(s(640.0, 700.0), 394758, 6))
		{
			return Frame.Near(s(342.0, 150.0), 394758, 6);
		}
		return false;
	}

	public static Scr Detect(Func<double, double, int> s, out int score)
	{
		score = 0;
		if (!TabletOpen(s))
		{
			return Scr.Closed;
		}
		score = 2;
		int c = s(1081.0, 500.0);
		if (Frame.R(c) < 48 && Frame.G(c) > 112 && Frame.B(c) > 96)
		{
			score++;
			return Scr.Home;
		}
		if (IsForm(s))
		{
			score += 4;
			return Scr.Form;
		}
		if (IsMap(s))
		{
			score += 3;
			return Scr.Map;
		}
		int c2 = s(1198.0, 123.0);
		if (MenuBackground(s) && LogoNear(s, 1198.0, 123.0))
		{
			score += 3;
			return Scr.SkyeMenu;
		}
		if (Frame.Near(c2, 3289651, 10) && Frame.Near(s(130.0, 376.0), 2171169, 10) && Frame.Near(s(160.0, 218.0), 1250067, 10))
		{
			score += 3;
			return Scr.Form;
		}
		if (Frame.Near(s(110.0, 127.0), 394758, 10) && Frame.Near(c2, 394758, 10) && !Frame.Near(s(342.0, 150.0), 394758, 6))
		{
			score += 2;
			return Scr.Map;
		}
		return Scr.Unknown;
	}

	public static Scr Detect(Func<double, double, int> s)
	{
		int score;
		return Detect(s, out score);
	}

	public static Func<double, double, int> On(Frame f, Layout l)
	{
		return (double x, double y) => f.At(l.X(x), l.Y(y));
	}
}
