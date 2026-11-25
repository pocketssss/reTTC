using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace CaptBot;

internal static class Ocr
{
	public const int BW = 56;

	public const int BH = 28;

	public const int X0 = 5;

	public const int X1 = 51;

	public const int Y0 = 3;

	public const int Y1 = 25;

	private const double Accept = 1.5;

	private const double AcceptScaled = 9.0;

	private const double Margin = 0.7;

	public const double Changed = 0.5;

	private static readonly Gray[] Hours = new Gray[24];

	private static readonly Gray[] Mins = new Gray[12];

	private static Gray FromPng(string b64)
	{
		using MemoryStream stream = new MemoryStream(Convert.FromBase64String(b64));
		using Bitmap b65 = new Bitmap(stream);
		return Gray.From(b65).Resize(56, 28);
	}

	public static void Init()
	{
		for (int i = 0; i < 24; i++)
		{
			Hours[i] = FromPng(DigitData.HourPng[i]);
		}
		for (int j = 0; j < 12; j++)
		{
			Mins[j] = FromPng(DigitData.MinPng[j]);
		}
	}

	public static int Count(bool hours)
	{
		if (!hours)
		{
			return 12;
		}
		return 24;
	}

	public static int Value(bool hours, int idx)
	{
		if (!hours)
		{
			return idx * 5;
		}
		return idx;
	}

	public static int Index(bool hours, int value)
	{
		if (!hours)
		{
			return value / 5;
		}
		return value;
	}

	public static Gray Template(bool hours, int idx)
	{
		return (hours ? Hours : Mins)[idx];
	}

	public static double Cost56(Gray a56, bool hours, int idx)
	{
		return Gray.Diff(a56, Template(hours, idx), 5, 51, 3, 25);
	}

	public static int Best(Gray field, bool hours, out double cost, out double second)
	{
		Gray a = field.Resize(56, 28);
		int result = -1;
		cost = double.MaxValue;
		second = double.MaxValue;
		for (int i = 0; i < Count(hours); i++)
		{
			double num = Cost56(a, hours, i);
			if (num < cost)
			{
				second = cost;
				cost = num;
				result = i;
			}
			else if (num < second)
			{
				second = num;
			}
		}
		return result;
	}

