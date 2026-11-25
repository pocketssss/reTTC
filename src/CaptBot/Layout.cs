using System;
using System.Drawing;
using System.Globalization;

namespace CaptBot;

public class Layout
{
	public const int BaseW = 1280;

	public const int BaseH = 1024;

	public double Sx = 1.0;

	public double Sy = 1.0;

	public double Ox;

	public double Oy;

	public string Model = "эталон 1280×1024";

	public bool Identity
	{
		get
		{
			if (Math.Abs(Sx - 1.0) < 1E-06 && Math.Abs(Sy - 1.0) < 1E-06 && Math.Abs(Ox) < 0.01)
			{
				return Math.Abs(Oy) < 0.01;
			}
			return false;
		}
	}

	public Layout()
	{
	}

	public Layout(double sx, double sy, double ox, double oy, string model)
	{
		Sx = sx;
		Sy = sy;
		Ox = ox;
		Oy = oy;
		Model = model;
	}

	public double X(double bx)
	{
		return Ox + bx * Sx;
	}

	public double Y(double by)
	{
		return Oy + by * Sy;
	}

	public double BX(double x)
	{
		return (x - Ox) / Sx;
	}

	public double BY(double y)
	{
		return (y - Oy) / Sy;
	}

	public double Distance(Layout o)
	{
		double num = 0.0;
		double[] array = new double[2] { 0.0, 1280.0 };
		foreach (double bx in array)
		{
			double[] array2 = new double[2] { 0.0, 1024.0 };
			foreach (double by in array2)
			{
				num = Math.Max(num, Math.Max(Math.Abs(X(bx) - o.X(bx)), Math.Abs(Y(by) - o.Y(by))));
			}
		}
		return num;
	}

	public static Layout Stretch(int w, int h)
	{
		return new Layout((double)w / 1280.0, (double)h / 1024.0, 0.0, 0.0, "растяжение");
	}

	public static Layout ByHeight(int w, int h)
	{
		double num = (double)h / 1024.0;
		return new Layout(num, num, ((double)w - 1280.0 * num) / 2.0, 0.0, "по высоте");
	}

	public static Layout ByWidth(int w, int h)
	{
		double num = (double)w / 1280.0;
		return new Layout(num, num, 0.0, ((double)h - 1024.0 * num) / 2.0, "по ширине");
	}

	public static Layout FromFrame(RectangleF cur, RectangleF bas)
	{
		double num = cur.Width / bas.Width;
		double num2 = cur.Height / bas.Height;
		return new Layout(num, num2, (double)cur.Left - (double)bas.Left * num, (double)cur.Top - (double)bas.Top * num2, "по рамке планшета");
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "{0} (масштаб {1:0.###}×{2:0.###}, сдвиг {3:0.#}, {4:0.#})", Model, Sx, Sy, Ox, Oy);
	}
}
