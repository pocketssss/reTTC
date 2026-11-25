using System;
using System.Collections.Generic;

namespace CaptBot;

internal static class PingOcr
{
	private class Gl
	{
		public int X0;

		public int W;

		public int H;

		public byte[,] P;
	}

	private class Tpl
	{
		public int D;

		public int W;

		public int H;

		public byte[,] P;
	}

	private const int Thr = 90;

	private const int MaxW = 8;

	private const double Accept = 38.0;

	public const int PW = 70;

	public const int PH = 64;

	private static readonly List<Tpl> T = new List<Tpl>();

	public static void Init()
	{
		string[] t = PingData.T;
		foreach (string text in t)
		{
			string[] array = text.Split(':');
			string[] array2 = array[1].Split('x');
			Tpl tpl = new Tpl();
			tpl.D = int.Parse(array[0]);
			tpl.W = int.Parse(array2[0]);
			tpl.H = int.Parse(array2[1]);
			tpl.P = new byte[tpl.W, tpl.H];
			for (int j = 0; j < tpl.H; j++)
			{
				for (int k = 0; k < tpl.W; k++)
				{
					tpl.P[k, j] = Convert.ToByte(array[2].Substring((j * tpl.W + k) * 2, 2), 16);
				}
			}
			T.Add(tpl);
		}
	}

	private static Gl Make(byte[,] L, int a, int e, int h)
	{
		int num = h;
		int num2 = -1;
		for (int i = a; i <= e; i++)
		{
			for (int j = 0; j < h; j++)
			{
				if (L[i, j] > 90)
				{
					if (j < num)
					{
						num = j;
					}
					if (j > num2)
					{
						num2 = j;
					}
				}
			}
		}
		if (num2 - num < 4)
		{
			return null;
		}
		Gl gl = new Gl();
		gl.X0 = a;
		gl.W = e - a + 1;
		gl.H = num2 - num + 1;
		gl.P = new byte[gl.W, gl.H];
		for (int k = 0; k < gl.W; k++)
		{
			for (int l = 0; l < gl.H; l++)
			{
				gl.P[k, l] = L[a + k, num + l];
			}
		}
		return gl;
	}

	private static double Cost(Gl g, Tpl t, int dx)
	{
		int num = Math.Max(g.W, t.W) + 1;
		int num2 = Math.Max(g.H, t.H);
		double num3 = 0.0;
		for (int i = -1; i < num; i++)
		{
			for (int j = 0; j < num2; j++)
			{
				double num4 = ((i + dx >= 0 && i + dx < g.W && j < g.H) ? g.P[i + dx, j] : 0);
				double num5 = ((i >= 0 && i < t.W && j < t.H) ? t.P[i, j] : 0);
				num3 += Math.Abs(num4 - num5);
			}
		}
		return num3 / (double)(num * num2);
	}

	public static bool ReadBand(Gray b, int y0, int h, out int value)
	{
		value = -1;
		int w = b.W;
		byte[,] array = new byte[w, h];
		int[] array2 = new int[w];
		long num = 0L;
		int num2 = 0;
		for (int i = 0; i < w; i++)
		{
			for (int j = 0; j < h; j++)
			{
				int num3 = b[i, Math.Min(b.H - 1, y0 + j)];
				array[i, j] = (byte)num3;
				if (num3 > 90)
				{
					array2[i] += num3;
					continue;
				}
				num += num3;
				num2++;
			}
		}
		if (num2 == 0 || num / num2 > 45)
		{
			return false;
		}
		List<Gl> list = new List<Gl>();
		int k = 0;
		while (k < w)
		{
			if (array2[k] == 0)
			{
				k++;
				continue;
			}
			int num4 = k;
			for (; k < w && array2[k] > 0; k++)
			{
			}
			int num5 = k - 1;
			List<int> list2 = new List<int>();
			int num6 = num4;
			while (num5 - num6 + 1 > 8)
			{
				int num7 = -1;
				int num8 = int.MaxValue;
				for (int l = num6 + 4; l <= Math.Min(num5 - 3, num6 + 8); l++)
				{
					if (array2[l] < num8)
					{
						num8 = array2[l];
						num7 = l;
					}
				}
				if (num7 < 0)
				{
					break;
				}
				list2.Add(num7);
				num6 = num7 + 1;
			}
			int a = num4;
			foreach (int item in list2)
			{
				Gl gl = Make(array, a, item, h);
				if (gl != null)
				{
					list.Add(gl);
				}
				a = item + 1;
			}
			Gl gl2 = Make(array, a, num5, h);
			if (gl2 != null)
			{
				list.Add(gl2);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		List<Gl> list3 = new List<Gl>();
		int num9 = -100;
		foreach (Gl item2 in list)
		{
			if (list3.Count <= 0 || item2.X0 - num9 <= 3)
			{
				list3.Add(item2);
				num9 = item2.X0 + item2.W - 1;
				continue;
			}
			break;
		}
		if (list3.Count < 1 || list3.Count > 3)
		{
			return false;
		}
		int num10 = 0;
		foreach (Gl item3 in list3)
		{
			int num11 = -1;
			double num12 = double.MaxValue;
			foreach (Tpl item4 in T)
			{
				if (Math.Abs(item4.W - item3.W) > 1 || Math.Abs(item4.H - item3.H) > 1)
				{
					continue;
				}
				for (int m = -1; m <= 1; m++)
				{
					double num13 = Cost(item3, item4, m);
					if (num13 < num12)
					{
						num12 = num13;
						num11 = item4.D;
					}
				}
			}
			if (num11 < 0 || num12 > 38.0)
			{
				return false;
			}
			num10 = num10 * 10 + num11;
		}
		value = num10;
		return true;
	}

	public static bool ReadPanel(Frame f, int x, int y, double scale, out int rtt, out int fps)
	{
		rtt = -1;
		fps = -1;
		Gray b = f.Gray(x, y, (int)Math.Round(70.0 * scale), (int)Math.Round(64.0 * scale)).Resize(70, 64);
		int value;
		bool flag = ReadBand(b, 42, 22, out value);
		if (ReadBand(b, 0, 21, out var value2) && value2 > 5 && value2 < 2000)
		{
			fps = value2;
		}
		if (flag && value > 0 && value < 2000)
		{
			rtt = value;
			return true;
		}
		return false;
	}
}
