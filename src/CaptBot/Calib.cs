using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class Calib
{
	private const int MapBg = 1447446;

	public static readonly object Sync = new object();

	private static CalibData data;

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

	public static readonly RectangleF BuiltinBase = new RectangleF(34f, 62f, 1219f, 906f);

	private static string AppDir => AppPaths.Data;

	public static string FilePath => Path.Combine(AppDir, "calib.json");

	public static string FolderFor(int w, int h)
	{
		return Path.Combine(AppDir, "calib", w + "x" + h);
	}

	public static string Stamp()
	{
		return Clock.Msk(Clock.UtcNow()).ToString("dd.MM.yyyy HH:mm");
	}

	private static CalibData D()
	{
		if (data != null)
		{
			return data;
		}
		try
		{
			if (File.Exists(FilePath))
			{
				data = Json.Deserialize<CalibData>(File.ReadAllText(FilePath, Encoding.UTF8));
			}
		}
		catch
		{
			data = null;
		}
		if (data == null)
		{
			data = new CalibData();
		}
		if (data.Items == null)
		{
			data.Items = new List<CalibEntry>();
		}
		return data;
	}

	private static void Save()
	{
		try
		{
			File.WriteAllText(FilePath, Json.Serialize(D()), Encoding.UTF8);
		}
		catch
		{
		}
	}

	public static CalibEntry Get(int w, int h)
	{
		lock (Sync)
		{
			CalibEntry calibEntry = D().Items.FirstOrDefault((CalibEntry i) => i.W == w && i.H == h);
			return (calibEntry != null && Uniform(calibEntry.ToLayout())) ? calibEntry.Copy() : null;
		}
	}

	public static bool Uniform(Layout l)
	{
		if (l.Sy > 0.0)
		{
			return Math.Abs(l.Sx / l.Sy - 1.0) <= 0.03;
		}
		return false;
	}

	public static void Put(CalibEntry e)
	{
		lock (Sync)
		{
			CalibEntry calibEntry = D().Items.FirstOrDefault((CalibEntry i) => i.W == e.W && i.H == e.H);
			if (calibEntry != null && calibEntry.ToLayout().Distance(e.ToLayout()) > 1.5)
			{
				FieldMemory.Forget(e.W, e.H);
			}
			D().Items.RemoveAll((CalibEntry i) => i.W == e.W && i.H == e.H);
			e.When = Stamp();
			D().Items.Add(e.Copy());
			Save();
		}
	}

	public static void Reset(int w, int h)
	{
		lock (Sync)
		{
			D().Items.RemoveAll((CalibEntry i) => i.W == w && i.H == h);
			Save();
		}
		FieldMemory.Forget(w, h);
		try
		{
			string path = Path.Combine(AppDir, "badges", w + "x" + h);
			if (Directory.Exists(path))
			{
				Directory.Delete(path, recursive: true);
			}
		}
		catch
		{
		}
	}

	public static bool BaseFrame(out RectangleF r)
	{
		lock (Sync)
		{
			if (D().HasBase && D().BaseW > 100.0 && D().BaseH > 100.0)
			{
				r = new RectangleF((float)D().BaseX, (float)D().BaseY, (float)D().BaseW, (float)D().BaseH);
				return true;
			}
		}
		r = BuiltinBase;
		return !r.IsEmpty;
	}

	private static void SetBase(RectangleF r)
	{
		lock (Sync)
		{
			D().HasBase = true;
			D().BaseX = r.X;
			D().BaseY = r.Y;
			D().BaseW = r.Width;
			D().BaseH = r.Height;
			Save();
		}
	}

	public static Layout Guess(int w, int h)
	{
		if (w == 1280 && h == 1024)
		{
			return new Layout();
		}
		Layout layout = ((w * 1024 >= h * 1280) ? Layout.ByHeight(w, h) : Layout.ByWidth(w, h));
		layout.Model = "предварительно, " + layout.Model;
		return layout;
	}

	private static bool IsFrameColor(int c)
	{
		return Frame.Near(c, 1184274, 5);
	}

	private static int ScanRun(Frame f, int x, int y, int dx, int dy, int run, int limit)
	{
		int num = 0;
		int num2 = 0;
		while (num2 < limit)
		{
			if (x < 0 || y < 0 || x >= f.W || y >= f.H)
			{
				return -1;
			}
			if (IsFrameColor(f.D[y * f.W + x]))
			{
				if (++num >= run)
				{
					if (dx == 0)
					{
						return y - dy * (run - 1);
					}
					return x - dx * (run - 1);
				}
			}
			else
			{
				num = 0;
			}
			num2++;
			x += dx;
			y += dy;
		}
		return -1;
	}

	private static bool Agree(List<int> v, int tol, out int m)
	{
		m = 0;
		if (v.Count < 3)
		{
			return false;
		}
		v.Sort();
		m = v[v.Count / 2];
		int mm = m;
		int num = v.Count((int x) => Math.Abs(x - mm) <= tol);
		return num >= 3;
	}

	public static bool FindFrame(Frame f, out RectangleF r)
	{
		r = RectangleF.Empty;
		int run = Math.Max(4, Math.Min(f.W, f.H) / 150);
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		List<int> list3 = new List<int>();
		List<int> list4 = new List<int>();
		double[] array = new double[5] { 0.3, 0.4, 0.5, 0.6, 0.7 };
		foreach (double num in array)
		{
			int y = (int)((double)f.H * num);
			int x = (int)((double)f.W * num);
			int item;
			if ((item = ScanRun(f, 0, y, 1, 0, run, f.W / 2)) >= 0)
			{
				list.Add(item);
			}
			if ((item = ScanRun(f, f.W - 1, y, -1, 0, run, f.W / 2)) >= 0)
			{
				list2.Add(item);
			}
			if ((item = ScanRun(f, x, 0, 0, 1, run, f.H / 2)) >= 0)
			{
				list3.Add(item);
			}
			if ((item = ScanRun(f, x, f.H - 1, 0, -1, run, f.H / 2)) >= 0)
			{
				list4.Add(item);
			}
		}
		if (!Agree(list, 3, out var m) || !Agree(list2, 3, out var m2) || !Agree(list3, 3, out var m3) || !Agree(list4, 3, out var m4))
		{
			return false;
		}
		if ((double)(m2 - m) < (double)f.W * 0.3 || (double)(m4 - m3) < (double)f.H * 0.3)
		{
			return false;
		}
		r = RectangleF.FromLTRB(m, m3, m2 + 1, m4 + 1);
		return true;
	}

	private static bool Plausible(Layout l, RectangleF fr, RectangleF bas)
	{
		double num = l.BX(fr.Left);
		double num2 = l.BY(fr.Top);
		double num3 = l.BX(fr.Right);
		double num4 = l.BY(fr.Bottom);
		if (!bas.IsEmpty)
		{
			if (Math.Abs(num - (double)bas.Left) <= 6.0 && Math.Abs(num2 - (double)bas.Top) <= 6.0 && Math.Abs(num3 - (double)bas.Right) <= 6.0)
			{
				return Math.Abs(num4 - (double)bas.Bottom) <= 6.0;
			}
			return false;
		}
		if (num > -12.0 && num < 46.0 && num2 > -12.0 && num2 < 74.0 && num3 > 1200.0 && num3 < 1292.0 && num4 > 914.0)
		{
			return num4 < 1036.0;
		}
		return false;
	}

	public static Layout Choose(Frame f, out Scr screen, out string note)
	{
		screen = Scr.Closed;
		note = "";
		RectangleF r;
		bool flag = FindFrame(f, out r);
		RectangleF r2;
		bool flag2 = BaseFrame(out r2);
		List<Layout> list = new List<Layout>();
		if (flag && flag2)
		{
			list.Add(Layout.FromFrame(r, r2));
		}
		if (f.W == 1280 && f.H == 1024)
		{
			list.Add(new Layout());
		}
		list.Add(Layout.Stretch(f.W, f.H));
		list.Add(Layout.ByHeight(f.W, f.H));
		list.Add(Layout.ByWidth(f.W, f.H));
		Layout layout = null;
		int num = int.MinValue;
		Scr scr = Scr.Closed;
		bool flag3 = false;
		foreach (Layout item in list)
		{
			if (!Uniform(item))
			{
				continue;
			}
			int score;
			Scr scr2 = Screens.Detect(Screens.On(f, item), out score);
			if (scr2 != Scr.Closed)
			{
				bool flag4 = flag && Plausible(item, r, flag2 ? r2 : RectangleF.Empty);
				if (flag)
				{
					score += (flag4 ? 2 : (-4));
				}
				if (score > num)
				{
					num = score;
					layout = item;
					scr = scr2;
					flag3 = flag4;
				}
			}
		}
		if (layout == null)
		{
			return null;
		}
		screen = scr;
		note = string.Concat("раскладка ", layout, ", экран: ", Runner.ScreenName(scr), (!flag) ? ", рамка не найдена" : (flag3 ? ", рамка сходится" : ", рамка не сходится"));
		if (f.W == 1280 && f.H == 1024 && layout.Identity && scr != 0 && flag && Plausible(layout, r, RectangleF.Empty) && (!BaseFrame(out var r3) || Math.Abs(r3.X - r.X) > 1f || Math.Abs(r3.Y - r.Y) > 1f || Math.Abs(r3.Width - r.Width) > 1f || Math.Abs(r3.Height - r.Height) > 1f))
		{
			SetBase(r);
			note += string.Format(CultureInfo.InvariantCulture, ". Эталон рамки планшета: new RectangleF({0}f, {1}f, {2}f, {3}f)", r.X, r.Y, r.Width, r.Height);
		}
		if (scr == Scr.Unknown && !flag3)
		{
			return null;
		}
		return layout;
	}

	private static bool IsBg(int c)
	{
		return Frame.Near(c, 1447446, 7);
	}

	private static int EdgeFrom(Frame f, int x, int y, int dx, int dy, int run, int limit)
	{
		int num = 0;
		int num2 = 0;
		while (num2 < limit)
		{
			if (x < 0 || y < 0 || x >= f.W || y >= f.H)
			{
				return -1;
			}
			if (IsBg(f.D[y * f.W + x]))
			{
				if (++num >= run)
				{
					if (dx == 0)
					{
						return y - dy * run;
					}
					return x - dx * run;
				}
			}
			else
			{
				num = 0;
			}
			num2++;
			x += dx;
			y += dy;
		}
		return -1;
	}

	public static bool MeasureMap(Frame f, Layout l, bool top, out double p, out double left, out double edge, out string note)
	{
		p = 0.0;
		left = 0.0;
		edge = 0.0;
		note = "";
		int run = Math.Max(5, (int)Math.Round(8.0 * Math.Min(l.Sx, l.Sy)));
		int x = (int)Math.Round(l.X(737.0));
		int limit = (int)Math.Round(700.0 * l.Sx);
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		double[] array = new double[5] { 300.0, 400.0, 500.0, 600.0, 700.0 };
		foreach (double by in array)
		{
			int y = (int)Math.Round(l.Y(by));
			int item;
			if ((item = EdgeFrom(f, x, y, -1, 0, run, limit)) >= 0)
			{
				list.Add(item);
			}
			if ((item = EdgeFrom(f, x, y, 1, 0, run, limit)) >= 0)
			{
				list2.Add(item);
			}
		}
		if (!Agree(list, 2, out var m) || !Agree(list2, 2, out var m2))
		{
			note = "края карты по горизонтали не найдены";
			return false;
		}
		double num = (double)(m2 - m + 1) / 58.0;
		if (num < 6.0 || num > 60.0)
		{
			note = "шаг квадрата " + num.ToString("0.0", CultureInfo.InvariantCulture) + " вне пределов";
			return false;
		}
		List<int> list3 = new List<int>();
		int y2 = (int)Math.Round(l.Y(500.0));
		int limit2 = (int)Math.Round(900.0 * l.Sy);
		int[] array2 = new int[5] { 8, 18, 29, 40, 50 };
		foreach (int num2 in array2)
		{
			int x2 = (int)Math.Round((double)m + ((double)num2 - 0.5) * num);
			int item2;
			if ((item2 = EdgeFrom(f, x2, y2, 0, (!top) ? 1 : (-1), run, limit2)) >= 0)
			{
				list3.Add(item2);
			}
		}
		if (!Agree(list3, 2, out var m3))
		{
			note = "край карты по вертикали не найден";
			return false;
		}
		p = num;
		left = m;
		edge = (top ? m3 : (m3 + 1));
		double num3 = 15.93 * l.Sx;
		note = string.Format(CultureInfo.InvariantCulture, "карта измерена: квадрат {0:0.00} px (по раскладке {1:0.00}), левый край {2}, {3} {4:0}", num, num3, m, top ? "верх" : "низ", edge);
		return true;
	}

	public static bool FindPing(Frame f, Layout l, out int px, out int py, out double scale)
	{
		px = 0;
		py = 0;
		scale = 1.0;
		double num = (double)f.H / 1024.0;
		List<double[]> list = new List<double[]>();
		list.Add(new double[3]
		{
			l.X(112.0),
			l.Y(874.0),
			Math.Min(l.Sx, l.Sy)
		});
		list.Add(new double[3]
		{
			112.0,
			f.H - 150,
			1.0
		});
		list.Add(new double[3]
		{
			112.0 * num,
			(double)f.H - 150.0 * num,
			num
		});
		list.Add(new double[3]
		{
			(double)(112 * f.W) / 1280.0,
			874.0 * num,
			num
		});
		list.Add(new double[3]
		{
			112.0 * num,
			(double)f.H - 150.0 * num,
			1.0
		});
		foreach (double[] item in list)
		{
			for (int i = 0; i <= 12; i++)
			{
				int[] array = new int[2] { 1, -1 };
				foreach (int num2 in array)
				{
					if (i == 0 && num2 < 0)
					{
						continue;
					}
					int num3 = num2 * i * 2;
					int[] array2 = new int[3] { 0, -6, 6 };
					foreach (int num4 in array2)
					{
						int num5 = (int)Math.Round(item[0]) + num4;
						int num6 = (int)Math.Round(item[1]) + num3;
						if (num5 >= 0 && num6 >= 0 && PingOcr.ReadPanel(f, num5, num6, item[2], out var _, out var fps) && fps > 0)
						{
							px = num5;
							py = num6;
							scale = item[2];
							return true;
						}
					}
				}
			}
		}
		return false;
	}
}
