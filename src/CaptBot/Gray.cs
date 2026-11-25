using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace CaptBot;

internal class Gray
{
	public readonly int W;

	public readonly int H;

	public readonly byte[] L;

	public byte this[int x, int y] => L[y * W + x];

	public Gray(int w, int h)
	{
		W = Math.Max(1, w);
		H = Math.Max(1, h);
		L = new byte[W * H];
	}

	public static Gray From(Bitmap b)
	{
		Frame frame = Frame.From(b);
		return frame.Gray(0, 0, frame.W, frame.H);
	}

	public Gray Resize(int w, int h)
	{
		if (w == W && h == H)
		{
			return this;
		}
		float[] array = new float[w * H];
		float[] array2 = new float[W];
		for (int i = 0; i < H; i++)
		{
			for (int j = 0; j < W; j++)
			{
				array2[j] = (int)L[i * W + j];
			}
			float[] sourceArray = Res1(array2, W, w);
			Array.Copy(sourceArray, 0, array, i * w, w);
		}
		Gray gray = new Gray(w, h);
		float[] array3 = new float[H];
		for (int k = 0; k < w; k++)
		{
			for (int l = 0; l < H; l++)
			{
				array3[l] = array[l * w + k];
			}
			float[] sourceArray = Res1(array3, H, h);
			for (int m = 0; m < h; m++)
			{
				gray.L[m * w + k] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(sourceArray[m])));
			}
		}
		return gray;
	}

	private static float[] Res1(float[] s, int n, int m)
	{
		float[] array = new float[m];
		double num = (double)n / (double)m;
		if (num >= 1.0)
		{
			for (int i = 0; i < m; i++)
			{
				double num2 = (double)i * num;
				double num3 = (double)(i + 1) * num;
				double num4 = 0.0;
				for (int j = (int)num2; (double)j < num3 && j < n; j++)
				{
					num4 += (double)s[j] * (Math.Min(num3, j + 1) - Math.Max(num2, j));
				}
				array[i] = (float)(num4 / num);
			}
		}
		else
		{
			for (int k = 0; k < m; k++)
			{
				double num5 = ((double)k + 0.5) * num - 0.5;
				if (num5 < 0.0)
				{
					num5 = 0.0;
				}
				if (num5 > (double)(n - 1))
				{
					num5 = n - 1;
				}
				int num6 = (int)num5;
				double num7 = num5 - (double)num6;
				array[k] = (float)((double)s[num6] * (1.0 - num7) + (double)s[Math.Min(n - 1, num6 + 1)] * num7);
			}
		}
		return array;
	}

	public Bitmap ToBitmap()
	{
		Bitmap bitmap = new Bitmap(W, H, PixelFormat.Format24bppRgb);
		BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, W, H), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
		try
		{
			byte[] array = new byte[Math.Abs(bitmapData.Stride)];
			for (int i = 0; i < H; i++)
			{
				for (int j = 0; j < W; j++)
				{
					array[j * 3 + 2] = (array[j * 3 + 1] = (array[j * 3] = L[i * W + j]));
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

	public static double Diff(Gray a, Gray t, int x0, int x1, int y0, int y1)
	{
		long num = 0L;
		int num2 = 0;
		x1 = Math.Min(x1, Math.Min(a.W, t.W));
		y1 = Math.Min(y1, Math.Min(a.H, t.H));
		for (int i = y0; i < y1; i++)
		{
			for (int j = x0; j < x1; j++)
			{
				num += Math.Abs(a.L[i * a.W + j] - t.L[i * t.W + j]);
				num2++;
			}
		}
		if (num2 != 0)
		{
			return (double)num / (double)num2;
		}
		return double.MaxValue;
	}
}
