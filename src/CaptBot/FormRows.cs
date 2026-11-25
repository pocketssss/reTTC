using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;

namespace CaptBot;

internal static class FormRows
{
	public class RowText
	{
		public string Name = "";

		public long Uid;
	}

	public const double Row0 = 415.0;

	public const double Step = 38.83;

	public const double Bottom = 925.0;

	public const double RightDx = 561.5;

	public const double NameX0 = 130.0;

	public const double NameX1 = 296.0;

	public const double UidX0 = 296.0;

	public const double UidX1 = 368.0;

	public static int MaxRows => (int)Math.Floor(13.134174607262427) + 1;

	public static double RowY(int k)
	{
		return 415.0 + (double)k * 38.83;
	}

	private static bool Ink(int c)
	{
		if (Frame.R(c) > 150 && Frame.G(c) > 150)
		{
			return Frame.B(c) > 150;
		}
		return false;
	}

	public static int Count(Frame f, Layout L, double dx)
	{
		int num = 0;
		for (int i = 0; i < MaxRows && HasName(f, L, dx, i); i++)
		{
			num++;
		}
		return num;
	}

	private static bool HasName(Frame f, Layout L, double dx, int k)
	{
		double num = RowY(k);
		for (double num2 = 130.0; num2 <= 250.0; num2 += 2.0)
		{
			for (int i = -6; i <= 6; i += 2)
			{
				if (Ink(f.At(L.X(num2 + dx), L.Y(num + (double)i))))
				{
					return true;
				}
			}
		}
		return false;
	}

	public static double NameWidth(Frame f, Layout L, double dx, int k)
	{
		double num = RowY(k);
		int num2 = (int)Math.Round(L.X(130.0 + dx));
		int num3 = (int)Math.Round(L.X(296.0 + dx));
		int num4 = (int)Math.Round(L.Y(num - 8.0));
		int num5 = (int)Math.Round(L.Y(num + 8.0));
		int num6 = -1;
		for (int i = num2; i <= num3; i++)
		{
			for (int j = num4; j <= num5; j++)
			{
				if (Ink(f.At(i, j)))
				{
					num6 = i;
					break;
				}
			}
		}
		if (num6 >= 0)
		{
			return (double)(num6 - num2) / L.Sx;
		}
		return -1.0;
	}

	public static int Shortest(Frame f, Layout L, double dx, int rows, double gap)
	{
		int num = -1;
		double num2 = double.MaxValue;
		double num3 = double.MaxValue;
		for (int i = 0; i < rows; i++)
		{
			double num4 = NameWidth(f, L, dx, i);
			if (!(num4 < 0.0))
			{
				if (num4 < num2)
				{
					num3 = num2;
					num2 = num4;
					num = i;
				}
				else if (num4 < num3)
				{
					num3 = num4;
				}
			}
		}
		if (num < 0 || !(num3 - num2 >= gap))
		{
			return -1;
		}
		return num;
	}

	public static int Online(Frame f, Layout L, double dx, int k)
	{
		double num = RowY(k);
		int num2 = 0;
		int num3 = 0;
		for (double num4 = 572.0; num4 <= 606.0; num4 += 1.0)
		{
			for (int i = -5; i <= 5; i++)
			{
				int c = f.At(L.X(num4 + dx), L.Y(num + (double)i));
				int num5 = Frame.R(c);
				int num6 = Frame.G(c);
				int num7 = Frame.B(c);
				if (num6 > 140 && num6 - num5 > 50 && num6 - num7 > 30)
				{
					num2++;
				}
				else if (num5 > 150 && num5 - num6 > 60 && num5 - num7 > 50)
				{
					num3++;
				}
			}
		}
		if (num2 >= 3 && num2 > num3)
		{
			return 1;
		}
		if (num3 >= 3)
		{
			return 0;
		}
		return -1;
	}

