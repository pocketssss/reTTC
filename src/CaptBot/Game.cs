using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace CaptBot;

internal class Game
{
	public IntPtr H;

	public int Pid;

	public Rectangle Client;

	public Layout L = new Layout();

	public CalibEntry Cal;

	public Action<string> Note = delegate
	{
	};

	private FieldMemory mem;

	private static DateTime lastPingSearch = DateTime.MinValue;

	public bool Native1280
	{
		get
		{
			if (Client.Width == 1280)
			{
				return Client.Height == 1024;
			}
			return false;
		}
	}

	public bool Calibrated
	{
		get
		{
			if (Cal != null)
			{
				return Cal.Verified;
			}
			return false;
		}
	}

	public string SizeText => Client.Width + "×" + Client.Height;

	public FieldMemory Memory
	{
		get
		{
			if (mem == null)
			{
				mem = new FieldMemory(Client.Width, Client.Height);
			}
			return mem;
		}
	}

	public bool PingKnown
	{
		get
		{
			if (Cal != null)
			{
				return Cal.PingOk;
			}
			return false;
		}
	}

	public Rectangle PingRect
	{
		get
		{
			if (PingKnown)
			{
				return new Rectangle(Cal.PingX, Cal.PingY, (int)Math.Round(70.0 * Cal.PingScale), (int)Math.Round(64.0 * Cal.PingScale));
			}
			return Rc(112.0, 874.0, 70.0, 64.0);
		}
	}

	public bool PingSearchDue
	{
		get
		{
			lock (typeof(Game))
			{
				return (DateTime.UtcNow - lastPingSearch).TotalSeconds >= 4.0;
			}
		}
	}

	public bool Find()
	{
		if (H != IntPtr.Zero && Native.IsWindow(H))
		{
			return true;
		}
		foreach (Process item in Process.GetProcessesByName("GTA5").Concat(Process.GetProcessesByName("GTA5_Enhanced")))
		{
			if (item.MainWindowHandle != IntPtr.Zero)
			{
				H = item.MainWindowHandle;
				Pid = item.Id;
				return true;
			}
		}
		H = IntPtr.Zero;
		return false;
	}

	public void Refresh()
	{
		Native.GetClientRect(H, out var r);
		Native.POINT p = default(Native.POINT);
		Native.ClientToScreen(H, ref p);
		Client = new Rectangle(p.X, p.Y, r.R - r.L, r.B - r.T);
		Cal = Calib.Get(Client.Width, Client.Height);
		L = ((Cal != null) ? Cal.ToLayout() : Calib.Guess(Client.Width, Client.Height));
		if (mem == null || mem.W != Client.Width || mem.H != Client.Height)
		{
			mem = new FieldMemory(Client.Width, Client.Height);
		}
	}

	public Point P(double x, double y)
	{
		return new Point(Client.X + (int)Math.Round(L.X(x)), Client.Y + (int)Math.Round(L.Y(y)));
	}

	public Rectangle Rc(double x, double y, double w, double h)
	{
		return new Rectangle((int)Math.Round(L.X(x)), (int)Math.Round(L.Y(y)), Math.Max(1, (int)Math.Round(w * L.Sx)), Math.Max(1, (int)Math.Round(h * L.Sy)));
	}

	public bool Activate()
	{
		if (Native.IsIconic(H))
		{
			Native.ShowWindow(H, 9);
		}
		if (Native.GetForegroundWindow() == H)
		{
			return true;
		}
		Native.SetForegroundWindow(H);
		Thread.Sleep(150);
		if (Native.GetForegroundWindow() == H)
		{
			return true;
		}
		Input.Tap(56, ext: false, 10);
		Native.SetForegroundWindow(H);
		Thread.Sleep(150);
		return Native.GetForegroundWindow() == H;
	}

	public bool Elevated()
	{
		IntPtr intPtr = Native.OpenProcess(4096u, inh: false, Pid);
		if (intPtr == IntPtr.Zero)
		{
			return true;
		}
		bool result = false;
		if (Native.OpenProcessToken(intPtr, 8u, out var t))
		{
			Native.GetTokenInformation(t, 20, out var info, 4, out var _);
			result = info != 0;
			Native.CloseHandle(t);
		}
		Native.CloseHandle(intPtr);
		return result;
	}

	public bool VisibleScreen(int x, int y)
	{
		Native.POINT p = default(Native.POINT);
		p.X = x;
		p.Y = y;
		IntPtr intPtr = Native.WindowFromPoint(p);
		if (intPtr != IntPtr.Zero)
		{
			if (!(intPtr == H))
			{
				return Native.GetAncestor(intPtr, 2u) == H;
			}
			return true;
		}
		return false;
	}

	public bool VisibleAt(double x, double y)
	{
		Point point = P(x, y);
		return VisibleScreen(point.X, point.Y);
	}

	public bool VisibleMain()
	{
		if (VisibleAt(36.0, 500.0) && VisibleAt(640.0, 63.0) && VisibleAt(1081.0, 500.0) && VisibleAt(640.0, 700.0))
		{
			return VisibleAt(1198.0, 123.0);
		}
		return false;
	}

