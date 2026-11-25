using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;

namespace CaptBot;

internal class Runner
{
	private enum SquareOpen
	{
		Opened,
		NoRequest,
		WrongZone,
		Timeout,
		Stopped
	}

	public class MapScan
	{
		public DateTime At;

		public int R0;

		public int R1;

		public List<int[]> Marks = new List<int[]>();
	}

	public const int S_TABLET = 0;

	public const int S_SKYE = 1;

	public const int S_MAP = 2;

	public const int S_SQUARE = 3;

	public const int S_TIME = 4;

	public const int S_PEOPLE = 5;

	public const int S_PING = 6;

	public const int S_FIRE = 7;

	private const double P = 15.93;

	private const double LeftEdge = 275.0;

	private const double TopEdge = 115.0;

	private const double BottomEdge = 884.0;

	public const int CaptMinAhead = 15;

	public const int CaptMaxAhead = 60;

	private const string TurnText = "первой бьёт семья с приоритетом";

	private const string TurnWait = "жду момент";

	public static readonly string[] StepNames = new string[8] { "Планшет", "Приложение семьи", "Карта территорий", "Квадрат", "Время капта", "Участники", "Замер пинга", "Нажатие «Запланировать атаку»" };

	public volatile bool Stop;

	public Action<string> Log = delegate
	{
	};

	public Action<string, string> Status = delegate
	{
	};

	public Action<int, StepState, string> StepUpdate = delegate
	{
	};

	public Action<int, int, double> PingUpdate = delegate
	{
	};

	public Action<FamilyInfo> FamilyUpdate = delegate
	{
	};

	public volatile bool WantFamily;

	public string ShotDir;

	private readonly Game g = new Game();

	private readonly List<PingSample> samples = new List<PingSample>();

	private int cur = -1;

	private Job job;

	private bool askedHome;

	private bool askedMenu;

	private bool mapLogged;

	public byte[] FailShot;

	private double medRtt;

	private double medFps;

	private double localMs;

	private int minRtt;

	private int maxRtt;

	private int nRtt;

	private string rttSrc = "";

	private string localSrc = "";

	public static PointF FamilyLabel = PointF.Empty;

	public static string FamilyLabelName = "";

	private static Point toggleAt;

	private static MapLabels.ToggleLook toggleOn;

	private static string toggleSize;

	private Point shiftH;

	private Point shiftM;

	private string shiftLogged;

	private bool fieldReadH;

	private bool fieldReadM;

	private bool targetExact;

	public Action<DateTime> CaptEnded = delegate
	{
	};

	public Action<AsapVerdict, string> AsapUpdate = delegate
	{
	};

	public Action<MapScan> Scanned = delegate
	{
	};

	public static volatile bool ShareMarkers = true;

	private volatile bool scanning;

	private bool watchZoomed;

	private volatile WarState asapSt;

	private volatile bool asapFeedStop;

	private readonly List<long[]> asapSeenEnds = new List<long[]>();

	private bool ocrWarned;

	private int mapShotN;

	private MapGrid locGrid;

	private string noClampSize;

	private bool unicodeTyping;

	private int ClickHold => Math.Max(20, Math.Min(70, FrameMs() * 3 / 2));

	private int ClickGap => Math.Max(20, Math.Min(90, FrameMs() * 6 / 5));

	private bool NoClamp
	{
		get
		{
			if (!(noClampSize == g.SizeText))
			{
				if (g.Cal != null)
				{
					return g.Cal.MapNoClamp;
				}
				return false;
			}
			return true;
		}
	}

	public static event Action<Job, DateTime> AttackPlaced;

	public static bool ParseSquare(string code, out int col, out int row)
	{
		col = 0;
		row = 0;
		Match match = Regex.Match((code ?? "").Trim().ToUpperInvariant(), "^([A-J]+)(\\d{1,2})$");
		if (!match.Success)
		{
			return false;
		}
		string value = match.Groups[1].Value;
		foreach (char c in value)
		{
			int num = c - 65 + 1;
			if (num == 10)
			{
				num = 0;
			}
			col = col * 10 + num;
		}
		row = int.Parse(match.Groups[2].Value);
		if (col >= 1 && col <= 58 && row >= 1)
		{
			return row <= 84;
		}
		return false;
	}

	private static string FamilyOf(Job j)
	{
		if (!string.IsNullOrEmpty(j.Family))
		{
			return "семьи " + j.Family;
		}
		return "вашей семьи";
	}