	public static List<RowText> Read(Bitmap client, Layout L, double dx, int rows)
	{
		if (rows <= 0 || !WinOcr.Available)
		{
			return null;
		}
		double num = 118.0 + dx;
		double num2 = 395.585;
		Rectangle srcRect = new Rectangle((int)Math.Round(L.X(num)), (int)Math.Round(L.Y(num2)), (int)Math.Round(250.0 * L.Sx), (int)Math.Round((double)rows * 38.83 * L.Sy));
		srcRect.Intersect(new Rectangle(0, 0, client.Width, client.Height));
		if (srcRect.Width < 20 || srcRect.Height < 10)
		{
			return null;
		}
		double num3 = Math.Max(2.0, 42.0 / (38.83 * L.Sy / 2.4));
		int width = (int)((double)srcRect.Width * num3);
		int height = (int)((double)srcRect.Height * num3);
		List<RowText> list = new List<RowText>();
		for (int i = 0; i < rows; i++)
		{
			list.Add(new RowText());
		}
		using Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
		using (Graphics graphics = Graphics.FromImage(bitmap))
		{
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
			graphics.DrawImage(client, new Rectangle(0, 0, width, height), srcRect, GraphicsUnit.Pixel);
		}
		Invert(bitmap);
		List<OcrLine> source = WinOcr.Recognize(bitmap, words: true, latin: true);
		foreach (OcrLine item in source.OrderBy((OcrLine o) => o.Box.X))
		{
			double num4 = num + (double)(item.Box.X + item.Box.Width / 2f) / num3 / L.Sx;
			double num5 = num2 + (double)(item.Box.Y + item.Box.Height / 2f) / num3 / L.Sy;
			int num6 = (int)Math.Floor((num5 - num2) / 38.83);
			if (num6 < 0 || num6 >= rows)
			{
				continue;
			}
			string text = (item.Text ?? "").Trim();
			if (text.Length == 0)
			{
				continue;
			}
			if (num4 >= 296.0 + dx)
			{
				string text2 = new string(text.Where(char.IsDigit).ToArray());
				if (text2.Length > 0 && text2.Length <= 9 && long.TryParse(text2, out var result))
				{
					list[num6].Uid = result;
				}
			}
			else
			{
				list[num6].Name = (list[num6].Name + " " + text).Trim();
			}
		}
		return list;
	}

	private static void Invert(Bitmap b)
	{
		Rectangle rect = new Rectangle(0, 0, b.Width, b.Height);
		BitmapData bitmapData = b.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
		try
		{
			byte[] array = new byte[bitmapData.Stride * b.Height];
			Marshal.Copy(bitmapData.Scan0, array, 0, array.Length);
			for (int i = 0; i < b.Height; i++)
			{
				for (int j = 0; j < b.Width; j++)
				{
					int num = i * bitmapData.Stride + j * 3;
					byte b2 = (byte)(255 - (array[num] + array[num + 1] + array[num + 2]) / 3);
					array[num] = (array[num + 1] = (array[num + 2] = b2));
				}
			}
			Marshal.Copy(array, 0, bitmapData.Scan0, array.Length);
		}
		finally
		{
			b.UnlockBits(bitmapData);
		}
	}

	public static List<int> ByFirstName(List<RowText> rows, string name)
	{
		List<int> list = new List<int>();
		string text = Key(name);
		if (rows == null || text.Length == 0)
		{
			return list;
		}
		for (int i = 0; i < rows.Count; i++)
		{
			string[] array = (rows[i].Name ?? "").Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length > 0 && Distance(Key(array[0]), text) <= ((text.Length >= 5) ? 1 : 0))
			{
				list.Add(i);
			}
		}
		return list;
	}

	public static string Key(string s)
	{
		return new string((s ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
	}

	public static int Distance(string a, string b)
	{
		int[] array = new int[b.Length + 1];
		int[] array2 = new int[b.Length + 1];
		for (int i = 0; i <= b.Length; i++)
		{
			array[i] = i;
		}
		for (int j = 1; j <= a.Length; j++)
		{
			array2[0] = j;
			for (int k = 1; k <= b.Length; k++)
			{
				array2[k] = Math.Min(Math.Min(array2[k - 1] + 1, array[k] + 1), array[k - 1] + ((a[j - 1] != b[k - 1]) ? 1 : 0));
			}
			int[] array3 = array;
			array = array2;
			array2 = array3;
		}
		return array[b.Length];
	}

	public static int Pick(List<RowText> rows, string name, long uid, out string how)
	{
		how = null;
		if (rows == null)
		{
			return -1;
		}
		if (uid > 0)
		{
			for (int i = 0; i < rows.Count; i++)
			{
				if (rows[i].Uid == uid)
				{
					how = "по static ID";
					return i;
				}
			}
		}
		string b = Key(name);
		int num = -1;
		int num2 = int.MaxValue;
		int num3 = int.MaxValue;
		for (int j = 0; j < rows.Count; j++)
		{
			string text = Key(rows[j].Name);
			if (text.Length != 0)
			{
				int num4 = Distance(text, b);
				if (num4 < num2)
				{
					num3 = num2;
					num2 = num4;
					num = j;
				}
				else if (num4 < num3)
				{
					num3 = num4;
				}
			}
		}
		if (num >= 0 && num2 <= 1 && num3 > num2)
		{
			how = ((num2 == 0) ? "по имени" : "по имени (распознано почти точно)");
			return num;
		}
		return -1;
	}
}