	public static double Cost56Loose(Gray a56, bool hours, int idx)
	{
		Gray gray = Template(hours, idx);
		double num = double.MaxValue;
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				long num2 = 0L;
				int num3 = 0;
				for (int k = 3; k < 25; k++)
				{
					for (int l = 5; l < 51; l++)
					{
						num2 += Math.Abs(a56.L[(k + i) * 56 + l + j] - gray.L[k * 56 + l]);
						num3++;
					}
				}
				num = Math.Min(num, (double)num2 / (double)num3);
			}
		}
		return num;
	}

	public static int FitCycle(List<Gray> crops, bool hours, out double best, out double second)
	{
		int num = Count(hours);
		best = double.MaxValue;
		second = double.MaxValue;
		if (crops.Count != num)
		{
			return -1;
		}
		double[,] array = new double[num, num];
		for (int i = 0; i < num; i++)
		{
			Gray a = crops[i].Resize(56, 28);
			double num2 = 0.0;
			double num3 = 0.0;
			for (int j = 0; j < num; j++)
			{
				double num4 = (array[i, j] = Cost56Loose(a, hours, j));
				num2 += num4;
				num3 += num4 * num4;
			}
			double num5 = num2 / (double)num;
			double num6 = Math.Sqrt(Math.Max(1E-09, num3 / (double)num - num5 * num5));
			for (int k = 0; k < num; k++)
			{
				array[i, k] = (array[i, k] - num5) / num6;
			}
		}
		int result = -1;
		for (int l = 0; l < num; l++)
		{
			double num7 = 0.0;
			for (int m = 0; m < num; m++)
			{
				num7 += array[m, (l + m) % num];
			}
			num7 /= (double)num;
			if (num7 < best)
			{
				second = best;
				best = num7;
				result = l;
			}
			else if (num7 < second)
			{
				second = num7;
			}
		}
		if (!(best <= -1.0) || !(second - best >= 0.4))
		{
			return -1;
		}
		return result;
	}

	public static double FieldDiff(Gray a, Gray b)
	{
		int x = (int)Math.Round((double)(5 * a.W) / 56.0);
		int x2 = (int)Math.Round((double)(51 * a.W) / 56.0);
		int y = (int)Math.Round((double)(3 * a.H) / 28.0);
		int y2 = (int)Math.Round((double)(25 * a.H) / 28.0);
		return Gray.Diff(a, b, x, x2, y, y2);
	}

	private static Gray Ink(Gray c)
	{
		int num = (int)Math.Round((double)(6 * c.W) / 56.0);
		int num2 = (int)Math.Round((double)(50 * c.W) / 56.0);
		int num3 = (int)Math.Round((double)(4 * c.H) / 28.0);
		int num4 = (int)Math.Round((double)(24 * c.H) / 28.0);
		int num5 = num2;
		int num6 = -1;
		int num7 = num4;
		int num8 = -1;
		for (int i = num3; i < num4; i++)
		{
			for (int j = num; j < num2; j++)
			{
				if (c[j, i] > 70)
				{
					num5 = Math.Min(num5, j);
					num6 = Math.Max(num6, j);
					num7 = Math.Min(num7, i);
					num8 = Math.Max(num8, i);
				}
			}
		}
		if (num6 < 0 || num8 - num7 < 3)
		{
			return null;
		}
		num5 = Math.Max(0, num5 - 1);
		num7 = Math.Max(0, num7 - 1);
		num6 = Math.Min(c.W - 1, num6 + 1);
		num8 = Math.Min(c.H - 1, num8 + 1);
		Gray gray = new Gray(num6 - num5 + 1, num8 - num7 + 1);
		for (int k = 0; k < gray.H; k++)
		{
			for (int l = 0; l < gray.W; l++)
			{
				gray.L[k * gray.W + l] = (byte)(255 - Math.Max(0, Math.Min(255, (c[num5 + l, num7 + k] - 13) * 255 / 235)));
			}
		}
		return gray;
	}

	public static string Digits(string t)
	{
		StringBuilder stringBuilder = new StringBuilder();
		string text = t ?? "";
		foreach (char c in text)
		{
			int num = "OoОоQDЗзlI|iSsБбBВg".IndexOf(c);
			stringBuilder.Append((num >= 0) ? "0000003311115566889"[num] : c);
		}
		return stringBuilder.ToString();
	}

	public static int TextCycle(List<Gray> crops, bool hours, out int votes, out int rival)
	{
		int num = Count(hours);
		votes = 0;
		rival = 0;
		if (crops.Count != num || !WinOcr.Available)
		{
			return -1;
		}
		int[] array = new int[num];
		Gray[] array2 = new Gray[num];
		int num2 = 0;
		int num3 = 112;
		for (int i = 0; i < num; i++)
		{
			array2[i] = Ink(crops[i]);
			if (array2[i] != null)
			{
				num2 = Math.Max(num2, array2[i].W * 56 / array2[i].H);
			}
		}
		if (num2 == 0)
		{
			return -1;
		}
		num2 += 70;
		using (Bitmap bitmap = new Bitmap(12 * num2 + 80, num / 12 * num3 + 60, PixelFormat.Format24bppRgb))
		{
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.Clear(Color.White);
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				for (int j = 0; j < num; j++)
				{
					if (array2[j] != null)
					{
						int num4 = array2[j].W * 56 / array2[j].H;
						using Bitmap bitmap2 = array2[j].ToBitmap();
						graphics.DrawImage(bitmap2, new Rectangle(40 + j % 12 * num2 + (num2 - num4) / 2, 30 + j / 12 * num3 + (num3 - 56) / 2, num4, 56), new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), GraphicsUnit.Pixel);
					}
				}
			}
			foreach (OcrLine item in WinOcr.Recognize(bitmap, words: true, latin: false))
			{
				int num5 = (int)Math.Floor((item.Box.X + item.Box.Width / 2f - 40f) / (float)num2);
				int num6 = (int)Math.Floor((item.Box.Y + item.Box.Height / 2f - 30f) / (float)num3);
				int num7 = num6 * 12 + num5;
				if (num5 < 0 || num5 >= 12 || num7 < 0 || num7 >= num || item.Box.Width > (float)num2)
				{
					continue;
				}
				string text = Digits(item.Text);
				if (text.Length == 0 || text.Length > 2 || !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var result))
				{
					continue;
				}
				bool num8;
				if (!hours)
				{
					if (result > 55)
					{
						continue;
					}
					num8 = result % 5 != 0;
				}
				else
				{
					num8 = result > 23;
				}
				if (!num8)
				{
					array[((Index(hours, result) - num7) % num + num) % num]++;
				}
			}
		}
		int result2 = -1;
		for (int k = 0; k < num; k++)
		{
			if (array[k] > votes)
			{
				rival = votes;
				votes = array[k];
				result2 = k;
			}
			else if (array[k] > rival)
			{
				rival = array[k];
			}
		}
		if (votes < (hours ? 6 : 4) || votes < 2 * rival + 1)
		{
			return -1;
		}
		return result2;
	}

	public static int Read(Gray field, bool hours, FieldMemory mem, out double cost)
	{
		if (mem != null)
		{
			int num = mem.Match(field, hours, out cost);
			if (num >= 0)
			{
				return num;
			}
			if (!hours && (mem.Full(hours) || field.W != 56 || field.H != 28))
			{
				return -1;
			}
		}
		double second;
		int num2 = Best(field, hours, out cost, out second);
		if (num2 < 0)
		{
			return -1;
		}
		if (field.W == 56 && field.H == 28)
		{
			if (!(cost <= 1.5))
			{
				return -1;
			}
			return Value(hours, num2);
		}
		if (cost > 9.0 || cost >= second * 0.7)
		{
			return -1;
		}
		if (mem != null && hours && cost <= 6.3 && cost < second * 0.5)
		{
			mem.Learn(field, hours, num2);
		}
		return Value(hours, num2);
	}
}