	public Bitmap Grab(Rectangle r)
	{
		Bitmap bitmap = new Bitmap(Math.Max(1, r.Width), Math.Max(1, r.Height), PixelFormat.Format24bppRgb);
		int num = 1;
		while (true)
		{
			try
			{
				using (Graphics graphics = Graphics.FromImage(bitmap))
				{
					graphics.CopyFromScreen(r.X, r.Y, 0, 0, bitmap.Size);
				}
				return bitmap;
			}
			catch (Win32Exception)
			{
				if (num >= 4)
				{
					bitmap.Dispose();
					throw new ScreenUnavailableException();
				}
				Thread.Sleep(30 * num);
			}
			num++;
		}
	}

	public Bitmap GrabClient()
	{
		return Grab(Client);
	}

	public Frame Shot()
	{
		using Bitmap b = GrabClient();
		return Frame.From(b);
	}

	public Bitmap GrabBase(double x, double y, double w, double h)
	{
		Rectangle r = Rc(x, y, w, h);
		r.Offset(Client.X, Client.Y);
		return Grab(r);
	}

	public Gray GrayBase(double x, double y, double w, double h)
	{
		using Bitmap b = GrabBase(x, y, w, h);
		return Gray.From(b);
	}

	public int S(Frame f, double x, double y)
	{
		return f.At(L.X(x), L.Y(y));
	}

	public static bool Near(int c, int rgb, int tol)
	{
		return Frame.Near(c, rgb, tol);
	}

	public static bool IsBlack(Frame f)
	{
		for (int i = 0; i < 64; i++)
		{
			if (f.At(f.W * (i % 8 * 2 + 1) / 16, f.H * (i / 8 * 2 + 1) / 16) != 0)
			{
				return false;
			}
		}
		return true;
	}

	public Scr Detect(Frame f)
	{
		Scr scr = Screens.Detect(Screens.On(f, L));
		if (Calibrated && scr != Scr.Closed)
		{
			return scr;
		}
		Scr screen;
		string note;
		Layout layout = Calib.Choose(f, out screen, out note);
		if (layout == null)
		{
			return scr;
		}
		if (Calibrated && screen == Scr.Unknown)
		{
			return scr;
		}
		if (Calibrated && layout.Distance(L) > 1.5)
		{
			Thread.Sleep(250);
			Frame f2 = Shot();
			Scr scr2 = Screens.Detect(Screens.On(f2, L));
			if (scr2 != Scr.Closed)
			{
				return scr2;
			}
			Scr screen2;
			string note2;
			Layout layout2 = Calib.Choose(f2, out screen2, out note2);
			if (layout2 == null || screen2 == Scr.Unknown)
			{
				if (screen2 != 0)
				{
					return screen2;
				}
				return scr;
			}
			if (layout2.Distance(layout) > 1.5)
			{
				return screen2;
			}
			Adopt(layout2, note2);
			return screen2;
		}
		if (!Calibrated)
		{
			for (int i = 0; i < 5; i++)
			{
				Thread.Sleep(200);
				Scr screen3;
				string note3;
				Layout layout3 = Calib.Choose(Shot(), out screen3, out note3);
				if (layout3 == null)
				{
					break;
				}
				bool flag = layout3.Distance(layout) <= 1.5;
				layout = layout3;
				screen = screen3;
				note = note3;
				if (flag)
				{
					break;
				}
			}
			Adopt(layout, note);
		}
		return screen;
	}

	public Scr Detect()
	{
		return Detect(Shot());
	}

	private void Adopt(Layout nl, string note)
	{
		CalibEntry calibEntry = Cal ?? new CalibEntry();
		bool flag = Cal == null || !Cal.Verified || Cal.ToLayout().Distance(nl) > 1.5;
		calibEntry.W = Client.Width;
		calibEntry.H = Client.Height;
		calibEntry.SetLayout(nl);
		calibEntry.Verified = true;
		if (flag)
		{
			calibEntry.Refined = false;
			calibEntry.FormGeo = false;
			calibEntry.MapP = 0.0;
			calibEntry.MapLeft = 0.0;
			calibEntry.MapTop = 0.0;
			calibEntry.MapBottom = 0.0;
			calibEntry.MapLabelTop = false;
			calibEntry.MapLabelBottom = false;
			calibEntry.MapNoClamp = false;
		}
		Calib.Put(calibEntry);
		Cal = calibEntry;
		L = nl;
		mem = new FieldMemory(Client.Width, Client.Height);
		Note("Калибровка под окно " + SizeText + ": " + note + ".");
	}

	public void SaveCal()
	{
		if (Cal == null)
		{
			Cal = new CalibEntry();
			Cal.W = Client.Width;
			Cal.H = Client.Height;
			Cal.SetLayout(L);
		}
		Calib.Put(Cal);
	}