	private bool Sleep(int ms)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < ms)
		{
			if (Stop)
			{
				return false;
			}
			Thread.Sleep(Math.Min(10, Math.Max(1, ms - (int)stopwatch.ElapsedMilliseconds)));
		}
		return !Stop;
	}

	private bool WaitUntil(DateTime utc)
	{
		bool highRes = Precise.HighRes;
		while (true)
		{
			if (Stop)
			{
				return false;
			}
			double totalMilliseconds = (utc - Clock.UtcNow()).TotalMilliseconds;
			if (totalMilliseconds <= 0.0)
			{
				break;
			}
			if (totalMilliseconds > 300.0)
			{
				Thread.Sleep(100);
			}
			else if (highRes && totalMilliseconds > 2.0)
			{
				Precise.Sleep(Math.Min(50.0, totalMilliseconds - 1.5));
			}
			else if (totalMilliseconds > 20.0)
			{
				Thread.Sleep(5);
			}
			else if (totalMilliseconds > 3.0)
			{
				Thread.Sleep(1);
			}
			else
			{
				Thread.SpinWait(50);
			}
		}
		return true;
	}

	private bool WaitScreen(Scr want, int timeoutMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			if (Stop)
			{
				return false;
			}
			if (g.Detect() == want)
			{
				return true;
			}
			Thread.Sleep(20);
		}
		return false;
	}

	private void ClickB(double x, double y)
	{
		Point point = g.P(x, y);
		Input.Click(point.X, point.Y, 45);
	}

	private void DragB(double x1, double y1, double x2, double y2)
	{
		Point point = g.P(x1, y1);
		Point point2 = g.P(x2, y2);
		Input.Drag(point.X, point.Y, point2.X, point2.Y, 12, 8);
	}

	private void Step(int i, StepState s, string detail)
	{
		if (s == StepState.Running)
		{
			cur = i;
		}
		StepUpdate(i, s, detail ?? "");
	}

	private void Fail(string msg)
	{
		Log("ОШИБКА: " + msg);
		KeepFailShot();
		Status("error", msg);
		if (cur >= 0)
		{
			Step(cur, StepState.Error, msg);
		}
		SaveShot("ошибка");
	}

	private void KeepFailShot()
	{
		try
		{
			if (g.H == IntPtr.Zero)
			{
				return;
			}
			using Bitmap bitmap = g.GrabClient();
			int num = Math.Min(bitmap.Width, 1600);
			int height = (int)Math.Round((double)bitmap.Height * (double)num / (double)bitmap.Width);
			using Bitmap bitmap2 = new Bitmap(bitmap, num, height);
			using MemoryStream memoryStream = new MemoryStream();
			bitmap2.Save(memoryStream, ImageFormat.Jpeg);
			FailShot = memoryStream.ToArray();
		}
		catch
		{
			FailShot = null;
		}
	}

	private void Stopped()
	{
		Log("Остановлено.");
		Status("stop", "остановлено");
		if (cur >= 0)
		{
			Step(cur, StepState.Skipped, "остановлено");
		}
	}

	public void SaveShot(string name)
	{
		try
		{
			if (ShotDir != null && !(g.H == IntPtr.Zero))
			{
				Directory.CreateDirectory(ShotDir);
				string text = Path.Combine(ShotDir, Clock.Msk(Clock.UtcNow()).ToString("yyyyMMdd_HHmmss") + "_" + name + ".png");
				using (Bitmap bitmap = g.GrabClient())
				{
					bitmap.Save(text, ImageFormat.Png);
				}
				Log("Скриншот: " + text);
			}
		}
		catch
		{
		}
	}

	private void ReadFamilyAsync(Frame f, Scr s)
	{
		if (!WantFamily)
		{
			return;
		}
		bool num;
		if (s != Scr.Home)
		{
			if (s != Scr.SkyeMenu)
			{
				return;
			}
			num = askedMenu;
		}
		else
		{
			num = askedHome;
		}
		if (num)
		{
			return;
		}
		if (s == Scr.Home)
		{
			askedHome = true;
		}
		else
		{
			askedMenu = true;
		}
		Layout i = g.L;
		ThreadPool.QueueUserWorkItem(delegate
		{
			FamilyInfo familyInfo;
			try
			{
				familyInfo = Family.Read(f, i, s);
			}
			catch (Exception ex)
			{
				familyInfo = new FamilyInfo
				{
					Error = ex.Message
				};
			}
			string text = OwnFamilyFromGame((s == Scr.SkyeMenu) ? 1500 : 300);
			if (!string.IsNullOrEmpty(text))
			{
				if (!string.IsNullOrEmpty(familyInfo.Name) && familyInfo.Name != text)
				{
					familyInfo.Seen = "распознано «" + familyInfo.Name + "»" + (string.IsNullOrEmpty(familyInfo.Seen) ? "" : ("; " + familyInfo.Seen));
				}
				familyInfo.Name = text;
				familyInfo.Error = null;
				if (s == Scr.SkyeMenu)
				{
					familyInfo.Where = "из событий игры";
				}
			}
			FamilyUpdate(familyInfo);
		});
	}

	private static string OwnFamilyFromGame(int waitMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (true)
		{
			string ownFamily = NetWatch.OwnFamily;
			if (!string.IsNullOrEmpty(ownFamily))
			{
				return ownFamily;
			}
			if (!NetWatch.Running || (DateTime.UtcNow - NetWatch.LastEventUtc).TotalSeconds > 15.0 || stopwatch.ElapsedMilliseconds >= waitMs)
			{
				break;
			}
			Thread.Sleep(50);
		}
		return null;
	}

	private bool SamplePing()
	{
		if (!g.PingKnown)
		{
			if (g.H == IntPtr.Zero || !g.PingSearchDue)
			{
				return false;
			}
			if (!g.LocatePing(g.Shot()))
			{
				return false;
			}
		}
		if (!g.ReadPing(out var rtt, out var fps))
		{
			return false;
		}
		PingSample pingSample = new PingSample();
		pingSample.T = Clock.UtcNow();
		pingSample.Rtt = rtt;
		pingSample.Fps = fps;
		samples.Add(pingSample);
		if (samples.Count > 2000)
		{
			samples.RemoveRange(0, 1000);
		}
		PingUpdate(rtt, fps, LeadMs());
		return true;
	}

	private static double Median(List<int> v)
	{
		if (v.Count == 0)
		{
			return 0.0;
		}
		v.Sort();
		if (v.Count % 2 != 1)
		{
			return (double)(v[v.Count / 2 - 1] + v[v.Count / 2]) / 2.0;
		}
		return v[v.Count / 2];
	}

	private double LeadMs()
	{
		DateTime dateTime = Clock.UtcNow().AddSeconds(-12.0);
		List<int> list = new List<int>();
		List<int> list2 = new List<int>();
		foreach (PingSample sample in samples)
		{
			if (sample.T >= dateTime)
			{
				list.Add(sample.Rtt);
				if (sample.Fps > 0)
				{
					list2.Add(sample.Fps);
				}
			}
		}
		medFps = Median(list2);
		if (NetWatch.RttStats(15.0, out var median, out var min, out var max, out var n) && n >= 5)
		{
			medRtt = median;
			minRtt = (int)Math.Floor(min);
			maxRtt = (int)Math.Ceiling(max);
			nRtt = n;
			rttSrc = "по трафику";
		}
		else
		{
			nRtt = list.Count;
			if (list.Count == 0)
			{
				medRtt = 0.0;
				return 0.0;
			}
			minRtt = list.Min();
			maxRtt = list.Max();
			medRtt = Median(list);
			rttSrc = "по панели";
		}
		if (job != null && !job.AutoPing)
		{
			return 0.0;
		}
		if (NetWatch.WireDelayCount >= 2)
		{
			localMs = NetWatch.WireDelayMs;
			localSrc = "замер по трафику";
		}
		else
		{
			localMs = ((medFps > 0.0) ? (500.0 / medFps) : 0.0);
			localSrc = ((medFps > 0.0) ? "полкадра" : "не известна");
		}
		return Math.Max(0.0, Math.Min(600.0, medRtt / 2.0 + localMs));
	}

	private string PingText(double lead)
	{
		if (nRtt == 0)
		{
			return "пинг не известен (панель не видна, трафик игры не читается)";
		}
		return $"пинг {medRtt:0} мс {rttSrc} (разброс {minRtt}–{maxRtt}, замеров {nRtt}) + задержка игры {localMs:0} мс ({localSrc}) → опережение {lead:0.0} мс";
	}

	private bool OpenHome()
	{
		for (int i = 1; i <= 4; i++)
		{
			if (Stop)
			{
				return false;
			}
			Scr scr = g.Detect();
			switch (scr)
			{
			case Scr.Home:
				return Sleep(200);
			case Scr.Form:
				ClickB(85.0, 113.0);
				WaitScreen(Scr.Map, 2500);
				if (!Sleep(300))
				{
					return false;
				}
				scr = g.Detect();
				break;
			}
			if (scr != Scr.Closed)
			{
				Input.Tap(80, ext: true, 40);
				if (!WaitScreen(Scr.Closed, 2500))
				{
					continue;
				}
				if (!Sleep(1100))
				{
					return false;
				}
			}
			Input.Tap(80, ext: true, 40);
			if (WaitScreen(Scr.Home, 3000))
			{
				return Sleep(200);
			}
			if (Stop)
			{
				return false;
			}
			Log("Планшет не открылся — пробую ещё раз (" + i + ")");
			if (!Sleep(800))
			{
				return false;
			}
		}
		Fail(g.Calibrated ? "планшет не открывается по стрелке вниз (может мешать анимация, наручники, авто и т.п.)" : ("планшет не узнан при окне " + g.SizeText + ": либо он не открывается по стрелке вниз, либо раскладка не подобралась (журнал, скриншот в screens)"));
		return false;
	}

	private bool OpenFamilyApp()
	{
		int num = 1;
		while (num <= 2)
		{
			ClickB(Family.AppIcon.X, Family.AppIcon.Y);
			NetWatch.MarkClick(Clock.UtcNow(), "приложение семьи");
			if (WaitFamilyMenu(5000))
			{
				return true;
			}
			if (Stop)
			{
				return false;
			}
			switch (g.Detect())
			{
			case Scr.Unknown:
				Log("Меню семьи не узнано по снимку, но приложение открылось — продолжаю.");
				return true;
			default:
				return false;
			case Scr.Home:
			{
				PointF familyLabel = FamilyLabel;
				if (num == 1 && !familyLabel.IsEmpty)
				{
					Log("Приложение семьи не открылось по значку — жму на подпись «" + FamilyLabelName + "».");
					ClickB(familyLabel.X, familyLabel.Y);
					if (WaitFamilyMenu(4000))
					{
						return true;
					}
					if (Stop)
					{
						return false;
					}
					switch (g.Detect())
					{
					case Scr.Unknown:
						return true;
					default:
						return false;
					case Scr.Home:
						break;
					}
				}
				if (num == 1)
				{
					Log("Приложение семьи не открылось — нажимаю значок ещё раз.");
					if (!Sleep(300))
					{
						return false;
					}
				}
				num++;
				break;
			}
			}
		}
		return false;
	}

	private bool WaitFamilyMenu(int timeoutMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		long num = -1L;
		long num2 = -1L;
		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			if (Stop)
			{
				return false;
			}
			Frame f = g.Shot();
			Scr scr = g.Detect(f);
			if (scr == Scr.SkyeMenu)
			{
				return true;
			}
			long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
			if (scr == Scr.Unknown && Screens.MenuBackground(Screens.On(f, g.L)))
			{
				if (num < 0)
				{
					num = elapsedMilliseconds;
				}
				else if (elapsedMilliseconds - num >= 500)
				{
					return true;
				}
			}
			else
			{
				num = -1L;
			}
			if (scr == Scr.Home)
			{
				if (num2 < 0)
				{
					num2 = elapsedMilliseconds;
				}
				else if (elapsedMilliseconds - num2 >= 2500)
				{
					return false;
				}
			}
			else
			{
				num2 = -1L;
			}
			Thread.Sleep(20);
		}
		return false;
	}

	private bool EnsureLabels()
	{
		Point point = g.P(1240.0, 990.0);
		int num = 0;
		Point c = Point.Empty;
		MapLabels.ToggleLook toggleLook = null;
		for (int i = 0; i < 6; i++)
		{
			if (Stop)
			{
				return false;
			}
			Frame f = g.Shot();
			Point center;
			int num2 = MapLabels.FindToggle(f, g.L, out center);
			if (num2 == 1)
			{
				Log("Подписи квадратов включены ✓" + ((num > 0) ? " (нажал галочку)" : ""));
				return true;
			}
			if (num2 < 0)
			{
				if (num > 0)
				{
					toggleLook = MapLabels.Look(f, g.L, c);
					if (toggleLook != null && toggleLook.PillShare < 0.6)
					{
						toggleAt = c;
						toggleOn = toggleLook;
						toggleSize = g.SizeText;
						Log(string.Concat("Подписи квадратов включены ✓ (нажал галочку, её вид сменился: ", toggleLook, ")"));
						return true;
					}
				}
				else if (toggleOn != null && toggleSize == g.SizeText && toggleOn.Same(MapLabels.Look(f, g.L, toggleAt)))
				{
					Log("Подписи квадратов включены ✓");
					return true;
				}
				if (i == 0)
				{
					Input.MoveTo(point.X, point.Y);
				}
				if (!Sleep(120))
				{
					return false;
				}
				continue;
			}
			if (num >= 2)
			{
				break;
			}
			Log("Подписи квадратов выключены — включаю галочку.");
			Input.Click(g.Client.X + center.X, g.Client.Y + center.Y, 45);
			num++;
			c = center;
			Input.MoveTo(point.X, point.Y);
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < 600)
			{
				if (!Sleep(40))
				{
					return false;
				}
				if (MapLabels.FindToggle(g.Shot(), g.L, out var _) == 1)
				{
					break;
				}
			}
		}
		Log((num > 0) ? ("Внимание: галочка подписей квадратов не включилась" + ((toggleLook != null) ? string.Concat(" (", toggleLook, ")") : "") + ".") : "Галочку подписей квадратов на панели карты не нашёл — не трогаю её.");
		return false;
	}

	private void WaitFormReady()
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		if (Sleep(60))
		{
			while (stopwatch.ElapsedMilliseconds < 700 && !Stop && !LeftHasRows())
			{
				Thread.Sleep(25);
			}
		}
	}

	private bool WaitField(bool hours, Gray before)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		bool flag = before == null;
		Gray gray = null;
		while (stopwatch.ElapsedMilliseconds < 400)
		{
			if (!Sleep(20))
			{
				return false;
			}
			Gray gray2 = FieldCrop(hours);
			if (!flag && Ocr.FieldDiff(before, gray2) >= 0.5)
			{
				flag = true;
			}
			if (flag && gray != null && Ocr.FieldDiff(gray, gray2) < 0.5)
			{
				return true;
			}
			gray = gray2;
		}
		return !Stop;
	}

	private int FrameMs()
	{
		DateTime dateTime = Clock.UtcNow().AddSeconds(-12.0);
		List<int> list = new List<int>();
		foreach (PingSample sample in samples)
		{
			if (sample.T >= dateTime && sample.Fps > 0)
			{
				list.Add(sample.Fps);
			}
		}
		double num = Median(list);
		if (num <= 0.0)
		{
			num = 30.0;
		}
		return (int)Math.Round(1000.0 / Math.Max(15.0, Math.Min(240.0, num)));
	}

	private void LocateFields()
	{
		shiftH = (shiftM = Point.Empty);
		TimeFields timeFields = TimeFields.Find(g.Shot(), g.L);
		if (timeFields == null)
		{
			return;
		}
		PointF pointF = TimeFields.Off(timeFields.Hours, g.L, 192.0);
		PointF pointF2 = TimeFields.Off(timeFields.Mins, g.L, 392.5);
		Func<float, int> func = (float v) => (!(Math.Abs(v) <= 2f)) ? ((int)Math.Round(v)) : 0;
		shiftH = new Point(func(pointF.X), func(pointF.Y));
		shiftM = new Point(func(pointF2.X), func(pointF2.Y));
		if (!shiftH.IsEmpty || !shiftM.IsEmpty)
		{
			string text = string.Format(CultureInfo.InvariantCulture, "Поля времени стоят не на обычном месте («Часы» {0:+0;-0;0}, {1:+0;-0;0} px, «Минуты» {2:+0;-0;0}, {3:+0;-0;0} px) — в игре другой шрифт, беру их по рамкам.", shiftH.X, shiftH.Y, shiftM.X, shiftM.Y);
			if (text != shiftLogged)
			{
				shiftLogged = text;
				Log(text);
			}
		}
	}

	private Point FieldShift(bool hours)
	{
		if (!hours)
		{
			return shiftM;
		}
		return shiftH;
	}

	private Gray FieldCrop(bool hours)
	{
		Rectangle r = g.Rc(hours ? 163 : 364, 204.0, 56.0, 28.0);
		r.Offset(FieldShift(hours));
		r.Offset(g.Client.X, g.Client.Y);
		using Bitmap b = g.Grab(r);
		return Gray.From(b);
	}

	private Point FieldNext(bool hours)
	{
		Point result = g.P(hours ? 229 : 429, 218.0);
		result.Offset(FieldShift(hours));
		return result;
	}

	private int ReadValue(bool hours)
	{
		double cost;
		return Ocr.Read(FieldCrop(hours), hours, g.Memory, out cost);
	}

	private bool StepField(bool hours, Point next, Gray before, out Gray after)
	{
		after = before;
		for (int i = 0; i < 3; i++)
		{
			if (Stop)
			{
				return false;
			}
			Input.Click(next.X, next.Y, ClickHold);
			Stopwatch stopwatch = Stopwatch.StartNew();
			Gray gray = null;
			while (stopwatch.ElapsedMilliseconds < 900)
			{
				if (!Sleep(20))
				{
					return false;
				}
				Gray gray2 = FieldCrop(hours);
				if (gray != null && Ocr.FieldDiff(before, gray2) >= 0.5 && Ocr.FieldDiff(gray, gray2) < 0.5)
				{
					after = gray2;
					return true;
				}
				gray = gray2;
			}
		}
		return false;
	}

	private bool Sweep(string name, bool hours)
	{
		int num = Ocr.Count(hours);
		Point next = FieldNext(hours);
		Gray before = FieldCrop(hours);
		FieldCycle fieldCycle = new FieldCycle(hours);
		fieldCycle.Add(before);
		for (int i = 0; i < 3 * num + 8; i++)
		{
			if (fieldCycle.Closed)
			{
				break;
			}
			if (!StepField(hours, next, before, out var after))
			{
				if (!Stop)
				{
					Log(name + ": поле не меняется от кнопки › — раскладка неточная или форма перекрыта.");
				}
				return false;
			}
			before = after;
			fieldCycle.Add(before);
		}
		if (!fieldCycle.Closed)
		{
			Log(name + ": числа в поле не складываются в круг из " + num + " значений — нажатия теряются или поле чем-то перекрыто.");
			return false;
		}
		string how;
		string diag;
		int num2 = fieldCycle.Solve(out how, out diag);
		if (num2 < 0)
		{
			Log(name + ": не понял, какое число в поле: " + diag + ".");
			return false;
		}
		g.Memory.LearnAll(fieldCycle.Crops, hours, num2);
		Log(string.Format(CultureInfo.InvariantCulture, "{0}: цифры запомнены для окна {1} {2}, в поле сейчас {3:00} ({4}).", name, g.SizeText, how, Ocr.Value(hours, num2), diag));
		return true;
	}

	private bool SetValue(string name, int target, bool hours)
	{
		Point point = FieldNext(hours);
		int num = 0;
		int num2 = 0;
		bool flag = false;
		for (int i = 0; i < 32; i++)
		{
			int num3 = ReadValue(hours);
			if (num3 >= 0)
			{
				if (hours)
				{
					fieldReadH = true;
				}
				else
				{
					fieldReadM = true;
				}
			}
			if (num3 == target)
			{
				Log(name + ": " + num3 + " ✓");
				return true;
			}
			if (num3 < 0)
			{
				int num4 = ((!hours) ? (++num) : (++num2));
				bool flag2 = (!g.Native1280 || !FieldShift(hours).IsEmpty) && ((!hours) ? (!g.Memory.Full(hours: false)) : (g.Memory.Count(hours: true) == 0 && num4 >= 2));
				if (!flag && (num4 > (hours ? 6 : 13) || flag2))
				{
					flag = true;
					Log(name + ": поле не распознаётся при окне " + g.SizeText + " — прокручиваю его и запоминаю цифры (один раз для этого размера).");
					if (!Sweep(name, hours))
					{
						if (!Stop)
						{
							break;
						}
						return false;
					}
					num2 = 0;
					num = 0;
					continue;
				}
				if (num4 > (hours ? 6 : 13))
				{
					break;
				}
				if (hours)
				{
					if (!Sleep(120))
					{
						return false;
					}
					continue;
				}
				Gray before = FieldCrop(hours);
				Input.Click(point.X, point.Y, ClickHold);
				if (!WaitField(hours, before))
				{
					return false;
				}
				continue;
			}
			int num5 = (hours ? (((target - num3) % 24 + 24) % 24) : (((target - num3) / 5 % 12 + 12) % 12));
			Log(name + ": сейчас " + num3 + ", нужно " + target + " — жму › " + num5 + " раз");
			Gray before2 = FieldCrop(hours);
			int clickHold = ClickHold;
			int clickGap = ClickGap;
			for (int j = 0; j < num5; j++)
			{
				if (Stop)
				{
					return false;
				}
				Input.Click(point.X, point.Y, clickHold);
				Thread.Sleep(clickGap);
			}
			if (!WaitField(hours, before2))
			{
				return false;
			}
		}
		Fail(name + ": не удалось выставить " + target + " (поле не распознаётся при окне " + g.SizeText + "; снимок в screens)");
		return false;
	}

	private int WaitCountRight(int expect, int maxMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		Sleep(60);
		int num = -1;
		int num2 = 0;
		int num3;
		do
		{
			num3 = CountRight();
			if (num3 >= expect || stopwatch.ElapsedMilliseconds >= maxMs || Stop)
			{
				return num3;
			}
			if (num3 == num)
			{
				if (++num2 >= 2)
				{
					return num3;
				}
			}
			else
			{
				num2 = 0;
			}
			num = num3;
		}
		while (Sleep(30));
		return num3;
	}

	private int CountRight()
	{
		Frame f = g.Shot();
		int num = 0;
		for (int i = 0; i < 35; i++)
		{
			double num2 = 415.0 + (double)i * 38.83;
			if (num2 > 925.0)
			{
				break;
			}
			bool flag = false;
			for (int j = 690; j <= 790; j += 2)
			{
				if (flag)
				{
					break;
				}
				for (int k = -6; k <= 6; k += 2)
				{
					if (flag)
					{
						break;
					}
					int c = g.S(f, j, num2 + (double)k);
					if (Frame.R(c) > 150 && Frame.G(c) > 150 && Frame.B(c) > 150)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				break;
			}
			num++;
		}
		return num;
	}

	public static string ScreenName(Scr s)
	{
		return s switch
		{
			Scr.Closed => "закрыт", 
			Scr.Home => "главный экран", 
			Scr.SkyeMenu => "меню семьи", 
			Scr.Map => "карта территорий", 
			Scr.Form => "форма атаки", 
			_ => "другой экран", 
		};
	}

	private string BadgePath(string code)
	{
		string path = Path.Combine(AppPaths.Data, "badges");
		if (!g.Native1280)
		{
			path = Path.Combine(path, g.Client.Width + "x" + g.Client.Height);
		}
		return Path.Combine(path, code + ".png");
	}

	private void SaveBadge(string code)
	{
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(BadgePath(code)));
			using Bitmap bitmap = g.GrabBase(112.0, 92.0, 60.0, 42.0);
			bitmap.Save(BadgePath(code), ImageFormat.Png);
		}
		catch
		{
		}
	}

	private bool BadgeMatches(string code)
	{
		try
		{
			string path = BadgePath(code);
			if (!File.Exists(path))
			{
				return false;
			}
			using Bitmap bitmap = new Bitmap(new MemoryStream(File.ReadAllBytes(path)));
			using Bitmap bitmap2 = g.GrabBase(112.0, 92.0, 60.0, 42.0);
			if (bitmap.Width != bitmap2.Width || bitmap.Height != bitmap2.Height)
			{
				return false;
			}
			Frame frame = Frame.From(bitmap);
			Frame frame2 = Frame.From(bitmap2);
			long num = 0L;
			for (int i = 0; i < frame.D.Length; i++)
			{
				int c = frame.D[i];
				int c2 = frame2.D[i];
				num += Math.Abs(Frame.R(c) - Frame.R(c2)) + Math.Abs(Frame.G(c) - Frame.G(c2)) + Math.Abs(Frame.B(c) - Frame.B(c2));
			}
			return (double)num / (3.0 * (double)frame.D.Length) < 4.0;
		}
		catch
		{
			return false;
		}
	}

	private bool LeftHasRows()
	{
		Frame f = g.Shot();
		for (int i = 130; i <= 250; i += 2)
		{
			for (int j = 409; j <= 421; j += 2)
			{
				int c = g.S(f, i, j);
				if (Frame.R(c) > 150 && Frame.G(c) > 150 && Frame.B(c) > 150)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool ResetForm()
	{
		ClickB(360.0, 338.0);
		Thread.Sleep(60);
		Input.Key(29, ext: false, up: false);
		Input.Tap(30, ext: false, 20);
		Input.Key(29, ext: false, up: true);
		Thread.Sleep(30);
		Input.Tap(14, ext: false, 20);
		if (!Sleep(300))
		{
			return false;
		}
		Point point = g.P(900.0, 415.0);
		int num = 0;
		for (int i = 0; i < 40; i++)
		{
			if (Stop)
			{
				return false;
			}
			if (CountRight() == 0)
			{
				break;
			}
			Input.Click(point.X, point.Y, ClickHold);
			num++;
			Thread.Sleep(ClickGap * 2);
		}
		if (!Sleep(150))
		{
			return false;
		}
		bool flag = LeftHasRows() && CountRight() == 0;
		Log("Форма уже была открыта: поиск очищен, убрано участников: " + num + (flag ? " ✓" : " — не получилось, открою заново"));
		return flag;
	}

	private bool Clamped(bool top)
	{
		Frame f = g.Shot();
		int num = 0;
		int num2 = 0;
		int[] array = new int[4] { 300, 450, 600, 750 };
		foreach (int num3 in array)
		{
			int[] array2 = new int[4] { 120, 180, 240, 265 };
			foreach (int num4 in array2)
			{
				num2++;
				if (Game.Near(g.S(f, num4, num3), 1447446, 6))
				{
					num++;
				}
			}
		}
		int c = (top ? g.S(f, 700.0, 105.0) : g.S(f, 700.0, 912.0));
		if (num >= num2 - 1)
		{
			return Game.Near(c, 1447446, 6);
		}
		return false;
	}

	private bool NormalizeMap(bool top)
	{
		for (int i = 1; i <= 4; i++)
		{
			Point point = g.P(640.0, 513.0);
			Input.Wheel(point.X, point.Y, -6, 40);
			if (!WaitStable(120, 300))
			{
				return false;
			}
			for (int j = 0; j < 2; j++)
			{
				if (top)
				{
					DragB(500.0, 200.0, 1200.0, 950.0);
				}
				else
				{
					DragB(500.0, 900.0, 1200.0, 150.0);
				}
				if (!Sleep(150))
				{
					return false;
				}
			}
			if (!Sleep(150))
			{
				return false;
			}
			if (Clamped(top))
			{
				return true;
			}
			Log("Карта ещё не откликается — повторяю (" + i + ")");
			if (!Sleep(500))
			{
				return false;
			}
		}
		return false;
	}

	private bool Target(Job j, bool top, out double tx, out double ty, out string note)
	{
		note = null;
		Frame f = g.Shot();
		CalibEntry cal = g.Cal;
		double p;
		double left;
		double edge;
		string note2;
		if (cal != null && cal.MapP > 0.0 && (cal.MapLabelTop || cal.MapLabelBottom))
		{
			p = cal.MapP;
			left = cal.MapLeft;
			bool flag = (top ? cal.MapLabelTop : cal.MapLabelBottom);
			edge = ((!flag) ? (top ? g.L.Y(115.0) : g.L.Y(884.0)) : (top ? cal.MapTop : cal.MapBottom));
			if (!mapLogged)
			{
				Log("Карта: беру замер по подписям квадратов" + (flag ? "" : " (эта сторона — по расчёту)") + ".");
			}
			mapLogged = true;
			targetExact = flag && !NoClamp;
		}
		else if (Calib.MeasureMap(f, g.L, top, out p, out left, out edge, out note2))
		{
			targetExact = !NoClamp;
			bool flag2 = cal == null || Math.Abs(cal.MapP - p) > 0.05 || Math.Abs(cal.MapLeft - left) > 1.0 || Math.Abs((top ? cal.MapTop : cal.MapBottom) - edge) > 1.0;
			if (cal != null)
			{
				cal.MapP = p;
				cal.MapLeft = left;
				if (top)
				{
					cal.MapTop = edge;
				}
				else
				{
					cal.MapBottom = edge;
				}
				if (flag2)
				{
					Calib.Put(cal);
				}
			}
			if (flag2 || !mapLogged)
			{
				Log("Карта: " + note2 + ".");
			}
			mapLogged = true;
		}
		else
		{
			bool flag3 = cal != null && cal.MapP > 0.0 && (top ? cal.MapTop : cal.MapBottom) > 0.0;
			targetExact = !NoClamp && (flag3 || g.Native1280);
			p = (flag3 ? cal.MapP : (15.93 * g.L.Sx));
			left = (flag3 ? cal.MapLeft : g.L.X(275.0));
			edge = ((!flag3) ? (top ? g.L.Y(115.0) : g.L.Y(884.0)) : (top ? cal.MapTop : cal.MapBottom));
			Log("Карта: " + note2 + " — беру " + (flag3 ? "прошлый замер" : "расчёт по раскладке") + ".");
		}
		tx = left + ((double)j.Col - 0.5) * p;
		ty = (top ? (edge + ((double)j.Row - 0.5) * p) : (edge - ((double)(84 - j.Row) + 0.5) * p));
		double num = ((cal != null && cal.MapTop > 0.0) ? cal.MapTop : g.L.Y(115.0));
		double num2 = ((cal != null && cal.MapBottom > 0.0) ? cal.MapBottom : g.L.Y(884.0));
		bool flag4 = (top ? (ty <= num2 - 1.5 * p) : (ty >= num + 1.5 * p));
		if (!flag4)
		{
			note = "строка " + j.Row + " не видна, когда карта прижата " + (top ? "вверх" : "вниз");
		}
		return flag4;
	}

	private void DragExact(int x, int y1, int y2)
	{
		Input.MoveTo(x, y1);
		Thread.Sleep(40);
		Input.Down();
		Thread.Sleep(60);
		for (int i = 1; i <= 40; i++)
		{
			Input.MoveTo(x, y1 + (y2 - y1) * i / 40);
			Thread.Sleep(12);
		}
		Thread.Sleep(300);
		Input.Up();
	}

	private bool QuickOpenSquare(Job j)
	{
		if (!ReadGrid(out var grid) || grid.P < 14.0)
		{
			return false;
		}
		PointF c = grid.Cell(j.Col, j.Row);
		if (!Clickable(c, grid))
		{
			return false;
		}
		Log(string.Format(CultureInfo.InvariantCulture, "Карта: {0} уже виден ({1}) — кликаю сразу, без перестройки карты.", j.Square, grid));
		ShotMap("клик_сразу_" + j.Square, new Point((int)Math.Round(c.X), (int)Math.Round(c.Y)));
		DateTime utcNow = DateTime.UtcNow;
		Point click = new Point((int)Math.Round(c.X), (int)Math.Round(c.Y));
		Input.Click(g.Client.X + click.X, g.Client.Y + click.Y, 45);
		NetWatch.MarkClick(Clock.UtcNow(), "квадрат " + j.Square);
		int seen;
		bool panel;
		switch (WaitSquareForm(j, utcNow, click, 2500, out seen, out panel))
		{
		case SquareOpen.Opened:
			WaitFormReady();
			ShotMap("форма_" + j.Square, Point.Empty);
			SaveBadge(j.Square);
			return true;
		case SquareOpen.WrongZone:
			BackFromWrongZone(j, seen);
			return false;
		default:
			if (Stop)
			{
				return false;
			}
			if (g.Detect() == Scr.Form)
			{
				ClickB(85.0, 113.0);
				WaitScreen(Scr.Map, 2500);
			}
			Log("Карта: с первого клика форма не открылась — перестраиваю карту.");
			return false;
		}
	}

	private SquareOpen WaitSquareForm(Job j, DateTime clickUtc, Point click, int timeoutMs, out int seen, out bool panel)
	{
		seen = 0;
		panel = false;
		bool flag = NetWatch.Running && (DateTime.UtcNow - NetWatch.LastEventUtc).TotalSeconds < 15.0;
		Zone zone = Zones.ByCode(j.Square);
		bool flag2 = false;
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (stopwatch.ElapsedMilliseconds < timeoutMs)
		{
			if (Stop)
			{
				return SquareOpen.Stopped;
			}
			if (flag && !flag2 && NetWatch.LastRequestUtc >= clickUtc && NetWatch.LastRequestZone > 0 && zone != null)
			{
				seen = NetWatch.LastRequestZone;
				if (seen != zone.Id)
				{
					return SquareOpen.WrongZone;
				}
				flag2 = true;
				Log("Из игры: игра открывает форму на " + zone.Label + " ✓ (id зоны в запросе совпал).");
			}
			Frame f = g.Shot();
			if (g.Detect(f) == Scr.Form)
			{
				return SquareOpen.Opened;
			}
			if (!panel && stopwatch.ElapsedMilliseconds >= 60 && MapLabels.FindAttackButton(f, g.L, click, out var center))
			{
				panel = true;
				Log("Карта: " + j.Square + " — особая зона, вместо формы карточка; жму «Совершить нападение».");
				ShotMap("карточка_" + j.Square, center);
				clickUtc = DateTime.UtcNow;
				Input.Click(g.Client.X + center.X, g.Client.Y + center.Y, 45);
				NetWatch.MarkClick(Clock.UtcNow(), "квадрат " + j.Square);
				stopwatch.Restart();
				continue;
			}
			if (flag && stopwatch.ElapsedMilliseconds > 600 && NetWatch.LastOutEventUtc < clickUtc && NetWatch.LastCaptureDataUtc < clickUtc)
			{
				Log("Из игры: за 0,6 с после " + (panel ? "«Совершить нападение»" : "клика") + " игра не запросила данные зоны — форма не откроется.");
				if (g.Detect() != Scr.Form)
				{
					return SquareOpen.NoRequest;
				}
				return SquareOpen.Opened;
			}
			Thread.Sleep(20);
		}
		return SquareOpen.Timeout;
	}

	private void BackFromWrongZone(Job j, int seen)
	{
		Zone zone = Zones.ById(seen);
		Log("Из игры: клик открыл зону " + ((zone != null) ? zone.Label : ("#" + seen)) + ", а нужна " + j.Square + " — возвращаюсь к карте и ищу заново.");
		if (WaitScreen(Scr.Form, 2500))
		{
			ClickB(85.0, 113.0);
			WaitScreen(Scr.Map, 2500);
		}
		Sleep(250);
	}

	private bool ConfirmHit(Job j, Point click)
	{
		Point client = new Point(click.X - g.Client.X, click.Y - g.Client.Y);
		ShotMap("после_клика_" + j.Square, client);
		if (ReadGrid(out var grid))
		{
			PointF pointF = grid.Cell(j.Col, j.Row);
			double num = Math.Max(Math.Abs(pointF.X - (float)client.X), Math.Abs(pointF.Y - (float)client.Y));
			bool flag = num < 0.35 * grid.P;
			Log(string.Format(CultureInfo.InvariantCulture, "Карта: после клика {0} по подписям ({1}) в {2:0} px от точки клика — {3}.", j.Square, grid, num, flag ? "клик был точный" : "клик мимо, ищу заново"));
			return flag;
		}
		bool flag2 = locGrid != null && locGrid.Labels >= 4;
		Log("Карта: после клика подписи не прочитались — " + (flag2 ? string.Concat("первая сетка была уверенной (", locGrid, "), считаю клик точным.") : "проверить клик нечем, ищу заново."));
		return flag2;
	}

	private bool OpenSquareForm(Job j)
	{
		bool flag = j.Row <= 45;
		bool available = WinOcr.Available;
		if (available && QuickOpenSquare(j))
		{
			return true;
		}
		if (Stop)
		{
			return false;
		}
		bool flag2 = false;
		for (int i = 1; i <= 3; i++)
		{
			if (!PrepareMap(flag, available))
			{
				if (!Stop)
				{
					Fail("карта не реагирует на колёсико/перетаскивание");
				}
				return false;
			}
			double tx;
			double ty;
			string note;
			bool flag3 = Target(j, flag, out tx, out ty, out note);
			if (available)
			{
				Rectangle rectangle = g.Rc(80.0, 175.0, 1120.0, 740.0);
				tx = Math.Max(rectangle.Left, Math.Min(rectangle.Right, tx));
				ty = Math.Max(rectangle.Top, Math.Min(rectangle.Bottom, ty));
			}
			else if (!flag3)
			{
				Log("Карта: " + note + ".");
				if (i == 1)
				{
					flag = !flag;
					continue;
				}
				if (flag)
				{
					flag = false;
					if (!NormalizeMap(top: false))
					{
						if (!Stop)
						{
							Fail("карта не реагирует на колёсико/перетаскивание");
						}
						return false;
					}
					Target(j, top: false, out tx, out ty, out note);
				}
				CalibEntry cal = g.Cal;
				double num = ((cal != null && cal.MapP > 0.0) ? cal.MapP : (15.93 * g.L.Sx));
				double num2 = ((cal != null && cal.MapTop > 0.0) ? cal.MapTop : g.L.Y(115.0));
				int num3 = (int)Math.Round(num2 + 3.0 * num - ty);
				Point point = g.P(737.0, 300.0);
				Log("Карта: строка " + j.Row + " посередине — сдвигаю карту вниз на " + num3 + " px.");
				DragExact(point.X, point.Y, point.Y + num3);
				if (!Sleep(250))
				{
					return false;
				}
				ty += (double)num3;
			}
			Point guess = new Point(g.Client.X + (int)Math.Round(tx), g.Client.Y + (int)Math.Round(ty));
			ShotMap("прицел_" + j.Square + "_" + i + (flag ? "_верх" : "_низ"), new Point((int)Math.Round(tx), (int)Math.Round(ty)));
			Input.Wheel(guess.X, guess.Y, 2, 70);
			if (!WaitStable(150, 400))
			{
				return false;
			}
			Point screen;
			bool flag4 = LocateByLabels(j.Col, j.Row, j.Square, guess, out screen);
			if (Stop)
			{
				return false;
			}
			if (flag4)
			{
				flag2 = true;
			}
			else if (available && !targetExact)
			{
				Log("Карта: подписи квадратов не прочитались, а расчёт при окне " + g.SizeText + " неточный — по нему не кликаю." + ((i < 3) ? " Проверяю галочку подписей и пробую заново." : ""));
				EnsureLabels();
				continue;
			}
			ShotMap("клик_" + j.Square + "_" + i, new Point(screen.X - g.Client.X, screen.Y - g.Client.Y));
			DateTime utcNow = DateTime.UtcNow;
			Point click = new Point(screen.X - g.Client.X, screen.Y - g.Client.Y);
			Input.Click(screen.X, screen.Y, 45);
			NetWatch.MarkClick(Clock.UtcNow(), "квадрат " + j.Square);
			int seen;
			bool panel;
			SquareOpen squareOpen = WaitSquareForm(j, utcNow, click, 3500, out seen, out panel);
			if (squareOpen == SquareOpen.Opened)
			{
				WaitFormReady();
				ShotMap("форма_" + j.Square, Point.Empty);
				SaveBadge(j.Square);
				return true;
			}
			if (squareOpen == SquareOpen.Stopped || Stop)
			{
				return false;
			}
			if (squareOpen == SquareOpen.WrongZone)
			{
				BackFromWrongZone(j, seen);
				continue;
			}
			if (panel)
			{
				Fail("особая зона " + j.Square + ": после «Совершить нападение» форма атаки не открылась — у " + FamilyOf(j) + " не выполнены требования для атаки этой зоны (они в карточке зоны) или она не прилегает к территории");
				return false;
			}
			if (g.Detect() != Scr.Map)
			{
				break;
			}
			if (flag4 && ConfirmHit(j, screen))
			{
				if (!Stop)
				{
					Fail("квадрат " + j.Square + " найден и нажат, но форма атаки не открылась: он не прилегает к территории " + FamilyOf(j) + " или сейчас недоступен");
				}
				return false;
			}
			Log("Форма не открылась с первого клика — ищу квадрат заново.");
		}
		if (!Stop)
		{
			if (available && !flag2)
			{
				Fail("квадрат " + j.Square + " не найден: подписи квадратов на карте не читаются (проверьте галочку подписей вверху карты)");
			}
			else
			{
				Fail("форма атаки не открылась: квадрат " + j.Square + " не прилегает к территории " + FamilyOf(j) + " или сейчас недоступен");
			}
		}
		return false;
	}

	public void Run(Job j)
	{
		if (j.ListOnly)
		{
			RunList(j);
			return;
		}
		job = j;
		g.Note = delegate(string m)
		{
			Log(m);
		};
		Native.timeBeginPeriod(1u);
		Precise.Init();
		try
		{
			for (int i = 0; i < StepNames.Length; i++)
			{
				Step(i, StepState.Pending, "");
			}
			cur = -1;
			bool flag = g.Find();
			if (flag)
			{
				g.Refresh();
			}
			if (!j.DryRun && !j.Watch)
			{
				DateTime dateTime = j.FireUtc.AddSeconds(-j.PrepSec);
				Status("wait", "жду начала подготовки в " + Clock.Fmt(dateTime));
				while (Clock.UtcNow() < dateTime)
				{
					if (Stop)
					{
						Stopped();
						return;
					}
					if (flag)
					{
						SamplePing();
					}
					double totalMilliseconds = (dateTime - Clock.UtcNow()).TotalMilliseconds;
					if (totalMilliseconds > 0.0)
					{
						Sleep((int)Math.Min(500.0, totalMilliseconds));
					}
				}
			}
			string square = j.Square;
			if (j.BeforePrep != null && !j.Watch && !j.Prepare)
			{
				try
				{
					j.BeforePrep(j);
				}
				catch (Exception ex)
				{
					Log("Умная цель: " + ex.Message + " — бью основную цель.");
				}
			}
			if (j.Cancel)
			{
				Status("idle", "");
				return;
			}
			if (j.Watch)
			{
				RunAsap(j);
				return;
			}
			if (j.Square != square)
			{
				Turn.Refresh();
			}
			if (!j.DryRun && !TurnEarly(j))
			{
				Stopped();
				return;
			}
			if (!g.Find())
			{
				Fail("окно GTA 5 не найдено");
				return;
			}
			g.Refresh();
			if (g.Client.Width < 640)
			{
				Fail("окно игры свёрнуто или слишком маленькое");
				return;
			}
			Log("Окно игры " + g.SizeText + (g.Calibrated ? string.Concat(", раскладка: ", g.L, ".") : ", калибровки для этого размера ещё нет — подберу по планшету."));
			Stopwatch sw = Stopwatch.StartNew();
			Status(j.DryRun ? "test" : "prep", "подготовка");
			if (!g.Activate())
			{
				Log("Внимание: не удалось вывести игру на передний план.");
			}
			if (!Sleep(150))
			{
				Stopped();
				return;
			}
			if (j.Prepare && !PrepareWindow(j, sw))
			{
				if (Stop)
				{
					Stopped();
				}
				return;
			}
			if (!OpenToForm(j, sw, reuse: false))
			{
				if (Stop)
				{
					Stopped();
				}
				return;
			}
			string text = g.RefineOnForm(g.Shot());
			if (text != null)
			{
				Log(text);
			}
			if (!FillForm(j, sw))
			{
				if (Stop)
				{
					Stopped();
				}
			}
			else
			{
				Fire(j);
			}
		}
		catch (Exception ex2)
		{
			Telemetry.Caught(ex2, "забив");
			Fail(ex2.Message);
		}
		finally
		{
			Native.timeEndPeriod(1u);
		}
	}

	private bool OpenToMapOrForm(Job j, Stopwatch sw, bool allowReuse, out bool formOpen)
	{
		formOpen = false;
		Frame f = g.Shot();
		if (Game.IsBlack(f))
		{
			Log("Внимание: снимок окна игры чёрный. В полноэкранном режиме программа не видит игру — включите в настройках игры «Оконный без рамки».");
		}
		Scr scr = g.Detect(f);
		Log("Планшет сейчас: " + ScreenName(scr) + ".");
		ReadFamilyAsync(f, scr);
		if (scr == Scr.Form)
		{
			if (allowReuse && BadgeMatches(j.Square))
			{
				formOpen = ResetForm();
			}
			if (Stop)
			{
				return false;
			}
			if (formOpen)
			{
				for (int i = 0; i <= 3; i++)
				{
					Step(i, StepState.Done, (i == 3) ? (j.Square + ", форма уже открыта") : "уже открыто");
				}
				return true;
			}
			Log("Открыта форма " + (BadgeMatches(j.Square) ? "этого квадрата, но не удалось её очистить" : "другого квадрата") + " — возвращаюсь на карту.");
			ClickB(85.0, 113.0);
			WaitScreen(Scr.Map, 2500);
			if (!Sleep(300))
			{
				return false;
			}
			scr = g.Detect();
		}
		switch (scr)
		{
		case Scr.Map:
		{
			for (int k = 0; k <= 2; k++)
			{
				Step(k, StepState.Done, "уже открыто");
			}
			EnsureLabels();
			return true;
		}
		case Scr.SkyeMenu:
			Step(0, StepState.Done, "уже открыто");
			Step(1, StepState.Done, "уже открыто");
			break;
		default:
			Step(0, StepState.Running, "");
			if (!OpenHome())
			{
				return false;
			}
			Step(0, StepState.Done, Sec(sw));
			ReadFamilyAsync(g.Shot(), Scr.Home);
			Step(1, StepState.Running, "");
			if (!OpenFamilyApp())
			{
				if (!Stop)
				{
					Fail("не открылось приложение семьи на планшете");
				}
				return false;
			}
			Step(1, StepState.Done, (string.IsNullOrEmpty(j.Family) ? "" : (j.Family + ", ")) + Sec(sw));
			break;
		}
		ReadFamilyAsync(g.Shot(), Scr.SkyeMenu);
		SamplePing();
		Step(2, StepState.Running, "");
		if (!OpenMapFromMenu())
		{
			if (!Stop)
			{
				Fail("не открылась карта территорий");
			}
			return false;
		}
		if (!WaitStable(150, 600))
		{
			return false;
		}
		EnsureLabels();
		Step(2, StepState.Done, Sec(sw));
		return true;
	}

	private bool OpenMapFromMenu()
	{
		bool flag = NetWatch.Running && (DateTime.UtcNow - NetWatch.LastEventUtc).TotalSeconds < 15.0;
		for (int i = 1; i <= 2; i++)
		{
			DateTime utcNow = DateTime.UtcNow;
			ClickB(1081.0, 515.0);
			NetWatch.MarkClick(Clock.UtcNow(), "карта территорий");
			Stopwatch stopwatch = Stopwatch.StartNew();
			long num = 6000L;
			long num2 = -1L;
			bool flag2 = false;
			while (stopwatch.ElapsedMilliseconds < num)
			{
				if (Stop)
				{
					return false;
				}
				Frame f = g.Shot();
				Scr scr = g.Detect(f);
				if (scr == Scr.Map)
				{
					return true;
				}
				long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				bool flag3 = flag && NetWatch.LastOutEventUtc >= utcNow;
				if (flag3)
				{
					num = 10000L;
				}
				bool flag4 = scr switch
				{
					Scr.Unknown => Screens.MenuBackground(Screens.On(f, g.L)), 
					Scr.SkyeMenu => true, 
					_ => false, 
				};
				if (!flag4)
				{
					num2 = -1L;
				}
				else if (num2 < 0)
				{
					num2 = elapsedMilliseconds;
				}
				if (i == 1 && flag4 && !flag3 && (flag ? (elapsedMilliseconds > 800) : (elapsedMilliseconds - num2 >= 3000)))
				{
					flag2 = true;
					break;
				}
				Thread.Sleep(20);
			}
			if (!flag2)
			{
				return false;
			}
			Log(flag ? "Из игры: после нажатия «Карта территорий» игра ничего не запросила — нажимаю ещё раз." : "Карта территорий не открылась за 3 с, на экране всё ещё меню семьи — нажимаю ещё раз.");
		}
		return false;
	}

	private bool OpenToForm(Job j, Stopwatch sw, bool reuse)
	{
		if (!OpenToMapOrForm(j, sw, !j.Watch || reuse, out var formOpen))
		{
			return false;
		}
		if (formOpen)
		{
			return true;
		}
		Step(3, StepState.Running, j.Square);
		if (!OpenSquareForm(j))
		{
			return false;
		}
		Step(3, StepState.Done, j.Square + ", " + Sec(sw));
		return true;
	}

	private bool SetTimeFields(Job j, Stopwatch sw)
	{
		Step(4, StepState.Running, "");
		LocateFields();
		if (!SetValue("Часы", j.CaptH, hours: true))
		{
			return false;
		}
		if (!SetValue("Минуты", j.CaptM, hours: false))
		{
			return false;
		}
		Step(4, StepState.Done, j.CaptH.ToString("00") + ":" + j.CaptM.ToString("00") + ", " + Sec(sw));
		return true;
	}

	private bool FillForm(Job j, Stopwatch sw)
	{
		if (!SetTimeFields(j, sw))
		{
			return false;
		}
		if (j.Roster != null)
		{
			return FillFormByList(j, sw);
		}
		Step(5, StepState.Running, "");
		Point point = g.P(300.0, 415.0);
		int clickHold = ClickHold;
		int clickGap = ClickGap;
		for (int i = 0; i < j.People; i++)
		{
			if (Stop)
			{
				return false;
			}
			Input.Click(point.X, point.Y, clickHold);
			Thread.Sleep(clickGap);
		}
		int num = Math.Min(j.People, 13);
		int num2 = WaitCountRight(num, 400);
		for (int k = 0; k < 2; k++)
		{
			if (num2 >= num)
			{
				break;
			}
			if (j.People > 13)
			{
				break;
			}
			if (!LeftHasRows())
			{
				break;
			}
			int num3 = num - num2;
			Log("Людей в правом списке " + num2 + " из " + num + " — добавляю ещё " + num3 + ".");
			for (int l = 0; l < num3; l++)
			{
				if (Stop)
				{
					return false;
				}
				Input.Click(point.X, point.Y, clickHold + 10);
				Thread.Sleep(clickGap * 2);
			}
			num2 = WaitCountRight(num, 400);
		}
		Log("Людей в правом списке (видимых): " + num2 + ((num2 >= num) ? " ✓" : " — меньше, чем нужно!"));
		Step(5, (num2 >= num) ? StepState.Done : StepState.Error, num2 + " человек, " + Sec(sw));
		ClickB(360.0, 338.0);
		Thread.Sleep(80);
		for (int m = 0; m < 6; m++)
		{
			Input.Tap(44, ext: false, 20);
			Thread.Sleep(25);
		}
		Point point2 = g.P(1085.0, 113.0);
		Input.MoveTo(point2.X, point2.Y);
		if (!Sleep(60))
		{
			return false;
		}
		Log("Всё заполнено за " + sw.ElapsedMilliseconds + " мс (кадр игры ~" + FrameMs() + " мс: нажатие " + clickHold + " мс, пауза " + clickGap + " мс).");
		SaveShot(j.DryRun ? "тест_заполнено" : "перед_нажатием");
		return true;
	}

	private void ConfirmLater(Job j, DateTime clickUtc)
	{
		Zone tz = Zones.At(j.Col, j.Row);
		if (tz == null)
		{
			return;
		}
		string own = ((!string.IsNullOrEmpty(NetWatch.OwnFamily)) ? NetWatch.OwnFamily : j.Family);
		int h = j.CaptH;
		int i = j.CaptM;
		NetCapt before = NetWatch.CaptOn(tz.Id);
		Thread thread = new Thread((ThreadStart)delegate
		{
			try
			{
				Func<NetCapt, bool> func = delegate(NetCapt c)
				{
					if (!string.IsNullOrEmpty(own))
					{
						return Zones.Key(c.A) == Zones.Key(own);
					}
					DateTime dateTime = Clock.Msk(War.FromUnix(c.Start));
					return dateTime.Hour == h && dateTime.Minute == i;
				};
				while (Clock.UtcNow() < clickUtc.AddSeconds(6.0))
				{
					KeyValuePair<DateTime, NetCapt>? keyValuePair = NetWatch.CaptAppeared(tz.Id, clickUtc.AddMilliseconds(-200.0));
					if (keyValuePair.HasValue)
					{
						NetCapt value = keyValuePair.Value.Value;
						double totalMilliseconds = (keyValuePair.Value.Key - clickUtc).TotalMilliseconds;
						string text = Clock.Msk(War.FromUnix(value.Start)).ToString("HH:mm");
						bool flag = func(value);
						Log(flag ? $"✓ Забив принят игрой: капт на {tz.Label} в {text} — событие пришло через {totalMilliseconds:0} мс после нажатия." : $"✗ На {tz.Label} капт поставили {value.A} (в {text}) — их событие пришло через {totalMilliseconds:0} мс после нашего нажатия: они были первыми.");
						Report(j, flag ? "accepted" : "outbid", flag ? text : value.A);
						return;
					}
					Thread.Sleep(20);
				}
				NetCapt netCapt = NetWatch.CaptOn(tz.Id);
				if (netCapt != null && !func(netCapt))
				{
					Log("✗ На " + tz.Label + " стоит капт " + netCapt.A + " в " + Clock.Msk(War.FromUnix(netCapt.Start)).ToString("HH:mm") + ((before != null) ? " (стоял ещё до нажатия)" : "") + " — наш забив не прошёл.");
					Report(j, "outbid", netCapt.A);
				}
				else if (netCapt != null)
				{
					Log("✓ На " + tz.Label + " стоит наш капт в " + Clock.Msk(War.FromUnix(netCapt.Start)).ToString("HH:mm") + ".");
					Report(j, "accepted", Clock.Msk(War.FromUnix(netCapt.Start)).ToString("HH:mm"));
				}
				else
				{
					Log("Подтверждения забива от игры за 6 с нет: изменения зон игра присылает, пока карта каптов открыта в планшете. Проверьте забив в планшете.");
					Report(j, "unknown", "");
				}
			}
			catch
			{
				Report(j, "unknown", "");
			}
		});
		thread.IsBackground = true;
		thread.Name = "TTC confirm";
		Thread thread2 = thread;
		thread2.Start();
	}

	private static void Report(Job j, string status, string detail)
	{
		Action<string, string> confirm = j.Confirm;
		if (confirm != null)
		{
			try
			{
				confirm(status, detail ?? "");
			}
			catch
			{
			}
		}
	}

	private void Fire(Job j)
	{
		Point point = g.P(1085.0, 113.0);
		Zone zone = Zones.At(j.Col, j.Row);
		NetCapt netCapt = ((zone != null && NetWatch.Running) ? NetWatch.CaptOn(zone.Id) : null);
		if (netCapt != null && Zones.Key(netCapt.A) != Zones.Key((!string.IsNullOrEmpty(NetWatch.OwnFamily)) ? NetWatch.OwnFamily : j.Family))
		{
			Log("Внимание: по данным игры на " + zone.Label + " уже стоит капт " + netCapt.A + " в " + Clock.Msk(War.FromUnix(netCapt.Start)).ToString("HH:mm") + " — игра, скорее всего, откажет.");
		}
		Step(6, StepState.Running, "");
		if (j.DryRun)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < 3000)
			{
				if (Stop)
				{
					Stopped();
					return;
				}
				SamplePing();
				Thread.Sleep(100);
			}
			double num = LeadMs();
			Log("Тест пинга: " + PingText(num) + ".");
			Step(6, (nRtt > 0) ? StepState.Done : StepState.Error, (nRtt > 0) ? $"{medRtt:0} мс {rttSrc} → −{num:0.0} мс" : "пинг не известен");
			Step(7, StepState.Skipped, "тест — не нажимаю");
			Log("ТЕСТ: кнопку «Запланировать атаку» НЕ нажимаю. Форму можно закрыть стрелкой «назад».");
			if (j.Prepare)
			{
				Log(PrepareSummary());
			}
			Status("testok", j.Prepare ? ("окно " + g.SizeText + " подготовлено, кнопка не нажата") : "тест завершён: всё заполнено, кнопка не нажата");
			return;
		}
		Status("ready", "всё заполнено — жду момент");
		if (j.TurnWaited && !TurnCaptTime(j))
		{
			if (Stop)
			{
				Stopped();
			}
			return;
		}
		DateTime dateTime = j.FireUtc.AddMilliseconds(0.0 - LeadMs() + (double)j.FineMs);
		while (Clock.UtcNow() < dateTime.AddMilliseconds(-400.0))
		{
			if (Stop)
			{
				Stopped();
				return;
			}
			SamplePing();
			dateTime = j.FireUtc.AddMilliseconds(0.0 - LeadMs() + (double)j.FineMs);
			double totalMilliseconds = (dateTime.AddMilliseconds(-400.0) - Clock.UtcNow()).TotalMilliseconds;
			if (totalMilliseconds > 0.0)
			{
				Thread.Sleep((int)Math.Min(100.0, totalMilliseconds));
			}
		}
		if (Turn.Held && (!WaitTurn(j) || !TurnCaptTime(j)))
		{
			if (Stop)
			{
				Stopped();
			}
			return;
		}
		double num2 = LeadMs();
		dateTime = j.FireUtc.AddMilliseconds(0.0 - num2 + (double)j.FineMs);
		if (j.TurnWaited)
		{
			dateTime = NotPast(dateTime, j);
		}
		if (nRtt == 0 && j.AutoPing)
		{
			Log("Внимание: пинг не известен (панель FPS/RTT не видна, трафик игры не читается) — нажимаю по ручной подстройке.");
		}
		Log("Опережение зафиксировано: " + PingText(num2) + ((j.FineMs != 0) ? (", подстройка " + j.FineMs + " мс") : "") + ". Нажатие в " + Clock.Fmt(dateTime) + ".");
		Step(6, StepState.Done, (nRtt > 0) ? $"{medRtt:0} мс {rttSrc} → −{num2:0.0} мс" : "без пинга");
		DateTime dateTime2;
		while (true)
		{
			Step(7, StepState.Running, "");
			if (Native.GetForegroundWindow() != g.H)
			{
				Log("Игра не на переднем плане — возвращаю.");
				g.Activate();
			}
			if (!j.Watch && g.Detect() != Scr.Form)
			{
				Log("Внимание: форма атаки не видна на экране!");
			}
			Input.MoveTo(point.X, point.Y);
			ThreadPriority priority = Thread.CurrentThread.Priority;
			bool flag = false;
			bool flag2 = false;
			try
			{
				Thread.CurrentThread.Priority = ThreadPriority.Highest;
			}
			catch
			{
			}
			try
			{
				flag = GC.TryStartNoGCRegion(16777216L);
			}
			catch
			{
				flag = false;
			}
			dateTime2 = DateTime.MinValue;
			try
			{
				if (!WaitUntil(dateTime.AddMilliseconds(-j.HoldMs)))
				{
					Stopped();
					return;
				}
				flag2 = Turn.Held;
				if (!flag2)
				{
					Input.Down();
					WaitUntil(dateTime);
					Input.Up();
					dateTime2 = Clock.UtcNow();
				}
			}
			finally
			{
				if (flag)
				{
					try
					{
						GC.EndNoGCRegion();
					}
					catch
					{
					}
				}
				try
				{
					Thread.CurrentThread.Priority = priority;
				}
				catch
				{
				}
			}
			if (!flag2)
			{
				break;
			}
			if (!WaitTurn(j) || !TurnCaptTime(j))
			{
				if (Stop)
				{
					Stopped();
				}
				return;
			}
			dateTime = NotPast(dateTime, j);
		}
		Turn.Fired();
		NetWatch.MarkClick(dateTime2, "Запланировать атаку");
		if (NetWatch.Running)
		{
			ConfirmLater(j, dateTime2);
		}
		else
		{
			Report(j, "unknown", "трафик игры не читается");
		}
		double totalMilliseconds2 = (dateTime2 - dateTime).TotalMilliseconds;
		Log($"★ НАЖАТО в {Clock.Fmt(dateTime2)} МСК (план {Clock.Fmt(dateTime)}, отклонение {totalMilliseconds2:+0.00;-0.00;0} мс). Расчётный приход на сервер ≈ {Clock.Fmt(dateTime2.AddMilliseconds(num2))}.");
		Step(7, StepState.Done, Clock.Fmt(dateTime2) + $" ({totalMilliseconds2:+0.0;-0.0;0} мс)");
		Status("done", "нажато в " + Clock.Fmt(dateTime2));
		Action<Job, DateTime> attackPlaced = Runner.AttackPlaced;
		if (attackPlaced != null)
		{
			try
			{
				attackPlaced(j, dateTime2.AddMilliseconds(num2));
			}
			catch
			{
			}
		}
		Thread.Sleep(1500);
		SaveShot("после_нажатия");
	}

	private static DateTime NotPast(DateTime clickUp, Job j)
	{
		DateTime dateTime = Clock.UtcNow().AddMilliseconds(j.HoldMs + 60);
		if (!(clickUp < dateTime))
		{
			return clickUp;
		}
		return dateTime;
	}

	private static string Sec(Stopwatch sw)
	{
		return ((double)sw.ElapsedMilliseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + " с";
	}

	private static bool IsAdmin()
	{
		return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
	}

	public static ProbeResult Probe(int[] me, bool wantFamily)
	{
		ProbeResult probeResult = new ProbeResult();
		Game game = new Game();
		List<string> notes = new List<string>();
		game.Note = delegate(string m)
		{
			notes.Add(m);
		};
		probeResult.Found = game.Find();
		if (!probeResult.Found)
		{
			return probeResult;
		}
		game.Refresh();
		probeResult.Size = game.SizeText;
		probeResult.W = game.Client.Width;
		probeResult.H = game.Client.Height;
		probeResult.Admin = game.Elevated() && !IsAdmin();
		if (Native.IsIconic(game.H) || game.Client.Width < 200 || game.Client.Height < 200)
		{
			probeResult.Tablet = "игра свёрнута";
			probeResult.Calibrated = game.Calibrated;
			return probeResult;
		}
		Frame f;
		try
		{
			f = game.Shot();
		}
		catch (ScreenUnavailableException)
		{
			probeResult.Tablet = "снимок экрана сейчас недоступен";
			probeResult.Calibrated = game.Calibrated;
			probeResult.Unavailable = true;
			return probeResult;
		}
		probeResult.Black = Game.IsBlack(f);
		Scr scr = Scr.Closed;
		bool flag = false;
		if (probeResult.Black)
		{
			probeResult.Tablet = "снимок окна чёрный";
		}
		else if (game.VisibleMain())
		{
			scr = game.Detect(f);
			flag = true;
			probeResult.Tablet = ScreenName(scr);
		}
		else
		{
			probeResult.Tablet = "не видно — игра перекрыта";
		}
		Rectangle pingRect = game.PingRect;
		int num = game.Client.X + pingRect.X;
		int num2 = game.Client.Y + pingRect.Y;
		int num3 = num + pingRect.Width;
		int num4 = num2 + pingRect.Height;
		if ((me != null && me[0] < num3 && me[2] > num && me[1] < num4 && me[3] > num2) || !game.VisibleScreen(num + pingRect.Width / 4, num4 - pingRect.Height / 6) || !game.VisibleScreen(num + pingRect.Width * 3 / 4, num2 + pingRect.Height / 10))
		{
			probeResult.Covered = true;
		}
		else if (!probeResult.Black && game.LocatePing(f))
		{
			PingOcr.ReadPanel(f, game.Cal.PingX, game.Cal.PingY, game.Cal.PingScale, out probeResult.Rtt, out probeResult.Fps);
		}
		if (wantFamily && flag && (scr == Scr.Home || scr == Scr.SkyeMenu))
		{
			try
			{
				probeResult.Family = Family.Read(f, game.L, scr);
			}
			catch (Exception ex2)
			{
				probeResult.Family = new FamilyInfo();
				probeResult.Family.Error = ex2.Message;
			}
		}
		probeResult.Calibrated = game.Calibrated;
		probeResult.PingKnown = game.PingKnown;
		probeResult.Model = (game.Calibrated ? game.L.Model : null);
		probeResult.Note = ((notes.Count > 0) ? string.Join(" ", notes.ToArray()) : null);
		return probeResult;
	}

	public static bool IsCaptMarker(Frame f)
	{
		return MarkerState(f, new Rectangle(0, 0, f.W, f.H)) != 0;
	}

	public static int MarkerState(Frame f, Rectangle r)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = r.X + r.Width * 12 / 100;
		int num5 = r.Right - r.Width * 12 / 100;
		int num6 = r.Y + r.Height * 12 / 100;
		int num7 = r.Bottom - r.Height * 12 / 100;
		for (int i = num6; i < num7; i++)
		{
			for (int j = num4; j < num5; j++)
			{
				int c = f.At(j, i);
				int num8 = Frame.R(c);
				int num9 = Frame.G(c);
				int num10 = Frame.B(c);
				num3++;
				if (num8 >= 130 && num8 <= 195 && num9 >= 22 && num9 <= 70 && num10 >= 22 && num10 <= 70 && num8 - num9 >= 85)
				{
					num++;
				}
				if (num8 > 210 && num9 > 210 && num10 > 210)
				{
					num2++;
				}
			}
		}
		if (num3 == 0 || num * 100 < num3 * 30 || num2 < 3)
		{
			return 0;
		}
		int num11 = 0;
		int num12 = 0;
		int num13 = r.Y + r.Height * 70 / 100;
		int num14 = r.Bottom - Math.Max(1, r.Height * 6 / 100);
		int num15 = r.X + r.Width * 15 / 100;
		int num16 = r.Right - r.Width * 15 / 100;
		for (int k = num13; k < num14; k++)
		{
			for (int l = num15; l < num16; l++)
			{
				int c2 = f.At(l, k);
				int num17 = Frame.R(c2);
				int num18 = Frame.G(c2);
				int num19 = Frame.B(c2);
				num12++;
				if (num17 >= 45 && num17 <= 110 && num18 <= 45 && num19 <= 45 && num17 - num18 >= 30)
				{
					num11++;
				}
			}
		}
		if (num12 <= 0 || num11 * 100 < num12 * 30)
		{
			return 2;
		}
		return 1;
	}

	private static MapScan ScanMap(Frame f, double x0, double y0, double p, double vt, double vb, DateTime at)
	{
		MapScan mapScan = new MapScan();
		mapScan.At = at;
		mapScan.R0 = 0;
		mapScan.R1 = -1;
		MapScan mapScan2 = mapScan;
		for (int i = 1; i <= 84; i++)
		{
			double num = y0 + (double)(i - 1) * p;
			if (!(num < vt + 1.0) && !(num + p > vb - 1.0))
			{
				if (mapScan2.R0 == 0)
				{
					mapScan2.R0 = i;
				}
				mapScan2.R1 = i;
			}
		}
		if (mapScan2.R0 == 0 || mapScan2.R1 - mapScan2.R0 < 3)
		{
			return null;
		}
		foreach (Zone item in Zones.All)
		{
			if (item.R0 < mapScan2.R0 || item.R1 > mapScan2.R1)
			{
				continue;
			}
			Rectangle r = new Rectangle((int)Math.Round(x0 + (double)(item.C0 - 1) * p), (int)Math.Round(y0 + (double)(item.R0 - 1) * p), (int)Math.Round((double)(item.C1 - item.C0 + 1) * p), (int)Math.Round((double)(item.R1 - item.R0 + 1) * p));
			if (r.Width >= 6 && r.Height >= 6 && r.X >= 0 && r.Y >= 0 && r.Right <= f.W && r.Bottom <= f.H)
			{
				int num2 = MarkerState(f, r);
				if (num2 != 0)
				{
					mapScan2.Marks.Add(new int[2] { item.Id, num2 });
				}
			}
		}
		return mapScan2;
	}

	private void ScanLater(Frame view0, Rectangle skip, double x0, double y0, double p, double vt, double vb, Frame ready)
	{
		if (!ShareMarkers || scanning)
		{
			return;
		}
		scanning = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				DateTime at = Clock.UtcNow();
				Frame frame = ready ?? g.Shot();
				if (SameView(view0, frame, skip))
				{
					MapScan mapScan = ScanMap(frame, x0, y0, p, vt, vb, at);
					if (mapScan != null)
					{
						Scanned(mapScan);
					}
				}
			}
			catch
			{
			}
			finally
			{
				scanning = false;
			}
		});
	}

	private bool ShowCell(int col, int row, out Point pt, out double p)
	{
		pt = Point.Empty;
		p = 0.0;
		Job job = new Job();
		job.Col = col;
		job.Row = row;
		job.Square = "";
		Job j = job;
		bool flag = row <= 45;
		watchZoomed = false;
		bool flag2 = CalibrateByLabels(flag);
		if (Stop)
		{
			return false;
		}
		if (!flag2 && WinOcr.Available && !g.Native1280)
		{
			ZoomOutOnly();
			Zone zone = Zones.At(col, row);
			if (BringIntoView(col, row, (zone != null) ? zone.Label : "квадрат", Point.Empty, out var grid, out var s))
			{
				watchZoomed = true;
				p = grid.P;
				pt = new Point(g.Client.X + (int)Math.Round(s.X), g.Client.Y + (int)Math.Round(s.Y));
				Log(string.Format(CultureInfo.InvariantCulture, "Слежка: квадрат найден по подписям на приближенной карте (квадрат {0:0} px).", p));
				return true;
			}
			if (Stop)
			{
				return false;
			}
			Log("Слежка: по подписям квадрат не нашёлся — беру расчёт.");
		}
		if (!NormalizeMap(flag))
		{
			if (!Stop)
			{
				Fail("карта не реагирует на колёсико/перетаскивание");
			}
			return false;
		}
		if (!Target(j, flag, out var tx, out var ty, out var note))
		{
			flag = !flag;
			if (!NormalizeMap(flag))
			{
				if (!Stop)
				{
					Fail("карта не реагирует на колёсико/перетаскивание");
				}
				return false;
			}
			if (!Target(j, flag, out tx, out ty, out note))
			{
				if (flag)
				{
					if (!NormalizeMap(top: false))
					{
						if (!Stop)
						{
							Fail("карта не реагирует на колёсико/перетаскивание");
						}
						return false;
					}
					Target(j, top: false, out tx, out ty, out note);
				}
				CalibEntry cal = g.Cal;
				double num = ((cal != null && cal.MapP > 0.0) ? cal.MapP : (15.93 * g.L.Sx));
				double num2 = ((cal != null && cal.MapTop > 0.0) ? cal.MapTop : g.L.Y(115.0));
				int num3 = (int)Math.Round(num2 + 6.0 * num - ty);
				Point point = g.P(737.0, 300.0);
				DragExact(point.X, point.Y, point.Y + num3);
				if (!Sleep(250))
				{
					return false;
				}
				ty += (double)num3;
			}
		}
		CalibEntry cal2 = g.Cal;
		p = ((cal2 != null && cal2.MapP > 0.0) ? cal2.MapP : (15.93 * g.L.Sx));
		pt = new Point(g.Client.X + (int)Math.Round(tx), g.Client.Y + (int)Math.Round(ty));
		return true;
	}

	private static bool SameView(Frame a, Frame b, Rectangle skip)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 1; i < 12; i++)
		{
			for (int j = 1; j < 10; j++)
			{
				int x = a.W * i / 12;
				int y = a.H * j / 10;
				if (!skip.Contains(x, y))
				{
					num2++;
					int c = a.At(x, y);
					int c2 = b.At(x, y);
					if (Math.Abs(Frame.R(c) - Frame.R(c2)) + Math.Abs(Frame.G(c) - Frame.G(c2)) + Math.Abs(Frame.B(c) - Frame.B(c2)) < 40)
					{
						num++;
					}
				}
			}
		}
		if (num2 > 0)
		{
			return num * 100 >= num2 * 80;
		}
		return false;
	}

	private static DateTime Ceil5(DateTime t)
	{
		DateTime dateTime = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0);
		if (dateTime < t)
		{
			dateTime = dateTime.AddMinutes(1.0);
		}
		int num = dateTime.Minute % 5;
		if (num != 0)
		{
			return dateTime.AddMinutes(5 - num);
		}
		return dateTime;
	}

	private static DateTime Floor5(DateTime t)
	{
		DateTime dateTime = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, 0);
		return dateTime.AddMinutes(-(dateTime.Minute % 5));
	}

	public static DateTime CaptWindow(DateTime nowMsk, DateTime baseMsk, int afterMin, double fillSec)
	{
		DateTime dateTime = Ceil5(baseMsk.AddMinutes(afterMin));
		DateTime dateTime2 = Ceil5(nowMsk.AddSeconds(fillSec).AddMinutes(15.0));
		DateTime dateTime3 = Floor5(nowMsk.AddMinutes(60.0));
		DateTime dateTime4 = ((dateTime < dateTime2) ? dateTime2 : ((dateTime > dateTime3) ? dateTime3 : dateTime));
		DateTime dateTime5 = dateTime4.Date.Add(Asap.DayFrom);
		if (dateTime4 < dateTime5 && dateTime5 <= dateTime3)
		{
			dateTime4 = dateTime5;
		}
		return dateTime4;
	}

	private void SetCaptTime(Job j, DateTime endUtc, double fillSec)
	{
		DateTime dateTime = Clock.UtcNow();
		DateTime utc = endUtc;
		if (dateTime - endUtc > TimeSpan.FromMinutes(2.0))
		{
			Log("Внимание: о конце капта узнал с опозданием на " + (int)(dateTime - endUtc).TotalMinutes + " мин — время капта считаю от текущего момента.");
			utc = dateTime;
		}
		DateTime dateTime2 = Ceil5(Clock.Msk(utc).AddMinutes(j.AfterMin));
		DateTime dateTime3 = CaptWindow(Clock.Msk(dateTime), Clock.Msk(utc), j.AfterMin, fillSec);
		if (dateTime3 != dateTime2)
		{
			Log("Капт на " + dateTime2.ToString("HH:mm") + " игра не примет (" + ((dateTime3.TimeOfDay == Asap.DayFrom && dateTime2 < dateTime3) ? "капты — с 15:00" : ("можно только +" + 15 + "…+" + 60 + " мин от забива")) + ") — ставлю " + dateTime3.ToString("HH:mm") + ".");
		}
		j.CaptH = dateTime3.Hour;
		j.CaptM = dateTime3.Minute;
	}

	private bool EnsureCaptWindow(Job j)
	{
		DateTime dateTime = Clock.Msk(Clock.UtcNow()).AddMilliseconds(LeadMs() + 1500.0);
		DateTime dateTime2 = dateTime.Date.AddHours(j.CaptH).AddMinutes(j.CaptM);
		if (dateTime2 < dateTime.AddHours(-12.0))
		{
			dateTime2 = dateTime2.AddDays(1.0);
		}
		if (dateTime2 - dateTime >= TimeSpan.FromMinutes(15.0))
		{
			return true;
		}
		DateTime dateTime3 = Ceil5(dateTime.AddMinutes(15.0));
		Log("Капт на " + dateTime2.ToString("HH:mm") + " уже ближе +" + 15 + " мин от забива — переставляю на " + dateTime3.ToString("HH:mm") + ".");
		if (dateTime3.Hour != j.CaptH && !SetValue("Часы", dateTime3.Hour, hours: true))
		{
			return false;
		}
		if (!SetValue("Минуты", dateTime3.Minute, hours: false))
		{
			return false;
		}
		j.CaptH = dateTime3.Hour;
		j.CaptM = dateTime3.Minute;
		return true;
	}

	private string AsapOwn(Job j)
	{
		if (string.IsNullOrEmpty(NetWatch.OwnFamily))
		{
			return j.Family;
		}
		return NetWatch.OwnFamily;
	}

	private AsapVerdict AsapNow(Job j, Zone tz)
	{
		List<long[]> seenEnds;
		lock (asapSeenEnds)
		{
			seenEnds = new List<long[]>(asapSeenEnds);
		}
		return Asap.Evaluate(asapSt, tz, AsapOwn(j), j.AfterMin, War.NowUnix(), seenEnds);
	}

	private void StartAsapFeed(string server)
	{
		asapFeedStop = false;
		Thread thread = new Thread((ThreadStart)delegate
		{
			WarState warState = null;
			bool flag = false;
			while (!asapFeedStop && !Stop)
			{
				string error = null;
				WarState warState2 = null;
				bool flag2 = warState != null && !string.IsNullOrEmpty(warState.StateVer);
				try
				{
					warState2 = War.Load(server, warState, flag2 ? 20 : 0, out error);
				}
				catch (Exception ex)
				{
					error = ex.Message;
				}
				if (warState2 == null)
				{
					if (!flag)
					{
						Log("Карта TTC не отвечает: " + error + " — решаю по своей игре и пробую снова.");
						flag = true;
					}
					FeedSleep(3000);
				}
				else
				{
					if (flag)
					{
						Log("Карта TTC снова отвечает.");
						flag = false;
					}
					warState = warState2;
					asapSt = warState2;
					if (!flag2)
					{
						FeedSleep(4000);
					}
				}
			}
		});
		thread.IsBackground = true;
		thread.Name = "TTC asap feed";
		Thread thread2 = thread;
		thread2.Start();
	}

	private void FeedSleep(int ms)
	{
		for (int i = 0; i < ms; i += 100)
		{
			if (asapFeedStop)
			{
				break;
			}
			if (Stop)
			{
				break;
			}
			Thread.Sleep(100);
		}
	}

	private void RunAsap(Job j)
	{
		Zone zone = Zones.At(j.Col, j.Row);
		if (zone == null)
		{
			Fail("квадрат " + j.Square + " не найден на карте");
			return;
		}
		asapSt = null;
		lock (asapSeenEnds)
		{
			asapSeenEnds.Clear();
		}
		if (!string.IsNullOrEmpty(j.Server))
		{
			StartAsapFeed(j.Server);
		}
		else
		{
			Log("Сервер вверху окна не выбран — карта TTC не участвует, решаю только по своей игре.");
		}
		try
		{
			if (NetWatch.Running && NetWatch.GameRunning && NetWatch.LastFullUtc == DateTime.MinValue && !SyncGameMap(j))
			{
				if (Stop)
				{
					Stopped();
				}
				return;
			}
			for (int i = 0; i < 30; i++)
			{
				if (asapSt != null)
				{
					break;
				}
				if (string.IsNullOrEmpty(j.Server))
				{
					break;
				}
				if (!Sleep(100))
				{
					Stopped();
					return;
				}
			}
			while (true)
			{
				if (!AsapWait(j, zone, out var v))
				{
					if (Stop)
					{
						Stopped();
					}
					break;
				}
				if (v.Level == 2)
				{
					AsapBlocked(j, v);
					break;
				}
				if (AsapStrike(j, zone, v) != 0)
				{
					break;
				}
				AsapVerdict asapVerdict = AsapNow(j, zone);
				if (asapVerdict.Level == 1 && asapVerdict.PressAt - 75 > War.NowUnix() + 120)
				{
					Log("До следующего возможного момента (" + War.Hms(asapVerdict.PressAt) + ") больше двух минут — закрываю форму, планшет свободен.");
					ReleaseTablet();
					for (int k = 0; k < StepNames.Length; k++)
					{
						Step(k, StepState.Pending, "");
					}
				}
			}
		}
		finally
		{
			asapFeedStop = true;
		}
	}

	private bool SyncGameMap(Job j)
	{
		if (!g.Find())
		{
			Log("Окно игры не найдено — всю карту каптов из игры взять не вышло, решаю по карте TTC.");
			return true;
		}
		g.Refresh();
		if (g.Client.Width < 640)
		{
			Log("Окно игры свёрнуто — всю карту каптов из игры взять не вышло, решаю по карте TTC.");
			return true;
		}
		Log("Из игры ещё не пришла вся карта каптов — один раз открываю карту территорий в планшете (сервер игры присылает её при открытии). Пару секунд окно игры не трогайте.");
		Status("prep", "открываю карту каптов в планшете");
		if (!g.Activate())
		{
			Log("Внимание: не удалось вывести игру на передний план.");
		}
		if (!Sleep(150))
		{
			return false;
		}
		bool flag = g.Detect() == Scr.Closed;
		Stopwatch sw = Stopwatch.StartNew();
		if (!OpenToMapOrForm(j, sw, allowReuse: false, out var _))
		{
			return false;
		}
		Stopwatch stopwatch = Stopwatch.StartNew();
		while (NetWatch.LastFullUtc == DateTime.MinValue && stopwatch.ElapsedMilliseconds < 6000)
		{
			if (!Sleep(100))
			{
				return false;
			}
		}
		if (NetWatch.LastFullUtc != DateTime.MinValue)
		{
			Log("Из игры: вся карта каптов есть — дальше конец каптов приходит событиями игры, планшет свободен.");
		}
		else
		{
			Log("Вся карта каптов из игры не пришла (её присылают один раз за вход в игру — видимо, карту открывали до запуска TTC). Новые капты и их конец TTC видит событиями игры; капт, которого нет в данных игры, у самого конца бот проверит по экрану.");
		}
		if (flag)
		{
			Input.Tap(80, ext: true, 40);
			WaitScreen(Scr.Closed, 2500);
		}
		for (int i = 0; i < StepNames.Length; i++)
		{
			Step(i, StepState.Pending, "");
		}
		return !Stop;
	}

	private void ReleaseTablet()
	{
		try
		{
			if (g.Detect() == Scr.Form)
			{
				ClickB(85.0, 113.0);
				WaitScreen(Scr.Map, 2500);
				Sleep(300);
			}
			if (g.Detect() != Scr.Closed)
			{
				Input.Tap(80, ext: true, 40);
				WaitScreen(Scr.Closed, 2500);
			}
		}
		catch
		{
		}
	}

	private bool AsapWait(Job j, Zone tz, out AsapVerdict v)
	{
		string text = null;
		DateTime dateTime = DateTime.UtcNow;
		do
		{
			v = AsapNow(j, tz);
			if (Stop)
			{
				return false;
			}
			AsapUpdate(v, "plan");
			if (v.Level != 1)
			{
				return true;
			}
			long num = v.PressAt - 75;
			string text2 = v.Headline + "|" + v.Detail + "|" + v.PressAt;
			if (text2 != text)
			{
				Log(v.Headline + ". " + v.Detail + " " + v.Plan);
				Status("watch", v.Detail);
				Step(0, StepState.Running, "свободен до " + War.Hms(Math.Max(num, War.NowUnix())));
				text = text2;
			}
			if (War.NowUnix() >= num)
			{
				return true;
			}
			if (g.H != IntPtr.Zero && DateTime.UtcNow >= dateTime)
			{
				dateTime = DateTime.UtcNow.AddSeconds(1.0);
				SamplePing();
			}
		}
		while (Sleep(250));
		return false;
	}

	private void AsapBlocked(Job j, AsapVerdict v)
	{
		Log(v.Headline + ": " + v.Detail + " " + v.Plan);
		Status("blocked", v.Detail);
		Step((cur >= 0) ? cur : 0, StepState.Skipped, "сегодня нельзя");
		AsapUpdate(v, "blocked");
	}

	private int AsapStrike(Job j, Zone tz, AsapVerdict v)
	{
		if (!g.Find())
		{
			Fail("окно GTA 5 не найдено");
			return 1;
		}
		g.Refresh();
		if (g.Client.Width < 640)
		{
			Fail("окно игры свёрнуто или слишком маленькое");
			return 1;
		}
		Log("Окно игры " + g.SizeText + (g.Calibrated ? string.Concat(", раскладка: ", g.L, ".") : ", калибровки для этого размера ещё нет — подберу по планшету."));
		Stopwatch sw = Stopwatch.StartNew();
		Status(j.DryRun ? "test" : "prep", "подготовка");
		if (!g.Activate())
		{
			Log("Внимание: не удалось вывести игру на передний план.");
		}
		if (!Sleep(150))
		{
			Stopped();
			return 1;
		}
		if (v.Level == 1 && v.Waiting.Count > 0 && v.ScreenWatch)
		{
			return AsapByScreen(j, tz, v, sw);
		}
		if (v.Level == 1)
		{
			Log(((v.Waiting.Count > 0) ? ("Капт на " + Asap.Squares(v.Waiting) + " может кончиться уже в ") : "Нажимать в ") + War.Hms(v.PressAt) + " — открываю форму атаки на " + j.Square + " и заполняю заранее.");
		}
		if (!OpenToForm(j, sw, reuse: true))
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		string text = g.RefineOnForm(g.Shot());
		if (text != null)
		{
			Log(text);
		}
		DateTime dateTime = ((v.Level == 1) ? War.FromUnix(Math.Max(v.PressAt, War.NowUnix())) : Clock.UtcNow());
		DateTime dateTime2 = Asap.CaptAt(dateTime, dateTime, j.AfterMin, (v.Level == 1) ? 2 : 3);
		j.CaptH = dateTime2.Hour;
		j.CaptM = dateTime2.Minute;
		if (v.Level == 1)
		{
			Log("Предварительно ставлю капт на " + dateTime2.ToString("HH:mm") + " (если нажму в " + Clock.Msk(dateTime).ToString("HH:mm:ss") + "); по факту поправлю время.");
		}
		if (!FillForm(j, sw))
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		AsapVerdict v2;
		long freedAt;
		string src;
		int num = AsapHold(j, tz, v, sw, out v2, out freedAt, out src);
		if (num < 0)
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		switch (num)
		{
		case 2:
			AsapBlocked(j, v2);
			return 1;
		case 0:
			return 0;
		default:
			AsapFire(j, v2, freedAt, src, sw);
			return 1;
		}
	}

	private int AsapHold(Job j, Zone tz, AsapVerdict v0, Stopwatch sw, out AsapVerdict v, out long freedAt, out string src)
	{
		v = v0;
		freedAt = 0L;
		src = null;
		if (v0.Level != 1)
		{
			return 1;
		}
		List<AsapCapt> waited = new List<AsapCapt>(v0.Waiting);
		string text = ((waited.Count > 0) ? ("конец капта на " + Asap.Squares(waited)) : ("нажатие в " + War.Hms(v0.PressAt)));
		Status("ready", "форма наготове — жду " + text);
		Step(6, StepState.Running, "форма наготове, жду " + text);
		Log("Форма наготове: жду " + text + ((waited.Count == 0) ? "." : (Asap.NetEvents ? " (событие игры приходит сразу)." : " (по карте TTC).")));
		AsapUpdate(v0, (waited.Count > 0) ? "form" : "timed");
		DateTime dateTime = DateTime.UtcNow.AddSeconds(1.0);
		DateTime dateTime2 = DateTime.UtcNow;
		DateTime dateTime3 = DateTime.MinValue;
		WarState warState = asapSt;
		int num = 0;
		bool flag = false;
		while (true)
		{
			if (Stop)
			{
				return -1;
			}
			bool flag2 = false;
			foreach (AsapCapt item in waited)
			{
				if (NetWatch.LastEnded(item.Zone, item.Start) != null)
				{
					flag2 = true;
					break;
				}
			}
			WarState warState2 = asapSt;
			if (flag2 || warState2 != warState || DateTime.UtcNow >= dateTime3)
			{
				warState = warState2;
				dateTime3 = DateTime.UtcNow.AddMilliseconds(250.0);
				v = AsapNow(j, tz);
				long num2 = War.NowUnix();
				if (v.Level == 2)
				{
					return 2;
				}
				if (waited.Count > 0)
				{
					AsapVerdict asapVerdict = v;
					if (!asapVerdict.Waiting.Any((AsapCapt c) => waited.Any((AsapCapt o) => Asap.Same(o, c))))
					{
						freedAt = 0L;
						src = null;
						foreach (AsapCapt o2 in waited)
						{
							List<AsapCapt> capts = asapVerdict.Capts;
							Func<AsapCapt, bool> predicate = (AsapCapt c) => c.Ended && Asap.Same(o2, c);
							AsapCapt asapCapt = (from c in capts.Where(predicate)
								orderby c.Exact descending, c.Own descending
								select c).FirstOrDefault();
							long num3 = ((asapCapt != null) ? Math.Min(asapCapt.EndHi, num2) : num2);
							if (num3 >= freedAt)
							{
								freedAt = num3;
								src = ((asapCapt == null) ? "капта больше нет в данных" : (asapCapt.Own ? "из своей игры" : ((!asapCapt.Exact) ? "по истории каптов, примерно" : (asapCapt.FromGame ? "из игры, по карте TTC" : "по карте TTC"))));
							}
						}
						if (asapVerdict.Waiting.Count > 0)
						{
							Log("Капт на " + Asap.Squares(waited) + " кончился (" + src + "), но цель снова занята: " + asapVerdict.Detail);
							return 0;
						}
						waited.Clear();
						if (asapVerdict.Level == 1)
						{
							Log("Капт кончился (" + src + "). " + asapVerdict.Detail + " " + asapVerdict.Plan);
							Status("ready", "форма наготове — нажатие в " + War.Hms(asapVerdict.PressAt));
							AsapUpdate(asapVerdict, "timed");
						}
					}
					else
					{
						AsapUpdate(v, "form");
						long num4 = waited.Max((AsapCapt c) => c.EndHi);
						if (!flag && num2 > num4 + 45)
						{
							Log("Капт на " + Asap.Squares(waited) + " должен был кончиться к " + War.Hm(num4) + ", а конца нет ни в игре, ни на карте TTC — видимо, его перенесли. Жду дальше.");
							flag = true;
						}
					}
				}
				if (waited.Count == 0)
				{
					if (v.Waiting.Count > 0)
					{
						Log("Цель снова занята: " + v.Detail);
						return 0;
					}
					if (v.Level != 1)
					{
						return 1;
					}
					if (num2 >= v.PressAt - 2)
					{
						return 1;
					}
				}
			}
			if (DateTime.UtcNow >= dateTime)
			{
				dateTime = DateTime.UtcNow.AddSeconds(1.0);
				if (g.Detect() != Scr.Form)
				{
					if (++num > 3)
					{
						Fail("форма атаки всё время закрывается — не получается держать её наготове");
						return -1;
					}
					Log("Форма атаки пропала с экрана — открываю и заполняю заново.");
					if (!OpenToForm(j, sw, reuse: true) || !FillForm(j, sw))
					{
						break;
					}
					Status("ready", "форма наготове — жду " + text);
				}
			}
			if (DateTime.UtcNow >= dateTime2)
			{
				dateTime2 = DateTime.UtcNow.AddMilliseconds(500.0);
				SamplePing();
			}
			Thread.Sleep(5);
		}
		return -1;
	}

	private void AsapFire(Job j, AsapVerdict v, long freedAt, string src, Stopwatch sw)
	{
		int captH = j.CaptH;
		int captM = j.CaptM;
		string text;
		if (v.Level == 1 && v.Exact && v.PressAt > War.NowUnix())
		{
			j.FireUtc = War.FromUnix(v.PressAt);
			DateTime dateTime = Asap.CaptAt(j.FireUtc, j.FireUtc, j.AfterMin, 2.0);
			j.CaptH = dateTime.Hour;
			j.CaptM = dateTime.Minute;
			Log("★ Забив ко времени: " + v.Detail + " Нажатие в " + Clock.Fmt(j.FireUtc) + ", капт на " + dateTime.ToString("HH:mm") + ".");
			text = "timed";
		}
		else
		{
			DateTime dateTime2 = Clock.UtcNow();
			DateTime dateTime3 = ((freedAt > 0) ? War.FromUnix(freedAt) : dateTime2);
			if (freedAt > 0)
			{
				CaptEnded(dateTime3);
			}
			SetCaptTime(j, dateTime3, 3.0);
			DateTime dateTime4 = Asap.AtLeast(new DateTime(2000, 1, 1, j.CaptH, j.CaptM, 0), v.CaptFrom, dateTime2);
			j.CaptH = dateTime4.Hour;
			j.CaptM = dateTime4.Minute;
			j.FireUtc = Clock.UtcNow();
			Log(string.Format("★ {0}Ставлю атаку на {1}, капт на {2:00}:{3:00}.", (freedAt > 0) ? ("Цель свободна с " + Clock.Msk(dateTime3).ToString("HH:mm:ss") + " МСК (" + src + "). ") : "", j.Square, j.CaptH, j.CaptM));
			text = "ended";
		}
		if (!Asap.CaptTimeOk(j.CaptH, j.CaptM))
		{
			Fail("капты ставят с 15:00 до 21:59 МСК — на " + j.CaptH.ToString("00") + ":" + j.CaptM.ToString("00") + " игра капт не примет");
			return;
		}
		AsapUpdate(v, text);
		if (j.CaptH != captH || j.CaptM != captM)
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			if (!SetTimeFields(j, sw))
			{
				if (Stop)
				{
					Stopped();
				}
				return;
			}
			Log("Время капта поправлено за " + stopwatch.ElapsedMilliseconds + " мс.");
		}
		if (text == "ended" && !EnsureCaptWindow(j))
		{
			if (Stop)
			{
				Stopped();
			}
		}
		else
		{
			Fire(j);
		}
	}

	private int AsapByScreen(Job j, Zone tz, AsapVerdict v, Stopwatch sw)
	{
		AsapCapt mainCapt = v.MainCapt;
		Zone zone = ((mainCapt != null) ? Zones.ById(mainCapt.Zone) : null);
		if (zone == null)
		{
			Fail("зона капта не найдена на карте");
			return 1;
		}
		Log((NetWatch.Running ? ("Капта на " + zone.Label + " нет в данных своей игры") : ("Свою игру TTC не читает" + (NetWatch.IsAdmin ? "" : " (нужны права администратора)"))) + " — его конец смотрю по экрану, на карте в планшете (и по карте TTC). Окно игры не трогайте.");
		if (!OpenToMapOrForm(j, sw, allowReuse: false, out var _))
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		if (!WatchCapt(j, mainCapt, out var endAt, out var src))
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		lock (asapSeenEnds)
		{
			asapSeenEnds.Add(new long[2] { mainCapt.Zone, endAt });
		}
		AsapVerdict asapVerdict = AsapNow(j, tz);
		if (asapVerdict.Level == 2)
		{
			AsapBlocked(j, asapVerdict);
			return 1;
		}
		if (asapVerdict.Waiting.Count > 0)
		{
			Log("Капт на " + zone.Label + " кончился (" + src + "), но цель ещё занята: " + asapVerdict.Detail);
			return 0;
		}
		SetCaptTime(j, War.FromUnix(endAt), 20.0);
		Step(3, StepState.Running, j.Square);
		if (!OpenSquareForm(j))
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		Step(3, StepState.Done, j.Square + ", " + Sec(sw));
		if (!FillForm(j, sw))
		{
			if (Stop)
			{
				Stopped();
			}
			return 1;
		}
		AsapFire(j, asapVerdict, endAt, src, sw);
		return 1;
	}

	private bool WatchCapt(Job j, AsapCapt wc, out long endAt, out string src)
	{
		endAt = 0L;
		src = null;
		Zone zone = Zones.ById(wc.Zone);
		string text = (j.WatchSquare = zone.Label);
		Step(3, StepState.Running, "слежу за каптом на " + text);
		Status("watch", "слежу за каптом на " + text + " по экрану");
		if (!ShowCell(zone.C0, zone.R1, out var pt, out var p))
		{
			return false;
		}
		int width = (int)Math.Round((double)(zone.C1 - zone.C0 + 1) * p);
		int num = (int)Math.Round((double)(zone.R1 - zone.R0 + 1) * p);
		Rectangle r = new Rectangle((int)Math.Round((double)pt.X - p / 2.0), (int)Math.Round((double)pt.Y + p / 2.0) - num, width, num);
		Rectangle skip = new Rectangle(r.X - g.Client.X, r.Y - g.Client.Y, r.Width, r.Height);
		Point point = g.P(1240.0, 990.0);
		Input.MoveTo(point.X, point.Y);
		if (!Sleep(350))
		{
			return false;
		}
		Log(string.Format(CultureInfo.InvariantCulture, "Слежу за {0}: зона на экране {1}×{2} px. Атака на {3} — сразу после конца капта, капт через +{4} мин.", text, r.Width, r.Height, j.Square, j.AfterMin));
		Frame frame = g.Shot();
		double x = (double)(pt.X - g.Client.X) - ((double)zone.C0 - 0.5) * p;
		double y = (double)(pt.Y - g.Client.Y) - ((double)zone.R1 - 0.5) * p;
		CalibEntry cal = g.Cal;
		double vt = ((cal != null && cal.MapTop > 0.0) ? cal.MapTop : g.L.Y(115.0));
		double vb = ((cal != null && cal.MapBottom > 0.0) ? cal.MapBottom : g.L.Y(884.0));
		DateTime dateTime = DateTime.MinValue;
		DateTime dateTime2 = DateTime.MinValue;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		int num2 = 0;
		DateTime utc = DateTime.MinValue;
		DateTime utcNow = DateTime.UtcNow;
		DateTime utcNow2 = DateTime.UtcNow;
		long since = Math.Min(wc.Start, War.NowUnix() - 30);
		while (true)
		{
			if (Stop)
			{
				return false;
			}
			NetCapt netCapt = NetWatch.LastEnded(zone.Id, since);
			if (netCapt != null)
			{
				endAt = netCapt.End;
				src = "из своей игры";
				break;
			}
			if (DateTime.UtcNow >= dateTime2)
			{
				dateTime2 = DateTime.UtcNow.AddMilliseconds(300.0);
				string src2;
				long num3 = Asap.EndedOnData(asapSt, zone.Id, since, out src2);
				if (num3 > 0)
				{
					endAt = num3;
					src = src2;
					break;
				}
			}
			if (!g.VisibleScreen(r.X + r.Width / 2, r.Y + r.Height / 2))
			{
				if (!flag3)
				{
					Log("Квадрат " + text + " закрыт другим окном — пока не вижу его, жду.");
					flag3 = true;
				}
				num2 = 0;
				Thread.Sleep(200);
				continue;
			}
			flag3 = false;
			bool flag4;
			using (Bitmap b = g.Grab(r))
			{
				flag4 = IsCaptMarker(Frame.From(b));
			}
			DateTime dateTime3 = Clock.UtcNow();
			if (flag4)
			{
				if (!flag)
				{
					flag = true;
					Log("На " + text + " отметка капта — слежу. Нажму сразу, как она пропадёт.");
					Step(3, StepState.Running, "капт на " + text + " идёт или ждёт начала, слежу");
				}
				num2 = 0;
			}
			else if (!flag)
			{
				if (!flag2)
				{
					Log("На " + text + " пока нет отметки капта (красной клетки с огнём) — жду, когда она появится.");
					flag2 = true;
				}
				if ((DateTime.UtcNow - utcNow2).TotalSeconds >= 30.0 && War.NowUnix() >= wc.EndLo)
				{
					endAt = War.NowUnix();
					src = "по экрану: отметки капта нет";
					break;
				}
			}
			else
			{
				if (num2 == 0)
				{
					utc = dateTime3;
				}
				num2++;
				if (num2 >= 3)
				{
					Frame frame2 = g.Shot();
					Scr scr = g.Detect(frame2);
					if (scr != Scr.Map)
					{
						Fail("во время слежки планшет ушёл с карты (" + ScreenName(scr) + ") — конец капта не пойман");
						return false;
					}
					if (!SameView(frame, frame2, skip))
					{
						Log("Карта сдвинулась — навожу на " + text + " заново.");
						if (!ShowCell(zone.C0, zone.R1, out pt, out p))
						{
							return false;
						}
						Input.MoveTo(point.X, point.Y);
						if (!Sleep(300))
						{
							return false;
						}
						frame = g.Shot();
						num2 = 0;
						x = (double)(pt.X - g.Client.X) - ((double)zone.C0 - 0.5) * p;
						y = (double)(pt.Y - g.Client.Y) - ((double)zone.R1 - 0.5) * p;
						continue;
					}
					if (!watchZoomed)
					{
						ScanLater(frame, Rectangle.Empty, x, y, p, vt, vb, frame2);
					}
					endAt = War.Unix(utc);
					src = "по экрану";
					break;
				}
			}
			if (ShareMarkers && !watchZoomed && num2 == 0 && (DateTime.UtcNow - dateTime).TotalSeconds >= 3.0)
			{
				dateTime = DateTime.UtcNow;
				ScanLater(frame, skip, x, y, p, vt, vb, null);
			}
			if ((DateTime.UtcNow - utcNow).TotalSeconds >= 5.0)
			{
				utcNow = DateTime.UtcNow;
				Scr scr2 = g.Detect();
				if (scr2 != Scr.Map)
				{
					Fail("во время слежки планшет ушёл с карты (" + ScreenName(scr2) + ")");
					return false;
				}
			}
			Thread.Sleep((num2 > 0) ? 15 : 35);
		}
		Log(string.Format("Капт на {0} закончился в {1} МСК ({2}, следил {3:0} мин).", text, Clock.Msk(War.FromUnix(endAt)).ToString("HH:mm:ss"), src, (DateTime.UtcNow - utcNow2).TotalMinutes));
		Step(3, StepState.Running, j.Square);
		return true;
	}

	private bool ReadGrid(out MapGrid grid)
	{
		return ReadGrid(out grid, Point.Empty);
	}

	private bool ReadGrid(out MapGrid grid, Point focus)
	{
		grid = null;
		if (!WinOcr.Available)
		{
			if (!ocrWarned)
			{
				Log("Подписи квадратов не читаю: " + WinOcr.Error + ". Кликаю по расчёту.");
				ocrWarned = true;
			}
			return false;
		}
		Point point = g.P(1240.0, 990.0);
		Input.MoveTo(point.X, point.Y);
		if (!Sleep(160))
		{
			return false;
		}
		Rectangle rectangle = g.Rc(45.0, 75.0, 1190.0, 880.0);
		rectangle.Intersect(new Rectangle(0, 0, g.Client.Width, g.Client.Height));
		if (rectangle.Width < 100 || rectangle.Height < 100)
		{
			return false;
		}
		double scale = Math.Max(1.0, Math.Min(2.5, 2.0 / Math.Max(0.1, g.L.Sy)));
		if (!focus.IsEmpty)
		{
			int num = (int)Math.Round(260.0 * g.L.Sx);
			int num2 = (int)Math.Round(210.0 * g.L.Sy);
			Rectangle r = new Rectangle(focus.X - num, focus.Y - num2, 2 * num, 2 * num2);
			r.Intersect(rectangle);
			if (r.Width >= 100 && r.Height >= 100 && ReadPart(r, scale, out grid))
			{
				return true;
			}
		}
		return ReadPart(rectangle, scale, out grid);
	}

	private bool ReadPart(Rectangle r, double scale, out MapGrid grid)
	{
		Rectangle r2 = r;
		r2.Offset(g.Client.X, g.Client.Y);
		int words;
		using (Bitmap src = g.Grab(r2))
		{
			grid = MapLabels.Read(src, scale, out words);
		}
		if (ShotDir != null)
		{
			Log(string.Format(CultureInfo.InvariantCulture, "Отладка карты: подписи в области ({0}, {1}, {2}×{3}), ×{4:0.##}, слов {5}{6}.", r.X, r.Y, r.Width, r.Height, scale, words, (grid == null) ? ", сетка не сложилась" : string.Format(CultureInfo.InvariantCulture, ", сетка Ax={0:0.0} Ay={1:0.0} P={2:0.00} подписей {3}", grid.Ax + (double)r.X, grid.Ay + (double)r.Y, grid.P, grid.Labels)));
		}
		if (grid == null)
		{
			return false;
		}
		grid = grid.Shift(r.X, r.Y, 1.0);
		return true;
	}

	private void ShotMap(string what, Point client)
	{
		if (ShotDir == null)
		{
			return;
		}
		try
		{
			Directory.CreateDirectory(ShotDir);
			string text = Path.Combine(ShotDir, Clock.Msk(Clock.UtcNow()).ToString("yyyyMMdd_HHmmss") + "_" + (++mapShotN).ToString("00") + "_карта_" + what + ".png");
			using (Bitmap bitmap = g.GrabClient())
			{
				bitmap.Save(text, ImageFormat.Png);
			}
			Log(string.Format(CultureInfo.InvariantCulture, "Отладка карты [{0}]: точка ({1}, {2}) в окне {3}, снимок {4}", what, client.X, client.Y, g.SizeText, text));
		}
		catch
		{
		}
	}

	private bool WaitStable(int minMs, int maxMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		if (!Sleep(minMs))
		{
			return false;
		}
		Rectangle r = g.Rc(80.0, 175.0, 1120.0, 740.0);
		r.Intersect(new Rectangle(0, 0, g.Client.Width, g.Client.Height));
		if (r.Width < 20 || r.Height < 20)
		{
			return Sleep(Math.Max(0, maxMs - (int)stopwatch.ElapsedMilliseconds));
		}
		r.Offset(g.Client.X, g.Client.Y);
		long num = 0L;
		int num2 = 0;
		while (stopwatch.ElapsedMilliseconds < maxMs)
		{
			long num3 = 1469598103934665603L;
			using (Bitmap b = g.Grab(r))
			{
				Frame frame = Frame.From(b);
				for (int i = 0; i < frame.D.Length; i += 7)
				{
					num3 = (num3 ^ frame.D[i]) * 1099511628211L;
				}
			}
			if (num3 == num)
			{
				if (++num2 >= 2)
				{
					return true;
				}
			}
			else
			{
				num2 = 0;
			}
			num = num3;
			if (!Sleep(55))
			{
				return false;
			}
		}
		return !Stop;
	}

	private bool DragBy(int dx, int dy, Rectangle safe)
	{
		int num = Math.Max(40, (int)((double)safe.Width * 0.8));
		int num2 = Math.Max(40, (int)((double)safe.Height * 0.8));
		int num3 = 0;
		while ((dx != 0 || dy != 0) && num3 < 12)
		{
			int num4 = Math.Max(-num, Math.Min(num, dx));
			int num5 = Math.Max(-num2, Math.Min(num2, dy));
			int num6 = g.Client.X + safe.X + safe.Width / 2;
			int num7 = g.Client.Y + safe.Y + safe.Height / 2;
			int num8 = num6 - num4 / 2;
			int num9 = num7 - num5 / 2;
			Input.MoveTo(num8, num9);
			Thread.Sleep(40);
			Input.Down();
			Thread.Sleep(60);
			for (int i = 1; i <= 40; i++)
			{
				Input.MoveTo(num8 + num4 * i / 40, num9 + num5 * i / 40);
				Thread.Sleep(12);
			}
			Thread.Sleep(300);
			Input.Up();
			if (!WaitStable(60, 300))
			{
				return false;
			}
			dx -= num4;
			dy -= num5;
			num3++;
		}
		return true;
	}

	private bool LocateByLabels(int col, int row, string name, Point guess, out Point screen)
	{
		screen = guess;
		Point focus = new Point(guess.X - g.Client.X, guess.Y - g.Client.Y);
		if (!BringIntoView(col, row, name, focus, out var grid, out var s))
		{
			SaveShot("подписи_не_прочитаны");
			return false;
		}
		double num = s.X - (float)(guess.X - g.Client.X);
		double num2 = s.Y - (float)(guess.Y - g.Client.Y);
		Log(string.Format(CultureInfo.InvariantCulture, "Карта: по подписям квадратов ({0}) {1} на экране ({2:0}, {3:0}), поправка к расчёту {4:+0;-0;0}, {5:+0;-0;0} px.", grid, name, s.X, s.Y, num, num2));
		screen = new Point(g.Client.X + (int)Math.Round(s.X), g.Client.Y + (int)Math.Round(s.Y));
		locGrid = grid;
		return true;
	}

	private bool BringIntoView(int col, int row, string name, Point focus, out MapGrid grid, out PointF s)
	{
		grid = null;
		s = PointF.Empty;
		Rectangle safe = g.Rc(80.0, 175.0, 1120.0, 740.0);
		Point point = new Point(g.Client.X + safe.X + safe.Width / 2, g.Client.Y + safe.Y + safe.Height / 2);
		int num = 0;
		int num2 = 0;
		int value = 0;
		int value2 = 0;
		PointF pointF = PointF.Empty;
		for (int i = 0; i < 10; i++)
		{
			if (Stop)
			{
				return false;
			}
			if (!ReadGrid(out var grid2, (i == 0) ? focus : Point.Empty))
			{
				if (num >= 2 || !WinOcr.Available)
				{
					return false;
				}
				num++;
				Log("Карта: подписи квадратов мелкие или не видны — приближаю.");
				Input.Wheel(point.X, point.Y, 1, 70);
				if (!WaitStable(150, 450))
				{
					return false;
				}
				continue;
			}
			PointF pointF2 = grid2.Cell(col, row);
			if (Clickable(pointF2, grid2))
			{
				grid = grid2;
				s = pointF2;
				return true;
			}
			if (num2 > 0 && (double)(Math.Abs(pointF2.X - pointF.X) + Math.Abs(pointF2.Y - pointF.Y)) < 0.3 * (double)(Math.Abs(value) + Math.Abs(value2)))
			{
				Log("Карта: " + name + " не подвести в видимую часть — карта дальше не сдвигается.");
				return false;
			}
			if (num2 >= 4)
			{
				return false;
			}
			num2++;
			int num3 = (int)Math.Round((double)safe.X + (double)safe.Width / 2.0 - (double)pointF2.X);
			int num4 = (int)Math.Round((double)safe.Y + (double)safe.Height / 2.0 - (double)pointF2.Y);
			Log("Карта: " + name + " за краем видимой части — сдвигаю карту на " + num3 + ", " + num4 + " px.");
			pointF = pointF2;
			value = num3;
			value2 = num4;
			if (!DragBy(num3, num4, safe))
			{
				return false;
			}
		}
		return false;
	}

	private bool Clickable(PointF c, MapGrid mg)
	{
		Rectangle rectangle = g.Rc(52.0, 150.0, 1180.0, 795.0);
		rectangle.Intersect(new Rectangle(0, 0, g.Client.Width, g.Client.Height));
		double num = Math.Max(4.0, 0.35 * mg.P);
		if ((double)c.X >= (double)rectangle.Left + num && (double)c.X <= (double)rectangle.Right - num && (double)c.Y >= (double)rectangle.Top + num)
		{
			return (double)c.Y <= (double)rectangle.Bottom - num;
		}
		return false;
	}

	private void SetNoClamp()
	{
		noClampSize = g.SizeText;
		if (g.Cal != null && !g.Cal.MapNoClamp)
		{
			g.Cal.MapNoClamp = true;
			Calib.Put(g.Cal);
		}
	}

	private bool PrepareMap(bool top, bool labels)
	{
		if (labels && NoClamp)
		{
			ZoomOutOnly();
			return !Stop;
		}
		if (NormalizeMap(top))
		{
			return true;
		}
		if (!labels || Stop)
		{
			return false;
		}
		SetNoClamp();
		Log("Карта при окне " + g.SizeText + " крупнее планшета и к краю не прижимается — квадрат найду по подписям.");
		return true;
	}

	private void ZoomOutOnly()
	{
		Point point = g.P(640.0, 513.0);
		Input.Wheel(point.X, point.Y, -6, 40);
		WaitStable(120, 300);
	}

	private bool PrepareWindow(Job j, Stopwatch sw)
	{
		Log("Калибровка под окно " + g.SizeText + ": меряю всё заранее. Кнопку «Запланировать атаку» не нажимаю.");
		if (!OpenToMapOrForm(j, sw, allowReuse: false, out var _))
		{
			return false;
		}
		if (WinOcr.Available && !g.Native1280)
		{
			if (!CalibrateByLabels(top: true) && Stop)
			{
				return false;
			}
			if (!CalibrateByLabels(top: false) && Stop)
			{
				return false;
			}
		}
		return !Stop;
	}

	private string PrepareSummary()
	{
		CalibEntry cal = g.Cal;
		Func<bool, string> func = (bool b) => (!b) ? "—" : "✓";
		bool native = g.Native1280;
		return "Окно " + g.SizeText + " подготовлено: раскладка планшета " + func(g.Calibrated) + ((cal != null && cal.Model != null) ? (" (" + cal.Model + ")") : "") + ", карта сверху " + func(native || (cal?.MapLabelTop ?? false)) + ", снизу " + func(native || (cal?.MapLabelBottom ?? false)) + ((cal != null && cal.MapNoClamp) ? " (карта к краю не прижимается — квадрат ищется по подписям)" : "") + ", поле «Часы» читается " + func(fieldReadH) + ", «Минуты» " + func(fieldReadM) + ", панель пинга " + func(g.PingKnown) + (NetWatch.Running ? " (пинг и так берётся из трафика игры)" : "") + ".";
	}

	private bool CalibrateByLabels(bool top)
	{
		CalibEntry cal = g.Cal;
		if (cal == null || !WinOcr.Available)
		{
			return false;
		}
		if (top ? cal.MapLabelTop : cal.MapLabelBottom)
		{
			return true;
		}
		if (NoClamp)
		{
			return false;
		}
		double num = ((cal.MapP > 0.0) ? cal.MapP : (15.93 * g.L.Sx));
		double num2 = ((cal.MapLeft > 0.0) ? cal.MapLeft : g.L.X(275.0));
		double num3 = ((!top) ? ((cal.MapBottom > 0.0) ? cal.MapBottom : g.L.Y(884.0)) : ((cal.MapTop > 0.0) ? cal.MapTop : g.L.Y(115.0)));
		int[,] array = ((!top) ? new int[2, 2]
		{
			{ 12, 48 },
			{ 46, 76 }
		} : new int[2, 2]
		{
			{ 12, 10 },
			{ 46, 38 }
		});
		double[] array2 = new double[2];
		double[] array3 = new double[2];
		double[] array4 = new double[2];
		double[] array5 = new double[2];
		Log("Карта: меряю её по подписям квадратов под окно " + g.SizeText + " (один раз, около 5 секунд).");
		for (int i = 0; i < 2; i++)
		{
			if (!NormalizeMap(top))
			{
				if (!Stop)
				{
					SetNoClamp();
				}
				return false;
			}
			double a = num2 + ((double)array[i, 0] - 0.5) * num;
			double a2 = (top ? (num3 + ((double)array[i, 1] - 0.5) * num) : (num3 - ((double)(84 - array[i, 1]) + 0.5) * num));
			Point point = new Point(g.Client.X + (int)Math.Round(a), g.Client.Y + (int)Math.Round(a2));
			array2[i] = point.X - g.Client.X;
			array3[i] = point.Y - g.Client.Y;
			Input.Wheel(point.X, point.Y, 2, 70);
			if (!Sleep(400))
			{
				return false;
			}
			MapGrid grid;
			bool flag = ReadGrid(out grid);
			ShotMap("замер_" + (top ? "верх" : "низ") + (i + 1), new Point((int)array2[i], (int)array3[i]));
			if (!flag)
			{
				Log("Карта: подписи квадратов не прочитались — беру расчёт.");
				return false;
			}
			array4[i] = grid.Col(array2[i]);
			array5[i] = grid.Row(array3[i]);
		}
		double num4 = (array2[1] - array2[0]) / (array4[1] - array4[0]);
		double num5 = (array3[1] - array3[0]) / (array5[1] - array5[0]);
		if (num4 < 4.0 || num5 < 4.0 || Math.Abs(num4 - num5) > 0.04 * Math.Max(num4, num5))
		{
			Log(string.Format(CultureInfo.InvariantCulture, "Карта: замер по подписям не сошёлся (шаг {0:0.00} и {1:0.00}) — беру расчёт.", num4, num5));
			return false;
		}
		double num6 = (num4 + num5) / 2.0;
		double num7 = (array2[0] - (array4[0] - 0.5) * num6 + array2[1] - (array4[1] - 0.5) * num6) / 2.0;
		double num8 = (top ? ((array3[0] - (array5[0] - 0.5) * num6 + array3[1] - (array5[1] - 0.5) * num6) / 2.0) : ((array3[0] + (84.0 - array5[0] + 0.5) * num6 + array3[1] + (84.0 - array5[1] + 0.5) * num6) / 2.0));
		Log(string.Format(CultureInfo.InvariantCulture, "Карта измерена по подписям: квадрат {0:0.00} px (расчёт {1:0.00}), левый край {2:0} (расчёт {3:0}), {4} {5:0} (расчёт {6:0}).", num6, num, num7, num2, top ? "верх" : "низ", num8, num3));
		cal.MapP = num6;
		cal.MapLeft = num7;
		if (top)
		{
			cal.MapTop = num8;
			cal.MapLabelTop = true;
		}
		else
		{
			cal.MapBottom = num8;
			cal.MapLabelBottom = true;
		}
		Calib.Put(cal);
		return true;
	}

	private static bool ScanOf(char c, out ushort sc, out bool shift)
	{
		sc = 0;
		shift = char.IsUpper(c);
		char c2 = char.ToLowerInvariant(c);
		int num;
		if ((num = "qwertyuiop".IndexOf(c2)) >= 0)
		{
			sc = (ushort)(16 + num);
		}
		else if ((num = "asdfghjkl".IndexOf(c2)) >= 0)
		{
			sc = (ushort)(30 + num);
		}
		else if ((num = "zxcvbnm".IndexOf(c2)) >= 0)
		{
			sc = (ushort)(44 + num);
		}
		else if (c >= '1' && c <= '9')
		{
			sc = (ushort)(2 + (c - 49));
			shift = false;
		}
		else
		{
			switch (c)
			{
			case '0':
				sc = 11;
				shift = false;
				break;
			case ' ':
				sc = 57;
				shift = false;
				break;
			case '-':
				sc = 12;
				shift = false;
				break;
			case '_':
				sc = 12;
				shift = true;
				break;
			case '.':
				sc = 52;
				shift = false;
				break;
			case '\'':
				sc = 40;
				shift = false;
				break;
			}
		}
		if (sc != 0)
		{
			return c2 < '\u0080';
		}
		return false;
	}

	private static void UnicodeChar(char c, int hold)
	{
		int size = Marshal.SizeOf(typeof(Native.INPUT));
		Native.INPUT iNPUT = default(Native.INPUT);
		iNPUT.type = 1u;
		iNPUT.u.ki.scan = c;
		iNPUT.u.ki.flags = 4u;
		Native.SendInput(1u, new Native.INPUT[1] { iNPUT }, size);
		Thread.Sleep(hold);
		iNPUT.u.ki.flags = 6u;
		Native.SendInput(1u, new Native.INPUT[1] { iNPUT }, size);
	}

	private static string Typable(string name)
	{
		string text = string.Join(" ", (name ?? "").Replace('_', ' ').Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
		if (text.Length <= 0 || !text.All((char c) => ScanOf(c, out var _, out var _)))
		{
			return null;
		}
		return text;
	}

	private void TypeText(string s, int hold, int gap)
	{
		foreach (char c in s)
		{
			if (Stop)
			{
				break;
			}
			ushort sc;
			bool shift;
			if (unicodeTyping)
			{
				UnicodeChar(c, hold);
				Thread.Sleep(gap);
			}
			else if (ScanOf(c, out sc, out shift))
			{
				if (shift)
				{
					Input.Key(42, ext: false, up: false);
					Thread.Sleep(4);
				}
				Input.Tap(sc, ext: false, hold);
				if (shift)
				{
					Thread.Sleep(4);
					Input.Key(42, ext: false, up: true);
				}
				Thread.Sleep(gap);
			}
		}
	}

	private void ClearSearch(int hold, int gap)
	{
		ClickB(360.0, 338.0);
		Thread.Sleep(Math.Max(40, gap));
		Input.Key(29, ext: false, up: false);
		Input.Tap(30, ext: false, hold);
		Input.Key(29, ext: false, up: true);
		Thread.Sleep(gap);
		Input.Tap(14, ext: false, hold);
		Thread.Sleep(gap);
	}

	private bool WaitLeftSettled(int minMs, int maxMs)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		if (!Sleep(minMs))
		{
			return false;
		}
		long num = 0L;
		int num2 = 0;
		while (stopwatch.ElapsedMilliseconds < maxMs)
		{
			long num3 = 1469598103934665603L;
			using (Bitmap b = g.GrabBase(100.0, 395.0, 520.0, 530.0))
			{
				Frame frame = Frame.From(b);
				for (int i = 0; i < frame.D.Length; i += 5)
				{
					num3 = (num3 ^ frame.D[i]) * 1099511628211L;
				}
			}
			if (num3 == num)
			{
				if (++num2 >= 2)
				{
					return true;
				}
			}
			else
			{
				num2 = 0;
			}
			num = num3;
			if (!Sleep(35))
			{
				return false;
			}
		}
		return !Stop;
	}

	private IntPtr EnglishLayout()
	{
		unicodeTyping = false;
		try
		{
			IntPtr intPtr = KbLayout.Current(g.H);
			if (KbLayout.IsEnglish(intPtr))
			{
				return IntPtr.Zero;
			}
			if (KbLayout.Switch(g.H, KbLayout.English(), 500))
			{
				Log("Раскладка игры на время ввода имён — английская.");
				return intPtr;
			}
			unicodeTyping = true;
			Log("Раскладку игры не переключить на английскую — ввожу имена символами Юникода.");
		}
		catch
		{
		}
		return IntPtr.Zero;
	}

	private List<RosterResult> AddByList(List<RosterEntry> list)
	{
		List<RosterResult> list2 = new List<RosterResult>();
		int num = FrameMs();
		int num2 = Math.Max(15, Math.Min(60, num + 6));
		int num3 = Math.Max(15, Math.Min(60, num + 6));
		IntPtr intPtr = EnglishLayout();
		try
		{
			int num4 = CountRight();
			for (int i = 0; i < list.Count; i++)
			{
				RosterEntry rosterEntry = list[i];
				if (Stop || num4 >= 35)
				{
					list2.Add(new RosterResult
					{
						Entry = rosterEntry,
						Status = "skipped",
						Note = (Stop ? "остановлено" : "в форме уже 35 человек")
					});
					continue;
				}
				string note = null;
				string text = ((rosterEntry.Alts != null && rosterEntry.Alts.Count > 1) ? ChooseAlt(rosterEntry, num2, num3, out note) : rosterEntry.Name);
				string text2 = Typable(text);
				if (text2 == null)
				{
					list2.Add(new RosterResult
					{
						Entry = rosterEntry,
						Status = "bad_name",
						Note = "в имени символы не латиницей"
					});
					Log("✗ " + text + ": имя не набрать латиницей.");
					continue;
				}
				RosterResult rosterResult = FindAndAdd(rosterEntry, text, text2, num2, num3);
				if (rosterResult.Status == "not_found" && !Stop)
				{
					rosterResult = FindAndAdd(rosterEntry, text, text2, num2 * 2, num3 * 2);
				}
				if (note != null)
				{
					rosterResult.Note = note + (string.IsNullOrEmpty(rosterResult.Note) ? "" : ("; " + rosterResult.Note));
				}
				if (rosterResult.Status == "added")
				{
					num4++;
				}
				list2.Add(rosterResult);
				Log(((rosterResult.Status == "added") ? "✓ " : "✗ ") + text + ((rosterEntry.Uid > 0) ? (" #" + rosterEntry.Uid) : "") + ": " + Roster.StatusText(rosterResult.Status) + ((rosterResult.Online == 0) ? ", не в игре" : "") + (string.IsNullOrEmpty(rosterResult.Note) ? "" : (" (" + rosterResult.Note + ")")));
			}
			if (!Stop)
			{
				ClearSearch(num2, num3);
			}
		}
		finally
		{
			if (intPtr != IntPtr.Zero)
			{
				KbLayout.Switch(g.H, intPtr, 0);
			}
		}
		return list2;
	}

	private string ChooseAlt(RosterEntry e, int hold, int gap, out string note)
	{
		string text = null;
		foreach (string alt in e.Alts)
		{
			if (Stop)
			{
				break;
			}
			string text2 = Typable(alt);
			if (text2 == null)
			{
				continue;
			}
			ClearSearch(hold, gap);
			TypeText(text2, hold, gap);
			if (!WaitLeftSettled(Math.Max(90, FrameMs() * 3), 900))
			{
				break;
			}
			Frame f = g.Shot();
			int num = FormRows.Count(f, g.L, 0.0);
			if (num == 0)
			{
				continue;
			}
			int num2 = 0;
			if (num > 1)
			{
				List<FormRows.RowText> rows = null;
				try
				{
					using Bitmap client = g.GrabClient();
					rows = FormRows.Read(client, g.L, 0.0, Math.Min(num, FormRows.MaxRows));
				}
				catch
				{
				}
				num2 = FormRows.Pick(rows, alt, e.Uid, out var _);
			}
			if (text == null)
			{
				text = alt;
			}
			if (num2 >= 0 && FormRows.Online(f, g.L, 0.0, num2) == 1)
			{
				note = "из " + string.Join(" / ", e.Alts) + " взят " + alt + ": в игре";
				return alt;
			}
		}
		note = ((text != null) ? ("из " + string.Join(" / ", e.Alts) + " никого нет в игре, взят " + text) : ("ни одного из " + string.Join(" / ", e.Alts) + " нет в списке семьи"));
		return text ?? e.Name;
	}

	private RosterResult FindAndAdd(RosterEntry e, string name, string q, int hold, int gap)
	{
		RosterResult rosterResult = new RosterResult();
		rosterResult.Entry = e;
		RosterResult rosterResult2 = rosterResult;
		ClearSearch(hold, gap);
		TypeText(q, hold, gap);
		if (!WaitLeftSettled(Math.Max(90, FrameMs() * 3), 900))
		{
			rosterResult2.Status = "skipped";
			rosterResult2.Note = "остановлено";
			return rosterResult2;
		}
		Frame f = g.Shot();
		int num = FormRows.Count(f, g.L, 0.0);
		if (num == 0)
		{
			rosterResult2.Status = "not_found";
			rosterResult2.Note = "нет в списке семьи";
			return rosterResult2;
		}
		int num2 = 0;
		if (num > 1)
		{
			string how = null;
			List<FormRows.RowText> rows = null;
			try
			{
				using Bitmap client = g.GrabClient();
				rows = FormRows.Read(client, g.L, 0.0, Math.Min(num, FormRows.MaxRows));
			}
			catch
			{
			}
			num2 = FormRows.Pick(rows, name, e.Uid, out how);
			if (num2 < 0 && name.IndexOf(' ') < 0)
			{
				List<int> list = FormRows.ByFirstName(rows, name);
				int num3 = list.FindIndex((int i) => FormRows.Online(f, g.L, 0.0, i) == 1);
				if (list.Count == 1)
				{
					num2 = list[0];
					how = "по имени без фамилии";
				}
				else if (num3 >= 0)
				{
					num2 = list[num3];
					how = "по имени без фамилии, кто в игре";
				}
				else if (list.Count > 1)
				{
					num2 = list[0];
					how = "по имени без фамилии, первый из " + list.Count;
				}
			}
			if (num2 < 0 && e.Uid <= 0)
			{
				num2 = FormRows.Shortest(f, g.L, 0.0, Math.Min(num, FormRows.MaxRows), 4.0);
				if (num2 >= 0)
				{
					how = "по длине имени";
				}
			}
			if (num2 < 0)
			{
				rosterResult2.Status = "ambiguous";
				rosterResult2.Note = "похожих: " + num + ((e.Uid > 0) ? ", static ID не найден среди них" : ", укажите static ID");
				return rosterResult2;
			}
			rosterResult2.Note = "из " + num + " похожих " + how;
		}
		rosterResult2.Online = FormRows.Online(f, g.L, 0.0, num2);
		Point point = g.P(300.0, FormRows.RowY(num2));
		int maxRows = FormRows.MaxRows;
		bool flag = num < maxRows;
		int num4 = CountRight();
		bool flag2 = num4 < maxRows;
		for (int j = 0; j < 2; j++)
		{
			if (Stop)
			{
				rosterResult2.Status = "skipped";
				rosterResult2.Note = "остановлено";
				return rosterResult2;
			}
			Input.Click(point.X, point.Y, ClickHold + j * 15);
			if (!flag && !flag2)
			{
				rosterResult2.Status = "added";
				rosterResult2.Note = ((rosterResult2.Note == null) ? "" : (rosterResult2.Note + "; ")) + "не проверено: списки длиннее экрана";
				return rosterResult2;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			Sleep(Math.Max(60, FrameMs() * 2));
			while (stopwatch.ElapsedMilliseconds < 700)
			{
				Frame f2 = g.Shot();
				if ((flag && FormRows.Count(f2, g.L, 0.0) < num) || (flag2 && FormRows.Count(f2, g.L, 561.5) > num4))
				{
					rosterResult2.Status = "added";
					return rosterResult2;
				}
				if (!Sleep(30))
				{
					break;
				}
			}
			Frame f3 = g.Shot();
			int num5 = FormRows.Count(f3, g.L, 0.0);
			int num6 = FormRows.Count(f3, g.L, 561.5);
			if ((flag && num5 < num) || (flag2 && num6 > num4))
			{
				rosterResult2.Status = "added";
				return rosterResult2;
			}
			if ((flag && num5 != num) || (flag2 && num6 != num4))
			{
				break;
			}
		}
		rosterResult2.Status = "error";
		rosterResult2.Note = "клик по строке не дошёл";
		return rosterResult2;
	}

	private bool FillFormByList(Job j, Stopwatch sw)
	{
		Step(5, StepState.Running, "по списку из Discord");
		RosterInfo rosterInfo = Roster.Fresh(j.Roster, 2500);
		if (rosterInfo == null || rosterInfo.Entries.Count == 0)
		{
			Log("Список из Discord пуст — беру первых " + j.People + " сверху, как обычно.");
			j.Roster = null;
			return FillForm(j, sw);
		}
		Log("Людей — по списку из Discord: " + rosterInfo.Entries.Count + (string.IsNullOrEmpty(rosterInfo.Title) ? "" : (" («" + rosterInfo.Title + "»)")) + ".");
		List<RosterResult> list = AddByList(rosterInfo.Entries);
		if (Stop)
		{
			return false;
		}
		int num = list.Count((RosterResult x) => x.Status == "added");
		Roster.Report(0L, rosterInfo.Ver, "done", j.Square, list, null);
		Step(5, (num == rosterInfo.Entries.Count) ? StepState.Done : StepState.Error, num + " из " + rosterInfo.Entries.Count + ", " + Sec(sw));
		ClickB(360.0, 338.0);
		Thread.Sleep(80);
		for (int i = 0; i < 6; i++)
		{
			Input.Tap(44, ext: false, 20);
			Thread.Sleep(25);
		}
		Point point = g.P(1085.0, 113.0);
		Input.MoveTo(point.X, point.Y);
		if (!Sleep(60))
		{
			return false;
		}
		Log("Всё заполнено за " + sw.ElapsedMilliseconds + " мс.");
		SaveShot(j.DryRun ? "тест_заполнено" : "перед_нажатием");
		return true;
	}

	private void RunList(Job j)
	{
		job = j;
		g.Note = delegate(string m)
		{
			Log(m);
		};
		Native.timeBeginPeriod(1u);
		RosterInfo rosterInfo = j.Roster;
		try
		{
			for (int i = 0; i < StepNames.Length; i++)
			{
				Step(i, StepState.Pending, "");
			}
			cur = -1;
			if (j.RosterFillId > 0)
			{
				Roster.Report(j.RosterFillId, rosterInfo?.Ver ?? 0, "running", j.Square, null, null);
			}
			rosterInfo = Roster.Fresh(rosterInfo, 2500);
			if (rosterInfo == null || rosterInfo.Entries.Count == 0)
			{
				ListFail(j, rosterInfo, "список из Discord пуст");
				return;
			}
			if (!g.Find())
			{
				ListFail(j, rosterInfo, "окно GTA 5 не найдено");
				return;
			}
			g.Refresh();
			if (g.Client.Width < 640)
			{
				ListFail(j, rosterInfo, "окно игры свёрнуто или слишком маленькое");
				return;
			}
			Stopwatch sw = Stopwatch.StartNew();
			Status("test", "собираю список");
			Log("Собираю список из Discord: " + rosterInfo.Entries.Count + " чел." + (string.IsNullOrEmpty(rosterInfo.Title) ? "" : (" («" + rosterInfo.Title + "»)")) + (string.IsNullOrEmpty(j.Square) ? " в открытой форме атаки." : (", форма атаки на " + j.Square + ". Кнопку атаки не нажимаю.")));
			if (!g.Activate())
			{
				Log("Внимание: не удалось вывести игру на передний план.");
			}
			if (!Sleep(150))
			{
				ListStopped(j, rosterInfo);
				return;
			}
			Scr scr = g.Detect();
			if (scr == Scr.Form && (string.IsNullOrEmpty(j.Square) || !j.SquareFromBot))
			{
				for (int k = 0; k <= 3; k++)
				{
					Step(k, StepState.Done, (k == 3) ? "форма уже открыта" : "уже открыто");
				}
				if (!ResetForm())
				{
					if (Stop)
					{
						ListStopped(j, rosterInfo);
					}
					else
					{
						ListFail(j, rosterInfo, "не получилось убрать прежних участников");
					}
					return;
				}
			}
			else
			{
				if (string.IsNullOrEmpty(j.Square))
				{
					ListFail(j, rosterInfo, "форма атаки не открыта, а квадрат не указан — откройте форму или впишите квадрат во вкладке «Списки»");
					return;
				}
				if (!ParseSquare(j.Square, out var col, out var row))
				{
					ListFail(j, rosterInfo, "неверный квадрат " + j.Square);
					return;
				}
				j.Col = col;
				j.Row = row;
				if (!OpenToForm(j, sw, reuse: false))
				{
					if (Stop)
					{
						ListStopped(j, rosterInfo);
					}
					else
					{
						ListFail(j, rosterInfo, "не открылась форма атаки на " + j.Square);
					}
					return;
				}
				if (CountRight() > 0 && !ResetForm())
				{
					if (Stop)
					{
						ListStopped(j, rosterInfo);
					}
					else
					{
						ListFail(j, rosterInfo, "не получилось убрать прежних участников");
					}
					return;
				}
			}
			Step(4, StepState.Skipped, "не нужно");
			Step(5, StepState.Running, rosterInfo.Entries.Count + " по списку");
			List<RosterResult> list = AddByList(rosterInfo.Entries);
			if (Stop)
			{
				Roster.Report(j.RosterFillId, rosterInfo.Ver, "failed", j.Square, list, "остановлено в TTC");
				Stopped();
				return;
			}
			int num = list.Count((RosterResult x) => x.Status == "added");
			Step(5, (num == rosterInfo.Entries.Count) ? StepState.Done : StepState.Error, num + " из " + rosterInfo.Entries.Count + ", " + Sec(sw));
			Step(6, StepState.Skipped, "не нужно");
			Step(7, StepState.Skipped, "не нажимаю");
			Roster.Report(j.RosterFillId, rosterInfo.Ver, "done", j.Square, list, null);
			string text = string.Join(", ", (from x in list
				where x.Status != "added"
				select x.Entry.Name).Take(6));
			Log("Список собран за " + Sec(sw) + ": добавлено " + num + " из " + rosterInfo.Entries.Count + ((text.Length > 0) ? ("; не добавлены: " + text) : "") + ". Кнопку «Запланировать атаку» не нажимал.");
			SaveShot("список_собран");
			Status("testok", "список собран: " + num + " из " + rosterInfo.Entries.Count);
		}
		catch (Exception ex)
		{
			Telemetry.Caught(ex, "Списки");
			ListFail(j, rosterInfo, ex.Message);
		}
		finally
		{
			Native.timeEndPeriod(1u);
		}
	}

	private void ListFail(Job j, RosterInfo ros, string msg)
	{
		Fail(msg);
		Roster.Report(j.RosterFillId, ros?.Ver ?? 0, "failed", j.Square, null, msg);
	}

	private void ListStopped(Job j, RosterInfo ros)
	{
		Stopped();
		Roster.Report(j.RosterFillId, ros?.Ver ?? 0, "failed", j.Square, null, "остановлено в TTC");
	}

	private bool TurnEarly(Job j)
	{
		if (!Turn.Held || Turn.Near)
		{
			return true;
		}
		Log("Очередь: первой бьёт семья с приоритетом, её забив позже нашего. Планшет свободен; подготовлю форму к её нажатию и нажму сразу после.");
		Status("hold", "жду момент");
		Step(0, StepState.Running, "жду момент");
		j.TurnWaited = true;
		while (Turn.Held && !Turn.Near)
		{
			if (!Sleep(100))
			{
				return false;
			}
		}
		return true;
	}

	private bool WaitTurn(Job j)
	{
		Log("Очередь: первой бьёт семья с приоритетом — нажму сразу после неё. Остановить — «Стоп» или F10.");
		Status("hold", "жду момент");
		Step(7, StepState.Running, "жду момент");
		j.TurnWaited = true;
		Turn.Waiting(w: true);
		Stopwatch stopwatch = Stopwatch.StartNew();
		DateTime dateTime = DateTime.UtcNow;
		while (Turn.Held)
		{
			if (Stop)
			{
				return false;
			}
			if (DateTime.UtcNow >= dateTime)
			{
				dateTime = DateTime.UtcNow.AddMilliseconds(500.0);
				SamplePing();
			}
			Thread.Sleep(5);
		}
		Turn.Waiting(w: false);
		if (!Sleep(1000))
		{
			return false;
		}
		Log("Очередь подошла через " + Sec(stopwatch) + " — нажимаю.");
		if (stopwatch.ElapsedMilliseconds > 4000 && g.Detect() != Scr.Form)
		{
			Log("Форма атаки пропала с экрана — открываю и заполняю заново.");
			if (!OpenToForm(j, stopwatch, reuse: true) || !FillForm(j, stopwatch))
			{
				return false;
			}
		}
		Status("ready", "нажимаю");
		return true;
	}

	private bool TurnCaptTime(Job j)
	{
		if (!EnsureCaptWindow(j))
		{
			return false;
		}
		if (Asap.CaptTimeOk(j.CaptH, j.CaptM))
		{
			return true;
		}
		Fail("капты ставят с 15:00 до 21:59 МСК — на " + j.CaptH.ToString("00") + ":" + j.CaptM.ToString("00") + " игра капт не примет");
		return false;
	}
}
