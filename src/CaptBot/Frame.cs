using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CaptBot;

internal class Frame
{
	public readonly int W;

	public readonly int H;

	public readonly int[] D;

	public Frame(int w, int h)
	{
		W = Math.Max(1, w);
		H = Math.Max(1, h);
		D = new int[W * H];
	}

	public static Frame From(Bitmap b)
	{
		Frame frame = new Frame(b.Width, b.Height);
		BitmapData bitmapData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
		try
		{
			int stride = bitmapData.Stride;
			byte[] array = new byte[Math.Abs(stride)];
			for (int i = 0; i < b.Height; i++)
			{
				Marshal.Copy(new IntPtr(bitmapData.Scan0.ToInt64() + (long)i * (long)stride), array, 0, array.Length);
				int num = i * frame.W;
				for (int j = 0; j < b.Width; j++)
				{
					frame.D[num + j] = (array[j * 3 + 2] << 16) | (array[j * 3 + 1] << 8) | array[j * 3];
				}
			}
			return frame;
		}
		finally
		{
			b.UnlockBits(bitmapData);
		}
	}

	public int At(int x, int y)
	{
		if (x < 0)
		{
			x = 0;
		}
		else if (x >= W)
		{
			x = W - 1;
		}
		if (y < 0)
		{
			y = 0;
		}
		else if (y >= H)
		{
			y = H - 1;
		}
		return D[y * W + x];
	}

	public int At(double x, double y)
	{
		return At((int)Math.Round(x), (int)Math.Round(y));
	}

	public Color ColorAt(double x, double y)
	{
		return Color.FromArgb(At(x, y) | -16777216);
	}

	public static int R(int c)
	{
		return (c >> 16) & 0xFF;
	}

	public static int G(int c)
	{
		return (c >> 8) & 0xFF;
	}

	public static int B(int c)
	{
		return c & 0xFF;
	}

	public static int Lum(int c)
	{
		return (R(c) + G(c) + B(c)) / 3;
	}

	public static bool Near(int c, int rgb, int tol)
	{
		if (Math.Abs(R(c) - R(rgb)) <= tol && Math.Abs(G(c) - G(rgb)) <= tol)
		{
			return Math.Abs(B(c) - B(rgb)) <= tol;
		}
		return false;
	}

	public Gray Gray(int x0, int y0, int w, int h)
	{
		Gray gray = new Gray(w, h);
		for (int i = 0; i < gray.H; i++)
		{
			for (int j = 0; j < gray.W; j++)
			{
				gray.L[i * gray.W + j] = (byte)Lum(At(x0 + j, y0 + i));
			}
		}
		return gray;
	}

	public Gray Gray(Rectangle r)
	{
		return Gray(r.X, r.Y, r.Width, r.Height);
	}

	public Bitmap ToBitmap(Rectangle r)
	{
		r.Width = Math.Max(1, r.Width);
		r.Height = Math.Max(1, r.Height);
		Bitmap bitmap = new Bitmap(r.Width, r.Height, PixelFormat.Format24bppRgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, r.Width, r.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
		try
		{
			byte[] array = new byte[Math.Abs(bitmapData.Stride)];
			for (int i = 0; i < r.Height; i++)
			{
				for (int j = 0; j < r.Width; j++)
				{
					int c = At(r.X + j, r.Y + i);
					array[j * 3] = (byte)B(c);
					array[j * 3 + 1] = (byte)G(c);
					array[j * 3 + 2] = (byte)R(c);
				}
				Marshal.Copy(array, 0, new IntPtr(bitmapData.Scan0.ToInt64() + (long)i * (long)bitmapData.Stride), array.Length);
			}
		}
		finally
		{
			bitmap.UnlockBits(bitmapData);
		}
		return bitmap;
	}
}