	public string RefineOnForm(Frame f)
	{
		if (Cal == null || !Cal.Verified || L.Identity || (Cal.Refined && Cal.FormGeo))
		{
			return null;
		}
		TimeFields timeFields = TimeFields.Find(f, L);
		bool flag = timeFields != null && timeFields.Left >= 0.0;
		double num = (flag ? (timeFields.Left - L.X(77.5)) : 0.0);
		double num2 = (flag ? ((double)timeFields.Hours.Y + (double)timeFields.Hours.Height / 2.0 - L.Y(218.5)) : 0.0);
		int num3 = (int)Math.Round(num);
		int num4 = (int)Math.Round(num2);
		if (Cal.Refined)
		{
			if (!flag)
			{
				return null;
			}
			if (Math.Abs(num) < 2.5 && Math.Abs(num2) < 2.5)
			{
				Cal.FormGeo = true;
				Calib.Put(Cal);
				return null;
			}
			return Shift(num3, num4, string.Format(CultureInfo.InvariantCulture, "Раскладка поправлена по рамке полей времени: сдвиг {0:+0;-0;0}, {1:+0;-0;0} px (раньше её сбило число в поле «Часы»).", num3, num4));
		}
		Rectangle rectangle = Rc(163.0, 204.0, 56.0, 28.0);
		int num5 = Math.Max(4, (int)Math.Ceiling(8.0 * Math.Max(L.Sx, L.Sy)));
		double num6 = double.MaxValue;
		double num7 = double.MaxValue;
		int num8 = 0;
		int num9 = 0;
		for (int i = -num5; i <= num5; i++)
		{
			for (int j = -num5; j <= num5; j++)
			{
				Ocr.Best(f.Gray(rectangle.X + j, rectangle.Y + i, rectangle.Width, rectangle.Height), hours: true, out var cost, out var _);
				if (j == 0 && i == 0)
				{
					num7 = cost;
				}
				if (cost < num6 - 1E-09 || (Math.Abs(cost - num6) < 1E-09 && Math.Abs(j) + Math.Abs(i) < Math.Abs(num8) + Math.Abs(num9)))
				{
					num6 = cost;
					num8 = j;
					num9 = i;
				}
			}
		}
		if (flag && (num6 > 12.0 || Math.Abs((double)num8 - num) > 1.6 || Math.Abs((double)num9 - num2) > 1.6))
		{
			return Shift(num3, num4, string.Format(CultureInfo.InvariantCulture, "Раскладка уточнена по рамке полей времени: сдвиг {0:+0;-0;0}, {1:+0;-0;0} px (по числу «Часы» вышло бы {2:+0;-0;0}, {3:+0;-0;0}, расхождение {4:0.0}).", num3, num4, num8, num9, num6));
		}
		if (num6 > 12.0)
		{
			return string.Format(CultureInfo.InvariantCulture, "Уточнить раскладку по форме не вышло: поле «Часы» не похоже на образцы (расхождение {0:0.0}).", num6);
		}
		return Shift(num8, num9, string.Format(CultureInfo.InvariantCulture, "Раскладка уточнена по полю «Часы»: сдвиг {0:+0;-0;0}, {1:+0;-0;0} px (расхождение {2:0.0} → {3:0.0}).", num8, num9, num7, num6));
	}

	private string Shift(int dx, int dy, string note)
	{
		string text = L.Model ?? "";
		if ((dx != 0 || dy != 0) && !text.EndsWith(", уточнено по форме"))
		{
			text += ", уточнено по форме";
		}
		Layout layout = new Layout(L.Sx, L.Sy, L.Ox + (double)dx, L.Oy + (double)dy, text);
		Cal.SetLayout(layout);
		Cal.Refined = true;
		Cal.FormGeo = true;
		Calib.Put(Cal);
		if (layout.Distance(L) > 1.5)
		{
			mem = new FieldMemory(Client.Width, Client.Height);
		}
		L = layout;
		return note;
	}

	public bool LocatePing(Frame f)
	{
		if (PingKnown)
		{
			return true;
		}
		lock (typeof(Game))
		{
			if ((DateTime.UtcNow - lastPingSearch).TotalSeconds < 4.0)
			{
				return false;
			}
			lastPingSearch = DateTime.UtcNow;
		}
		if (!Calib.FindPing(f, L, out var px, out var py, out var scale))
		{
			return false;
		}
		SaveCal();
		Cal.PingOk = true;
		Cal.PingX = px;
		Cal.PingY = py;
		Cal.PingScale = scale;
		Calib.Put(Cal);
		Note(string.Format(CultureInfo.InvariantCulture, "Панель пинга найдена: {0}, {1}, масштаб {2:0.##}.", px, py, scale));
		return true;
	}

	public bool ReadPing(out int rtt, out int fps)
	{
		rtt = -1;
		fps = -1;
		if (!PingKnown)
		{
			return false;
		}
		Rectangle pingRect = PingRect;
		pingRect.Offset(Client.X, Client.Y);
		using Bitmap b = Grab(pingRect);
		return PingOcr.ReadPanel(Frame.From(b), 0, 0, Cal.PingScale, out rtt, out fps);
	}
}
