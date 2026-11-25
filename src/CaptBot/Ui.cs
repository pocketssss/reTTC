using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CaptBot;

internal class Ui
{
	private class StepRow
	{
		public Border Dot;

		public TextBlock Mark;

		public TextBlock Name;

		public TextBlock Detail;

		public System.Windows.Shapes.Rectangle Link;
	}

	private class CellHome
	{
		public int Col;

		public int Row;

		public int ColSpan;

		public Thickness Margin;

		public System.Windows.HorizontalAlignment HAlign;
	}

	private static class VzFlash
	{
		public struct FLASHWINFO
		{
			public uint cbSize;

			public IntPtr hwnd;

			public uint dwFlags;

			public uint uCount;

			public uint dwTimeout;
		}

		[DllImport("user32.dll")]
		public static extern bool FlashWindowEx(ref FLASHWINFO fi);
	}

	private const int NtpEveryMin = 20;

	private const string TargetMapHint = "Колесо — масштаб, перетаскивание — сдвиг, клик — сделать целью, двойной клик — окно карты";

	private const int HistoryDays = 3;

	private const int AfterMinLo = 15;

	private const int AfterMinHi = 60;

	private const double DesignW = 1100.0;

	private const double DesignH = 902.0;

	private const double MinScale = 0.8;

	private const double WideMinW = 940.0;

	private const double WinMinW = 500.0;

	private const double WinMinH = 460.0;

	private const int TitleLevels = 13;

	private const int VzCellsDefault = 6;

	private const int PlanRetries = 2;

	private const int PlanLateMin = 30;

	private const string PromoCode = "Skye";

	private const double PromoH = 30.0;

	private readonly Window w;

	private readonly bool demo;

	private System.Windows.Controls.TextBox SquareBox;

	private System.Windows.Controls.TextBox FireBox;

	private System.Windows.Controls.TextBox LogBox;

	private TextBlock SquareInfo;

	private TextBlock CaptHText;

	private TextBlock CaptMText;

	private TextBlock FireInfo;

	private TextBlock PeopleText;

	private TextBlock PrepText;

	private TextBlock Countdown;

	private TextBlock NowText;

	private TextBlock HeroCap;

	private TextBlock StatusText;

	private TextBlock PingValue;

	private TextBlock PingSub;

	private TextBlock LeadCap;

	private TextBlock LeadValue;

	private TextBlock LeadSub;

	private TextBlock FineText;

	private TextBlock SparkHint;

	private TextBlock RdGame;

	private TextBlock RdPing;

	private TextBlock RdClock;

	private TextBlock RdTab;

	private Border ProgFill;

	private Border CaptHBox;

	private Border CaptMBox;

	private Shape StatusDot;

	private Shape RdGameDot;

	private Shape RdPingDot;

	private Shape RdClockDot;

	private Shape RdTabDot;

	private Polyline SparkLine;

	private Canvas SparkCanvas;

	private StackPanel StepsPanel;

	private System.Windows.Controls.Panel Inputs;

	private System.Windows.Controls.Panel TargetGrid;

	private System.Windows.Controls.CheckBox AutoPing;

	private System.Windows.Controls.Button BtnStart;

	private System.Windows.Controls.Button BtnTest;

	private System.Windows.Controls.Button BtnStop;

	private System.Windows.Controls.Button ClockBtn;

	private System.Windows.Controls.Button TabSteps;

	private System.Windows.Controls.Button TabLog;

	private System.Windows.Controls.Button FineMinus;

	private System.Windows.Controls.Button FinePlus;

	private Grid TitleBar;

	private Grid RootGrid;

	private System.Windows.Controls.Image LogoImg;

	private MapView targetMap;

	private SolidColorBrush targetFill;

	private bool targetPicked;

	private Settings cfg;

	private readonly string dir = AppPaths.Data;

	private readonly JavaScriptSerializer json = new JavaScriptSerializer();

	private Runner runner;

	private volatile bool running;

	private volatile bool closing;

	private bool dryRun;

	private DateTime fireUtc = DateTime.MinValue;

	private DateTime startUtc = DateTime.MinValue;

	private DateTime lastPingAt = DateTime.MinValue;

	private string statusKind = "idle";

	private string statusMsg = "";

	private DateTime holdSince = DateTime.MinValue;

	private readonly List<int> pingHist = new List<int>();

	private int lastRtt = -1;

	private int lastFps = -1;

	private bool probing;

	private readonly List<StepRow> rows = new List<StepRow>();

	private static readonly System.Windows.Media.Brush Chalk = B("#ECE7DC");

	private static readonly System.Windows.Media.Brush Fog = B("#8E929B");

	private static readonly System.Windows.Media.Brush Dim = B("#5A5E66");

	private static readonly System.Windows.Media.Brush RuleB = B("#2B2F37");

	private static readonly System.Windows.Media.Brush Slate = B("#1D2026");

	private static readonly System.Windows.Media.Brush CellBg = B("#1A1D22");

	private static readonly System.Windows.Media.Brush CellOut = B("#121417");

	private static readonly System.Windows.Media.Brush GoB = B("#46D39A");

	private static readonly System.Windows.Media.Brush WarnB = B("#F2B33D");

	private static readonly System.Windows.Media.Brush StopB = B("#F0506E");

	private static readonly System.Windows.Media.Brush SignalB = B("#8B5CF6");

	private static readonly System.Windows.Media.Brush White = B("#FFFFFF");

	private static readonly System.Windows.Media.Color SignalC = C("#8B5CF6");

	private static readonly System.Windows.Media.Color GoC = C("#46D39A");

	private DateTime ntpLast = DateTime.MinValue;

	private DateTime ntpPreFireFor = DateTime.MinValue;

	private bool ntpBusy;

	private bool ntpEverOk;

	private System.Windows.Controls.Button LicBtn;

	private Shape LicDot;

	private TextBlock LicText;

	private volatile bool rechecking;

	private bool recheckAgain;

	private bool updateNoted;

	private System.Windows.Controls.Button ServerBtn;

	private System.Windows.Controls.Button FamilyBtn;

	private System.Windows.Controls.Button GameBtn;

	private TextBlock ServerText;

	private TextBlock FamilyText;

	private Shape ServerDot;

	private Shape FamilyDot;

	private List<ServerInfo> servers = new List<ServerInfo>();

	private volatile bool serversLoading;

	private volatile bool familyWanted = true;

	private int familyFails;

	private int lastW;

	private int lastH;

	private string lastNote;

	private Popup pop;

	private string familyPending;

	private bool familyForce;

	private WarState war;

	private string warError;

	private volatile bool warLoading;

	private DateTime warNext = DateTime.MinValue;

	private string warServerSeen = "";

	private int warTick;

	private Window mapWin;

	private MapView mapView;

	private StackPanel mapCard;

	private StackPanel mapFamilies;

	private TextBlock mapTitle;

	private TextBlock mapSource;

	private TextBlock mapHover;

	private System.Windows.Controls.Image mapServerIcon;

	private System.Windows.Controls.Image titleServerIcon;

	private System.Windows.Controls.Button tabFamBtn;

	private System.Windows.Controls.Button tabHistBtn;

	private UIElement famSearchBox;

	private UIElement histTopBox;

	private ScrollViewer famScroll;

	private ScrollViewer histScroll;

	private Border mapHoverBox;

	private System.Windows.Controls.TextBox mapSearch;

	private bool mapMarking;

	public static bool MapDemo;

	private volatile bool patrolBusy;

	private bool patrolLogged;

	private string patrolWhy;

	private int[] patrolLimits;

	private bool patrolNetLogged;

	private int warGen;

	private bool warWaiting;

	private string warInflightSrv;

	private System.Windows.Controls.ToolTip targetTip;

	private bool cardTicks;

	private string famStatsOpen = "";

	private readonly Dictionary<string, FamilyStats> famStats = new Dictionary<string, FamilyStats>();

	private readonly Dictionary<string, DateTime> famStatsAt = new Dictionary<string, DateTime>();

	private string famStatsError;

	private bool famStatsLoading;

	private System.Windows.Controls.Button UpdBtn;

	private TextBlock UpdText;

	private string updLatest;

	private volatile bool updating;

	private Border updCard;

	private TextBlock UpdTitle;

	private string updDismissed;

	private WarState hist;

	private string histError;

	private string histFamily = "";

	private volatile bool histLoading;

	private DateTime histNext = DateTime.MinValue;

	private StackPanel histTop;

	private StackPanel histList;

	private System.Windows.Controls.Button histFamBtn;

	private readonly HashSet<long> histOpenDays = new HashSet<long>();

	private readonly HashSet<int> histOpenCapts = new HashSet<int>();

	private bool histTab;

	private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

	private System.Windows.Controls.Button modeTimeBtn;

	private System.Windows.Controls.Button modeAfterBtn;

	private TextBlock afterText;

	private TextBlock asapHead;

	private TextBlock asapDetail;

	private TextBlock asapPlan;

	private TextBlock asapSrc;

	private Border afterBox;

	private Border asapBar;

	private FrameworkElement targetMark;

	private StackPanel snipePanel;

	private UIElement[] timeRows;

	private DateTime snipeEndUtc = DateTime.MinValue;

	private AsapVerdict asapV;

	private string asapPhase = "";

	private DateTime asapNext = DateTime.MinValue;

	private Grid layoutRoot;

	private Grid layoutHost;

	private ScaleTransform layoutScale;

	private Border rootBorder;

	private System.Windows.Controls.Button maxBtn;

	private Thickness rootMargin;

	private CornerRadius rootRadius;

	private BodyScroller bodyScroll;

	private Grid bodyHost;

	private bool narrowLayout;

	private double targetW0;

	private double targetH0;

	private readonly Dictionary<UIElement, CellHome> cellHome = new Dictionary<UIElement, CellHome>();

	private readonly Dictionary<Grid, GridLength[]> colHome = new Dictionary<Grid, GridLength[]>();

	private readonly HashSet<Grid> bodiesReady = new HashSet<Grid>();

	private Grid bottomGrid;

	private StackPanel titleLeft;

	private StackPanel titleRight;

	private bool titleFitting;

	private int titleLevel;

	private readonly Dictionary<System.Windows.Controls.Button, Thickness> barMargin = new Dictionary<System.Windows.Controls.Button, Thickness>();

	private readonly HashSet<UIElement> fitHidden = new HashSet<UIElement>();

	private System.Windows.Controls.Button netBtn;

	private TextBlock netDot;

	private TextBlock netText;

	private volatile bool netSending;

	private string netLastErr;

	private string netMismatchLogged;

	private bool netStarted;

	private string prepHintSize;

	private string prepLastSize;

	private System.Windows.Controls.Button prepBtn;

	private RosterCfg rcfg = new RosterCfg();

	private System.Windows.Controls.Button modeBtn;

	private System.Windows.Controls.Button listGoBtn;

	private TextBlock modeText;

	private TextBlock rosterHead;

	private TextBlock rosterSub;

	private TextBlock rosterRun;

	private TextBlock strikeHint;

	private StackPanel listPanel;

	private StackPanel rosterRows;

	private System.Windows.Controls.TextBox listSquareBox;

	private System.Windows.Controls.CheckBox armedBox;

	private System.Windows.Controls.CheckBox strikeBox;

	private string rosterError;

	private static readonly Regex VzChannelRx = new Regex("^https://discord\\.com/channels/(?:[0-9]+|@me)/[0-9]+$");

	private static readonly System.Windows.Media.Brush WellB = B("#101215");

	private VzzCfg vcfg = new VzzCfg();

	private bool vzzOn;

	private bool vzRunning;

	private bool vzFailed;

	private string vzNote;

	private string vzCode = "";

	private string vzMessage = "";

	private string vzShownColor;

	private string vzGotAt = "";

	private string vzSentAt = "";

	private string vzDoubt = "";

	private string vzState;

	private bool vzChecking;

	private VzzWatcher vzWatcher;

	private Thread vzThread;

	private DispatcherTimer vzSaver;

	private DispatcherTimer vzReveal;

	private Grid mainBody;

	private Grid vzBody;

	private System.Windows.Controls.Panel mainBar;

	private System.Windows.Controls.Panel mainButtons;

	private System.Windows.Controls.Panel vzBar;

	private System.Windows.Controls.Panel vzButtons;

	private System.Windows.Controls.TextBox vzLine1;

	private System.Windows.Controls.TextBox vzChannel;

	private TextBlock vzLine1Hint;

	private TextBlock vzChannelHint;

	private TextBlock vzColorText;

	private TextBlock vzDetail;

	private TextBlock vzPrevLine1;

	private TextBlock vzPrevColor;

	private TextBlock vzPrevCode;

	private Border vzColorSwatch;

	private Border vzPrevSwatch;

	private System.Windows.Controls.Button vzColorBtn;

	private System.Windows.Controls.Button vzToggle;

	private System.Windows.Controls.Button vzCopyBtn;

	private System.Windows.Controls.CheckBox vzSend;

	private System.Windows.Controls.CheckBox vzCheck;

	private System.Windows.Controls.CheckBox vzCopy;

	private System.Windows.Controls.CheckBox vzSound;

	private TextBlock vzStatusText;

	private TextBlock vzMeta;

	private TextBlock vzAfter;

	private TextBlock vzAfterSub;

	private TextBlock vzPlace;

	private TextBlock vzPlaceSub;

	private TextBlock vzSure;

	private TextBlock vzSureSub;

	private Shape vzStatusDot;

	private Shape vzModeDot;

	private StackPanel vzCells;

	private StackPanel vzLog;

	private TextBlock vzLogEmpty;

	private ScrollViewer vzLogScroll;

	private Shape vzBrowserDot;

	private Shape vzLoginDot;

	private Shape vzChatDot;

	private Shape vzOcrDot;

	private TextBlock vzBrowserText;

	private TextBlock vzLoginText;

	private TextBlock vzChatText;

	private TextBlock vzOcrText;

	private static readonly TimeSpan CaptFrom = new TimeSpan(15, 0, 0);

	private static readonly TimeSpan CaptTo = new TimeSpan(21, 59, 59);

	private PlanCfg pcfg = new PlanCfg();

	private bool planOn;

	private bool planRunning;

	private bool planSawRun;

	private bool planLockShown;

	private PlanEntry planCurrent;

	private Job planJob;

	private Grid planBody;

	private Grid planMain;

	private Grid planLock;

	private System.Windows.Controls.Panel planBar;

	private System.Windows.Controls.Panel planButtons;

	private StackPanel planRows;

	private StackPanel planFamRows;

	private StackPanel planOptRows;

	private System.Windows.Controls.TextBox planSqBox;

	private System.Windows.Controls.TextBox planCaptBox;

	private System.Windows.Controls.TextBox planFireBox;

	private TextBlock planSqHint;

	private TextBlock planCaptHint;

	private TextBlock planFireHint;

	private TextBlock planMinText;

	private TextBlock planStatusText;

	private TextBlock planPeople;

	private TextBlock planPeopleSub;

	private TextBlock planMainText;

	private TextBlock planBest;

	private TextBlock planFamHead;

	private TextBlock planNote;

	private TextBlock planNow;

	private Shape planStatusDot;

	private Shape planRosterDot;

	private Shape planDataDot;

	private TextBlock planRosterText;

	private TextBlock planDataText;

	private System.Windows.Controls.Button planGo;

	private System.Windows.Controls.Button planUpd;

	private System.Windows.Controls.CheckBox planSmart;

	private System.Windows.Controls.CheckBox planNeutral;

	private System.Windows.Controls.CheckBox planRebid;

	private List<PlanFamily> planFams;

	private PlanSuggest planSug;

	private int planCapts;

	private int planMedian = 32;

	private string planError;

	private string planPushed;

	private volatile bool planLoading;

	private DateTime planLoadedAt = DateTime.MinValue;

	private DateTime planPushAt = DateTime.MinValue;

	private FrameworkElement modeIconHost;

	private NotifyIcon tray;

	private volatile bool quitting;

	private bool hiddenStart;

	private bool autostartBusy;

	private bool autostartQuiet;

	private System.Windows.Controls.CheckBox autostartSw;

	private readonly List<Window> trayHidden = new List<Window>();

	private static readonly System.Windows.Media.Brush PromoCodeB = B("#B79CFF");

	private static readonly System.Windows.Media.Brush PromoBg = B("#148B5CF6");

	private FrameworkElement promoText;

	private FrameworkElement promoDone;

	private DispatcherTimer promoTimer;

	private static readonly JavaScriptSerializer PJson = new JavaScriptSerializer
	{
		MaxJsonLength = 16777216
	};

	private ProbeResult lastProbeR;

	private string presMode;

	private string presTarget;

	private Dictionary<string, object> presResult;

	private string presSig = "";

	private DateTime presNextUtc = DateTime.MinValue;

	private volatile bool presSending;

	private bool? tgLinked;

	private List<Dictionary<string, object>> famBoard = new List<Dictionary<string, object>>();

	private System.Windows.Controls.Button tgBtn;

	private System.Windows.Controls.Button reportBtn;

	private Shape tgDot;

	private Border onbCard;

	private StackPanel onbList;

	private volatile bool reportSending;

	private string reportedMsg;

	private volatile string farewell;

	private DateTime farewellUtc;

	private Grid prioBody;

	private System.Windows.Controls.Panel prioBar;

	private System.Windows.Controls.Panel prioButtons;

	private bool prioOn;

	private System.Windows.Controls.TextBox prioFam;

	private TextBlock prioFamHint;

	private TextBlock prioServer;

	private TextBlock prioNote;

	private TextBlock prioStatusText;

	private TextBlock prioSiteText;

	private TextBlock prioCopiesText;

	private Shape prioStatusDot;

	private Shape prioSiteDot;

	private Shape prioCopiesDot;

	private StackPanel prioClaims;

	private StackPanel prioLog;

	private System.Windows.Controls.Button prioGo;

	private System.Windows.Controls.Button prioTake;

	private Dictionary<string, object> prioState;

	private bool prioFilled;

	private bool prioSaving;

	private volatile bool prioLoading;

	private static bool GameFeedsMap
	{
		get
		{
			if (NetWatch.Running && NetWatch.GameRunning && NetWatch.LastFullUtc != DateTime.MinValue && NetWatch.Limits != null)
			{
				return (Clock.UtcNow() - NetWatch.LastUpdateUtc).TotalHours < 2.0;
			}
			return false;
		}
	}

	private bool MapOpen
	{
		get
		{
			if (mapWin != null)
			{
				return mapWin.IsVisible;
			}
			return false;
		}
	}

	private string RosterCfgPath => System.IO.Path.Combine(dir, "roster.json");

	private string VzzCfgPath => System.IO.Path.Combine(dir, "vzz.json");

	private string PlanCfgPath => System.IO.Path.Combine(dir, "plan.json");

	private static bool PlanLinked => Roster.Last?.Linked ?? false;

	private static bool PriorityAllowed
	{
		get
		{
			LicenseState current = License.Current;
			if (current != null && current.Ok)
			{
				return current.Role == "owner";
			}
			return false;
		}
	}

	private static System.Windows.Media.Color C(string hex)
	{
		return (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex);
	}

	private static SolidColorBrush B(string hex)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush(C(hex));
		solidColorBrush.Freeze();
		return solidColorBrush;
	}

	private T F<T>(string n) where T : class
	{
		if (!(w.FindName(n) is T result))
		{
			throw new Exception("нет элемента " + n);
		}
		return result;
	}

	private static Stream Res(string name)
	{
		return Assembly.GetExecutingAssembly().GetManifestResourceStream(name);
	}

	public Ui(bool demo)
	{
		Ui ui = this;
		this.demo = demo;
		using (Stream stream = Res("MainWindow.xaml"))
		{
			w = (Window)XamlReader.Load(stream);
		}
		SquareBox = F<System.Windows.Controls.TextBox>("SquareBox");
		FireBox = F<System.Windows.Controls.TextBox>("FireBox");
		LogBox = F<System.Windows.Controls.TextBox>("LogBox");
		SquareInfo = F<TextBlock>("SquareInfo");
		CaptHText = F<TextBlock>("CaptHText");
		CaptMText = F<TextBlock>("CaptMText");
		FireInfo = F<TextBlock>("FireInfo");
		PeopleText = F<TextBlock>("PeopleText");
		PrepText = F<TextBlock>("PrepText");
		Countdown = F<TextBlock>("Countdown");
		NowText = F<TextBlock>("NowText");
		HeroCap = F<TextBlock>("HeroCap");
		StatusText = F<TextBlock>("StatusText");
		PingValue = F<TextBlock>("PingValue");
		PingSub = F<TextBlock>("PingSub");
		LeadCap = F<TextBlock>("LeadCap");
		LeadValue = F<TextBlock>("LeadValue");
		LeadSub = F<TextBlock>("LeadSub");
		FineText = F<TextBlock>("FineText");
		SparkHint = F<TextBlock>("SparkHint");
		RdGame = F<TextBlock>("RdGame");
		RdPing = F<TextBlock>("RdPing");
		RdClock = F<TextBlock>("RdClock");
		RdTab = F<TextBlock>("RdTab");
		ProgFill = F<Border>("ProgFill");
		CaptHBox = F<Border>("CaptHBox");
		CaptMBox = F<Border>("CaptMBox");
		StatusDot = F<Shape>("StatusDot");
		RdGameDot = F<Shape>("RdGameDot");
		RdPingDot = F<Shape>("RdPingDot");
		RdClockDot = F<Shape>("RdClockDot");
		RdTabDot = F<Shape>("RdTabDot");
		SparkLine = F<Polyline>("SparkLine");
		SparkCanvas = F<Canvas>("SparkCanvas");
		StepsPanel = F<StackPanel>("StepsPanel");
		Inputs = F<System.Windows.Controls.Panel>("Inputs");
		TargetGrid = F<System.Windows.Controls.Panel>("TargetGrid");
		AutoPing = F<System.Windows.Controls.CheckBox>("AutoPing");
		BtnStart = F<System.Windows.Controls.Button>("BtnStart");
		BtnTest = F<System.Windows.Controls.Button>("BtnTest");
		BtnStop = F<System.Windows.Controls.Button>("BtnStop");
		ClockBtn = F<System.Windows.Controls.Button>("ClockBtn");
		TabSteps = F<System.Windows.Controls.Button>("TabSteps");
		TabLog = F<System.Windows.Controls.Button>("TabLog");
		FineMinus = F<System.Windows.Controls.Button>("FineMinus");
		FinePlus = F<System.Windows.Controls.Button>("FinePlus");
		TitleBar = F<Grid>("TitleBar");
		RootGrid = F<Grid>("RootGrid");
		LogoImg = F<System.Windows.Controls.Image>("LogoImg");
		try
		{
			Stream stream2 = Res("logo.png");
			if (stream2 != null)
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = stream2;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				LogoImg.Source = bitmapImage;
			}
			Stream stream3 = Res("app.ico");
			if (stream3 != null)
			{
				w.Icon = BitmapFrame.Create(stream3, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
			}
		}
		catch
		{
		}
		RootGrid.SizeChanged += delegate
		{
			RootGrid.Clip = new RectangleGeometry(new Rect(0.0, 0.0, RootGrid.ActualWidth, RootGrid.ActualHeight), 11.0, 11.0);
		};
		LoadCfg();
		InitTray();
		BuildGrid();
		BuildSteps();
		Wire();
		ApplyCfg();
		InitLicense();
		InitWorld();
		InitWar();
		InitNet();
		InitPrep();
		InitUpdate();
		InitSnipe();
		InitLayout();
		InitRoster();
		InitPromo();
		InitPresence();
		DispatcherTimer dispatcherTimer = new DispatcherTimer(DispatcherPriority.Render);
		dispatcherTimer.Interval = TimeSpan.FromMilliseconds(40.0);
		dispatcherTimer.Tick += delegate
		{
			Tick();
		};
		dispatcherTimer.Start();
		DispatcherTimer dispatcherTimer2 = new DispatcherTimer();
		dispatcherTimer2.Interval = TimeSpan.FromMilliseconds(900.0);
		dispatcherTimer2.Tick += delegate
		{
			ProbeGame();
		};
		dispatcherTimer2.Start();
		Thread thread = new Thread(HotkeyLoop);
		thread.IsBackground = true;
		thread.Start();
		DispatcherTimer dispatcherTimer3 = new DispatcherTimer();
		dispatcherTimer3.Interval = TimeSpan.FromSeconds(1.0);
		dispatcherTimer3.Tick += delegate
		{
			ClockTick();
		};
		dispatcherTimer3.Start();
		w.Loaded += delegate
		{
			if (demo)
			{
				ui.DemoFill();
			}
			else
			{
				ui.ProbeGame();
				ui.SyncNtp();
				ui.StartTelemetry();
				ui.Log("TTC запущен. «Калибровка» вверху проходит всё под ваше окно, кроме кнопки «Запланировать атаку». F10 останавливает.");
			}
		};
		w.Closing += delegate(object s, CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				closing = true;
				if (runner != null)
				{
					runner.Stop = true;
				}
				SaveCfg();
			}
		};
	}

	public void Run(bool tray)
	{
		System.Windows.Application application = new System.Windows.Application();
		application.ShutdownMode = ShutdownMode.OnMainWindowClose;
		application.SessionEnding += delegate(object s, SessionEndingCancelEventArgs e)
		{
			quitting = true;
			Farewell((e.ReasonSessionEnding == ReasonSessionEnding.Logoff) ? "logoff" : "off");
		};
		application.DispatcherUnhandledException += delegate(object s, DispatcherUnhandledExceptionEventArgs e)
		{
			Telemetry.Crash(e.Exception, "окно");
		};
		application.Exit += delegate
		{
			Telemetry.Save();
		};
		if (tray && !SingleInstance.ShowPending)
		{
			StartHidden();
		}
		application.Run(w);
	}

	private void Wire()
	{
		TitleBar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
		{
			if (e.ButtonState == MouseButtonState.Pressed)
			{
				w.DragMove();
			}
		};
		F<System.Windows.Controls.Button>("BtnMin").Click += delegate
		{
			w.WindowState = WindowState.Minimized;
		};
		F<System.Windows.Controls.Button>("BtnClose").Click += delegate
		{
			w.Close();
		};
		SquareBox.TextChanged += delegate
		{
			ValidateSquare();
		};
		FireBox.TextChanged += delegate
		{
			ValidateFire();
		};
		F<System.Windows.Controls.Button>("CaptHUp").Click += delegate
		{
			SetCapt(cfg.CaptH + 1, cfg.CaptM);
		};
		F<System.Windows.Controls.Button>("CaptHDown").Click += delegate
		{
			SetCapt(cfg.CaptH - 1, cfg.CaptM);
		};
		F<System.Windows.Controls.Button>("CaptMUp").Click += delegate
		{
			SetCapt(cfg.CaptH, cfg.CaptM + 5);
		};
		F<System.Windows.Controls.Button>("CaptMDown").Click += delegate
		{
			SetCapt(cfg.CaptH, cfg.CaptM - 5);
		};
		CaptHBox.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetCapt(cfg.CaptH + Math.Sign(e.Delta), cfg.CaptM);
		};
		CaptMBox.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetCapt(cfg.CaptH, cfg.CaptM + 5 * Math.Sign(e.Delta));
		};
		F<System.Windows.Controls.Button>("ChipP10").Click += delegate
		{
			ShiftFire(10);
		};
		F<System.Windows.Controls.Button>("ChipP1m").Click += delegate
		{
			ShiftFire(60);
		};
		F<System.Windows.Controls.Button>("ChipRound").Click += delegate
		{
			DateTime dateTime2 = Clock.Msk(Clock.UtcNow());
			FireBox.Text = new DateTime(dateTime2.Year, dateTime2.Month, dateTime2.Day, dateTime2.Hour, dateTime2.Minute, 0).AddMinutes(1.0).ToString("HH:mm:ss");
		};
		F<System.Windows.Controls.Button>("ChipSoon").Click += delegate
		{
			DateTime dateTime = Clock.Msk(Clock.UtcNow()).AddSeconds(40.0);
			FireBox.Text = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second).ToString("HH:mm:ss");
		};
		FireBox.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			ShiftFire(Math.Sign(e.Delta));
		};
		F<System.Windows.Controls.Button>("PeopleMinus").Click += delegate
		{
			SetPeople(cfg.People - 1);
		};
		F<System.Windows.Controls.Button>("PeoplePlus").Click += delegate
		{
			SetPeople(cfg.People + 1);
		};
		PeopleText.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetPeople(cfg.People + Math.Sign(e.Delta));
		};
		F<System.Windows.Controls.Button>("PrepMinus").Click += delegate
		{
			SetPrep(cfg.PrepSec - 5);
		};
		F<System.Windows.Controls.Button>("PrepPlus").Click += delegate
		{
			SetPrep(cfg.PrepSec + 5);
		};
		PrepText.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetPrep(cfg.PrepSec + 5 * Math.Sign(e.Delta));
		};
		FineMinus.Click += delegate
		{
			SetFine(cfg.FineMs - 5);
		};
		FinePlus.Click += delegate
		{
			SetFine(cfg.FineMs + 5);
		};
		FineText.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetFine(cfg.FineMs + 5 * Math.Sign(e.Delta));
		};
		AutoPing.Checked += delegate
		{
			cfg.AutoPing = true;
			UpdateLead();
		};
		AutoPing.Unchecked += delegate
		{
			cfg.AutoPing = false;
			UpdateLead();
		};
		ClockBtn.Click += delegate
		{
			SyncNtp();
		};
		TabSteps.Click += delegate
		{
			ShowTab(log: false);
		};
		TabLog.Click += delegate
		{
			ShowTab(log: true);
		};
		System.Windows.Controls.Button tabSteps = TabSteps;
		Visibility visibility2 = (TabLog.Visibility = Visibility.Collapsed);
		tabSteps.Visibility = visibility2;
		BtnStart.Click += delegate
		{
			Start(dry: false);
		};
		if (BtnTest.Parent is System.Windows.Controls.Panel panel)
		{
			panel.Children.Remove(BtnTest);
		}
		BtnStop.Click += delegate
		{
			if (runner != null)
			{
				runner.Stop = true;
			}
		};
	}

	private void ShowTab(bool log)
	{
		log = false;
		LogBox.Visibility = ((!log) ? Visibility.Collapsed : Visibility.Visible);
		StepsPanel.Visibility = (log ? Visibility.Collapsed : Visibility.Visible);
		TabLog.Foreground = (log ? Chalk : Fog);
		TabSteps.Foreground = (log ? Fog : Chalk);
		if (log)
		{
			LogBox.ScrollToEnd();
		}
	}

	private void SetCapt(int h, int m)
	{
		if (m >= 60)
		{
			m -= 60;
		}
		if (m < 0)
		{
			m += 60;
		}
		h = (h % 24 + 24) % 24;
		cfg.CaptH = h;
		cfg.CaptM = m - m % 5;
		CaptHText.Text = cfg.CaptH.ToString("00");
		CaptMText.Text = cfg.CaptM.ToString("00");
	}

	private void SetPeople(int n)
	{
		cfg.People = Math.Max(1, Math.Min(35, n));
		PeopleText.Text = cfg.People.ToString();
	}

	private void SetPrep(int n)
	{
		cfg.PrepSec = Math.Max(10, Math.Min(120, n));
		PrepText.Text = cfg.PrepSec.ToString();
	}

	private void SetFine(int n)
	{
		cfg.FineMs = Math.Max(-300, Math.Min(300, n));
		FineText.Text = ((cfg.FineMs > 0) ? "+" : "") + cfg.FineMs + " мс";
		UpdateLead();
	}

	private void ShiftFire(int sec)
	{
		if (!Clock.ParseTime(FireBox.Text, out var t))
		{
			t = Clock.Msk(Clock.UtcNow()).TimeOfDay;
		}
		t = TimeSpan.FromSeconds((TimeSpan.FromSeconds(Math.Floor(t.TotalSeconds) + (double)sec).TotalSeconds % 86400.0 + 86400.0) % 86400.0);
		FireBox.Text = t.ToString("hh\\:mm\\:ss");
	}

	private void BuildGrid()
	{
		targetFill = new SolidColorBrush(SignalC);
		targetMap = new MapView
		{
			TargetBrush = targetFill,
			CenterCellPx = 40.0
		};
		TargetGrid.Children.Clear();
		TargetGrid.Children.Add(targetMap);
		TargetGrid.Clip = new RectangleGeometry(new Rect(0.0, 0.0, TargetGrid.Width, TargetGrid.Height), 5.0, 5.0);
		targetMap.ZoneClick += delegate(Zone z)
		{
			if (running || Zones.ByCode(SquareBox.Text.Trim().ToUpperInvariant()) == z)
			{
				targetMap.Selected = targetMap.Target;
				targetMap.InvalidateVisual();
			}
			else
			{
				targetPicked = true;
				try
				{
					SquareBox.Text = z.Label;
				}
				finally
				{
					targetPicked = false;
				}
				WarFamily warFamily = ((war == null) ? null : war.OwnerOf(z));
				Log("Цель с карты: " + z.Label + ((warFamily != null) ? (" (" + warFamily.Title + ")") : "") + ".");
			}
		};
	}

	private void UpdateGrid(bool ok, int col, int row)
	{
		Zone zone = (ok ? Zones.At(col, row) : null);
		if (zone != null && zone != targetMap.Target && !targetPicked)
		{
			targetMap.CenterOn(zone);
		}
		targetMap.Target = zone;
		targetMap.Selected = zone;
		targetMap.InvalidateVisual();
		PaintWatchCells();
	}

	private void FlashTarget()
	{
		ColorAnimation colorAnimation = new ColorAnimation(GoC, SignalC, TimeSpan.FromMilliseconds(900.0));
		colorAnimation.RepeatBehavior = new RepeatBehavior(3.0);
		targetFill.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
	}

	private bool ValidateSquare()
	{
		string text = SquareBox.Text.Trim().ToUpperInvariant();
		int col;
		int row;
		bool flag = Runner.ParseSquare(text, out col, out row);
		UpdateGrid(flag, col, row);
		if (flag)
		{
			SquareInfo.Foreground = Fog;
			SquareInfo.Text = "Колонка " + col + ", строка " + row + ". Должен граничить с территорией " + FamilyPhrase() + ".";
			SquareBox.BorderBrush = RuleB;
		}
		else
		{
			SquareInfo.Foreground = StopB;
			SquareInfo.Text = ((text.Length == 0) ? "Впишите код квадрата с карты, например AI40." : "Такого кода нет на карте. Пример: AI40.");
			SquareBox.BorderBrush = StopB;
		}
		return flag;
	}

	private bool ValidateFire()
	{
		TimeSpan t;
		bool flag = Clock.ParseTime(FireBox.Text, out t);
		FireBox.BorderBrush = (flag ? RuleB : StopB);
		if (!flag)
		{
			FireInfo.Foreground = StopB;
			FireInfo.Text = "Нужен формат ЧЧ:ММ:СС, например 19:00:00. Можно с миллисекундами: 19:00:00.250.";
		}
		else
		{
			FireInfo.Foreground = Fog;
		}
		return flag;
	}

	private void BuildSteps()
	{
		StepsPanel.Children.Clear();
		rows.Clear();
		int num = Runner.StepNames.Length;
		for (int i = 0; i < num; i++)
		{
			Grid grid = new Grid();
			grid.Height = 30.0;
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(34.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			StepRow stepRow = new StepRow();
			stepRow.Link = new System.Windows.Shapes.Rectangle
			{
				Width = 1.0,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				Margin = new Thickness(10.5, 22.0, 0.0, 0.0)
			};
			if (i == num - 1)
			{
				stepRow.Link.Visibility = Visibility.Hidden;
			}
			stepRow.Dot = new Border
			{
				Width = 22.0,
				Height = 22.0,
				CornerRadius = new CornerRadius(4.0),
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top
			};
			stepRow.Mark = new TextBlock
			{
				FontSize = 12.0,
				FontWeight = FontWeights.SemiBold,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center
			};
			stepRow.Dot.Child = stepRow.Mark;
			stepRow.Name = new TextBlock
			{
				Text = Runner.StepNames[i],
				FontSize = 14.0,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
			};
			stepRow.Detail = new TextBlock
			{
				FontSize = 13.0,
				FontStretch = FontStretches.Condensed,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
				Foreground = Fog
			};
			Grid.SetColumn(stepRow.Name, 1);
			Grid.SetColumn(stepRow.Detail, 2);
			grid.Children.Add(stepRow.Link);
			grid.Children.Add(stepRow.Dot);
			grid.Children.Add(stepRow.Name);
			grid.Children.Add(stepRow.Detail);
			StepsPanel.Children.Add(grid);
			rows.Add(stepRow);
			SetStep(i, StepState.Pending, "");
		}
		ShowTab(log: false);
	}

	private void SetStep(int i, StepState st, string detail)
	{
		if (i >= 0 && i < rows.Count)
		{
			Telemetry.Step(i, st, detail);
			StepRow stepRow = rows[i];
			stepRow.Dot.BorderThickness = new Thickness(0.0);
			stepRow.Link.Fill = ((st == StepState.Done) ? B("#5546D39A") : RuleB);
			switch (st)
			{
			case StepState.Pending:
				stepRow.Dot.Background = System.Windows.Media.Brushes.Transparent;
				stepRow.Dot.BorderBrush = RuleB;
				stepRow.Dot.BorderThickness = new Thickness(1.0);
				stepRow.Mark.Text = (i + 1).ToString();
				stepRow.Mark.Foreground = Dim;
				stepRow.Name.Foreground = Fog;
				break;
			case StepState.Running:
				stepRow.Dot.Background = SignalB;
				stepRow.Mark.Text = (i + 1).ToString();
				stepRow.Mark.Foreground = White;
				stepRow.Name.Foreground = Chalk;
				break;
			case StepState.Done:
				stepRow.Dot.Background = B("#1C3A2E");
				stepRow.Mark.Text = "✓";
				stepRow.Mark.Foreground = GoB;
				stepRow.Name.Foreground = Chalk;
				break;
			case StepState.Error:
				stepRow.Dot.Background = B("#3A1C24");
				stepRow.Mark.Text = "✕";
				stepRow.Mark.Foreground = StopB;
				stepRow.Name.Foreground = Chalk;
				break;
			case StepState.Skipped:
				stepRow.Dot.Background = System.Windows.Media.Brushes.Transparent;
				stepRow.Dot.BorderBrush = RuleB;
				stepRow.Dot.BorderThickness = new Thickness(1.0);
				stepRow.Mark.Text = "–";
				stepRow.Mark.Foreground = Dim;
				stepRow.Name.Foreground = Fog;
				break;
			}
			stepRow.Detail.Text = detail;
			stepRow.Detail.Foreground = ((st == StepState.Error) ? StopB : ((st == StepState.Done && i == 7) ? GoB : Fog));
			if (st == StepState.Error && detail.Length > 52)
			{
				stepRow.Detail.Text = detail.Substring(0, 50) + "…";
			}
		}
	}

	private void SetStatus(string kind, string msg)
	{
		statusKind = kind;
		statusMsg = msg ?? "";
		Telemetry.Status(kind);
		string text;
		System.Windows.Media.Brush fill;
		switch (kind)
		{
		case "wait":
			text = "Жду начала подготовки";
			fill = SignalB;
			break;
		case "watch":
			text = "Жду, когда можно бить";
			fill = SignalB;
			break;
		case "blocked":
			text = "Сегодня не забить";
			fill = StopB;
			break;
		case "prep":
			text = "Готовлю форму";
			fill = WarnB;
			break;
		case "test":
			text = "Идёт тест";
			fill = SignalB;
			break;
		case "testok":
			text = "Тест пройден, кнопка не нажата";
			fill = GoB;
			break;
		case "ready":
			text = "Форма готова, жду момент";
			fill = SignalB;
			break;
		case "hold":
			text = "Жду момент";
			fill = SignalB;
			holdSince = Clock.UtcNow();
			break;
		case "done":
			text = "Забито";
			fill = GoB;
			break;
		case "error":
			text = "Ошибка, подробности в шагах ниже";
			fill = StopB;
			break;
		case "stop":
			text = "Остановлено";
			fill = Fog;
			break;
		default:
			text = "Ожидание";
			fill = Fog;
			break;
		}
		StatusText.Text = text;
		StatusDot.Fill = fill;
		if (kind == "done")
		{
			FlashTarget();
		}
	}

	private static string Span(TimeSpan t)
	{
		if (t < TimeSpan.Zero)
		{
			t = TimeSpan.Zero;
		}
		return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds / 100}";
	}

	private void Tick()
	{
		DateTime dateTime = Clock.UtcNow();
		NowText.Text = "Сейчас " + Clock.Msk(dateTime).ToString("HH:mm:ss.fff") + " по МСК";
		if (running && statusKind == "hold")
		{
			HeroCap.Text = "Жду момент для нажатия";
			Countdown.Text = Span(dateTime - holdSince);
			Countdown.Foreground = SignalB;
			ProgFill.Width = 0.0;
		}
		else if (!SnipeTick(dateTime))
		{
			FrameworkElement frameworkElement = ProgFill.Parent as FrameworkElement;
			TimeSpan t2;
			if (running && !dryRun && fireUtc != DateTime.MinValue)
			{
				TimeSpan t = fireUtc - dateTime;
				HeroCap.Text = "До забива в " + Clock.Fmt(fireUtc);
				Countdown.Text = Span(t);
				Countdown.Foreground = ((t.TotalSeconds <= 10.0) ? SignalB : Chalk);
				double totalMilliseconds = (fireUtc - startUtc).TotalMilliseconds;
				double totalMilliseconds2 = (dateTime - startUtc).TotalMilliseconds;
				ProgFill.Width = ((frameworkElement != null && totalMilliseconds > 0.0) ? (frameworkElement.ActualWidth * Math.Max(0.0, Math.Min(1.0, totalMilliseconds2 / totalMilliseconds))) : 0.0);
			}
			else if (running && dryRun)
			{
				HeroCap.Text = "Тест идёт, кнопка «Запланировать атаку» нажата не будет";
				Countdown.Text = Span(dateTime - startUtc);
				Countdown.Foreground = Chalk;
				ProgFill.Width = 0.0;
			}
			else if (statusKind == "done")
			{
				HeroCap.Text = "Кнопка нажата";
				Countdown.Text = "Забито";
				Countdown.Foreground = GoB;
				ProgFill.Width = frameworkElement?.ActualWidth ?? 0.0;
			}
			else if (Clock.ParseTime(FireBox.Text, out t2))
			{
				DateTime dateTime2 = Clock.NextMskUtc(t2);
				bool flag = Clock.Msk(dateTime2).Date != Clock.Msk(dateTime).Date;
				HeroCap.Text = "До забива в " + Clock.Fmt(dateTime2) + (flag ? ", завтра" : "");
				Countdown.Text = Span(dateTime2 - dateTime);
				Countdown.Foreground = Fog;
				ProgFill.Width = 0.0;
				bool flag2 = (dateTime2 - dateTime).TotalSeconds < (double)(cfg.PrepSec + 5);
				FireInfo.Text = (flag2 ? ("Слишком близко: подготовке нужно около " + cfg.PrepSec + " с. Поставьте время позже.") : ("Подготовка начнётся в " + Clock.Fmt(dateTime2.AddSeconds(-cfg.PrepSec)) + ". Нажму с опережением на пинг."));
				FireInfo.Foreground = (flag2 ? WarnB : Fog);
			}
			else
			{
				HeroCap.Text = "До забива";
				Countdown.Text = "--:--:--.-";
				Countdown.Foreground = Dim;
				ProgFill.Width = 0.0;
			}
		}
	}

	private void UpdateReadyPing()
	{
		bool flag = lastRtt > 0 && (DateTime.UtcNow - lastPingAt).TotalSeconds < 90.0;
		RdPingDot.Fill = (flag ? GoB : Dim);
		RdPing.Text = (flag ? ("Пинг " + lastRtt + " мс") : "Пинг прочитаю при забиве");
	}

	private void OnPing(int rtt, int fps, double lead)
	{
		lastRtt = rtt;
		if (fps > 0)
		{
			lastFps = fps;
		}
		lastPingAt = DateTime.UtcNow;
		UpdateReadyPing();
		pingHist.Add(rtt);
		if (pingHist.Count > 90)
		{
			pingHist.RemoveAt(0);
		}
		PingValue.Text = rtt.ToString();
		PingValue.Foreground = ((rtt < 160) ? Chalk : WarnB);
		int num = pingHist.Min();
		int num2 = pingHist.Max();
		PingSub.Text = ((num == num2) ? "держится ровно" : ("колеблется от " + num + " до " + num2)) + ((lastFps > 0) ? (", FPS " + lastFps) : "");
		if (lead >= 0.0)
		{
			ShowLead(lead);
		}
		else
		{
			UpdateLead();
		}
		DrawSpark();
	}

	private void UpdateLead()
	{
		if (lastRtt <= 0)
		{
			LeadCap.Text = "Нажму раньше на";
			LeadValue.Text = (cfg.AutoPing ? "—" : ((cfg.FineMs == 0) ? "0 мс" : FineText.Text));
			LeadSub.Text = (cfg.AutoPing ? "посчитаю, когда увижу пинг" : "пинг не учитывается");
		}
		else
		{
			List<int> list = pingHist.Skip(Math.Max(0, pingHist.Count - 12)).ToList();
			list.Sort();
			double num = list[list.Count / 2];
			double num2 = ((lastFps > 0) ? (1000.0 / (double)lastFps) : 0.0);
			ShowLead(cfg.AutoPing ? (num / 2.0 + num2 / 2.0) : 0.0);
		}
	}

	private void ShowLead(double lead)
	{
		double num = lead - (double)cfg.FineMs;
		LeadCap.Text = ((num >= 0.0) ? "Нажму раньше на" : "Нажму позже на");
		LeadValue.Text = Math.Abs(num).ToString("0.0") + " мс";
		double num2 = ((lastFps > 0) ? (1000.0 / (double)lastFps) : 0.0);
		string text = ((cfg.FineMs != 0) ? $", поправка {cfg.FineMs:+0;−0} мс" : "");
		LeadSub.Text = (cfg.AutoPing ? $"половина пинга {Math.Max(0.0, lead - num2 / 2.0):0} мс и полкадра {num2 / 2.0:0.0} мс{text}" : ("пинг не учитывается" + text));
	}

	private void DrawSpark()
	{
		double actualWidth = SparkCanvas.ActualWidth;
		double actualHeight = SparkCanvas.ActualHeight;
		if (pingHist.Count >= 2 && !(actualWidth < 10.0) && !(actualHeight < 10.0))
		{
			SparkHint.Visibility = Visibility.Collapsed;
			int num = pingHist.Min();
			int num2 = pingHist.Max();
			double num3 = Math.Max(10, num2 - num);
			PointCollection pointCollection = new PointCollection();
			int count = pingHist.Count;
			for (int i = 0; i < count; i++)
			{
				double x = (actualWidth - 2.0) * (double)i / (double)(count - 1) + 1.0;
				double y = 4.0 + (actualHeight - 10.0) * (1.0 - (double)(pingHist[i] - num) / num3);
				pointCollection.Add(new System.Windows.Point(x, y));
			}
			SparkLine.Points = pointCollection;
		}
	}

	private int[] MyScreenRect()
	{
		double num = 1.0;
		PresentationSource presentationSource = PresentationSource.FromVisual(w);
		if (presentationSource != null && presentationSource.CompositionTarget != null)
		{
			num = presentationSource.CompositionTarget.TransformToDevice.M11;
		}
		if (w.WindowState == WindowState.Minimized)
		{
			return null;
		}
		return new int[4]
		{
			(int)(w.Left * num),
			(int)(w.Top * num),
			(int)((w.Left + w.ActualWidth) * num),
			(int)((w.Top + w.ActualHeight) * num)
		};
	}

	private void ProbeGame()
	{
		if (running || probing || closing || demo)
		{
			return;
		}
		probing = true;
		int[] me = MyScreenRect();
		bool wantFamily = familyWanted;
		ThreadPool.QueueUserWorkItem(delegate
		{
			ProbeResult r;
			try
			{
				r = Runner.Probe(me, wantFamily);
			}
			catch (Exception ex)
			{
				r = new ProbeResult();
				r.Note = "Ошибка проверки игры: " + ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					probing = false;
					OnProbe(r);
				});
			}
			catch
			{
				probing = false;
			}
		});
	}

	private void SyncNtp()
	{
		SyncNtp(quiet: false, null);
	}

	private void SyncNtp(bool quiet, string why)
	{
		if (ntpBusy)
		{
			return;
		}
		ntpBusy = true;
		if (!quiet)
		{
			RdClock.Text = "Сверяю часы";
			RdClockDot.Fill = Dim;
		}
		ClockBtn.IsEnabled = false;
		Thread thread = new Thread((ThreadStart)delegate
		{
			double off;
			double rtt;
			string host;
			string note;
			bool ok = Clock.Sync(out off, out rtt, out host, out note);
			if (!closing)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					ntpBusy = false;
					ClockBtn.IsEnabled = true;
					if (ok && NearFire(20.0))
					{
						Log($"Сверка часов пришла за секунды до подготовки — поправку не меняю (новая {off:+0;−0;0} мс, сейчас {Clock.OffsetMs:+0;−0;0} мс).");
					}
					else if (ok)
					{
						double offsetMs = Clock.OffsetMs;
						bool flag = !ntpEverOk;
						Clock.OffsetMs = off;
						ntpLast = DateTime.UtcNow;
						ntpEverOk = true;
						RdClockDot.Fill = GoB;
						RdClock.Text = string.Format("Часы сверены в {0}, поправка {1:+0;−0;0} мс", Clock.Msk(Clock.UtcNow()).ToString("HH:mm"), off);
						if (!quiet || why != null || flag || Math.Abs(off - offsetMs) >= 15.0)
						{
							Log(string.Format("Часы сверены{0} с {1}: часы ПК {2} на {3:0} мс (задержка до сервера времени {4:0} мс, ошибка не больше ±{5:0} мс; {6}){7}.", (why != null) ? (" " + why) : "", host, (off >= 0.0) ? "отстают" : "спешат", Math.Abs(off), rtt, rtt / 2.0, note, (flag || Math.Abs(off - offsetMs) < 1.0) ? "" : $" — с прошлой сверки часы ушли на {Math.Abs(off - offsetMs):0} мс"));
						}
					}
					else
					{
						ntpLast = DateTime.UtcNow.AddMinutes(-15.0);
						if (!ntpEverOk)
						{
							RdClockDot.Fill = WarnB;
							RdClock.Text = "Часы не сверены, нажмите, чтобы повторить";
						}
						Log(ntpEverOk ? "Не удалось сверить часы — оставляю прошлую поправку, повторю через 5 минут." : "Не удалось сверить часы, работаю по часам ПК.");
					}
				});
			}
		});
		thread.IsBackground = true;
		thread.Start();
	}

	private bool NearFire(double sec)
	{
		if (!running || dryRun || fireUtc == DateTime.MinValue)
		{
			return false;
		}
		return (fireUtc - Clock.UtcNow()).TotalSeconds < (double)cfg.PrepSec + sec;
	}

	private void ClockTick()
	{
		if (demo || closing || ntpBusy)
		{
			return;
		}
		if (running && !dryRun && fireUtc != DateTime.MinValue)
		{
			double num = (fireUtc - Clock.UtcNow()).TotalSeconds - (double)cfg.PrepSec;
			if (num > 60.0 && num < 150.0 && ntpPreFireFor != fireUtc)
			{
				ntpPreFireFor = fireUtc;
				SyncNtp(quiet: true, "перед забивом");
				return;
			}
			if (num <= 60.0)
			{
				return;
			}
		}
		if (ntpLast != DateTime.MinValue && (DateTime.UtcNow - ntpLast).TotalMinutes >= 20.0)
		{
			SyncNtp(quiet: true, null);
		}
	}

	private void Start(bool dry)
	{
		Start(dry, prepare: false);
	}

	private void Start(bool dry, bool prepare)
	{
		if (running)
		{
			return;
		}
		if (prepare)
		{
			dry = true;
		}
		if (!LicenseGate())
		{
			return;
		}
		ReadCfg();
		SaveCfg();
		if (!ValidateSquare() || !Runner.ParseSquare(cfg.Square, out var col, out var row))
		{
			Log("Неверный код квадрата.");
			SquareBox.Focus();
		}
		else
		{
			if (!prepare && !ConfirmTarget(cfg.Square))
			{
				return;
			}
			Job j = new Job();
			j.Square = cfg.Square;
			j.Col = col;
			j.Row = row;
			j.CaptH = cfg.CaptH;
			j.CaptM = cfg.CaptM;
			j.People = cfg.People;
			j.PrepSec = cfg.PrepSec;
			j.DryRun = dry;
			j.AutoPing = cfg.AutoPing;
			j.FineMs = cfg.FineMs;
			j.Family = cfg.Family;
			j.Prepare = prepare;
			RosterForStrike(j);
			PlanForStrike(j);
			ServerInfo serverInfo = CurrentServer();
			if (serverInfo != null && serverInfo.Live && (serverInfo.TechWorks || !serverInfo.Online))
			{
				Log("Внимание: на сервере " + serverInfo.Title + (serverInfo.TechWorks ? " техработы" : " недоступен") + " (по данным Majestic Wiki).");
			}
			if (cfg.AfterCapt && !prepare)
			{
				if (!PrepareSnipe(j, dry))
				{
					return;
				}
			}
			else if (!dry)
			{
				if (!ValidateFire() || !Clock.ParseTime(cfg.FireTime, out var t))
				{
					Log("Неверное время забива.");
					FireBox.Focus();
					return;
				}
				j.FireUtc = Clock.NextMskUtc(t);
				double totalSeconds = (j.FireUtc - Clock.UtcNow()).TotalSeconds;
				if (totalSeconds < (double)(cfg.PrepSec + 3))
				{
					Log("До забива меньше, чем нужно на подготовку (" + cfg.PrepSec + " с). Поставьте время позже.");
					return;
				}
				Log(string.Format("Назначено: {0}, капт на {1:00}:{2:00}, {3} чел. Пакет должен прийти на сервер в {4} МСК ({5}). Подготовка за {6} с.", j.Square, j.CaptH, j.CaptM, j.People, Clock.Fmt(j.FireUtc), Clock.Msk(j.FireUtc).ToString("dd.MM"), j.PrepSec));
				if (totalSeconds > 43200.0)
				{
					Log("Внимание: до забива больше 12 часов, проверьте время.");
				}
			}
			else if (!prepare)
			{
				Log("Тест: " + j.Square + ", капт на " + j.CaptH.ToString("00") + ":" + j.CaptM.ToString("00") + ", " + j.People + " чел. Кнопку не нажимаю.");
			}
			dryRun = dry;
			fireUtc = j.FireUtc;
			startUtc = Clock.UtcNow();
			targetFill.BeginAnimation(SolidColorBrush.ColorProperty, null);
			runner = new Runner();
			runner.ShotDir = null;
			SnipeWire(runner);
			runner.Log = delegate(string m)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					Log(m);
				});
			};
			runner.Status = delegate(string k, string m)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					SetStatus(k, m);
				});
			};
			runner.StepUpdate = delegate(int i, StepState st, string d)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					SetStep(i, st, d);
				});
			};
			runner.PingUpdate = delegate(int r, int f, double l)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					OnPing(r, f, l);
				});
			};
			runner.WantFamily = familyWanted;
			runner.FamilyUpdate = delegate(FamilyInfo fi)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					OnFamily(fi);
				});
			};
			Telemetry.Begin(prepare ? "calib" : ((planCurrent != null) ? "plan" : (cfg.AfterCapt ? "after" : (dry ? "test" : "strike"))), j.Square, j.People);
			PresenceBegin(prepare ? "calib" : ((planCurrent != null) ? "plan" : (cfg.AfterCapt ? "asap" : (dry ? "test" : "strike"))), j.Square);
			if (!dry)
			{
				Turn.Begin(j, cfg.Server);
			}
			running = true;
			Lock(r: true);
			ShowTab(log: false);
			SetStatus(dry ? "test" : "wait", "");
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					runner.Run(j);
				}
				catch (Exception ex2)
				{
					Exception ex = ex2;
					Telemetry.Caught(ex, "поток забива");
					w.Dispatcher.BeginInvoke((Action)delegate
					{
						Log("Ошибка: " + ex.Message);
					});
				}
				finally
				{
					running = false;
					try
					{
						w.Dispatcher.BeginInvoke((Action)delegate
						{
							Lock(r: false);
						});
					}
					catch
					{
					}
				}
			});
			thread.IsBackground = true;
			thread.Start();
		}
	}

	private void DemoFill()
	{
		running = true;
		dryRun = false;
		startUtc = Clock.UtcNow().AddSeconds(-14.0);
		fireUtc = Clock.UtcNow().AddSeconds(17.0);
		Lock(r: true);
		SetStatus("ready", "демо");
		string[] array = new string[6] { "уже открыт", "уже открыто", "1,3 с", "AI40, 3,5 с", "20:00, 4,6 с", "10 человек, 6,3 с" };
		for (int i = 0; i < array.Length; i++)
		{
			SetStep(i, StepState.Done, array[i]);
		}
		SetStep(6, StepState.Running, "слежу за пингом");
		int[] array2 = new int[16]
		{
			128, 126, 126, 131, 144, 144, 139, 133, 126, 124,
			126, 129, 134, 130, 127, 126
		};
		int[] array3 = array2;
		foreach (int num in array3)
		{
			OnPing(num, 118, (double)num / 2.0 + 4.2);
		}
		RdGameDot.Fill = GoB;
		RdGame.Text = "Игра 1920×1080, калибровка ✓";
		ServerDot.Fill = GoB;
		ServerText.Text = "RU1 · New York · 907 онлайн";
		FamilyDot.Fill = GoB;
		FamilyText.Text = "Семья Skye";
		RdClockDot.Fill = GoB;
		RdClock.Text = "Часы сверены, поправка −604 мс";
		RdTabDot.Fill = GoB;
		RdTab.Text = "Планшет: форма атаки";
	}

	private void Lock(bool r)
	{
		Inputs.IsEnabled = !r;
		AutoPing.IsEnabled = !r;
		FineMinus.IsEnabled = !r;
		FinePlus.IsEnabled = !r;
		BtnStart.IsEnabled = !r;
		BtnTest.IsEnabled = !r;
		BtnStop.IsEnabled = r;
		if (!r)
		{
			Telemetry.End();
			PresenceEnd();
			Turn.End();
			if (statusKind == "hold" || statusKind == "wait" || statusKind == "prep" || statusKind == "ready" || statusKind == "test")
			{
				SetStatus("idle", "");
			}
			fireUtc = DateTime.MinValue;
		}
	}

	private void HotkeyLoop()
	{
		bool flag = false;
		while (!closing)
		{
			bool flag2 = (Native.GetAsyncKeyState(121) & 0x8000) != 0;
			if (flag2 && !flag && running && runner != null)
			{
				runner.Stop = true;
				try
				{
					w.Dispatcher.BeginInvoke((Action)delegate
					{
						Log("Экстренная остановка (F10).");
					});
				}
				catch
				{
				}
			}
			flag = flag2;
			Thread.Sleep(15);
		}
	}

	private void Log(string m)
	{
		string text = Clock.Fmt(Clock.UtcNow()) + "  " + m;
		LogBox.AppendText(text + "\r\n");
		if (LogBox.LineCount > 600)
		{
			LogBox.Text = LogBox.Text.Substring(LogBox.Text.Length / 2);
		}
		LogBox.ScrollToEnd();
		Telemetry.FromLog(m);
	}

	private void StartTelemetry()
	{
		if (demo || MapDemo)
		{
			return;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		try
		{
			System.Drawing.Rectangle bounds = Screen.PrimaryScreen.Bounds;
			dictionary["screen"] = bounds.Width + "×" + bounds.Height;
			dictionary["monitors"] = Screen.AllScreens.Length;
			PresentationSource presentationSource = PresentationSource.FromVisual(w);
			if (presentationSource != null && presentationSource.CompositionTarget != null)
			{
				dictionary["dpi"] = Math.Round(presentationSource.CompositionTarget.TransformToDevice.M11, 2);
			}
		}
		catch
		{
		}
		dictionary["tray"] = hiddenStart;
		Telemetry.Start(dictionary);
	}

	private void LoadCfg()
	{
		try
		{
			string path = System.IO.Path.Combine(dir, "captbot.json");
			if (File.Exists(path))
			{
				cfg = json.Deserialize<Settings>(File.ReadAllText(path, Encoding.UTF8));
			}
		}
		catch
		{
			cfg = null;
		}
		if (cfg == null)
		{
			cfg = new Settings();
		}
		if (!cfg.StartWithWindowsChosen)
		{
			cfg.StartWithWindows = true;
		}
	}

	private void ApplyCfg()
	{
		if (cfg.WinLeft != int.MinValue && cfg.WinTop != int.MinValue && (double)cfg.WinLeft >= SystemParameters.VirtualScreenLeft - 50.0 && (double)cfg.WinLeft < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 200.0 && (double)cfg.WinTop >= SystemParameters.VirtualScreenTop - 50.0 && (double)cfg.WinTop < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 200.0)
		{
			w.WindowStartupLocation = WindowStartupLocation.Manual;
			w.Left = cfg.WinLeft;
			w.Top = cfg.WinTop;
		}
		SquareBox.Text = (demo ? "AI40" : (cfg.Square ?? "AI40"));
		FireBox.Text = (demo ? "19:00:00" : (string.IsNullOrEmpty(cfg.FireTime) ? "19:00:00" : cfg.FireTime));
		SetCapt(demo ? 20 : cfg.CaptH, (!demo) ? cfg.CaptM : 0);
		SetPeople(cfg.People);
		SetPrep(cfg.PrepSec);
		SetFine(cfg.FineMs);
		AutoPing.IsChecked = cfg.AutoPing;
		ValidateSquare();
		ValidateFire();
		UpdateLead();
	}

	private void ReadCfg()
	{
		cfg.Square = SquareBox.Text.Trim().ToUpperInvariant();
		cfg.FireTime = FireBox.Text.Trim();
		cfg.AutoPing = AutoPing.IsChecked == true;
		if (w.WindowState == WindowState.Normal && w.IsLoaded)
		{
			cfg.WinLeft = (int)w.Left;
			cfg.WinTop = (int)w.Top;
		}
	}

	private void SaveCfg()
	{
		if (demo)
		{
			return;
		}
		try
		{
			ReadCfg();
			File.WriteAllText(System.IO.Path.Combine(dir, "captbot.json"), json.Serialize(cfg), Encoding.UTF8);
		}
		catch
		{
		}
	}

	private void InitLicense()
	{
		LicBtn = F<System.Windows.Controls.Button>("LicBtn");
		LicDot = F<Shape>("LicDot");
		LicText = F<TextBlock>("LicText");
		if (demo)
		{
			LicDot.Fill = GoB;
			LicText.Text = "Ключ: 29 д";
			return;
		}
		LicBtn.Click += delegate
		{
			ChangeKey();
		};
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromSeconds(30.0);
		int i = 0;
		dispatcherTimer.Tick += delegate
		{
			UpdateLic();
			if (++i % 20 == 0)
			{
				RecheckLicense();
				ThreadPool.QueueUserWorkItem(delegate
				{
					License.Touch();
				});
			}
		};
		dispatcherTimer.Start();
		NetWatch.OwnFamilyChanged += delegate(string fam)
		{
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					LicenseState current2 = License.Current;
					if (current2 != null && current2.FamilySeat)
					{
						Log("Ключ семейной подписки: сверяю семью " + fam + " с сервером.");
						RecheckLicense();
					}
				});
			}
			catch
			{
			}
		};
		w.Loaded += delegate
		{
			UpdateLic();
			LicenseState current = License.Current;
			if (current != null)
			{
				Log("Ключ " + current.Key + ": " + current.Term() + (current.Offline ? (". " + current.Message) : "") + ".");
				NoteUpdate(current);
			}
		};
	}

	private void UpdateLic()
	{
		if (!demo)
		{
			LicenseState current = License.Current;
			bool flag = License.UsableNow();
			LicDot.Fill = ((!flag) ? StopB : (current.Offline ? WarnB : GoB));
			if (current == null)
			{
				LicText.Text = "Нет ключа";
			}
			else if (!flag)
			{
				LicText.Text = "Ключ не действует";
			}
			else if (current.Lifetime)
			{
				LicText.Text = "Ключ навсегда";
			}
			else
			{
				TimeSpan timeSpan = current.ExpiresUtc - Clock.UtcNow();
				LicText.Text = "Ключ: " + ((timeSpan.TotalDays >= 1.0) ? ((int)timeSpan.TotalDays + " д") : ((timeSpan.TotalHours >= 1.0) ? ((int)timeSpan.TotalHours + " ч") : (Math.Max(1, (int)timeSpan.TotalMinutes) + " мин")));
			}
			LicBtn.ToolTip = ((current == null) ? "Ключ не введён" : (current.Key + "\n" + (current.Ok ? current.Term() : current.Message) + (current.Offline ? ("\n" + current.Message) : "") + (current.FamilySeat ? ("\n" + FamilyLine(current)) : ""))) + "\nНажмите, чтобы сменить ключ";
		}
	}

	private static string FamilyLine(LicenseState c)
	{
		string text = "Семейная подписка" + ((c.Family.Length > 0) ? (": семья " + c.Family + ((c.FamilyServer.Length > 0) ? (" (" + c.FamilyServer + ")") : "")) : ", семья ещё не закреплена");
		return text + (c.FamilyNeed ? "\nСемья не подтверждена: откройте в планшете приложение семьи" : "");
	}

	private void NoteUpdate(LicenseState st)
	{
		ShowUpdate(st.Latest);
		if (!updateNoted && Updater.IsNewer(st.Latest, "2.9.13"))
		{
			updateNoted = true;
			Log("Вышла новая версия TTC " + st.Latest + " (у вас 2.9.13). Нажмите «Обновить до " + st.Latest + "» вверху окна — программа скачает её и перезапустится.");
		}
	}

	private bool LicenseGate()
	{
		if (demo)
		{
			Log("Это демо-режим: забив в нём недоступен. Запустите программу без --demo.");
			return false;
		}
		if (!License.UsableNow())
		{
			LicenseState current = License.Current;
			string text = ((current != null && current.Message.Length > 0) ? current.Message : "ключ не введён");
			Log("Запуск невозможен: " + text + ".");
			bool flag = LicenseDialog.Show(w, auto: false, text);
			UpdateLic();
			if (!flag)
			{
				return false;
			}
		}
		if (!FamilyGate())
		{
			return false;
		}
		RecheckLicense();
		return true;
	}

	private bool FamilyGate()
	{
		LicenseState current = License.Current;
		if (current == null || !current.FamilySeat || !current.FamilyNeed)
		{
			return true;
		}
		string ownFamily = NetWatch.OwnFamily;
		if (string.IsNullOrEmpty(ownFamily))
		{
			FamilyNote("Ключ семейной подписки: TTC ещё не видела вашу семью в игре" + ((current.Family.Length > 0) ? (" (подписка семьи " + current.Family + ")") : "") + ". Откройте в планшете приложение семьи — TTC сверит семью с сервером, и можно запускать." + (NetWatch.Running ? "" : " Для этого TTC должна видеть трафик игры: запустите её от имени администратора."));
			return false;
		}
		LicenseState licenseState = License.Check(current.Key);
		if (licenseState.Ok)
		{
			License.Current = licenseState;
		}
		else if (licenseState.Definitive)
		{
			License.Current = licenseState;
			UpdateLic();
			FamilyNote("Запуск невозможен: " + licenseState.Message + ".");
			return false;
		}
		UpdateLic();
		current = License.Current;
		if (current.FamilyNeed)
		{
			FamilyNote("Ключ семейной подписки: не удалось сверить семью " + ownFamily + " с сервером" + (licenseState.Ok ? "" : (" (" + licenseState.Message + ")")) + ". Попробуйте ещё раз.");
			return false;
		}
		Log("Семья " + ownFamily + " подтверждена для ключа семейной подписки.");
		return true;
	}

	private void FamilyNote(string msg)
	{
		Log(msg);
		try
		{
			if (w.IsActive)
			{
				System.Windows.MessageBox.Show(w, msg, "TTC");
			}
			else if (tray != null)
			{
				tray.ShowBalloonTip(10000, "TTC: семейная подписка", (msg.Length > 250) ? (msg.Substring(0, 247) + "…") : msg, ToolTipIcon.Warning);
			}
		}
		catch
		{
		}
	}

	private void RecheckLicense()
	{
		if (demo || closing)
		{
			return;
		}
		if (rechecking)
		{
			recheckAgain = true;
			return;
		}
		LicenseState current = License.Current;
		string key = ((current != null && current.Key.Length > 0) ? current.Key : License.SavedKey());
		if (key.Length == 0)
		{
			return;
		}
		rechecking = true;
		Thread thread = new Thread((ThreadStart)delegate
		{
			LicenseState st = License.Check(key);
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					rechecking = false;
					if (st.Ok)
					{
						License.Current = st;
						NoteUpdate(st);
					}
					else if (st.Definitive)
					{
						License.Current = st;
						Log("Ключ больше не действует: " + st.Message + ".");
						if (running && runner != null)
						{
							runner.Stop = true;
							Log("Забив остановлен: ключ недействителен.");
						}
					}
					else
					{
						Log("Проверка ключа: " + st.Message);
					}
					UpdateLic();
					if (recheckAgain)
					{
						recheckAgain = false;
						RecheckLicense();
					}
				});
			}
			catch
			{
				rechecking = false;
			}
		});
		thread.IsBackground = true;
		thread.Start();
	}

	private void ChangeKey()
	{
		if (running)
		{
			Log("Сменить ключ можно, когда забив не запущен.");
			return;
		}
		if (LicenseDialog.Show(w, auto: false, null))
		{
			LicenseState current = License.Current;
			Log("Ключ " + current.Key + " активирован: " + current.Term() + ".");
			NoteUpdate(current);
		}
		UpdateLic();
	}

	private void InitWorld()
	{
		ServerBtn = F<System.Windows.Controls.Button>("ServerBtn");
		FamilyBtn = F<System.Windows.Controls.Button>("FamilyBtn");
		GameBtn = F<System.Windows.Controls.Button>("GameBtn");
		ServerText = F<TextBlock>("ServerText");
		FamilyText = F<TextBlock>("FamilyText");
		ServerDot = F<Shape>("ServerDot");
		FamilyDot = F<Shape>("FamilyDot");
		servers = MajesticWiki.Cached();
		if (servers.Count == 0)
		{
			servers = MajesticWiki.Builtin();
		}
		if (!demo)
		{
			UpdateServer();
			UpdateFamily();
			ServerBtn.Click += delegate
			{
				ShowServers();
			};
			FamilyBtn.Click += delegate
			{
				ShowFamily();
			};
			GameBtn.Click += delegate
			{
				ShowGameMenu();
			};
			DispatcherTimer dispatcherTimer = new DispatcherTimer();
			dispatcherTimer.Interval = TimeSpan.FromMinutes(5.0);
			dispatcherTimer.Tick += delegate
			{
				LoadServers(quiet: true);
			};
			dispatcherTimer.Start();
			w.Loaded += delegate
			{
				LoadServers(quiet: false);
			};
		}
	}

	private void LoadServers(bool quiet)
	{
		if (serversLoading || closing)
		{
			return;
		}
		serversLoading = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			List<ServerInfo> i;
			string err;
			try
			{
				i = MajesticWiki.Load(out err);
			}
			catch (Exception ex)
			{
				i = null;
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					serversLoading = false;
					if (i != null && i.Count > 0)
					{
						servers = i;
					}
					UpdateServer();
					if (!quiet)
					{
						if (err != null)
						{
							Log("Список серверов с Majestic Wiki не загрузился (" + err + ") — показываю сохранённый.");
						}
						else
						{
							Log("Серверы с Majestic Wiki: " + servers.Count + ", в игре сейчас " + servers.Sum((ServerInfo x) => x.Players).ToString("#,0") + " человек.");
						}
					}
				});
			}
			catch
			{
				serversLoading = false;
			}
		});
	}

	private ServerInfo CurrentServer()
	{
		return servers.FirstOrDefault((ServerInfo x) => string.Equals(x.Id, cfg.Server, StringComparison.OrdinalIgnoreCase));
	}

	private void UpdateServer()
	{
		ServerInfo serverInfo = CurrentServer();
		if (serverInfo == null)
		{
			ServerText.Text = "Выберите сервер";
			ServerDot.Fill = WarnB;
			ServerBtn.ToolTip = "Сервер Majestic RP, на котором вы играете: список, онлайн и техработы берутся с Majestic Wiki";
		}
		else
		{
			ServerText.Text = serverInfo.Title + ((serverInfo.State.Length > 0) ? (" · " + serverInfo.State) : "");
			ServerDot.Fill = ((!serverInfo.Live) ? Dim : (serverInfo.TechWorks ? WarnB : ((!serverInfo.Online) ? StopB : GoB)));
			ServerBtn.ToolTip = "Сервер " + serverInfo.Title + (serverInfo.Live ? "" : "\nОнлайн не обновлён: нет связи с Majestic Wiki") + "\nНажмите, чтобы выбрать другой";
		}
	}

	private void ShowServers()
	{
		StackPanel stackPanel = new StackPanel();
		foreach (ServerInfo server in servers)
		{
			ServerInfo si = server;
			bool flag = string.Equals(si.Id, cfg.Server, StringComparison.OrdinalIgnoreCase);
			Grid grid = new Grid();
			grid.MinWidth = 340.0;
			Grid grid2 = grid;
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(28.0)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid2.Children.Add(Images.Badge("/war/server-icon/" + si.Id.ToLowerInvariant() + ".svg", null, null, 18.0));
			TextBlock textBlock = new TextBlock();
			textBlock.Text = si.Title;
			textBlock.FontSize = 13.5;
			textBlock.Foreground = (flag ? SignalB : Chalk);
			textBlock.FontWeight = (flag ? FontWeights.SemiBold : FontWeights.Normal);
			TextBlock element = textBlock;
			TextBlock textBlock2 = new TextBlock();
			textBlock2.Text = si.State;
			textBlock2.FontSize = 12.5;
			textBlock2.Foreground = (si.TechWorks ? WarnB : Fog);
			textBlock2.Margin = new Thickness(18.0, 0.0, 0.0, 0.0);
			TextBlock element2 = textBlock2;
			Grid.SetColumn(element, 1);
			Grid.SetColumn(element2, 2);
			grid2.Children.Add(element);
			grid2.Children.Add(element2);
			stackPanel.Children.Add(MenuButton(grid2, delegate
			{
				cfg.Server = si.Id;
				UpdateServer();
				SaveCfg();
				Log("Сервер: " + si.Title + ".");
			}));
		}
		stackPanel.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Height = 1.0,
			Fill = RuleB,
			Margin = new Thickness(8.0, 5.0, 8.0, 5.0)
		});
		stackPanel.Children.Add(MenuButton(new TextBlock
		{
			Text = "Карта каптов этого сервера на вики ↗",
			FontSize = 13.0,
			Foreground = Fog
		}, delegate
		{
			MajesticWiki.OpenCaptures(cfg.Server);
		}));
		stackPanel.Children.Add(MenuButton(new TextBlock
		{
			Text = "Обновить список с вики",
			FontSize = 13.0,
			Foreground = Fog
		}, delegate
		{
			LoadServers(quiet: false);
		}));
		ShowPopup(ServerBtn, new ScrollViewer
		{
			Content = stackPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			MaxHeight = 540.0
		});
	}

	private System.Windows.Controls.Button MenuButton(UIElement content, Action click)
	{
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = (Style)w.FindResource("Plain");
		button.Content = content;
		button.Padding = new Thickness(12.0, 7.0, 12.0, 7.0);
		button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			if (pop != null)
			{
				pop.IsOpen = false;
			}
			click();
		};
		return button2;
	}

	private void ShowPopup(UIElement target, UIElement content)
	{
		ShowPopup(target, content, above: false);
	}

	private void ShowPopup(UIElement target, UIElement content, bool above)
	{
		if (pop != null)
		{
			pop.IsOpen = false;
		}
		Border border = new Border();
		border.Background = Slate;
		border.BorderBrush = RuleB;
		border.BorderThickness = new Thickness(1.0);
		border.CornerRadius = new CornerRadius(8.0);
		border.Padding = new Thickness(4.0);
		border.Child = content;
		border.Margin = new Thickness(0.0, 0.0, 12.0, 12.0);
		border.Effect = new DropShadowEffect
		{
			BlurRadius = 18.0,
			ShadowDepth = 0.0,
			Opacity = 0.55,
			Color = Colors.Black
		};
		Border border2 = border;
		TextElement.SetFontFamily(border2, w.FontFamily);
		TextElement.SetForeground(border2, Chalk);
		pop = new Popup
		{
			PlacementTarget = target,
			Placement = (above ? PlacementMode.Top : PlacementMode.Bottom),
			VerticalOffset = (above ? (-6) : 6),
			StaysOpen = false,
			AllowsTransparency = true,
			PopupAnimation = PopupAnimation.Fade,
			Child = border2
		};
		pop.IsOpen = true;
	}

	private string FamilyPhrase()
	{
		if (!string.IsNullOrEmpty(cfg.Family))
		{
			return "семьи " + cfg.Family;
		}
		return "вашей семьи";
	}

	private void UpdateFamily()
	{
		bool flag = !string.IsNullOrEmpty(cfg.Family);
		FamilyText.Text = (flag ? ("Семья " + cfg.Family) : (familyWanted ? "Семья: откройте планшет" : "Семья не прочитана"));
		FamilyDot.Fill = (flag ? GoB : (familyWanted ? Dim : WarnB));
		FamilyBtn.ToolTip = (flag ? ("Семья " + cfg.Family + (familyWanted ? "\nПроверю название при открытии планшета" : "")) : "Название семьи читается с планшета: подпись приложения на главном экране или заголовок меню семьи") + "\nНажмите, чтобы исправить или прочитать заново";
	}

	private List<string> KnownFamilies()
	{
		List<string> list = NetWatch.FamilyNames();
		if (war != null)
		{
			foreach (WarFamily family in war.Families)
			{
				if (!string.IsNullOrEmpty(family.Name))
				{
					list.Add(family.Name);
				}
			}
		}
		return list;
	}

	private void OnGameFamily(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			string text = cfg.Family ?? "";
			familyPending = null;
			familyForce = false;
			familyWanted = false;
			familyFails = 0;
			if (runner != null)
			{
				runner.WantFamily = false;
			}
			if (text == name)
			{
				UpdateFamily();
				return;
			}
			cfg.Family = name;
			UpdateFamily();
			ValidateSquare();
			SaveCfg();
			Log((text.Length == 0) ? ("Семья " + name + " — из событий игры.") : (Family.SameFamily(text, name) ? ("Семья " + name + " — из событий игры (было «" + text + "»).") : ("Семья сменилась: было «" + text + "», из событий игры — «" + name + "».")));
		}
	}

	private void OnFamily(FamilyInfo fi)
	{
		if (fi == null || !familyWanted)
		{
			return;
		}
		if (!string.IsNullOrEmpty(fi.Name))
		{
			familyFails = 0;
			if (fi.Where == "из событий игры")
			{
				if (!string.IsNullOrEmpty(fi.Seen))
				{
					Log("Семья: " + fi.Seen + " — беру название из событий игры.");
				}
				OnGameFamily(fi.Name);
				return;
			}
			string name = fi.Name;
			string text = Family.Snap(name, KnownFamilies());
			string text2 = text ?? name;
			string text3 = cfg.Family ?? "";
			bool flag = text3.Length > 0 && (Family.SameFamily(text3, text2) || Family.SameFamily(text3, name));
			string text4 = ((text != null && Family.MatchKey(text) != Family.MatchKey(name)) ? (" (распознано «" + name + "», это семья сервера «" + text + "»)") : "");
			if (text3.Length > 0 && !flag && !familyForce && (familyPending == null || !Family.SameFamily(familyPending, text2)))
			{
				familyPending = text2;
				Log("На планшете прочитано «" + text2 + "»" + text4 + ", а сохранено «" + text3 + "». Если прочитаю так же ещё раз — сменю семью (вдруг вы перешли в другую).");
				return;
			}
			string text5 = ((!flag) ? text2 : ((text != null && Family.MatchKey(text) == Family.MatchKey(text3)) ? text : text3));
			bool flag2 = text5 != text3;
			bool flag3 = text3.Length > 0 && !flag;
			familyPending = null;
			familyForce = false;
			cfg.Family = text5;
			familyWanted = false;
			if (runner != null)
			{
				runner.WantFamily = false;
			}
			if (!fi.Label.IsEmpty && fi.Where == "главный экран")
			{
				Runner.FamilyLabel = fi.Label;
				Runner.FamilyLabelName = text5;
			}
			UpdateFamily();
			ValidateSquare();
			SaveCfg();
			if (flag3)
			{
				Log("Семья сменилась: было «" + text3 + "», на планшете — «" + text5 + "»" + text4 + ".");
			}
			else
			{
				Log("Семья " + text5 + " — прочитано с планшета (" + fi.Where + ")" + text4 + (flag2 ? "." : ", без изменений."));
			}
			return;
		}
		familyFails++;
		if (familyFails == 1 || familyFails == 3)
		{
			Log("Семью прочитать не вышло (" + fi.Where + "): " + (fi.Error ?? "нет текста") + (string.IsNullOrEmpty(fi.Seen) ? "" : (". На снимке: " + fi.Seen)) + ((fi.ImagePath != null) ? (". Снимок подписи: " + fi.ImagePath) : "") + (string.IsNullOrEmpty(cfg.Family) ? ". Название можно вписать, нажав «Семья» вверху." : "."));
		}
		if (familyFails >= 3)
		{
			familyWanted = false;
			if (runner != null)
			{
				runner.WantFamily = false;
			}
			UpdateFamily();
		}
	}

	private void ShowFamily()
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Width = 320.0;
		stackPanel.Margin = new Thickness(12.0, 10.0, 12.0, 12.0);
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "Название семьи",
			Foreground = Fog,
			FontSize = 12.5,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		});
		System.Windows.Controls.TextBox tb = new System.Windows.Controls.TextBox
		{
			Text = (cfg.Family ?? ""),
			FontSize = 16.0,
			Background = CellBg,
			Foreground = Chalk,
			BorderBrush = RuleB,
			CaretBrush = SignalB,
			SelectionBrush = SignalB,
			Padding = new Thickness(8.0, 6.0, 8.0, 6.0),
			MaxLength = 32
		};
		stackPanel2.Children.Add(tb);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "Читается с планшета при первом его открытии: подпись приложения семьи на главном экране или заголовок меню семьи. Если прочиталось неверно — исправьте.",
			Foreground = Fog,
			FontSize = 12.0,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		});
		string text = new string[2] { "family_home.png", "family_menu.png" }.Select((string n) => System.IO.Path.Combine(dir, "calib", n)).Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc)
			.FirstOrDefault();
		if (text != null)
		{
			try
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
				bitmapImage.UriSource = new Uri(text);
				bitmapImage.EndInit();
				stackPanel2.Children.Add(new TextBlock
				{
					Text = "Что программа видела на планшете:",
					Foreground = Fog,
					FontSize = 12.0,
					Margin = new Thickness(0.0, 10.0, 0.0, 4.0)
				});
				stackPanel2.Children.Add(new Border
				{
					BorderBrush = RuleB,
					BorderThickness = new Thickness(1.0),
					HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
					Child = new System.Windows.Controls.Image
					{
						Source = bitmapImage,
						MaxHeight = 90.0,
						MaxWidth = 296.0,
						Stretch = Stretch.Uniform
					}
				});
			}
			catch
			{
			}
		}
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel3.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		StackPanel stackPanel4 = stackPanel3;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = (Style)w.FindResource("Primary");
		button.Content = "Сохранить";
		button.FontSize = 13.5;
		button.Height = 34.0;
		button.Padding = new Thickness(14.0, 0.0, 14.0, 0.0);
		System.Windows.Controls.Button button2 = button;
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = (Style)w.FindResource("Btn");
		button3.Content = "Прочитать заново";
		button3.FontSize = 13.5;
		button3.Height = 34.0;
		button3.Padding = new Thickness(14.0, 0.0, 14.0, 0.0);
		button3.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		System.Windows.Controls.Button button4 = button3;
		stackPanel4.Children.Add(button2);
		stackPanel4.Children.Add(button4);
		stackPanel2.Children.Add(stackPanel4);
		Action doSave = delegate
		{
			string text2 = (tb.Text ?? "").Trim();
			string text3 = cfg.Family ?? "";
			cfg.Family = ((text2.Length == 0) ? "" : (Family.Clean(text2) ?? text2));
			familyWanted = false;
			if (runner != null)
			{
				runner.WantFamily = false;
			}
			UpdateFamily();
			ValidateSquare();
			SaveCfg();
			if (cfg.Family != text3)
			{
				Log((cfg.Family.Length > 0) ? ("Семья " + cfg.Family + " — вписано вручную.") : "Название семьи очищено.");
			}
			if (pop != null)
			{
				pop.IsOpen = false;
			}
		};
		button2.Click += delegate
		{
			doSave();
		};
		tb.KeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				doSave();
			}
		};
		button4.Click += delegate
		{
			familyWanted = true;
			familyFails = 0;
			familyForce = true;
			familyPending = null;
			if (runner != null)
			{
				runner.WantFamily = true;
			}
			UpdateFamily();
			Log("Прочитаю название семьи, когда в игре будет открыт планшет (главный экран или меню семьи).");
			if (pop != null)
			{
				pop.IsOpen = false;
			}
		};
		ShowPopup(FamilyBtn, stackPanel2);
		tb.Focus();
		tb.SelectAll();
	}

	private void OnProbe(ProbeResult r)
	{
		if (!r.Unavailable)
		{
			lastProbeR = r;
			bool flag = r.Found && (r.W < 200 || r.H < 200);
			if (r.Found && !flag)
			{
				lastW = r.W;
				lastH = r.H;
			}
			System.Windows.Media.Brush fill;
			string text;
			string toolTip;
			if (!r.Found)
			{
				fill = StopB;
				text = "Игра не запущена";
				toolTip = "Окно GTA 5 не найдено";
			}
			else if (flag)
			{
				fill = WarnB;
				text = "Игра свёрнута";
				toolTip = "Разверните окно игры: в свёрнутом окне программа не видит планшет";
			}
			else if (r.Admin)
			{
				fill = WarnB;
				text = "Запустите TTC от администратора";
				toolTip = "Игра запущена от администратора — иначе нажатия до неё не дойдут";
			}
			else if (r.Black)
			{
				fill = WarnB;
				text = "Нужен режим «Оконный без рамки»";
				toolTip = "Снимок окна игры чёрный: в эксклюзивном полноэкранном режиме программа не видит игру";
			}
			else if (r.Calibrated)
			{
				fill = GoB;
				text = "Игра " + r.Size + ", калибровка ✓";
				toolTip = "Раскладка: " + r.Model + (r.PingKnown ? "\nПанель пинга найдена" : "\nПанель пинга ещё не найдена") + "\nНажмите: подготовить под это окно (все замеры заранее) или откалибровать заново";
			}
			else
			{
				fill = WarnB;
				text = "Игра " + r.Size + ": нужна калибровка";
				toolTip = "Под окно " + r.Size + " калибровки ещё нет.\nНажмите → «Подготовить под это окно»: бот заранее померит планшет, карту и поля формы (кнопку атаки не нажмёт).\nИли просто откройте в игре планшет — раскладка подберётся сама";
			}
			RdGameDot.Fill = fill;
			RdGame.Text = text;
			GameBtn.ToolTip = toolTip;
			if (r.Note != null && r.Note != lastNote)
			{
				Log(r.Note);
				lastNote = r.Note;
			}
			Telemetry.Game(r.Found, r.Size, r.Model, r.Calibrated, r.Black);
			PrepHint(r);
			if (r.Rtt > 0)
			{
				OnPing(r.Rtt, r.Fps, -1.0);
			}
			else if (r.Covered)
			{
				PingSub.Text = "панель пинга в игре закрыта другим окном";
			}
			else if (lastRtt <= 0)
			{
				PingSub.Text = ((!r.Found) ? "игра не запущена" : (r.PingKnown ? "жду, когда панель RTT будет на тёмном фоне" : "ищу в игре панель FPS/RTT"));
			}
			UpdateReadyPing();
			RdTabDot.Fill = ((r.Tablet == null || r.Tablet == "игра свёрнута") ? Dim : ((r.Tablet.StartsWith("не видно") || r.Tablet.StartsWith("снимок")) ? WarnB : GoB));
			RdTab.Text = ((r.Tablet == null) ? "Планшет не виден" : ("Планшет: " + r.Tablet));
			if (r.Family != null)
			{
				OnFamily(r.Family);
			}
		}
	}

	private void AskRecalibrate()
	{
		if (!running && lastW > 0)
		{
			string text = lastW + "×" + lastH;
			if (System.Windows.MessageBox.Show(w, "Откалибровать заново под окно " + text + "?\n\nПрограмма забудет раскладку планшета, замеры карты, место панели пинга и выученные цифры для этого размера окна и подберёт их снова, когда вы откроете планшет.", "TTC", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				Calib.Reset(lastW, lastH);
				Log("Калибровка под окно " + text + " сброшена — откройте в игре планшет.");
			}
		}
	}

	private Style St(string k)
	{
		return (Style)w.FindResource(k);
	}

	private void InitWar()
	{
		if (demo)
		{
			return;
		}
		if (ServerBtn.Parent is System.Windows.Controls.Panel panel)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			StackPanel stackPanel2 = stackPanel;
			stackPanel2.Children.Add(Icons.Make("map", 15.0, SignalB));
			stackPanel2.Children.Add(new TextBlock
			{
				Text = "Карта",
				FontSize = 13.0,
				Margin = new Thickness(7.0, 0.0, 0.0, 0.0),
				Foreground = Chalk
			});
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = stackPanel2;
			button.Margin = new Thickness(0.0, 0.0, 20.0, 0.0);
			button.ToolTip = "Карта территорий сервера: владельцы, атаки, защиты и КД семей. Клик по зоне — цель забива";
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				OpenMap();
			};
			panel.Children.Insert(panel.Children.IndexOf(ServerBtn), button2);
		}
		if (ServerBtn.Content is System.Windows.Controls.Panel panel2)
		{
			titleServerIcon = new System.Windows.Controls.Image
			{
				Width = 18.0,
				Height = 18.0,
				Margin = new Thickness(8.0, 0.0, -2.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				Visibility = Visibility.Collapsed
			};
			RenderOptions.SetBitmapScalingMode(titleServerIcon, BitmapScalingMode.HighQuality);
			panel2.Children.Insert(1, titleServerIcon);
		}
		SetServerIcons(cfg.Server);
		InitTargetMap();
		SquareBox.TextChanged += delegate
		{
			UpdateWarInfo();
		};
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromSeconds(1.0);
		DispatcherTimer dispatcherTimer2 = dispatcherTimer;
		dispatcherTimer2.Tick += delegate
		{
			string text = cfg.Server ?? "";
			if (text != warServerSeen)
			{
				hist = null;
				histFamily = "";
				histOpenDays.Clear();
				histNext = DateTime.MinValue;
				SetServerIcons(text);
			}
			if (MapDemo && text != warServerSeen)
			{
				LoadDemoWar();
				RenderMap();
			}
			if (!MapDemo && text != warServerSeen)
			{
				warServerSeen = text;
				war = null;
				warNext = DateTime.MinValue;
				RenderMap();
			}
			if (!MapDemo && DateTime.UtcNow >= warNext)
			{
				LoadWar();
			}
			if (MapOpen && histTab && DateTime.UtcNow >= histNext)
			{
				LoadHistory();
			}
			UpdateWarInfo();
			if (++warTick % 5 == 0)
			{
				RenderTargetMap();
			}
			if (MapOpen && mapView != null && warTick % 5 == 0)
			{
				mapView.InvalidateVisual();
				if (mapView.Hover != null && mapHoverBox.Visibility == Visibility.Visible)
				{
					ShowHover(mapView.Hover);
				}
				if (cardTicks && !mapMarking && warTick % 20 == 0)
				{
					RenderCard();
				}
			}
		};
		dispatcherTimer2.Start();
		if (MapDemo)
		{
			LoadDemoWar();
			return;
		}
		Runner.AttackPlaced += delegate(Job j, DateTime arrival)
		{
			w.Dispatcher.BeginInvoke((Action)delegate
			{
				ReportAttack(j, arrival);
			});
		};
		DispatcherTimer dispatcherTimer3 = new DispatcherTimer();
		dispatcherTimer3.Interval = TimeSpan.FromSeconds(4.0);
		DispatcherTimer dispatcherTimer4 = dispatcherTimer3;
		dispatcherTimer4.Tick += delegate
		{
			PatrolTick();
		};
		dispatcherTimer4.Start();
	}

	private void PatrolTick()
	{
		string srv = cfg.Server ?? "";
		string fam = (cfg.Family ?? "").Trim();
		if (running || closing || patrolBusy || srv.Length == 0 || License.SavedKey().Length == 0)
		{
			return;
		}
		if (GameFeedsMap)
		{
			if (!patrolNetLogged)
			{
				Log("Дозор по экрану выключен: капты, владельцы и лимиты семьи приходят из событий игры.");
				patrolNetLogged = true;
			}
			return;
		}
		patrolNetLogged = false;
		patrolBusy = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string why = null;
			string err = null;
			PatrolScan sc = null;
			try
			{
				sc = Patrol.Scan(out why);
				if (sc != null)
				{
					err = War.ReportSeen(srv, sc.At, sc.R0, sc.R1, sc.C0, sc.C1, sc.Marks, fam, sc.Limits, sc.MayEnd);
				}
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					patrolBusy = false;
					if (sc == null)
					{
						if (why != null && why != patrolWhy)
						{
							patrolWhy = why;
						}
					}
					else
					{
						patrolWhy = null;
						if (err != null)
						{
							if (!patrolLogged)
							{
								Log("Дозор: не отправлено на карту TTC: " + err + ".");
							}
							patrolLogged = true;
						}
						else
						{
							if (!patrolLogged)
							{
								Log("Дозор: карта в планшете открыта — капты, которые на ней видно" + ((fam.Length > 0) ? (", и «⚔/\ud83d\udee1» семьи " + fam) : "") + " уходят на общую карту TTC. Программа ничего не нажимает.");
								patrolLogged = true;
							}
							if (sc.Limits != null && (patrolLimits == null || !sc.Limits.SequenceEqual(patrolLimits)))
							{
								patrolLimits = sc.Limits;
								if (fam.Length > 0)
								{
									Log("Дозор: в игре у " + fam + " атак " + sc.Limits[0] + "/" + sc.Limits[1] + ", защит " + sc.Limits[2] + "/" + sc.Limits[3] + ".");
								}
							}
						}
					}
				});
			}
			catch
			{
				patrolBusy = false;
			}
		});
	}

	private void LoadDemoWar()
	{
		warServerSeen = cfg.Server ?? "";
		string text = System.IO.Path.Combine(dir, "war_demo_" + warServerSeen.ToLowerInvariant() + ".json");
		if (!File.Exists(text))
		{
			text = System.IO.Path.Combine(dir, "war_demo.json");
		}
		try
		{
			war = War.FromJson(File.ReadAllText(text, Encoding.UTF8));
			if (string.IsNullOrEmpty(cfg.Server))
			{
				cfg.Server = (warServerSeen = (war.Server ?? "").ToLowerInvariant());
			}
			Log("Демо карты: " + System.IO.Path.GetFileName(text) + " (" + war.Server + ", " + war.Families.Count + " семей, " + war.OwnerIdx.Count + " зон с владельцами). " + (string.Equals(war.Server, cfg.Server, StringComparison.OrdinalIgnoreCase) ? "" : ("Для выбранного сервера демо-данных нет — показан " + war.Server + ". ")) + "На сервер TTC ничего не отправляется.");
			UpdateServer();
			UpdateFamily();
			UpdateWarInfo();
		}
		catch (Exception ex)
		{
			war = null;
			Log("Демо карты: не прочитан " + text + ": " + ex.Message);
		}
	}

	private WarFamily DemoFamily(string name)
	{
		WarFamily warFamily = war.Family(name);
		if (warFamily != null)
		{
			return warFamily;
		}
		WarFamily warFamily2 = new WarFamily();
		warFamily2.Index = war.Families.Count;
		warFamily2.Name = name;
		warFamily2.Key = Zones.Key(name);
		warFamily2.AtkMax = war.Attacks;
		warFamily2.DefMax = war.Defenses;
		warFamily = warFamily2;
		war.Families.Add(warFamily);
		return warFamily;
	}

	private void LoadWar()
	{
		LoadWar(urgent: false);
	}

	private void LoadWar(bool urgent)
	{
		string srv = cfg.Server ?? "";
		bool mapOpen = MapOpen;
		warNext = DateTime.UtcNow.AddSeconds(mapOpen ? 30 : 60);
		if (closing || srv.Length == 0 || License.SavedKey().Length == 0)
		{
			return;
		}
		bool flag = warWaiting && (urgent || warInflightSrv != srv);
		if (warLoading && !flag)
		{
			return;
		}
		WarState prev = war;
		int wait = ((!urgent && mapOpen && prev != null && string.Equals(prev.Server, srv, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(prev.StateVer)) ? 25 : 0);
		int gen = ++warGen;
		warLoading = true;
		warWaiting = wait > 0;
		warInflightSrv = srv;
		ThreadPool.QueueUserWorkItem(delegate
		{
			WarState st;
			string err;
			try
			{
				st = War.Load(srv, prev, wait, out err);
			}
			catch (Exception ex)
			{
				st = null;
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					if (gen == warGen)
					{
						warLoading = false;
						warWaiting = false;
						if (!(srv != (cfg.Server ?? "")))
						{
							if (st != null)
							{
								if (warError != null)
								{
									Log("Карта территорий снова обновляется.");
								}
								warError = null;
								war = st;
								if (MapOpen)
								{
									warNext = ((!string.IsNullOrEmpty(st.StateVer)) ? DateTime.UtcNow : DateTime.UtcNow.AddSeconds(20.0));
								}
							}
							else
							{
								if (warError == null)
								{
									Log("Карта территорий не обновилась: " + err + ".");
								}
								warError = err;
								warNext = DateTime.UtcNow.AddSeconds(30.0);
							}
							RenderMap();
							UpdateWarInfo();
						}
					}
				});
			}
			catch
			{
				if (gen == warGen)
				{
					warLoading = false;
					warWaiting = false;
				}
			}
		});
	}

	private void UpdateWarInfo()
	{
		string text = (SquareBox.Text ?? "").Trim().ToUpperInvariant();
		Zone zone = Zones.ByCode(text);
		if (zone == null || war == null)
		{
			return;
		}
		WarFamily warFamily = war.OwnerOf(zone);
		long start = War.Unix(WarRules.CaptUtc(cfg.CaptH, cfg.CaptM, Clock.UtcNow()));
		SlotCheck slotCheck = WarRules.Check(war, cfg.Family, zone, start);
		string text2 = ((zone.Label == text) ? text : (text + " входит в зону " + zone.Label)) + ((warFamily == null) ? ": владелец неизвестен или зона свободна." : (": " + warFamily.Title + ", защит осталось " + warFamily.DefLeft + " из " + warFamily.DefMax + ((warFamily.DefCd != null && warFamily.DefCd[1] > War.NowUnix()) ? (", КД до " + War.Window(warFamily.DefCd)) : "") + "."));
		int num = slotCheck.Level;
		if (cfg.AfterCapt)
		{
			AsapVerdict asapVerdict = asapV;
			num = ((asapVerdict != null && asapVerdict.Level == 2) ? 2 : 0);
		}
		else
		{
			string text3 = text2;
			text2 = text3 + " Капт в " + cfg.CaptH.ToString("00") + ":" + cfg.CaptM.ToString("00") + ": " + slotCheck.Text + ".";
		}
		if (TargetAdjacent(text) == false)
		{
			text2 = text2 + " Не граничит с территорией " + FamilyPhrase() + ": атаковать можно только соседние зоны.";
			if (num < 1)
			{
				num = 1;
			}
		}
		text2 += FamilyTargetNote(text);
		if (SquareInfo.Text != text2)
		{
			SquareInfo.Text = text2;
		}
		SquareInfo.Foreground = num switch
		{
			1 => WarnB, 
			2 => StopB, 
			_ => Fog, 
		};
	}

	private void InitTargetMap()
	{
		targetTip = new System.Windows.Controls.ToolTip
		{
			Content = "Колесо — масштаб, перетаскивание — сдвиг, клик — сделать целью, двойной клик — окно карты"
		};
		targetMap.ToolTip = targetTip;
		ToolTipService.SetShowDuration(targetMap, 60000);
		targetMap.HoverChanged += delegate(Zone z)
		{
			targetTip.Content = ((z == null) ? "Колесо — масштаб, перетаскивание — сдвиг, клик — сделать целью, двойной клик — окно карты" : (ZoneText(z) + "\n\nклик — сделать целью, двойной клик — окно карты"));
		};
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(Icons.Make("external", 12.0, Fog));
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "Карта",
			FontSize = 12.0,
			Foreground = Chalk,
			Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Plain");
		button.ToolTip = "Открыть карту территорий: владельцы, капты, семьи сервера";
		button.Margin = new Thickness(6.0);
		button.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		button.VerticalAlignment = VerticalAlignment.Top;
		button.Content = new Border
		{
			Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 29, 32, 38)),
			BorderBrush = RuleB,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(5.0),
			Padding = new Thickness(7.0, 3.0, 8.0, 4.0),
			Child = stackPanel2
		};
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			OpenMap();
		};
		TargetGrid.Children.Add(button2);
		TargetGrid.PreviewMouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
		{
			if (e.ClickCount >= 2)
			{
				e.Handled = true;
				OpenMap();
			}
		};
	}

	private void RenderTargetMap()
	{
		if (targetMap != null)
		{
			targetMap.State = war;
			targetMap.OwnKey = Zones.Key(cfg.Family);
			targetMap.InvalidateVisual();
		}
	}

	private void PaintWatchCells()
	{
		if (targetMap != null)
		{
			AsapCapt asapCapt = ((cfg.AfterCapt && asapV != null && asapV.Level == 1) ? asapV.MainCapt : null);
			Zone zone = ((asapCapt != null) ? Zones.ById(asapCapt.Zone) : null);
			if (zone != targetMap.Watch)
			{
				targetMap.Watch = zone;
				targetMap.InvalidateVisual();
			}
		}
	}

	private void ReportAttack(Job j, DateTime arrivalUtc)
	{
		string srv = cfg.Server ?? "";
		string fam = ((!string.IsNullOrEmpty(j.Family)) ? j.Family : cfg.Family);
		if (srv.Length == 0 || string.IsNullOrEmpty(fam))
		{
			Log("Атака не отправлена на карту: " + ((srv.Length == 0) ? "не выбран сервер" : "не известна ваша семья") + ".");
			return;
		}
		DateTime capt = WarRules.CaptUtc(j.CaptH, j.CaptM, arrivalUtc);
		string square = j.Square;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err = War.Report(srv, fam, square, capt, arrivalUtc, manual: false);
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					Log((err == null) ? ("Атака " + fam + " на " + square + " (капт " + Clock.Msk(capt).ToString("HH:mm") + ") отправлена на карту — её видят все копии TTC на сервере.") : ("Атака не отправлена на карту: " + err + "."));
					warNext = DateTime.MinValue;
				});
			}
			catch
			{
			}
		});
	}

	private void OpenMap()
	{
		if (mapWin != null && mapWin.IsLoaded)
		{
			if (mapWin.WindowState == WindowState.Minimized)
			{
				mapWin.WindowState = WindowState.Normal;
			}
			mapWin.Activate();
			SelectFromSquare();
		}
		else
		{
			BuildMap();
			mapWin.Show();
			mapWin.Dispatcher.BeginInvoke((Action)delegate
			{
				SelectFromSquare();
			}, DispatcherPriority.Loaded);
			warNext = DateTime.MinValue;
		}
	}

	private void BuildMap()
	{
		mapWin = new Window
		{
			Title = "TTC — карта территорий",
			Width = 1260.0,
			Height = 860.0,
			MinWidth = 900.0,
			MinHeight = 560.0,
			WindowStyle = WindowStyle.None,
			ResizeMode = ResizeMode.CanResize,
			WindowStartupLocation = WindowStartupLocation.CenterOwner,
			Background = (System.Windows.Media.Brush)w.FindResource("Asphalt"),
			FontFamily = w.FontFamily,
			FontSize = 14.0,
			Foreground = Chalk,
			ShowInTaskbar = true,
			Icon = w.Icon
		};
		if (w.IsLoaded)
		{
			mapWin.Owner = w;
		}
		TextOptions.SetTextFormattingMode(mapWin, TextFormattingMode.Display);
		WindowChrome.SetWindowChrome(mapWin, new WindowChrome
		{
			CaptionHeight = 52.0,
			ResizeBorderThickness = new Thickness(6.0),
			GlassFrameThickness = new Thickness(0.0),
			CornerRadius = new CornerRadius(0.0),
			UseAeroCaptionButtons = false
		});
		Grid grid = new Grid();
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(52.0)
		});
		grid.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		Border frame = new Border
		{
			BorderBrush = RuleB,
			BorderThickness = new Thickness(1.0),
			Background = (System.Windows.Media.Brush)w.FindResource("Asphalt"),
			Child = grid
		};
		TextElement.SetForeground(frame, Chalk);
		TextElement.SetFontFamily(frame, w.FontFamily);
		mapWin.Content = frame;
		mapWin.StateChanged += delegate
		{
			frame.Margin = ((mapWin.WindowState == WindowState.Maximized) ? new Thickness(7.0) : new Thickness(0.0));
		};
		Grid grid2 = new Grid();
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		DockPanel dockPanel = new DockPanel();
		dockPanel.VerticalAlignment = VerticalAlignment.Center;
		dockPanel.Margin = new Thickness(20.0, 0.0, 12.0, 0.0);
		DockPanel dockPanel2 = dockPanel;
		mapTitle = new TextBlock
		{
			FontSize = 17.0,
			FontWeight = FontWeights.SemiBold,
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		mapSource = new TextBlock
		{
			FontSize = 12.5,
			Foreground = Fog,
			Margin = new Thickness(14.0, 2.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		mapServerIcon = new System.Windows.Controls.Image
		{
			Width = 22.0,
			Height = 22.0,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		RenderOptions.SetBitmapScalingMode(mapServerIcon, BitmapScalingMode.HighQuality);
		DockPanel.SetDock(mapServerIcon, Dock.Left);
		DockPanel.SetDock(mapTitle, Dock.Left);
		dockPanel2.Children.Add(mapServerIcon);
		dockPanel2.Children.Add(mapTitle);
		dockPanel2.Children.Add(mapSource);
		grid2.Children.Add(dockPanel2);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
		StackPanel stackPanel2 = stackPanel;
		Grid.SetColumn(stackPanel2, 1);
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Plain");
		button.Content = new TextBlock
		{
			Text = "Обновить",
			FontSize = 13.0,
			Foreground = Fog
		};
		button.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			warNext = DateTime.MinValue;
			LoadWar(urgent: true);
		};
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = St("Plain");
		button3.Content = new TextBlock
		{
			Text = "Уведомления в Telegram",
			FontSize = 13.0,
			Foreground = SignalB
		};
		button3.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
		button3.ToolTip = "Бот пришлёт: на вашу семью объявлен капт или он замечен на её квадрате, капт закончился и кто победил";
		System.Windows.Controls.Button button4 = button3;
		button4.Click += delegate
		{
			OpenTelegram();
		};
		System.Windows.Controls.Button button5 = new System.Windows.Controls.Button();
		button5.Style = St("Plain");
		button5.Content = new TextBlock
		{
			Text = "Вся карта",
			FontSize = 13.0,
			Foreground = Fog
		};
		button5.Margin = new Thickness(0.0, 0.0, 14.0, 0.0);
		System.Windows.Controls.Button button6 = button5;
		button6.Click += delegate
		{
			mapView.Fit();
		};
		System.Windows.Controls.Button button7 = new System.Windows.Controls.Button();
		button7.Style = St("WinBtn");
		button7.Content = Icons.Make("minimize", 13.0);
		System.Windows.Controls.Button button8 = button7;
		button8.Click += delegate
		{
			mapWin.WindowState = WindowState.Minimized;
		};
		System.Windows.Controls.Button button9 = new System.Windows.Controls.Button();
		button9.Style = St("WinBtn");
		button9.Content = Icons.Make("maximize", 13.0);
		button9.ToolTip = "Развернуть на весь экран";
		System.Windows.Controls.Button button10 = button9;
		button10.Click += delegate
		{
			mapWin.WindowState = ((mapWin.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
		};
		System.Windows.Controls.Button button11 = new System.Windows.Controls.Button();
		button11.Style = St("WinClose");
		button11.Content = Icons.Make("close", 13.0);
		System.Windows.Controls.Button button12 = button11;
		button12.Click += delegate
		{
			mapWin.Close();
		};
		System.Windows.Controls.Button[] array = new System.Windows.Controls.Button[6] { button4, button2, button6, button8, button10, button12 };
		foreach (System.Windows.Controls.Button button13 in array)
		{
			WindowChrome.SetIsHitTestVisibleInChrome(button13, hitTestVisible: true);
			stackPanel2.Children.Add(button13);
		}
		grid2.Children.Add(stackPanel2);
		System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
		rectangle.Height = 1.0;
		rectangle.Fill = RuleB;
		rectangle.VerticalAlignment = VerticalAlignment.Bottom;
		System.Windows.Shapes.Rectangle element = rectangle;
		Grid.SetColumnSpan(element, 2);
		grid2.Children.Add(element);
		grid.Children.Add(grid2);
		Grid grid3 = new Grid();
		Grid.SetRow(grid3, 1);
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(384.0)
		});
		grid.Children.Add(grid3);
		Grid grid4 = new Grid();
		mapView = new MapView();
		grid4.Children.Add(mapView);
		mapHover = new TextBlock
		{
			FontSize = 13.0,
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 420.0
		};
		mapHoverBox = new Border
		{
			Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(230, 29, 32, 38)),
			BorderBrush = RuleB,
			BorderThickness = new Thickness(1.0),
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(10.0, 6.0, 10.0, 7.0),
			Margin = new Thickness(12.0),
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Child = mapHover,
			Visibility = Visibility.Collapsed,
			IsHitTestVisible = false
		};
		grid4.Children.Add(mapHoverBox);
		grid4.Children.Add(new TextBlock
		{
			Text = "Колесо — масштаб, перетаскивание — сдвиг, клик — выбрать зону",
			FontSize = 12.0,
			Foreground = Fog,
			IsHitTestVisible = false,
			Margin = new Thickness(14.0, 0.0, 0.0, 10.0),
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Bottom
		});
		grid3.Children.Add(grid4);
		Grid grid5 = new Grid();
		grid5.Background = Slate;
		Grid grid6 = grid5;
		Grid.SetColumn(grid6, 1);
		grid6.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid6.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid6.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.Children.Add(grid6);
		System.Windows.Shapes.Rectangle rectangle2 = new System.Windows.Shapes.Rectangle();
		rectangle2.Width = 1.0;
		rectangle2.Fill = RuleB;
		rectangle2.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		rectangle2.IsHitTestVisible = false;
		System.Windows.Shapes.Rectangle element2 = rectangle2;
		Grid.SetColumn(element2, 1);
		grid3.Children.Add(element2);
		mapCard = new StackPanel
		{
			Margin = new Thickness(18.0, 16.0, 18.0, 14.0)
		};
		grid6.Children.Add(mapCard);
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Margin = new Thickness(18.0, 4.0, 18.0, 8.0);
		StackPanel stackPanel4 = stackPanel3;
		Grid.SetRow(stackPanel4, 1);
		stackPanel4.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Height = 1.0,
			Fill = RuleB,
			Margin = new Thickness(-18.0, 0.0, -18.0, 12.0)
		});
		StackPanel stackPanel5 = new StackPanel();
		stackPanel5.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel5.Margin = new Thickness(-8.0, 0.0, 0.0, 8.0);
		StackPanel stackPanel6 = stackPanel5;
		tabFamBtn = new System.Windows.Controls.Button
		{
			Style = St("Plain"),
			Content = "Семьи сервера",
			FontSize = 14.0,
			Padding = new Thickness(8.0, 4.0, 8.0, 4.0)
		};
		tabHistBtn = new System.Windows.Controls.Button
		{
			Style = St("Plain"),
			Content = "История каптов",
			FontSize = 14.0,
			Padding = new Thickness(8.0, 4.0, 8.0, 4.0),
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0)
		};
		tabFamBtn.Click += delegate
		{
			ShowFamiliesTab();
		};
		tabHistBtn.Click += delegate
		{
			ShowHistoryTab();
		};
		stackPanel6.Children.Add(tabFamBtn);
		stackPanel6.Children.Add(tabHistBtn);
		stackPanel4.Children.Add(stackPanel6);
		mapSearch = new System.Windows.Controls.TextBox
		{
			Style = St("Field"),
			FontSize = 14.0,
			Height = 34.0,
			Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
			ToolTip = "Поиск по названию или тегу семьи"
		};
		TextBlock ph = new TextBlock
		{
			Text = "Поиск по названию или тегу",
			Foreground = Dim,
			FontSize = 13.5,
			Margin = new Thickness(11.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			IsHitTestVisible = false
		};
		mapSearch.TextChanged += delegate
		{
			ph.Visibility = ((mapSearch.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			RenderFamilies();
		};
		Grid grid7 = new Grid();
		grid7.Children.Add(mapSearch);
		grid7.Children.Add(ph);
		stackPanel4.Children.Add(grid7);
		famSearchBox = grid7;
		BuildHistory(out var top, out var list);
		histTopBox = top;
		stackPanel4.Children.Add(top);
		grid6.Children.Add(stackPanel4);
		mapFamilies = new StackPanel
		{
			Margin = new Thickness(10.0, 0.0, 10.0, 12.0)
		};
		famScroll = new ScrollViewer
		{
			Content = mapFamilies,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		Grid.SetRow(famScroll, 2);
		grid6.Children.Add(famScroll);
		histScroll = new ScrollViewer
		{
			Content = list,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		};
		Grid.SetRow(histScroll, 2);
		grid6.Children.Add(histScroll);
		if (histTab)
		{
			ShowHistoryTab();
		}
		else
		{
			ShowFamiliesTab();
		}
		mapView.ZoneClick += delegate
		{
			mapMarking = false;
			RenderCard();
		};
		mapView.HoverChanged += delegate(Zone z)
		{
			ShowHover(z);
		};
		mapWin.Closed += delegate
		{
			mapWin = null;
			mapView = null;
			histList = null;
		};
		mapWin.KeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				mapWin.Close();
			}
		};
		RenderMap();
	}

	private void ShowFamiliesTab()
	{
		histTab = false;
		if (tabFamBtn != null)
		{
			tabFamBtn.Foreground = Chalk;
			tabHistBtn.Foreground = Fog;
			mapCard.Visibility = Visibility.Visible;
			famSearchBox.Visibility = Visibility.Visible;
			famScroll.Visibility = Visibility.Visible;
			histTopBox.Visibility = Visibility.Collapsed;
			histScroll.Visibility = Visibility.Collapsed;
		}
	}

	private void ShowHistoryTab()
	{
		histTab = true;
		if (tabFamBtn != null)
		{
			tabFamBtn.Foreground = Fog;
			tabHistBtn.Foreground = Chalk;
			mapCard.Visibility = Visibility.Collapsed;
			famSearchBox.Visibility = Visibility.Collapsed;
			famScroll.Visibility = Visibility.Collapsed;
			histTopBox.Visibility = Visibility.Visible;
			histScroll.Visibility = Visibility.Visible;
			RenderHistory();
			if (hist == null || DateTime.UtcNow >= histNext)
			{
				LoadHistory();
			}
		}
	}

	private void SetServerIcons(string server)
	{
		if (string.IsNullOrEmpty(server))
		{
			if (titleServerIcon != null)
			{
				titleServerIcon.Visibility = Visibility.Collapsed;
			}
			return;
		}
		Images.Get("/war/server-icon/" + server.ToLowerInvariant() + ".svg", delegate(ImageSource src)
		{
			if (string.Equals(server, cfg.Server, StringComparison.OrdinalIgnoreCase))
			{
				System.Windows.Controls.Image[] array = new System.Windows.Controls.Image[2] { titleServerIcon, mapServerIcon };
				foreach (System.Windows.Controls.Image image in array)
				{
					if (image != null)
					{
						image.Source = src;
						image.Visibility = Visibility.Visible;
					}
				}
			}
		});
	}

	private void SelectFromSquare()
	{
		if (mapView != null)
		{
			Zone zone = Zones.ByCode((SquareBox.Text ?? "").Trim().ToUpperInvariant());
			if (zone != null)
			{
				mapView.Selected = zone;
				mapView.CenterOn(zone);
				RenderCard();
			}
		}
	}

	private void RenderMap()
	{
		RenderTargetMap();
		if (mapView != null)
		{
			ServerInfo serverInfo = CurrentServer();
			mapTitle.Text = "Карта территорий" + ((serverInfo != null) ? (": " + serverInfo.Title) : "");
			SetServerIcons(cfg.Server);
			mapSource.Text = (string.IsNullOrEmpty(cfg.Server) ? "выберите сервер вверху главного окна" : ((License.SavedKey().Length == 0) ? "нужен ключ программы" : ((war != null) ? (war.SourceText + ((warError != null) ? ". Сейчас нет связи с сервером TTC" : "")) : ((warError != null) ? ("нет связи с сервером TTC: " + warError) : "загружаю…"))));
			mapView.State = war;
			mapView.OwnKey = Zones.Key(cfg.Family);
			mapView.InvalidateVisual();
			RenderCard();
			RenderFamilies();
		}
	}

	private void ShowHover(Zone z)
	{
		if (z == null)
		{
			mapHoverBox.Visibility = Visibility.Collapsed;
			return;
		}
		mapHover.Text = ZoneText(z);
		mapHoverBox.Visibility = Visibility.Visible;
	}

	private string ZoneText(Zone z)
	{
		WarFamily warFamily = ((war == null) ? null : war.OwnerOf(z));
		ZoneType zoneType = ZoneTypes.Of(z.Sub);
		string text = z.Label + ((zoneType != null) ? (", " + zoneType.Name + (zoneType.Business ? " (бизнес)" : "")) : "") + "\n" + ((warFamily == null) ? "без владельца" : (warFamily.Title + "\nатак " + warFamily.AtkLeft + " из " + warFamily.AtkMax + ", защит " + warFamily.DefLeft + " из " + warFamily.DefMax));
		if (warFamily != null)
		{
			text += ((StatusLine(warFamily, War.NowUnix()).Length > 0) ? ("\n" + StatusLine(warFamily, War.NowUnix())) : "");
		}
		long now = War.NowUnix();
		if (war != null)
		{
			foreach (WarCapt item in (from c in war.Active(now, z)
				orderby c.Start
				select c).Take(2))
			{
				string text2 = text;
				text = text2 + "\nкапт: " + item.A + " → " + (item.D ?? "?") + ", " + CaptState(item, now);
			}
		}
		LiveCapt liveCapt = ((war == null) ? null : war.LiveAt(z));
		if (liveCapt != null)
		{
			text = text + "\n" + liveCapt.StateText(now) + " (видят игроки TTC)";
		}
		return text;
	}

	private string LiveLine(WarFamily f, long now)
	{
		if (war == null)
		{
			return "";
		}
		List<string> list = new List<string>();
		foreach (LiveCapt item in war.Live)
		{
			if (item.Active(now))
			{
				Zone zone = Zones.ById(item.ZoneId);
				WarFamily warFamily = war.OwnerOf(zone);
				if (warFamily != null && warFamily.Key == f.Key)
				{
					list.Add(((item.StartedAt > 0) ? "идёт капт на " : "запланирован капт на ") + zone.Label);
				}
			}
		}
		return string.Join(", ", list);
	}

	private static string StatusLine(WarFamily f, long now)
	{
		List<string> list = new List<string>();
		if (f.InCaptEnd != null && f.InCaptEnd[1] > now)
		{
			list.Add(((f.InCaptRole == "d") ? "защищается" : "атакует") + " до " + War.Window(f.InCaptEnd));
		}
		if (f.DefCd != null && f.DefCd[1] > now)
		{
			list.Add("КД защиты до " + War.Window(f.DefCd));
		}
		if (f.AtkCd != null && f.AtkCd[1] > now)
		{
			list.Add("КД атаки до " + War.Window(f.AtkCd));
		}
		return string.Join(", ", list);
	}

	private void RenderCard()
	{
		if (mapCard == null)
		{
			return;
		}
		mapCard.Children.Clear();
		cardTicks = false;
		Zone z = ((mapView == null) ? null : mapView.Selected);
		long now = War.NowUnix();
		if (z == null)
		{
			mapCard.Children.Add(new TextBlock
			{
				Text = "Выберите зону",
				FontSize = 20.0,
				FontWeight = FontWeights.SemiBold,
				Margin = new Thickness(0.0, 0.0, 0.0, 8.0)
			});
			mapCard.Children.Add(new TextBlock
			{
				Text = "Кликните квадрат на карте: здесь будет его владелец, сколько у семьи осталось защит, её КД и можно ли поставить капт на выбранное время.",
				Style = St("Hint")
			});
			mapCard.Children.Add(Legend());
			return;
		}
		WarFamily f = ((war == null) ? null : war.OwnerOf(z));
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.Children.Add(new TextBlock
		{
			Text = z.Label,
			Style = St("Num"),
			FontSize = 40.0,
			Margin = new Thickness(0.0, -6.0, 16.0, 0.0),
			VerticalAlignment = VerticalAlignment.Top
		});
		StackPanel stackPanel = new StackPanel();
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel2 = stackPanel;
		Grid.SetColumn(stackPanel2, 1);
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel4 = stackPanel3;
		if (f != null)
		{
			FrameworkElement frameworkElement = FamBadge(f, 22.0);
			frameworkElement.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
			stackPanel4.Children.Add(frameworkElement);
		}
		stackPanel4.Children.Add(new TextBlock
		{
			Text = ((f != null) ? f.Title : ((war == null) ? "нет данных" : "без владельца")),
			FontSize = 15.0,
			FontWeight = FontWeights.SemiBold,
			TextTrimming = TextTrimming.CharacterEllipsis,
			MaxWidth = 230.0
		});
		stackPanel2.Children.Add(stackPanel4);
		string text = (z.Big ? "крупная зона 2×2" : "зона 1×1") + ((ZoneTypes.Of(z.Sub) != null) ? (". " + ZoneTypes.Describe(z.Sub)) : "");
		if (f != null && war.Since.TryGetValue(z.Id, out var value) && value > 0)
		{
			text = text + ", у семьи с " + Clock.Msk(War.FromUnix(value)).ToString("dd.MM HH:mm");
		}
		stackPanel2.Children.Add(new TextBlock
		{
			Text = text,
			FontSize = 12.5,
			Foreground = Fog,
			Margin = new Thickness(0.0, 3.0, 0.0, 0.0),
			TextWrapping = TextWrapping.Wrap
		});
		grid.Children.Add(stackPanel2);
		mapCard.Children.Add(grid);
		if (f != null)
		{
			StackPanel stackPanel5 = new StackPanel();
			stackPanel5.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel5.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
			StackPanel stackPanel6 = stackPanel5;
			stackPanel6.Children.Add(Stat("Атак осталось", f.AtkLeft, f.AtkMax));
			stackPanel6.Children.Add(Stat("Защит осталось", f.DefLeft, f.DefMax));
			mapCard.Children.Add(stackPanel6);
			string text2 = StatusLine(f, now);
			if (f.MansionSoon)
			{
				text2 = text2 + ((text2.Length > 0) ? ". " : "") + "Особняк взят сегодня: 3 защиты будут с 00:00";
			}
			if (text2.Length > 0)
			{
				mapCard.Children.Add(new TextBlock
				{
					Text = text2,
					Style = St("Hint"),
					Foreground = ((f.InCaptEnd != null || f.DefCd != null) ? WarnB : Fog),
					Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
				});
			}
			long[] array = war.IngameOf(f.Name);
			if (array != null)
			{
				mapCard.Children.Add(new TextBlock
				{
					Text = "В игре: ⚔ " + array[0] + "/" + array[1] + "   \ud83d\udee1 " + array[2] + "/" + array[3] + " — по планшету игрока TTC, " + War.Hm(array[4]) + ".",
					Style = St("Hint"),
					Foreground = Chalk,
					Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
				});
			}
			mapCard.Children.Add(FamilyStatsBlock(f));
		}
		string ownKey = Zones.Key(cfg.Family);
		if (ownKey.Length > 0 && war != null && (f == null || f.Key != ownKey) && war.OwnerIdx.Any((KeyValuePair<int, int> kv) => kv.Value >= 0 && kv.Value < war.Families.Count && war.Families[kv.Value].Key == ownKey))
		{
			bool flag = Zones.Neighbors(z).Any(delegate(Zone n)
			{
				WarFamily warFamily = war.OwnerOf(n);
				return warFamily != null && warFamily.Key == ownKey;
			});
			mapCard.Children.Add(new TextBlock
			{
				Text = (flag ? ("Граничит с территорией " + FamilyPhrase() + ".") : ("Не граничит с территорией " + FamilyPhrase() + ": атаковать можно только соседние зоны.")),
				Style = St("Hint"),
				Foreground = (flag ? GoB : WarnB),
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			});
		}
		if (war != null)
		{
			List<WarCapt> list = (from c in war.Capts
				where c.EndHi > now - 1800 && (c.ZoneId == z.Id || (f != null && (Zones.Key(c.A) == f.Key || Zones.Key(c.D) == f.Key)))
				orderby c.Start
				select c).Take(5).ToList();
			if (list.Count > 0)
			{
				mapCard.Children.Add(new TextBlock
				{
					Text = "Капты",
					Style = St("Label"),
					Margin = new Thickness(0.0, 14.0, 0.0, 4.0)
				});
				foreach (WarCapt item in list)
				{
					mapCard.Children.Add(CaptRow(item, now));
				}
			}
		}
		LiveCapt liveCapt = ((war == null) ? null : war.LiveAt(z));
		if (liveCapt != null)
		{
			mapCard.Children.Add(new TextBlock
			{
				Text = char.ToUpper(liveCapt.StateText(now)[0]) + liveCapt.StateText(now).Substring(1) + " — по картам игроков TTC.",
				Style = St("Hint"),
				Foreground = ((!liveCapt.Active(now)) ? Fog : ((liveCapt.StartedAt > 0) ? StopB : WarnB)),
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
			});
		}
		long start = War.Unix(WarRules.CaptUtc(cfg.CaptH, cfg.CaptM, Clock.UtcNow()));
		SlotCheck slotCheck = WarRules.Check(war, cfg.Family, z, start);
		mapCard.Children.Add(new TextBlock
		{
			Text = "Капт в " + cfg.CaptH.ToString("00") + ":" + cfg.CaptM.ToString("00") + ": " + slotCheck.Text + ".",
			Style = St("Hint"),
			FontSize = 13.0,
			Foreground = ((slotCheck.Level == 2) ? StopB : ((slotCheck.Level == 1) ? WarnB : ((slotCheck.Level == 0) ? GoB : Fog))),
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		});
		WrapPanel wrapPanel = new WrapPanel();
		wrapPanel.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		WrapPanel wrapPanel2 = wrapPanel;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Primary");
		button.Content = "Сделать целью";
		button.FontSize = 13.5;
		button.Height = 36.0;
		button.Padding = new Thickness(14.0, 0.0, 14.0, 0.0);
		button.Margin = new Thickness(0.0, 0.0, 8.0, 8.0);
		System.Windows.Controls.Button button2 = button;
		button2.IsEnabled = !running;
		button2.Click += delegate
		{
			SquareBox.Text = z.Label;
			Log("Цель с карты: " + z.Label + ((f != null) ? (" (" + f.Title + ")") : "") + ".");
			RenderCard();
		};
		wrapPanel2.Children.Add(button2);
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = St("Btn");
		button3.Content = "Ближайшее время";
		button3.FontSize = 13.5;
		button3.Height = 36.0;
		button3.Padding = new Thickness(14.0, 0.0, 14.0, 0.0);
		button3.Margin = new Thickness(0.0, 0.0, 8.0, 8.0);
		button3.ToolTip = "Поставить время капта на ближайшие 5 минут, когда у цели и у вашей семьи нет КД и хватает атак и защит";
		System.Windows.Controls.Button button4 = button3;
		button4.IsEnabled = war != null && !running;
		button4.Click += delegate
		{
			SlotCheck check;
			long num = WarRules.NextFree(war, cfg.Family, z, Math.Max(start - 86400, War.NowUnix() + 60), out check);
			if (num == 0)
			{
				Log("Свободного времени капта на " + z.Label + " в ближайшие сутки нет.");
			}
			else
			{
				DateTime dateTime = Clock.Msk(War.FromUnix(num));
				SetCapt(dateTime.Hour, dateTime.Minute);
				SaveCfg();
				Log("Время капта для " + z.Label + ": " + dateTime.ToString("HH:mm") + ((check != null && check.Level == 1) ? (" (" + check.Text + ")") : "") + ".");
				RenderCard();
				UpdateWarInfo();
			}
		};
		wrapPanel2.Children.Add(button4);
		System.Windows.Controls.Button button5 = new System.Windows.Controls.Button();
		button5.Style = St("Btn");
		button5.Content = "Бить, как только можно";
		button5.FontSize = 13.5;
		button5.Height = 36.0;
		button5.Padding = new Thickness(14.0, 0.0, 14.0, 0.0);
		button5.Margin = new Thickness(0.0, 0.0, 8.0, 8.0);
		button5.ToolTip = "Сделать целью в режиме «как только можно»: бот сам дождётся, когда эту зону можно бить\n(конец капта с её семьёй или вашей, КД), и сразу поставит атаку";
		System.Windows.Controls.Button button6 = button5;
		button6.IsEnabled = !running;
		button6.Click += delegate
		{
			AsapFromMap(z);
			RenderCard();
		};
		wrapPanel2.Children.Add(button6);
		System.Windows.Controls.Button button7 = new System.Windows.Controls.Button();
		button7.Style = St("Plain");
		button7.Content = new TextBlock
		{
			Text = "Отметить атаку из игры",
			FontSize = 13.0,
			Foreground = Fog
		};
		button7.Margin = new Thickness(0.0, 8.0, 0.0, 8.0);
		button7.ToolTip = "Видели в игре, что на эту зону поставили атаку? Отметьте — её увидят все копии TTC на сервере";
		System.Windows.Controls.Button button8 = button7;
		button8.Click += delegate
		{
			mapMarking = !mapMarking;
			RenderCard();
		};
		button8.IsEnabled = war != null;
		wrapPanel2.Children.Add(button8);
		mapCard.Children.Add(wrapPanel2);
		if (mapMarking)
		{
			mapCard.Children.Add(MarkForm(z, f));
		}
	}

	private UIElement Stat(string label, int left, int max)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(0.0, 0.0, 28.0, 0.0);
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(new TextBlock
		{
			Text = label,
			FontSize = 12.5,
			Foreground = Fog
		});
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel4 = stackPanel3;
		stackPanel4.Children.Add(new TextBlock
		{
			Text = left.ToString(),
			Style = St("Num"),
			FontSize = 28.0,
			Foreground = ((left == 0) ? StopB : Chalk)
		});
		stackPanel4.Children.Add(new TextBlock
		{
			Text = " из " + max,
			FontSize = 14.0,
			Foreground = Fog,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(0.0, 0.0, 0.0, 5.0)
		});
		stackPanel2.Children.Add(stackPanel4);
		return stackPanel2;
	}

	private static bool FromGame(WarCapt c)
	{
		return (c.Src ?? "").Split(',').Contains("net");
	}

	private static string CaptState(WarCapt c, long now)
	{
		string text = (FromGame(c) ? " · из игры" : "");
		if (c.Start > now)
		{
			return "начнётся через " + MapView.Mins(c.Start - now) + " (в " + War.Hm(c.Start) + ")" + text;
		}
		if (c.Winner == "a")
		{
			return "атака взяла зону" + (c.ExactEnd ? (" в " + War.Hms(c.EndHi)) : "") + text;
		}
		if (c.Winner == "d")
		{
			return "защита отбилась" + (c.ExactEnd ? (" в " + War.Hms(c.EndHi)) : "") + text;
		}
		if (c.EndHi > now)
		{
			string text2 = ((c.EndLo > now && c.EndLo != c.EndHi) ? (MapView.Mins(c.EndLo - now).Replace(" мин", "") + "–" + MapView.Mins(c.EndHi - now)) : MapView.Mins(c.EndHi - now));
			return "идёт с " + War.Hm(c.Start) + ", кончится через " + text2 + " (" + War.Window(new long[2] { c.EndLo, c.EndHi }) + ")" + text;
		}
		return "кончился " + War.Window(new long[2] { c.EndLo, c.EndHi }) + ", итог не отмечен" + text;
	}

	private UIElement CaptRow(WarCapt c, long now)
	{
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
		Grid grid2 = grid;
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(46.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid2.Children.Add(new TextBlock
		{
			Text = War.Hm(c.Start),
			Style = St("Num"),
			FontSize = 15.0,
			VerticalAlignment = VerticalAlignment.Top
		});
		string text = CaptState(c, now);
		if (c.Start > now || c.EndHi > now)
		{
			cardTicks = true;
		}
		TextBlock textBlock = new TextBlock();
		textBlock.Text = c.A + " атакует " + (c.D ?? "?") + ((c.Square != null) ? (" (" + c.Square + ")") : "") + "\n" + text;
		textBlock.FontSize = 12.5;
		textBlock.TextWrapping = TextWrapping.Wrap;
		textBlock.Foreground = ((c.Start <= now && c.EndHi > now) ? WarnB : Fog);
		TextBlock element = textBlock;
		Grid.SetColumn(element, 1);
		grid2.Children.Add(element);
		if (c.Start <= now && c.Winner == null && now <= c.Start + 10800)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.VerticalAlignment = VerticalAlignment.Top;
			StackPanel stackPanel2 = stackPanel;
			string[] array = new string[2] { "a", "d" };
			foreach (string text2 in array)
			{
				string wv = text2;
				System.Windows.Controls.Button button = new System.Windows.Controls.Button();
				button.Style = St("Plain");
				button.Content = new TextBlock
				{
					Text = ((text2 == "a") ? "взяли" : "отбились"),
					FontSize = 12.5,
					Foreground = ((text2 == "a") ? SignalB : Fog)
				};
				button.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
				button.ToolTip = ((text2 == "a") ? (c.A + " взяли зону — она сразу перейдёт к ним на карте у всех") : ((c.D ?? "Защита") + " отбились — зона остаётся за ними")) + ". Отмечайте сразу после конца капта: КД 15/5 минут посчитаются от этого момента";
				System.Windows.Controls.Button button2 = button;
				button2.Click += delegate
				{
					MarkResult(c, wv);
				};
				stackPanel2.Children.Add(button2);
			}
			Grid.SetColumn(stackPanel2, 2);
			grid2.Children.Add(stackPanel2);
		}
		return grid2;
	}

	private void MarkResult(WarCapt c, string winner)
	{
		long end = Math.Max(c.Start, Math.Min(War.NowUnix(), c.Start + (long)(war.PrepMin + war.FightMin) * 60L));
		string what = ((winner == "a") ? (c.A + " взяли ") : ((c.D ?? "Защита") + " отбили ")) + (c.Square ?? "зону");
		if (MapDemo)
		{
			c.Winner = winner;
			c.ExactEnd = true;
			c.EndLo = (c.EndHi = end);
			WarFamily warFamily = DemoFamily(c.A);
			WarFamily warFamily2 = ((c.D == null) ? null : DemoFamily(c.D));
			if (winner == "a" && c.ZoneId > 0)
			{
				if (war.OwnerIdx.TryGetValue(c.ZoneId, out var value) && value >= 0 && value < war.Families.Count)
				{
					war.Families[value].Zones--;
				}
				war.OwnerIdx[c.ZoneId] = warFamily.Index;
				war.Since[c.ZoneId] = end;
				warFamily.Zones++;
			}
			int num = ((warFamily2 == null) ? war.NeutralAtkCdMin : war.AtkCdMin);
			warFamily.AtkCd = ((num > 0) ? new long[2]
			{
				end + (long)num * 60L,
				end + (long)num * 60L
			} : null);
			warFamily.InCaptEnd = null;
			if (warFamily2 != null)
			{
				warFamily2.DefCd = new long[2]
				{
					end + (long)war.DefCdMin * 60L,
					end + (long)war.DefCdMin * 60L
				};
				warFamily2.InCaptEnd = null;
			}
			Log("Демо: " + what + " в " + War.Hm(end) + ".");
			RenderMap();
			UpdateWarInfo();
			return;
		}
		string srv = cfg.Server;
		int id = c.Id;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err = War.End(srv, id, War.FromUnix(end), winner);
			w.Dispatcher.BeginInvoke((Action)delegate
			{
				Log((err == null) ? ("Итог капта: " + what + " — карта обновлена у всех копий TTC на сервере.") : ("Итог капта не отмечен: " + err + "."));
				warNext = DateTime.MinValue;
				LoadWar();
			});
		});
	}

	private UIElement MarkForm(Zone z, WarFamily owner)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "Какая семья атакует " + z.Label + ((owner != null) ? (" (" + owner.Name + ")") : "") + " и во сколько капт",
			Style = St("Hint")
		});
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
		Grid grid2 = grid;
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(78.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		System.Windows.Controls.TextBox att = new System.Windows.Controls.TextBox
		{
			Style = St("Field"),
			FontSize = 14.0,
			Height = 34.0,
			Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
			Text = (cfg.Family ?? ""),
			MaxLength = 32
		};
		System.Windows.Controls.TextBox tm = new System.Windows.Controls.TextBox
		{
			Style = St("Field"),
			FontSize = 14.0,
			Height = 34.0,
			Padding = new Thickness(8.0, 0.0, 8.0, 0.0),
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0),
			MaxLength = 5,
			Text = cfg.CaptH.ToString("00") + ":" + cfg.CaptM.ToString("00")
		};
		Grid.SetColumn(tm, 1);
		System.Windows.Controls.Button ok = new System.Windows.Controls.Button
		{
			Style = St("Btn"),
			Content = "Отметить",
			FontSize = 13.5,
			Height = 34.0,
			Padding = new Thickness(12.0, 0.0, 12.0, 0.0)
		};
		Grid.SetColumn(ok, 2);
		grid2.Children.Add(att);
		grid2.Children.Add(tm);
		grid2.Children.Add(ok);
		stackPanel2.Children.Add(grid2);
		TextBlock msg = new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		stackPanel2.Children.Add(msg);
		ok.Click += delegate
		{
			string a = (att.Text ?? "").Trim();
			string srv = cfg.Server;
			TimeSpan result;
			if (a.Length == 0)
			{
				msg.Foreground = StopB;
				msg.Text = "Впишите атакующую семью.";
			}
			else if (!TimeSpan.TryParse((tm.Text ?? "").Trim(), out result) || result.TotalHours >= 24.0)
			{
				msg.Foreground = StopB;
				msg.Text = "Время капта — ЧЧ:ММ, например 20:00.";
			}
			else
			{
				DateTime capt = WarRules.CaptUtc(result.Hours, result.Minutes, Clock.UtcNow().AddMinutes(-((war != null) ? (war.PrepMin + war.FightMin) : 20)));
				if (MapDemo)
				{
					WarFamily warFamily = DemoFamily(a);
					WarFamily warFamily2 = war.OwnerOf(z);
					long num = War.Unix(capt);
					war.Capts.Add(new WarCapt
					{
						Id = -1 - war.Capts.Count,
						A = warFamily.Name,
						D = warFamily2?.Name,
						ZoneId = z.Id,
						Square = z.Label,
						Start = num,
						EndLo = num + (long)war.PrepMin * 60L,
						EndHi = num + (long)(war.PrepMin + war.FightMin) * 60L,
						Src = "demo"
					});
					if (num >= war.DayStart && num < war.DayStart + 86400 && warFamily2 != null)
					{
						warFamily.AtkUsed++;
						warFamily2.DefUsed++;
					}
					Log("Демо: отмечена атака " + warFamily.Name + " на " + z.Label + ", капт " + Clock.Msk(capt).ToString("HH:mm") + ".");
					mapMarking = false;
					RenderMap();
					UpdateWarInfo();
				}
				else
				{
					ok.IsEnabled = false;
					msg.Foreground = Fog;
					msg.Text = "Отправляю…";
					ThreadPool.QueueUserWorkItem(delegate
					{
						string err = War.Report(srv, a, z.Label, capt, DateTime.MinValue, manual: true);
						w.Dispatcher.BeginInvoke((Action)delegate
						{
							if (err != null)
							{
								ok.IsEnabled = true;
								msg.Foreground = StopB;
								msg.Text = "Не отправлено: " + err + ".";
							}
							else
							{
								Log("Отмечена атака " + a + " на " + z.Label + ", капт " + Clock.Msk(capt).ToString("HH:mm") + ".");
								mapMarking = false;
								warNext = DateTime.MinValue;
								LoadWar();
							}
						});
					});
				}
			}
		};
		return stackPanel2;
	}

	private UIElement Legend()
	{
		StackPanel p = new StackPanel
		{
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0)
		};
		Action<System.Windows.Media.Brush, bool, string> action = delegate(System.Windows.Media.Brush b, bool outline, string text)
		{
			Grid grid = new Grid
			{
				Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
			};
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(24.0)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.Children.Add(new System.Windows.Shapes.Rectangle
			{
				Width = 14.0,
				Height = 10.0,
				RadiusX = 2.0,
				RadiusY = 2.0,
				Fill = (outline ? null : b),
				Stroke = (outline ? b : null),
				StrokeThickness = 2.0,
				VerticalAlignment = VerticalAlignment.Top,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			});
			TextBlock element = new TextBlock
			{
				Text = text,
				FontSize = 12.5,
				Foreground = Fog,
				TextWrapping = TextWrapping.Wrap
			};
			Grid.SetColumn(element, 1);
			grid.Children.Add(element);
			p.Children.Add(grid);
		};
		action(SignalB, arg2: true, "территория вашей семьи");
		action(StopB, arg2: true, "на зоне идёт капт; внизу — до скольки он идёт");
		action(WarnB, arg2: true, "капт запланирован (пунктир); внизу — через сколько начнётся");
		action(WarnB, arg2: false, "особняк: 3 защиты со следующих суток");
		action(Dim, arg2: false, "цвет зоны — семья-владелец");
		return p;
	}

	private void RenderFamilies()
	{
		if (mapFamilies == null)
		{
			return;
		}
		mapFamilies.Children.Clear();
		if (war == null)
		{
			return;
		}
		string q = (mapSearch.Text ?? "").Trim().ToLowerInvariant();
		string own = Zones.Key(cfg.Family);
		long now = War.NowUnix();
		IEnumerable<WarFamily> enumerable = from f in war.Families
			where q.Length == 0 || f.Name.ToLowerInvariant().Contains(q) || (f.Tag ?? "").ToLowerInvariant().Contains(q)
			orderby f.Key == own descending, f.Zones descending, f.Name
			select f;
		int num = 0;
		foreach (WarFamily item in enumerable)
		{
			if (++num > 300)
			{
				break;
			}
			WarFamily fam = item;
			Grid grid = new Grid();
			grid.MinWidth = 330.0;
			Grid grid2 = grid;
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(26.0)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			FrameworkElement frameworkElement = FamBadge(item, 18.0);
			frameworkElement.VerticalAlignment = VerticalAlignment.Top;
			frameworkElement.Margin = new Thickness(0.0, 1.0, 0.0, 0.0);
			grid2.Children.Add(frameworkElement);
			StackPanel stackPanel = new StackPanel();
			Grid.SetColumn(stackPanel, 1);
			stackPanel.Children.Add(new TextBlock
			{
				Text = item.Title,
				FontSize = 13.5,
				Foreground = ((item.Key == own) ? SignalB : Chalk),
				FontWeight = ((item.Key == own) ? FontWeights.SemiBold : FontWeights.Normal),
				TextTrimming = TextTrimming.CharacterEllipsis
			});
			string text = StatusLine(item, now);
			string text2 = LiveLine(item, now);
			if (text2.Length > 0)
			{
				text = ((text.Length > 0) ? (text + ", " + text2) : text2);
			}
			stackPanel.Children.Add(new TextBlock
			{
				Text = ((item.Key == own) ? "ваша семья, " : "") + item.Zones + " " + Plural(item.Zones, "зона", "зоны", "зон") + ((item.Mansion || item.MansionSoon) ? ", особняк" : "") + ((text.Length > 0) ? ("\n" + text) : ""),
				FontSize = 12.0,
				Foreground = ((text.Length > 0) ? WarnB : Fog),
				TextWrapping = TextWrapping.Wrap
			});
			grid2.Children.Add(stackPanel);
			TextBlock textBlock = new TextBlock();
			textBlock.FontSize = 13.0;
			textBlock.TextAlignment = TextAlignment.Right;
			textBlock.VerticalAlignment = VerticalAlignment.Top;
			textBlock.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
			TextBlock textBlock2 = textBlock;
			textBlock2.Inlines.Add(new Run("А " + item.AtkLeft + "/" + item.AtkMax)
			{
				Foreground = ((item.AtkLeft == 0) ? StopB : Chalk)
			});
			textBlock2.Inlines.Add(new Run("   З " + item.DefLeft + "/" + item.DefMax)
			{
				Foreground = ((item.DefLeft == 0) ? StopB : Chalk)
			});
			textBlock2.ToolTip = "Атак осталось " + item.AtkLeft + " из " + item.AtkMax + ", защит " + item.DefLeft + " из " + item.DefMax + " (сутки с 00:00 МСК)";
			Grid.SetColumn(textBlock2, 2);
			grid2.Children.Add(textBlock2);
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = grid2;
			button.Padding = new Thickness(8.0, 6.0, 8.0, 6.0);
			button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
			button.Background = ((mapView != null && mapView.HighlightKey == item.Key) ? CellBg : System.Windows.Media.Brushes.Transparent);
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				if (mapView != null)
				{
					mapView.HighlightKey = ((mapView.HighlightKey == fam.Key) ? "" : fam.Key);
					mapView.InvalidateVisual();
					RenderFamilies();
				}
			};
			button2.ToolTip = "Подсветить зоны семьи на карте";
			mapFamilies.Children.Add(button2);
		}
		if (num == 0)
		{
			mapFamilies.Children.Add(new TextBlock
			{
				Text = ((q.Length > 0) ? "Ничего не найдено." : "Семей пока нет: сервер ещё не получил карту."),
				Style = St("Hint"),
				Margin = new Thickness(8.0, 6.0, 8.0, 0.0)
			});
		}
	}

	private UIElement FamilyStatsBlock(WarFamily f)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(0.0, 8.0, 0.0, 0.0);
		StackPanel stackPanel2 = stackPanel;
		bool open = famStatsOpen == f.Key;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Plain");
		button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		button.Content = Icons.Label(open ? "chevron-down" : "chevron-right", "Статистика семьи за 30 дней", 13.0);
		button.Foreground = SignalB;
		button.FontSize = 13.0;
		button.ToolTip = "Атаки и защиты за сутки, неделю и месяц, победы и поражения, соперники, в какие часы семья атакует";
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			famStatsOpen = (open ? "" : f.Key);
			RenderCard();
		};
		stackPanel2.Children.Add(button2);
		if (!open)
		{
			return stackPanel2;
		}
		FamilyStats value;
		bool flag = famStats.TryGetValue(f.Key, out value);
		if (!flag || !famStatsAt.TryGetValue(f.Key, out var value2) || DateTime.UtcNow - value2 > TimeSpan.FromSeconds(60.0))
		{
			LoadFamilyStats(f.Name, f.Key);
		}
		if (!flag)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = (famStatsError ?? "Загружаю…"),
				Style = St("Hint"),
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			});
			return stackPanel2;
		}
		Grid g = new Grid
		{
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		int[] array = new int[3] { 92, 0, 0 };
		foreach (int num in array)
		{
			g.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = ((num > 0) ? new GridLength(num) : new GridLength(1.0, GridUnitType.Star))
			});
		}
		string[] array2 = new string[3] { "day", "week", "month" };
		string[] array3 = new string[3] { "сутки", "7 дней", "30 дней" };
		Action<int, int, string, System.Windows.Media.Brush> action = delegate(int r, int c, string t, System.Windows.Media.Brush br)
		{
			TextBlock element = new TextBlock
			{
				Text = t,
				FontSize = 12.5,
				Foreground = (br ?? Chalk),
				Margin = new Thickness(0.0, 0.0, 8.0, 3.0)
			};
			Grid.SetRow(element, r);
			Grid.SetColumn(element, c);
			g.Children.Add(element);
		};
		for (int j = 0; j < 4; j++)
		{
			g.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
		}
		action(0, 1, "атаки (взяли/нет)", Fog);
		action(0, 2, "защиты (отбились/нет)", Fog);
		for (int k = 0; k < 3; k++)
		{
			if (!value.Periods.TryGetValue(array2[k], out var value3))
			{
				value3 = new int[6];
			}
			action(k + 1, 0, array3[k], Fog);
			action(k + 1, 1, value3[0] + "  (" + value3[1] + "/" + value3[2] + ")", null);
			action(k + 1, 2, value3[3] + "  (" + value3[4] + "/" + value3[5] + ")", null);
		}
		stackPanel2.Children.Add(g);
		if (value.Rivals.Count > 0)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = "Соперники: " + string.Join(", ", (from r in value.Rivals.Take(4)
					select r.Name + " " + r.N + ((r.W + r.Lo > 0) ? (" (" + r.W + ":" + r.Lo + ")") : "")).ToArray()),
				Style = St("Hint"),
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			});
		}
		if (value.Hours.Count > 0)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = "Чаще атакует в " + string.Join(", ", value.Hours.Select((int[] h) => h[0].ToString("00") + ":00 (" + h[1] + ")").ToArray()) + " МСК",
				Style = St("Hint"),
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
			});
		}
		foreach (FamilyStats.Item item in value.Recent.Take(6))
		{
			string text = ((item.Win == true) ? (item.Attack ? "взяли" : "отбились") : ((item.Win == false) ? (item.Attack ? "не взяли" : "сдали") : "итог неизвестен"));
			stackPanel2.Children.Add(new TextBlock
			{
				Text = Clock.Msk(War.FromUnix(item.Start)).ToString("dd.MM HH:mm") + "  " + (item.Attack ? "⚔ на " : "\ud83d\udee1 от ") + (item.Vs ?? "?") + ((item.Square != null) ? (" (" + item.Square + ")") : "") + " — " + text,
				FontSize = 12.0,
				Foreground = ((item.Win == true) ? GoB : ((item.Win == false) ? StopB : Fog)),
				Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
			});
		}
		if (value.HistoryFrom > 0)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = "История у TTC — с " + Clock.Msk(War.FromUnix(value.HistoryFrom)).ToString("dd.MM") + ".",
				Style = St("Hint"),
				FontSize = 11.5,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			});
		}
		return stackPanel2;
	}

	private void LoadFamilyStats(string family, string key)
	{
		if (famStatsLoading || string.IsNullOrEmpty(cfg.Server) || MapDemo)
		{
			return;
		}
		famStatsLoading = true;
		famStatsAt[key] = DateTime.UtcNow;
		string srv = cfg.Server;
		ThreadPool.QueueUserWorkItem(delegate
		{
			FamilyStats fs = null;
			string err;
			try
			{
				fs = War.LoadFamily(srv, family, out err);
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					famStatsLoading = false;
					if (fs != null)
					{
						famStats[key] = fs;
						famStatsError = null;
					}
					else
					{
						famStatsError = "Статистика не загрузилась: " + err + ".";
					}
					if (famStatsOpen == key)
					{
						RenderCard();
					}
				});
			}
			catch
			{
				famStatsLoading = false;
			}
		});
	}

	private void OpenTelegram()
	{
		string srv = cfg.Server ?? "";
		string fam = (cfg.Family ?? "").Trim();
		if (srv.Length == 0)
		{
			Log("Уведомления в Telegram: сначала выберите сервер.");
			return;
		}
		if (fam.Length == 0)
		{
			Log("Уведомления в Telegram: впишите свою семью (кнопка «Семья» вверху окна).");
			return;
		}
		Log("Уведомления в Telegram: получаю ссылку на бота…");
		ThreadPool.QueueUserWorkItem(delegate
		{
			string url = null;
			string err;
			try
			{
				url = War.TelegramLink(srv, fam, out err);
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					if (url == null)
					{
						Log("Уведомления в Telegram: " + err + ".");
					}
					else if (!url.StartsWith("https://t.me/", StringComparison.Ordinal))
					{
						Log("Уведомления в Telegram: сервер вернул неожиданную ссылку.");
					}
					else if (AppPaths.OpenUrl(url))
					{
						Log("Откройте бота в Telegram и нажмите «Старт» — уведомления о каптах семьи " + fam + " включатся.");
					}
					else
					{
						Log("Не открылся Telegram. Ссылка: " + url);
					}
				});
			}
			catch
			{
			}
		});
	}

	private static string Plural(int n, string one, string few, string many)
	{
		int num = Math.Abs(n) % 100;
		int num2 = num % 10;
		if (num > 10 && num < 20)
		{
			return many;
		}
		switch (num2)
		{
		case 1:
			return one;
		case 2:
		case 3:
		case 4:
			return few;
		default:
			return many;
		}
	}

	private void InitUpdate()
	{
		if (demo || MapDemo)
		{
			return;
		}
		DispatcherTimer t = new DispatcherTimer
		{
			Interval = TimeSpan.FromSeconds(6.0)
		};
		t.Tick += delegate
		{
			t.Stop();
			ThreadPool.QueueUserWorkItem(delegate
			{
				Updater.Cleanup();
			});
		};
		t.Start();
	}

	private void ShowUpdate(string latest)
	{
		if (demo || MapDemo || !Updater.IsNewer(latest, "2.9.13"))
		{
			return;
		}
		updLatest = latest;
		if (updDismissed == latest)
		{
			return;
		}
		if (updCard == null)
		{
			BuildUpdateCard();
		}
		if (updCard != null)
		{
			if (!updating)
			{
				UpdTitle.Text = "Вышла TTC " + latest;
				UpdText.Text = "У вас 2.9.13. Программа скачает сборку с сайта, проверит подпись сервера и перезапустится — настройки и калибровка сохранятся.";
				UpdBtn.Content = "Обновить";
				UpdBtn.IsEnabled = true;
			}
			updCard.Visibility = Visibility.Visible;
		}
	}

	private void BuildUpdateCard()
	{
		Grid rootGrid = RootGrid;
		if (rootGrid != null)
		{
			StackPanel stackPanel = new StackPanel();
			Grid grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid.Children.Add(new System.Windows.Shapes.Rectangle
			{
				Width = 8.0,
				Height = 8.0,
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = SignalB,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0, 0.0, 9.0, 0.0)
			});
			UpdTitle = new TextBlock
			{
				FontSize = 14.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = Chalk,
				VerticalAlignment = VerticalAlignment.Center
			};
			Grid.SetColumn(UpdTitle, 1);
			grid.Children.Add(UpdTitle);
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = new TextBlock
			{
				Text = "×",
				FontSize = 17.0,
				Foreground = Fog
			};
			button.Padding = new Thickness(6.0, 0.0, 2.0, 0.0);
			button.ToolTip = "Позже";
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				DismissUpdate();
			};
			Grid.SetColumn(button2, 2);
			grid.Children.Add(button2);
			stackPanel.Children.Add(grid);
			UpdText = new TextBlock
			{
				FontSize = 12.5,
				Foreground = Fog,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
			};
			stackPanel.Children.Add(UpdText);
			StackPanel stackPanel2 = new StackPanel();
			stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel2.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
			StackPanel stackPanel3 = stackPanel2;
			UpdBtn = new System.Windows.Controls.Button
			{
				Style = St("Primary"),
				Content = "Обновить",
				FontSize = 13.5,
				Height = 34.0,
				Padding = new Thickness(18.0, 0.0, 18.0, 0.0)
			};
			UpdBtn.Click += delegate
			{
				DoUpdate();
			};
			System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
			button3.Style = St("Btn");
			button3.Content = "Позже";
			button3.FontSize = 13.5;
			button3.Height = 34.0;
			button3.Padding = new Thickness(14.0, 0.0, 14.0, 0.0);
			button3.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
			System.Windows.Controls.Button button4 = button3;
			button4.Click += delegate
			{
				DismissUpdate();
			};
			stackPanel3.Children.Add(UpdBtn);
			stackPanel3.Children.Add(button4);
			stackPanel.Children.Add(stackPanel3);
			updCard = new Border
			{
				Background = Slate,
				BorderBrush = SignalB,
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(10.0),
				Padding = new Thickness(16.0, 12.0, 14.0, 14.0),
				Width = 340.0,
				Child = stackPanel,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 66.0, 18.0, 0.0),
				Effect = new DropShadowEffect
				{
					BlurRadius = 22.0,
					ShadowDepth = 0.0,
					Opacity = 0.6,
					Color = Colors.Black
				},
				Visibility = Visibility.Collapsed
			};
			Grid.SetRowSpan(updCard, Math.Max(1, rootGrid.RowDefinitions.Count));
			Grid.SetColumnSpan(updCard, Math.Max(1, rootGrid.ColumnDefinitions.Count));
			System.Windows.Controls.Panel.SetZIndex(updCard, 1000);
			rootGrid.Children.Add(updCard);
		}
	}

	private void DismissUpdate()
	{
		if (!updating)
		{
			updDismissed = updLatest;
			if (updCard != null)
			{
				updCard.Visibility = Visibility.Collapsed;
			}
			Log("Обновление до " + updLatest + " отложено — напомню при следующем запуске TTC.");
		}
	}

	private void DoUpdate()
	{
		if (updating)
		{
			return;
		}
		if (running)
		{
			Log("Сейчас идёт забив — обновлюсь после него: нажмите «Обновить» ещё раз, когда он закончится.");
			return;
		}
		updating = true;
		UpdBtn.IsEnabled = false;
		UpdBtn.Content = "Скачиваю…";
		UpdText.Text = "Скачиваю " + updLatest + " с сайта и проверяю подпись…";
		Log("Обновление: скачиваю версию " + updLatest + " с сайта.");
		string site = "https://timetocapture.ru";
		ThreadPool.QueueUserWorkItem(delegate
		{
			string newPath;
			string ver;
			string err = Updater.Fetch(site, out newPath, out ver);
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					FinishUpdate(err, newPath, ver);
				});
			}
			catch
			{
			}
		});
	}

	private void FinishUpdate(string err, string newPath, string ver)
	{
		Action<string> action = delegate(string why)
		{
			updating = false;
			UpdBtn.IsEnabled = true;
			UpdBtn.Content = "Обновить";
			UpdText.Text = why;
		};
		if (err != null)
		{
			Log("Обновление не удалось: " + err + ".");
			action("Не вышло: " + err + ". Нажмите «Обновить» ещё раз.");
			return;
		}
		if (running)
		{
			Log("Новая версия скачана, но уже идёт забив — нажмите «Обновить», когда он закончится.");
			action("Идёт забив — нажмите «Обновить», когда он закончится.");
			return;
		}
		string exePath = Updater.ExePath;
		string text = Updater.Swap(exePath, newPath);
		if (text != null)
		{
			Log("Обновление не удалось: " + text + ".");
			action("Не вышло: " + text + ". Нажмите «Обновить» ещё раз.");
			return;
		}
		SaveCfg();
		SingleInstance.Release();
		if (!Updater.Start(exePath))
		{
			SingleInstance.Reacquire();
			Updater.Rollback(exePath);
			Log("Новая версия не запустилась — вернул прежнюю. Скачайте сборку в личном кабинете: https://timetocapture.ru/cabinet");
			action("Новая версия не запустилась — вернул прежнюю. Скачайте сборку в личном кабинете.");
		}
		else
		{
			Log("Обновлено до " + ver + " — перезапускаю.");
			ExitApp();
		}
	}

	private void BuildHistory(out UIElement top, out UIElement list)
	{
		histTop = new StackPanel
		{
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		};
		histFamBtn = new System.Windows.Controls.Button
		{
			Style = St("Btn"),
			Height = 34.0,
			HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
			Padding = new Thickness(10.0, 0.0, 10.0, 0.0)
		};
		histFamBtn.Click += delegate
		{
			ShowHistoryFamilies();
		};
		histTop.Children.Add(histFamBtn);
		histList = new StackPanel
		{
			Margin = new Thickness(10.0, 0.0, 10.0, 12.0)
		};
		top = histTop;
		list = histList;
	}

	private void LoadHistory()
	{
		string srv = cfg.Server ?? "";
		histNext = DateTime.UtcNow.AddSeconds(60.0);
		if (MapDemo)
		{
			hist = war;
			RenderHistory();
		}
		else
		{
			if (histLoading || closing || srv.Length == 0 || License.SavedKey().Length == 0)
			{
				return;
			}
			histLoading = true;
			ThreadPool.QueueUserWorkItem(delegate
			{
				WarState h;
				string err;
				try
				{
					h = War.LoadHistory(srv, 3, out err);
				}
				catch (Exception ex)
				{
					h = null;
					err = ex.Message;
				}
				try
				{
					w.Dispatcher.BeginInvoke((Action)delegate
					{
						histLoading = false;
						if (!(srv != (cfg.Server ?? "")))
						{
							if (h != null)
							{
								hist = h;
								histError = null;
							}
							else
							{
								histError = err;
								histNext = DateTime.UtcNow.AddSeconds(20.0);
							}
							RenderHistory();
						}
					});
				}
				catch
				{
					histLoading = false;
				}
			});
		}
	}

	private long DayOf(long t, long todayStart)
	{
		long num = todayStart;
		while (t < num)
		{
			num -= 86400;
		}
		for (; t >= num + 86400; num += 86400)
		{
		}
		return num;
	}

	private string DayTitle(long d, long todayStart)
	{
		string text = Clock.Msk(War.FromUnix(d)).ToString("d MMMM", Ru);
		if (d != todayStart)
		{
			if (d != todayStart - 86400)
			{
				return text;
			}
			return "Вчера, " + text;
		}
		return "Сегодня, " + text;
	}

	private WarFamily HistFamily(string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			return null;
		}
		WarFamily warFamily = (hist ?? war)?.Families.FirstOrDefault((WarFamily x) => x.Key == key);
		if (warFamily == null && war != null)
		{
			warFamily = war.Families.FirstOrDefault((WarFamily x) => x.Key == key);
		}
		return warFamily;
	}

	private void RenderHistory()
	{
		if (histList == null)
		{
			return;
		}
		histList.Children.Clear();
		WarFamily sel = HistFamily(histFamily);
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		if (sel != null)
		{
			grid.Children.Add(FamBadge(sel, 20.0));
		}
		TextBlock textBlock = new TextBlock();
		textBlock.Text = ((sel == null) ? "Все семьи" : sel.Title);
		textBlock.FontSize = 14.0;
		textBlock.Margin = new Thickness((sel != null) ? 8 : 0, 0.0, 0.0, 0.0);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
		TextBlock element = textBlock;
		Grid.SetColumn(element, 1);
		grid.Children.Add(element);
		FrameworkElement frameworkElement = Icons.Make("chevron-down", 12.0, Fog);
		frameworkElement.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
		Grid.SetColumn(frameworkElement, 2);
		grid.Children.Add(frameworkElement);
		histFamBtn.Content = grid;
		histFamBtn.ToolTip = "Выберите семью — покажу её капты и сколько у неё сегодня осталось атак и защит";
		while (histTop.Children.Count > 1)
		{
			histTop.Children.RemoveAt(1);
		}
		WarState warState = hist;
		if (warState == null)
		{
			histList.Children.Add(new TextBlock
			{
				Text = (string.IsNullOrEmpty(cfg.Server) ? "Выберите сервер вверху главного окна." : ((License.SavedKey().Length == 0 && !MapDemo) ? "Нужен ключ программы." : ((histError != null) ? ("История не загрузилась: " + histError + ".") : "Загружаю историю каптов…"))),
				Style = St("Hint"),
				Margin = new Thickness(8.0, 8.0, 8.0, 0.0)
			});
			return;
		}
		long num = War.NowUnix();
		long today = ((warState.DayStart > 0) ? warState.DayStart : DayOf(num, War.Unix(Clock.Msk(Clock.UtcNow()).Date.AddHours(-3.0))));
		if (sel != null)
		{
			histTop.Children.Add(FamilySummary(sel, warState, today, num));
		}
		List<WarCapt> list = (from c in warState.Capts
			where sel == null || Zones.Key(c.A) == sel.Key || Zones.Key(c.D) == sel.Key
			orderby c.Start descending
			select c).ToList();
		if (list.Count == 0)
		{
			histList.Children.Add(new TextBlock
			{
				Text = ((sel == null) ? ("За " + 3 + " дня каптов нет.") : ("У семьи " + sel.Name + " за " + 3 + " дня каптов нет.")),
				Style = St("Hint"),
				Margin = new Thickness(8.0, 8.0, 8.0, 0.0)
			});
			return;
		}
		if (histOpenDays.Count == 0)
		{
			histOpenDays.Add(DayOf(list[0].Start, today));
		}
		foreach (IGrouping<long, WarCapt> item in from c in list
			group c by DayOf(c.Start, today))
		{
			long d = item.Key;
			bool flag = histOpenDays.Contains(d);
			int num2 = ((sel != null) ? item.Count((WarCapt c) => Zones.Key(c.A) == sel.Key) : 0);
			int num3 = ((sel != null) ? item.Count((WarCapt c) => Zones.Key(c.D) == sel.Key) : 0);
			Grid grid2 = new Grid();
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			StackPanel element2 = Icons.Label(flag ? "chevron-down" : "chevron-right", DayTitle(d, today), 13.0);
			TextElement.SetFontSize(element2, 14.0);
			TextElement.SetFontWeight(element2, FontWeights.SemiBold);
			grid2.Children.Add(element2);
			TextBlock textBlock2 = new TextBlock();
			textBlock2.Text = ((sel == null) ? (item.Count() + " " + Plural(item.Count(), "капт", "капта", "каптов")) : ("атак " + num2 + ", защит " + num3));
			textBlock2.FontSize = 12.5;
			textBlock2.Foreground = Fog;
			textBlock2.VerticalAlignment = VerticalAlignment.Center;
			TextBlock element3 = textBlock2;
			Grid.SetColumn(element3, 1);
			grid2.Children.Add(element3);
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = grid2;
			button.Padding = new Thickness(8.0, 9.0, 8.0, 7.0);
			button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
			button.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				if (!histOpenDays.Remove(d))
				{
					histOpenDays.Add(d);
				}
				RenderHistory();
			};
			histList.Children.Add(button2);
			histList.Children.Add(new System.Windows.Shapes.Rectangle
			{
				Height = 1.0,
				Fill = RuleB,
				Margin = new Thickness(8.0, 0.0, 8.0, 2.0)
			});
			if (!flag)
			{
				continue;
			}
			foreach (WarCapt item2 in item)
			{
				histList.Children.Add(HistoryRow(item2, warState, num, sel));
			}
		}
		string text = ((warState.HistorySource != null) ? ("Источник истории: " + warState.HistorySource + ((warState.HistorySynced > 0) ? (", " + War.Ago(warState.HistorySynced, num)) : "") + ".") : "");
		if (text.Length > 0)
		{
			histList.Children.Add(new TextBlock
			{
				Text = text,
				FontSize = 12.0,
				Foreground = Dim,
				Margin = new Thickness(8.0, 10.0, 8.0, 0.0)
			});
		}
	}

	private UIElement FamilySummary(WarFamily f, WarState h, long today, long now)
	{
		int num = h.Capts.Count((WarCapt c) => Zones.Key(c.A) == f.Key && c.Start >= today && c.Start < today + 86400);
		int num2 = h.Capts.Count((WarCapt c) => Zones.Key(c.D) == f.Key && c.Start >= today && c.Start < today + 86400);
		int num3 = ((f.AtkMax > 0) ? f.AtkMax : h.Attacks);
		int num4 = ((f.DefMax > 0) ? f.DefMax : (f.Mansion ? h.MansionDefenses : h.Defenses));
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(0.0, 10.0, 0.0, 2.0);
		StackPanel stackPanel2 = stackPanel;
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel4 = stackPanel3;
		stackPanel4.Children.Add(Stat("Атак сегодня осталось", Math.Max(0, num3 - num), num3));
		stackPanel4.Children.Add(Stat("Защит осталось", Math.Max(0, num4 - num2), num4));
		stackPanel2.Children.Add(stackPanel4);
		List<string> list = new List<string>();
		list.Add("Использовано: атак " + num + ", защит " + num2 + " (сутки с 00:00 МСК).");
		if (f.Mansion)
		{
			list.Add("Есть особняк — 3 защиты.");
		}
		else if (f.MansionSoon)
		{
			list.Add("Особняк взят сегодня — 3 защиты будут с 00:00.");
		}
		string text = StatusLine(f, now);
		if (text.Length > 0)
		{
			list.Add(char.ToUpper(text[0]) + text.Substring(1) + ".");
		}
		stackPanel2.Children.Add(new TextBlock
		{
			Text = string.Join(" ", list),
			Style = St("Hint"),
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
			Foreground = ((text.Length > 0) ? WarnB : Fog)
		});
		return stackPanel2;
	}

	private UIElement HistoryRow(WarCapt c, WarState h, long now, WarFamily sel)
	{
		bool flag = histOpenCapts.Contains(c.Id);
		WarFamily f = h.Family(c.A) ?? ((war != null) ? war.Family(c.A) : null);
		WarFamily f2 = ((c.D == null) ? null : (h.Family(c.D) ?? ((war != null) ? war.Family(c.D) : null)));
		Grid grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(46.0)
		});
		grid.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid.Children.Add(new TextBlock
		{
			Text = War.Hm(c.Start),
			Style = St("Num"),
			FontSize = 15.0,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(0.0, 1.0, 0.0, 0.0)
		});
		StackPanel stackPanel = new StackPanel();
		Grid.SetColumn(stackPanel, 1);
		WrapPanel wrapPanel = new WrapPanel();
		wrapPanel.Children.Add(FamTag(f, c.A, sel != null && Zones.Key(c.A) == sel.Key));
		wrapPanel.Children.Add(new TextBlock
		{
			Text = "атакует",
			FontSize = 12.5,
			Foreground = Fog,
			Margin = new Thickness(6.0, 0.0, 6.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		wrapPanel.Children.Add(FamTag(f2, c.D ?? "?", sel != null && c.D != null && Zones.Key(c.D) == sel.Key));
		stackPanel.Children.Add(wrapPanel);
		string text;
		System.Windows.Media.Brush foreground;
		if (c.Start > now)
		{
			text = "впереди";
			foreground = Fog;
		}
		else if (c.Winner == "a")
		{
			text = "атака взяла зону";
			foreground = GoB;
		}
		else if (c.Winner == "d")
		{
			text = "защита отбилась";
			foreground = SignalB;
		}
		else if (c.EndHi > now)
		{
			text = "идёт";
			foreground = WarnB;
		}
		else
		{
			text = "итог неизвестен";
			foreground = Fog;
		}
		TextBlock textBlock = new TextBlock();
		textBlock.FontSize = 12.5;
		textBlock.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
		textBlock.TextWrapping = TextWrapping.Wrap;
		TextBlock textBlock2 = textBlock;
		textBlock2.Inlines.Add(new Run((c.Square ?? "зона ?") + (string.IsNullOrEmpty(c.Place) ? "" : (", " + c.Place)) + ", ")
		{
			Foreground = Fog
		});
		textBlock2.Inlines.Add(new Run(text)
		{
			Foreground = foreground
		});
		stackPanel.Children.Add(textBlock2);
		if (flag)
		{
			stackPanel.Children.Add(HistoryDetails(c, now));
		}
		grid.Children.Add(stackPanel);
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Plain");
		button.Content = grid;
		button.Padding = new Thickness(8.0, 7.0, 8.0, 7.0);
		button.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
		button.Background = (flag ? CellBg : System.Windows.Media.Brushes.Transparent);
		button.ToolTip = (flag ? null : "Подробности капта");
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			if (!histOpenCapts.Remove(c.Id))
			{
				histOpenCapts.Add(c.Id);
			}
			RenderHistory();
		};
		return button2;
	}

	private UIElement HistoryDetails(WarCapt c, long now)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(0.0, 8.0, 0.0, 2.0);
		StackPanel stackPanel2 = stackPanel;
		List<string> list = new List<string>();
		string text = (c.ExactEnd ? War.Hm(c.EndHi) : War.Window(new long[2] { c.EndLo, c.EndHi }));
		list.Add("Начало " + War.Hm(c.Start) + ((c.Start > now) ? "" : (", конец " + text + (c.ExactEnd ? (" (" + Math.Max(0L, (c.EndHi - c.Start) / 60) + " мин)") : ""))));
		if (c.ANum > 0 || c.DNum > 0)
		{
			list.Add("Участники: " + c.ANum + " в атаке, " + c.DNum + " в защите");
		}
		if (c.ADmg > 0 || c.DDmg > 0)
		{
			list.Add("Урон: атака " + c.ADmg.ToString("#,0", Ru) + ", защита " + c.DDmg.ToString("#,0", Ru));
		}
		if (war != null && c.EndHi <= now)
		{
			list.Add("КД после капта: у " + (c.D ?? "защиты") + " до " + War.Window(new long[2]
			{
				c.EndLo + (long)war.DefCdMin * 60L,
				c.EndHi + (long)war.DefCdMin * 60L
			}) + ((war.AtkCdMin > 0) ? (", у " + c.A + " до " + War.Window(new long[2]
			{
				c.EndLo + (long)war.AtkCdMin * 60L,
				c.EndHi + (long)war.AtkCdMin * 60L
			})) : (", у " + c.A + " КД нет")));
		}
		foreach (string item in list)
		{
			stackPanel2.Children.Add(new TextBlock
			{
				Text = item,
				FontSize = 12.5,
				Foreground = Chalk,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0.0, 0.0, 0.0, 3.0)
			});
		}
		Zone z = ((c.ZoneId > 0) ? Zones.ById(c.ZoneId) : null);
		if (z != null)
		{
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = new TextBlock
			{
				Text = "Показать " + z.Label + " на карте",
				FontSize = 12.5,
				Foreground = SignalB
			};
			button.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
			button.Margin = new Thickness(0.0, 2.0, 0.0, 0.0);
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate(object s, RoutedEventArgs e)
			{
				if (mapView != null)
				{
					mapView.Selected = z;
					mapView.CenterOn(z);
					ShowFamiliesTab();
					RenderCard();
					e.Handled = true;
				}
			};
			stackPanel2.Children.Add(button2);
		}
		return stackPanel2;
	}

	private UIElement FamTag(WarFamily f, string name, bool hl)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add((f != null) ? FamBadge(f, 16.0) : Images.Badge(null, Dim, null, 16.0));
		stackPanel2.Children.Add(new TextBlock
		{
			Text = name,
			FontSize = 13.5,
			FontWeight = (hl ? FontWeights.SemiBold : FontWeights.Normal),
			Foreground = (hl ? SignalB : Chalk),
			Margin = new Thickness(5.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		return stackPanel2;
	}

	private FrameworkElement FamBadge(WarFamily f, double size)
	{
		return Images.Badge(f.Logo, new SolidColorBrush(MapView.FamilyColor(f)), new SolidColorBrush(MapView.OutlineColor(f)), size);
	}

	private void ShowHistoryFamilies()
	{
		WarState warState = hist ?? war;
		StackPanel stackPanel = new StackPanel();
		stackPanel.Children.Add(MenuButton(new TextBlock
		{
			Text = "Все семьи",
			FontSize = 13.5,
			Foreground = ((histFamily.Length == 0) ? SignalB : Chalk)
		}, delegate
		{
			histFamily = "";
			RenderHistory();
		}));
		if (warState != null)
		{
			string own = Zones.Key(cfg.Family);
			foreach (WarFamily item in (from x in warState.Families
				orderby x.Key == own descending, x.Zones descending, x.Name
				select x).Take(200))
			{
				WarFamily fam = item;
				Grid grid = new Grid();
				grid.MinWidth = 300.0;
				Grid grid2 = grid;
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = new GridLength(1.0, GridUnitType.Star)
				});
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = GridLength.Auto
				});
				grid2.Children.Add(FamBadge(item, 18.0));
				TextBlock textBlock = new TextBlock();
				textBlock.Text = item.Title + ((item.Key == own) ? " (ваша)" : "");
				textBlock.FontSize = 13.5;
				textBlock.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
				textBlock.Foreground = ((item.Key == histFamily) ? SignalB : Chalk);
				textBlock.VerticalAlignment = VerticalAlignment.Center;
				TextBlock element = textBlock;
				Grid.SetColumn(element, 1);
				grid2.Children.Add(element);
				TextBlock textBlock2 = new TextBlock();
				textBlock2.Text = "А " + item.AtkLeft + "/" + item.AtkMax + "   З " + item.DefLeft + "/" + item.DefMax;
				textBlock2.FontSize = 12.5;
				textBlock2.Foreground = Fog;
				textBlock2.Margin = new Thickness(14.0, 0.0, 0.0, 0.0);
				textBlock2.VerticalAlignment = VerticalAlignment.Center;
				TextBlock element2 = textBlock2;
				Grid.SetColumn(element2, 2);
				grid2.Children.Add(element2);
				stackPanel.Children.Add(MenuButton(grid2, delegate
				{
					histFamily = fam.Key;
					histOpenDays.Clear();
					RenderHistory();
				}));
			}
		}
		ShowPopup(histFamBtn, new ScrollViewer
		{
			Content = stackPanel,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			MaxHeight = 520.0
		});
	}

	private void InitSnipe()
	{
		System.Windows.Controls.Panel panel = FireBox.Parent as System.Windows.Controls.Panel;
		System.Windows.Controls.Panel panel2 = CaptHBox.Parent as System.Windows.Controls.Panel;
		int num = ((panel == null) ? (-1) : Inputs.Children.IndexOf(panel));
		int num2 = ((panel2 == null) ? (-1) : Inputs.Children.IndexOf(panel2));
		if (num >= 1 && num2 >= 1)
		{
			if (Inputs.Children[0] is TextBlock textBlock)
			{
				Inputs.Children.RemoveAt(0);
				textBlock.Margin = new Thickness(0.0);
				StackPanel stackPanel = new StackPanel();
				stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
				stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
				StackPanel stackPanel2 = stackPanel;
				targetMark = Dot(SignalB);
				stackPanel2.Children.Add(targetMark);
				stackPanel2.Children.Add(textBlock);
				Inputs.Children.Insert(0, stackPanel2);
				num = Inputs.Children.IndexOf(panel);
				num2 = Inputs.Children.IndexOf(panel2);
			}
			timeRows = new UIElement[4]
			{
				Inputs.Children[num2 - 1],
				panel2,
				panel,
				FireInfo
			};
			Inputs.Children[num - 1].Visibility = Visibility.Collapsed;
			FireBox.ToolTip = "Когда нажать «Запланировать атаку»: время прихода пакета на сервер, МСК, ЧЧ:ММ:СС (можно с миллисекундами)";
			StackPanel stackPanel3 = new StackPanel();
			stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel3.Margin = new Thickness(0.0, 16.0, 0.0, 4.0);
			StackPanel stackPanel4 = stackPanel3;
			stackPanel4.Children.Add(new TextBlock
			{
				Text = "Забить",
				Style = St("Label"),
				Margin = new Thickness(0.0, 0.0, 12.0, 6.0),
				VerticalAlignment = VerticalAlignment.Center
			});
			modeTimeBtn = new System.Windows.Controls.Button
			{
				Style = St("Quick"),
				Content = "ко времени (МСК)",
				ToolTip = "Нажать «Запланировать атаку» точно в указанное время (время прихода пакета на сервер, МСК)"
			};
			modeAfterBtn = new System.Windows.Controls.Button
			{
				Style = St("Quick"),
				Content = "как только можно",
				ToolTip = "Бот сам видит, что мешает бить цель (капт с её семьёй или вашей, КД, лимиты суток),\nи ставит атаку в самый ранний момент — сразу после конца того капта или к концу КД"
			};
			modeTimeBtn.Click += delegate
			{
				SetSnipeMode(after: false);
			};
			modeAfterBtn.Click += delegate
			{
				SetSnipeMode(after: true);
			};
			stackPanel4.Children.Add(modeTimeBtn);
			stackPanel4.Children.Add(modeAfterBtn);
			Inputs.Children.Insert(num - 1, stackPanel4);
			snipePanel = new StackPanel();
			Grid grid = new Grid();
			grid.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
			Grid grid2 = grid;
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(150.0)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(16.0)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid2.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			grid2.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			TextBlock textBlock2 = new TextBlock();
			textBlock2.Text = "Наш капт через";
			textBlock2.Style = St("Label");
			TextBlock element = textBlock2;
			Grid.SetColumnSpan(element, 3);
			grid2.Children.Add(element);
			afterBox = BuildAfterBox();
			Grid.SetRow(afterBox, 1);
			grid2.Children.Add(afterBox);
			TextBlock textBlock3 = new TextBlock();
			textBlock3.Text = "после того, как цель освободится. Вверх до 5 минут; игра принимает +15…+60 от нажатия.";
			textBlock3.Style = St("Hint");
			textBlock3.VerticalAlignment = VerticalAlignment.Center;
			TextBlock element2 = textBlock3;
			Grid.SetRow(element2, 1);
			Grid.SetColumn(element2, 2);
			grid2.Children.Add(element2);
			snipePanel.Children.Add(grid2);
			Grid grid3 = new Grid();
			grid3.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(3.0)
			});
			grid3.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			asapBar = new Border
			{
				CornerRadius = new CornerRadius(1.5),
				Background = Fog,
				Margin = new Thickness(0.0, 2.0, 0.0, 2.0)
			};
			grid3.Children.Add(asapBar);
			StackPanel stackPanel5 = new StackPanel();
			stackPanel5.Margin = new Thickness(12.0, 0.0, 0.0, 0.0);
			StackPanel stackPanel6 = stackPanel5;
			Grid.SetColumn(stackPanel6, 1);
			grid3.Children.Add(stackPanel6);
			asapHead = new TextBlock
			{
				FontSize = 14.5,
				FontWeight = FontWeights.SemiBold,
				Foreground = Chalk,
				TextWrapping = TextWrapping.Wrap
			};
			asapDetail = new TextBlock
			{
				Style = St("Hint"),
				Foreground = Chalk,
				Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
			};
			asapPlan = new TextBlock
			{
				Style = St("Hint"),
				Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
			};
			asapSrc = new TextBlock
			{
				Style = St("Hint"),
				Foreground = Dim,
				FontSize = 12.0,
				Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
			};
			stackPanel6.Children.Add(asapHead);
			stackPanel6.Children.Add(asapDetail);
			stackPanel6.Children.Add(asapPlan);
			stackPanel6.Children.Add(asapSrc);
			snipePanel.Children.Add(new Border
			{
				Margin = new Thickness(0.0, 16.0, 0.0, 0.0),
				Padding = new Thickness(12.0, 10.0, 12.0, 11.0),
				CornerRadius = new CornerRadius(8.0),
				Background = Slate,
				BorderBrush = RuleB,
				BorderThickness = new Thickness(1.0),
				Child = grid3
			});
			Inputs.Children.Insert(Inputs.Children.IndexOf(FireInfo) + 1, snipePanel);
			SetAfter(cfg.AfterMin, save: false);
			SquareBox.TextChanged += delegate
			{
				RefreshAsap(force: true);
			};
			SetSnipeMode(cfg.AfterCapt, save: false);
		}
	}

	private static FrameworkElement Dot(System.Windows.Media.Brush b)
	{
		Border border = new Border();
		border.Width = 8.0;
		border.Height = 8.0;
		border.CornerRadius = new CornerRadius(2.0);
		border.Background = b;
		border.Margin = new Thickness(0.0, 1.0, 8.0, 0.0);
		border.VerticalAlignment = VerticalAlignment.Center;
		return border;
	}

	private Border BuildAfterBox()
	{
		Border border = new Border();
		border.CornerRadius = new CornerRadius(8.0);
		border.Background = Slate;
		border.BorderBrush = RuleB;
		border.BorderThickness = new Thickness(1.0);
		border.Height = 52.0;
		border.ToolTip = "Через сколько минут после того, как цель освободилась (кончился капт с её семьёй или КД), ставим наш капт.\nВверх до 5 минут; 15–60 минут, шаг 5: колёсико мыши или стрелки";
		Border border2 = border;
		Grid grid = new Grid();
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Margin = new Thickness(14.0, 0.0, 0.0, 2.0);
		StackPanel stackPanel2 = stackPanel;
		afterText = new TextBlock
		{
			Style = St("Num"),
			FontSize = 28.0,
			Text = "+15",
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel2.Children.Add(afterText);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "мин",
			Foreground = Fog,
			FontSize = 14.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(6.0, 0.0, 0.0, 6.0)
		});
		grid.Children.Add(stackPanel2);
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		stackPanel3.VerticalAlignment = VerticalAlignment.Center;
		stackPanel3.Margin = new Thickness(0.0, 0.0, 6.0, 0.0);
		StackPanel stackPanel4 = stackPanel3;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Small");
		button.Content = Icons.Make("chevron-up", 11.0);
		button.Height = 20.0;
		button.Width = 28.0;
		button.FontSize = 9.0;
		button.Margin = new Thickness(0.0, 0.0, 0.0, 2.0);
		System.Windows.Controls.Button button2 = button;
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = St("Small");
		button3.Content = Icons.Make("chevron-down", 11.0);
		button3.Height = 20.0;
		button3.Width = 28.0;
		button3.FontSize = 9.0;
		System.Windows.Controls.Button button4 = button3;
		button2.Click += delegate
		{
			SetAfter(cfg.AfterMin + 5);
		};
		button4.Click += delegate
		{
			SetAfter(cfg.AfterMin - 5);
		};
		stackPanel4.Children.Add(button2);
		stackPanel4.Children.Add(button4);
		grid.Children.Add(stackPanel4);
		border2.Child = grid;
		border2.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetAfter(cfg.AfterMin + 5 * Math.Sign(e.Delta));
			e.Handled = true;
		};
		return border2;
	}

	private void SetAfter(int min, bool save = true)
	{
		min = Math.Max(15, Math.Min(60, min - min % 5));
		cfg.AfterMin = min;
		if (afterText != null)
		{
			afterText.Text = "+" + min;
		}
		RefreshAsap(force: true);
		UpdateWarInfo();
		if (save)
		{
			SaveCfg();
		}
	}

	private void SetSnipeMode(bool after, bool save = true)
	{
		cfg.AfterCapt = after;
		UIElement[] array = timeRows;
		foreach (UIElement uIElement in array)
		{
			uIElement.Visibility = (after ? Visibility.Collapsed : Visibility.Visible);
		}
		snipePanel.Visibility = ((!after) ? Visibility.Collapsed : Visibility.Visible);
		if (targetMark != null)
		{
			targetMark.Visibility = ((!after) ? Visibility.Collapsed : Visibility.Visible);
		}
		FrameworkElement frameworkElement = ((PrepText.Parent is FrameworkElement) ? (((FrameworkElement)PrepText.Parent).Parent as FrameworkElement) : null);
		if (frameworkElement != null)
		{
			frameworkElement.Visibility = (after ? Visibility.Hidden : Visibility.Visible);
		}
		Icons.Set(BtnStart, after ? "clock" : "strike", after ? "Ждать и забить" : "Запустить забив");
		BtnStart.ToolTip = (after ? "Бот сам дождётся, когда цель можно бить, и сразу поставит атаку. Остановить — «Стоп» или F10" : null);
		Mark(modeTimeBtn, !after);
		Mark(modeAfterBtn, after);
		RefreshAsap(force: true);
		PaintWatchCells();
		UpdateWarInfo();
		if (save)
		{
			SaveCfg();
		}
	}

	private void Mark(System.Windows.Controls.Button b, bool on)
	{
		if (b != null)
		{
			b.BorderBrush = (on ? SignalB : RuleB);
			b.Foreground = (on ? Chalk : Fog);
		}
	}

	private string OwnFamilyName()
	{
		if (string.IsNullOrEmpty(NetWatch.OwnFamily))
		{
			return cfg.Family;
		}
		return NetWatch.OwnFamily;
	}

	private void RefreshAsap(bool force)
	{
		if (snipePanel == null || !cfg.AfterCapt || (!force && DateTime.UtcNow < asapNext))
		{
			return;
		}
		asapNext = DateTime.UtcNow.AddSeconds(1.0);
		if (!running || asapPhase == "")
		{
			Zone zone = Zones.ByCode((SquareBox.Text ?? "").Trim().ToUpperInvariant());
			try
			{
				asapV = ((zone == null) ? null : Asap.Evaluate(war, zone, OwnFamilyName(), cfg.AfterMin, War.NowUnix(), null));
			}
			catch (Exception ex)
			{
				asapV = null;
				Telemetry.Caught(ex, "решение «как только можно»");
			}
		}
		ShowAsap();
	}

	private void ShowAsap()
	{
		if (asapHead != null)
		{
			AsapVerdict asapVerdict = asapV;
			if (asapVerdict == null)
			{
				bool flag = Zones.ByCode((SquareBox.Text ?? "").Trim().ToUpperInvariant()) == null;
				asapBar.Background = Fog;
				asapHead.Foreground = Chalk;
				asapHead.Text = (flag ? "Выберите цель" : "Считаю…");
				SetText(asapDetail, flag ? "Впишите квадрат, который бьём, или кликните его на карте выше. Что мешает его бить и когда можно, TTC посчитает сам." : "");
				SetText(asapPlan, "");
				SetText(asapSrc, "");
			}
			else
			{
				System.Windows.Media.Brush brush = ((asapVerdict.Level == 0) ? GoB : ((asapVerdict.Level == 2) ? StopB : ((asapVerdict.Level < 0) ? WarnB : SignalB)));
				asapBar.Background = brush;
				asapHead.Foreground = ((asapVerdict.Level == 1) ? Chalk : brush);
				asapHead.Text = asapVerdict.Headline;
				SetText(asapDetail, asapVerdict.Detail);
				SetText(asapPlan, (running && asapPhase != "" && asapPhase != "plan") ? PhaseText(asapVerdict) : asapVerdict.Plan);
				SetText(asapSrc, asapVerdict.Src);
			}
			PaintWatchCells();
		}
	}

	private static void SetText(TextBlock t, string s)
	{
		if (t.Text != s)
		{
			t.Text = s ?? "";
		}
		t.Visibility = (string.IsNullOrEmpty(s) ? Visibility.Collapsed : Visibility.Visible);
	}

	private string PhaseText(AsapVerdict v)
	{
		AsapCapt mainCapt = v.MainCapt;
		return asapPhase switch
		{
			"form" => "Форма атаки наготове: как только капт на " + ((mainCapt != null) ? mainCapt.Square : "?") + " кончится, бот поправит время капта и сразу нажмёт.", 
			"timed" => "Форма атаки наготове: нажатие в " + War.Hms(v.PressAt) + ", капт на " + v.CaptMsk.ToString("HH:mm") + ".", 
			"ended" => "Цель свободна — бот ставит атаку.", 
			_ => v.Plan, 
		};
	}

	private bool PrepareSnipe(Job j, bool dry)
	{
		if (Zones.At(j.Col, j.Row) == null)
		{
			Log("Квадрат " + j.Square + " не найден на карте территорий.");
			SquareBox.Focus();
			return false;
		}
		asapPhase = "";
		RefreshAsap(force: true);
		AsapVerdict asapVerdict = asapV;
		if (asapVerdict != null && asapVerdict.Level == 2)
		{
			Log(asapVerdict.Headline + ": " + asapVerdict.Detail + " " + asapVerdict.Plan);
			return false;
		}
		j.Watch = true;
		j.AfterMin = cfg.AfterMin;
		j.FireUtc = DateTime.MinValue;
		j.Server = cfg.Server;
		snipeEndUtc = DateTime.MinValue;
		Log((dry ? "Тест «как только можно»" : "Ждать и забить") + ": " + j.Square + ", капт через " + j.AfterMin + " мин после того, как цель освободится." + ((asapVerdict != null) ? (" Сейчас: " + asapVerdict.Headline + ". " + asapVerdict.Detail) : "") + (dry ? " Кнопку не нажимаю." : ""));
		return true;
	}

	private void SnipeWire(Runner r)
	{
		if (r == null)
		{
			return;
		}
		r.CaptEnded = delegate(DateTime t)
		{
			w.Dispatcher.BeginInvoke((Action)delegate
			{
				snipeEndUtc = t;
			});
		};
		r.AsapUpdate = delegate(AsapVerdict v, string ph)
		{
			Turn.Eta((v != null && v.Level == 1) ? v.PressAt : 0);
			w.Dispatcher.BeginInvoke((Action)delegate
			{
				if (running)
				{
					asapV = v;
					asapPhase = ph;
					ShowAsap();
					UpdateWarInfo();
				}
			});
		};
		Runner.ShareMarkers = true;
		string srv = cfg.Server ?? "";
		r.Scanned = delegate(Runner.MapScan sc)
		{
			if (srv.Length == 0 || !Runner.ShareMarkers)
			{
				return;
			}
			string err = War.ReportSeen(srv, sc.At, sc.R0, sc.R1, sc.Marks);
			if (err != null)
			{
				try
				{
					w.Dispatcher.BeginInvoke((Action)delegate
					{
						Log("Обзор каптов не отправлен: " + err + ".");
					});
					return;
				}
				catch
				{
					return;
				}
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					warNext = DateTime.MinValue;
				});
			}
			catch
			{
			}
		};
	}

	private bool SnipeTick(DateTime now)
	{
		if (!cfg.AfterCapt || timeRows == null)
		{
			return false;
		}
		if (!running && asapPhase != "")
		{
			asapPhase = "";
		}
		RefreshAsap(force: false);
		FrameworkElement frameworkElement = ProgFill.Parent as FrameworkElement;
		ProgFill.Width = 0.0;
		AsapVerdict asapVerdict = asapV;
		string text = "--:--:--.-";
		if (running)
		{
			string text2 = (dryRun ? "Тест: " : "");
			if (asapPhase == "ended")
			{
				HeroCap.Text = text2 + ((snipeEndUtc != DateTime.MinValue) ? ("Цель свободна с " + Clock.Msk(snipeEndUtc).ToString("HH:mm:ss") + " — ставлю атаку") : "Цель свободна — ставлю атаку");
				Countdown.Text = Span(now - ((snipeEndUtc != DateTime.MinValue) ? snipeEndUtc : startUtc));
				Countdown.Foreground = SignalB;
				return true;
			}
			if (asapVerdict != null && asapVerdict.Level == 1)
			{
				DateTime dateTime = War.FromUnix(asapVerdict.PressAt);
				AsapCapt mainCapt = asapVerdict.MainCapt;
				if (asapVerdict.Exact || asapPhase == "timed")
				{
					HeroCap.Text = text2 + "До забива в " + Clock.Fmt(dateTime);
					TimeSpan t = dateTime - now;
					Countdown.Text = Span(t);
					Countdown.Foreground = ((t.TotalSeconds <= 10.0) ? SignalB : Chalk);
					double totalMilliseconds = (dateTime - startUtc).TotalMilliseconds;
					double totalMilliseconds2 = (now - startUtc).TotalMilliseconds;
					ProgFill.Width = ((frameworkElement != null && totalMilliseconds > 0.0) ? (frameworkElement.ActualWidth * Math.Max(0.0, Math.Min(1.0, totalMilliseconds2 / totalMilliseconds))) : 0.0);
				}
				else if (dateTime > now)
				{
					HeroCap.Text = text2 + ((asapPhase == "form") ? "Форма наготове · " : "") + "капт на " + ((mainCapt != null) ? mainCapt.Square : "?") + " кончится не раньше " + Clock.Msk(dateTime).ToString("HH:mm:ss");
					Countdown.Text = Span(dateTime - now);
					Countdown.Foreground = Chalk;
				}
				else
				{
					HeroCap.Text = text2 + ((asapPhase == "form") ? "Форма наготове · жду" : "Жду") + " конца капта на " + ((mainCapt != null) ? mainCapt.Square : "?");
					Countdown.Text = Span(now - dateTime);
					Countdown.Foreground = SignalB;
				}
				return true;
			}
			HeroCap.Text = text2 + "Готовлю атаку на " + (SquareBox.Text ?? "").Trim().ToUpperInvariant();
			Countdown.Text = Span(now - startUtc);
			Countdown.Foreground = Chalk;
			return true;
		}
		if (statusKind == "done" || statusKind == "testok" || statusKind == "error")
		{
			return false;
		}
		Zone zone = Zones.ByCode((SquareBox.Text ?? "").Trim().ToUpperInvariant());
		Countdown.Text = text;
		Countdown.Foreground = Dim;
		if (zone == null || asapVerdict == null)
		{
			HeroCap.Text = "Впишите квадрат цели";
			return true;
		}
		if (asapVerdict.Level == 2)
		{
			HeroCap.Text = asapVerdict.Headline + ": " + zone.Label;
			return true;
		}
		if (asapVerdict.Level == 1)
		{
			DateTime dateTime2 = War.FromUnix(asapVerdict.PressAt);
			HeroCap.Text = zone.Label + (asapVerdict.Exact ? " можно бить с " : " освободится не раньше ") + Clock.Msk(dateTime2).ToString("HH:mm:ss");
			if (dateTime2 > now)
			{
				Countdown.Text = Span(dateTime2 - now);
				Countdown.Foreground = Fog;
			}
			return true;
		}
		HeroCap.Text = "Нажмите «Ждать и забить»: " + zone.Label + ", капт на " + asapVerdict.CaptMsk.ToString("HH:mm");
		return true;
	}

	private void AsapFromMap(Zone z)
	{
		if (!running)
		{
			SquareBox.Text = z.Label;
			if (!cfg.AfterCapt)
			{
				SetSnipeMode(after: true);
			}
			RefreshAsap(force: true);
			Log("Цель с карты: " + z.Label + " — забить, как только станет можно.");
		}
	}

	private void InitLayout()
	{
		rootBorder = w.Content as Border;
		if (rootBorder == null)
		{
			return;
		}
		rootMargin = rootBorder.Margin;
		rootRadius = rootBorder.CornerRadius;
		w.Content = null;
		layoutScale = new ScaleTransform(1.0, 1.0);
		layoutHost = new Grid
		{
			Width = 1100.0,
			Height = 902.0,
			LayoutTransform = layoutScale,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top
		};
		layoutHost.Children.Add(rootBorder);
		layoutRoot = new Grid
		{
			Background = System.Windows.Media.Brushes.Transparent
		};
		TextElement.SetForeground(layoutRoot, w.Foreground);
		TextElement.SetFontFamily(layoutRoot, w.FontFamily);
		TextElement.SetFontSize(layoutRoot, w.FontSize);
		TextOptions.SetTextRenderingMode(layoutRoot, TextOptions.GetTextRenderingMode(w));
		layoutRoot.Children.Add(layoutHost);
		layoutRoot.SizeChanged += delegate
		{
			FitLayout();
		};
		w.Content = layoutRoot;
		w.ResizeMode = ResizeMode.CanResize;
		WindowChrome.SetWindowChrome(w, new WindowChrome
		{
			CaptionHeight = 0.0,
			ResizeBorderThickness = new Thickness(10.0),
			GlassFrameThickness = new Thickness(0.0),
			CornerRadius = new CornerRadius(0.0),
			UseAeroCaptionButtons = false
		});
		w.MinWidth = 500.0;
		w.MinHeight = 460.0;
		Rect workArea = SystemParameters.WorkArea;
		double num = (((double)cfg.WinW >= w.MinWidth) ? ((double)cfg.WinW) : 1100.0);
		double num2 = (((double)cfg.WinH >= w.MinHeight) ? ((double)cfg.WinH) : 902.0);
		double num3 = Math.Min(1.0, Math.Min((workArea.Width - 8.0) / num, (workArea.Height - 8.0) / num2));
		w.Width = Math.Max(w.MinWidth, Math.Round(num * num3));
		w.Height = Math.Max(w.MinHeight, Math.Round(num2 * num3));
		if (w.WindowStartupLocation == WindowStartupLocation.Manual)
		{
			w.Left = Math.Max(workArea.Left, Math.Min(w.Left, workArea.Right - w.Width));
			w.Top = Math.Max(workArea.Top, Math.Min(w.Top, workArea.Bottom - w.Height));
		}
		System.Windows.Controls.Button button = F<System.Windows.Controls.Button>("BtnMin");
		if (button.Parent is System.Windows.Controls.Panel panel)
		{
			maxBtn = new System.Windows.Controls.Button
			{
				Style = St("WinBtn"),
				Content = Icons.Make("maximize", 13.0),
				ToolTip = "Развернуть на весь экран"
			};
			maxBtn.Click += delegate
			{
				ToggleMax();
			};
			panel.Children.Insert(panel.Children.IndexOf(button) + 1, maxBtn);
		}
		TitleBar.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				ToggleMax();
				e.Handled = true;
			}
		};
		InitBodyScroll();
		InitBottomBar();
		InitTitleFit();
		SplitHead(StatusText.Parent as FrameworkElement, StatusText);
		FrameworkElement tg = TargetGrid;
		if (tg != null)
		{
			tg.SizeChanged += delegate
			{
				tg.Clip = new RectangleGeometry(new Rect(0.0, 0.0, tg.ActualWidth, tg.ActualHeight), 5.0, 5.0);
			};
		}
		w.StateChanged += delegate
		{
			ApplyWindowState();
		};
		w.SizeChanged += delegate
		{
			if (w.WindowState == WindowState.Normal && w.IsLoaded)
			{
				cfg.WinW = (int)w.ActualWidth;
				cfg.WinH = (int)w.ActualHeight;
			}
		};
		if (cfg.WinMax)
		{
			w.Loaded += delegate
			{
				w.WindowState = WindowState.Maximized;
			};
		}
		ApplyWindowState();
	}

	private void FitLayout()
	{
		double actualWidth = layoutRoot.ActualWidth;
		double actualHeight = layoutRoot.ActualHeight;
		if (!(actualWidth < 50.0) && !(actualHeight < 50.0))
		{
			double num = Math.Max(0.8, Math.Min(actualWidth / 1100.0, actualHeight / 902.0));
			if (Math.Abs(num - 1.0) < 0.01)
			{
				num = 1.0;
			}
			ScaleTransform scaleTransform = layoutScale;
			double scaleX = (layoutScale.ScaleY = num);
			scaleTransform.ScaleX = scaleX;
			double num3 = Math.Floor(actualWidth / num);
			double height = Math.Floor(actualHeight / num);
			layoutHost.Width = num3;
			layoutHost.Height = height;
			TextOptions.SetTextFormattingMode(layoutHost, (num == 1.0) ? TextFormattingMode.Display : TextFormattingMode.Ideal);
			ApplyLayoutMode(num3 < 940.0);
		}
	}

	private void ToggleMax()
	{
		w.WindowState = ((w.WindowState != WindowState.Maximized) ? WindowState.Maximized : WindowState.Normal);
	}

	private void ApplyWindowState()
	{
		bool flag = w.WindowState == WindowState.Maximized;
		cfg.WinMax = flag;
		rootBorder.Margin = (flag ? new Thickness(0.0) : rootMargin);
		rootBorder.CornerRadius = (flag ? new CornerRadius(0.0) : rootRadius);
		layoutRoot.Margin = (flag ? new Thickness(7.0) : new Thickness(0.0));
		if (maxBtn != null)
		{
			maxBtn.Content = Icons.Make(flag ? "restore" : "maximize", 13.0);
			maxBtn.ToolTip = (flag ? "Вернуть обычный размер" : "Развернуть на весь экран");
		}
	}

	private void InitBodyScroll()
	{
		if (Inputs.Parent is Grid grid && RootGrid != null && grid.Parent == RootGrid)
		{
			int row = Grid.GetRow(grid);
			int index = RootGrid.Children.IndexOf(grid);
			RootGrid.Children.Remove(grid);
			bodyHost = new Grid
			{
				Margin = new Thickness(0.0, 0.0, 0.0, 12.0)
			};
			Grid.SetRow(grid, 0);
			bodyHost.Children.Add(grid);
			bodyScroll = new BodyScroller
			{
				Content = bodyHost,
				Focusable = false,
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
			};
			DependencyObject[] array = new DependencyObject[5] { CaptHBox, CaptMBox, PeopleText, PrepText, FineText };
			foreach (DependencyObject item in array)
			{
				bodyScroll.WheelOwners.Add(item);
			}
			Grid.SetRow(bodyScroll, row);
			RootGrid.Children.Insert(index, bodyScroll);
		}
	}

	private IEnumerable<Grid> TwoColumnBodies()
	{
		if (Inputs.Parent is Grid main)
		{
			yield return main;
		}
		if (vzBody != null)
		{
			yield return vzBody;
		}
		if (planMain != null)
		{
			yield return planMain;
		}
		if (prioBody != null)
		{
			yield return prioBody;
		}
	}

	private void ApplyLayoutMode(bool narrow)
	{
		foreach (Grid item in TwoColumnBodies().ToList())
		{
			PrepareBody(item);
			ArrangeBody(item, narrow);
		}
		ArrangeBottom(narrow);
		StretchTargetMap(narrow);
		if (planMinText != null)
		{
			bodyScroll.WheelOwners.Add(planMinText);
		}
		foreach (StepRow row in rows)
		{
			row.Name.TextTrimming = TextTrimming.CharacterEllipsis;
		}
		narrowLayout = narrow;
	}

	private void PrepareBody(Grid g)
	{
		if (!bodiesReady.Add(g))
		{
			return;
		}
		Grid grid = g.Children.OfType<Grid>().FirstOrDefault((Grid c) => Grid.GetColumn(c) == 2);
		if (grid == null || grid.RowDefinitions.Count == 0)
		{
			return;
		}
		int last = grid.RowDefinitions.Count - 1;
		if (grid.RowDefinitions[last].Height.IsStar)
		{
			foreach (FrameworkElement item in (from c in grid.Children.OfType<FrameworkElement>()
				where Grid.GetRow(c) == last
				select c).ToList())
			{
				int index = grid.Children.IndexOf(item);
				grid.Children.RemoveAt(index);
				ElasticBox elasticBox = new ElasticBox();
				elasticBox.Shown = ((g == Inputs.Parent) ? 290 : 220);
				elasticBox.Margin = item.Margin;
				ElasticBox elasticBox2 = elasticBox;
				item.Margin = new Thickness(0.0);
				Grid.SetRow(elasticBox2, last);
				Grid.SetColumn(elasticBox2, Grid.GetColumn(item));
				Grid.SetColumnSpan(elasticBox2, Grid.GetColumnSpan(item));
				elasticBox2.Child = item;
				grid.Children.Insert(index, elasticBox2);
			}
		}
		if (g == vzBody && vzStatusText != null)
		{
			SplitHead(vzStatusText.Parent as FrameworkElement, vzStatusText);
		}
		if (g == prioBody && prioStatusText != null)
		{
			SplitHead(prioStatusText.Parent as FrameworkElement, prioStatusText);
		}
		if (g == planMain)
		{
			if (planStatusText != null)
			{
				SplitHead(planStatusText.Parent as FrameworkElement, planStatusText);
			}
			if (planFamHead != null)
			{
				planFamHead.TextAlignment = TextAlignment.Right;
				SplitHead(planFamHead, planFamHead);
			}
		}
	}

	private void ArrangeBody(Grid g, bool narrow)
	{
		if (!colHome.TryGetValue(g, out var value))
		{
			value = (colHome[g] = g.ColumnDefinitions.Select((ColumnDefinition c) => c.Width).ToArray());
		}
		if (value.Length < 3)
		{
			return;
		}
		for (int i = 0; i < 3; i++)
		{
			g.ColumnDefinitions[i].Width = ((!narrow) ? value[i] : ((i == 0) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(0.0)));
		}
		if (narrow && g.RowDefinitions.Count != 2)
		{
			g.RowDefinitions.Clear();
			g.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			g.RowDefinitions.Add(new RowDefinition
			{
				Height = new GridLength(1.0, GridUnitType.Star)
			});
		}
		if (!narrow && g.RowDefinitions.Count != 0)
		{
			g.RowDefinitions.Clear();
		}
		foreach (FrameworkElement item in g.Children.OfType<FrameworkElement>())
		{
			CellHome cellHome = Home(item);
			bool flag = cellHome.Col >= 2;
			Grid.SetColumn(item, (!narrow) ? cellHome.Col : 0);
			Grid.SetRow(item, narrow ? (flag ? 1 : 0) : cellHome.Row);
			item.Margin = ((narrow && flag) ? new Thickness(cellHome.Margin.Left, cellHome.Margin.Top + 30.0, cellHome.Margin.Right, cellHome.Margin.Bottom) : cellHome.Margin);
		}
	}

	private CellHome Home(FrameworkElement c)
	{
		if (!this.cellHome.TryGetValue(c, out var value))
		{
			value = (this.cellHome[c] = new CellHome
			{
				Col = Grid.GetColumn(c),
				Row = Grid.GetRow(c),
				ColSpan = Grid.GetColumnSpan(c),
				Margin = c.Margin,
				HAlign = c.HorizontalAlignment
			});
		}
		return value;
	}

	private void StretchTargetMap(bool narrow)
	{
		FrameworkElement targetGrid = TargetGrid;
		Border border = ((targetGrid == null) ? null : (targetGrid.Parent as Border));
		if (border != null)
		{
			if (targetW0 == 0.0)
			{
				targetW0 = targetGrid.Width;
				targetH0 = targetGrid.Height;
			}
			targetGrid.Width = (narrow ? double.NaN : targetW0);
			targetGrid.Height = (narrow ? (targetH0 + 20.0) : targetH0);
			border.HorizontalAlignment = (narrow ? System.Windows.HorizontalAlignment.Stretch : System.Windows.HorizontalAlignment.Left);
		}
	}

	private void InitBottomBar()
	{
		FrameworkElement frameworkElement = BtnStart.Parent as FrameworkElement;
		bottomGrid = ((frameworkElement == null) ? null : (frameworkElement.Parent as Grid));
		if (bottomGrid != null && RootGrid.RowDefinitions.Count >= 3)
		{
			RowDefinition rowDefinition = RootGrid.RowDefinitions[2];
			rowDefinition.MinHeight = (rowDefinition.Height.IsAbsolute ? rowDefinition.Height.Value : 80.0);
			rowDefinition.Height = GridLength.Auto;
			Thickness margin = bottomGrid.Margin;
			bottomGrid.Margin = new Thickness(margin.Left, 12.0, margin.Right, 12.0);
			bottomGrid.VerticalAlignment = VerticalAlignment.Center;
		}
	}

	private void ArrangeBottom(bool narrow)
	{
		if (bottomGrid == null)
		{
			return;
		}
		if (narrow && bottomGrid.RowDefinitions.Count != 2)
		{
			bottomGrid.RowDefinitions.Clear();
			bottomGrid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
			bottomGrid.RowDefinitions.Add(new RowDefinition
			{
				Height = GridLength.Auto
			});
		}
		if (!narrow && bottomGrid.RowDefinitions.Count != 0)
		{
			bottomGrid.RowDefinitions.Clear();
		}
		foreach (FrameworkElement item in bottomGrid.Children.OfType<FrameworkElement>())
		{
			CellHome cellHome = Home(item);
			bool flag = cellHome.Col >= 1;
			Grid.SetColumn(item, (!narrow) ? cellHome.Col : 0);
			Grid.SetColumnSpan(item, narrow ? Math.Max(1, bottomGrid.ColumnDefinitions.Count) : cellHome.ColSpan);
			Grid.SetRow(item, (narrow && flag) ? 1 : cellHome.Row);
			item.Margin = ((narrow && flag) ? new Thickness(cellHome.Margin.Left, cellHome.Margin.Top + 10.0, cellHome.Margin.Right, cellHome.Margin.Bottom) : cellHome.Margin);
			item.HorizontalAlignment = ((narrow && flag) ? System.Windows.HorizontalAlignment.Right : cellHome.HAlign);
		}
	}

	private static void SplitHead(FrameworkElement right, TextBlock text)
	{
		Grid head = ((right == null) ? null : (right.Parent as Grid));
		if (head == null || head.ColumnDefinitions.Count > 0)
		{
			return;
		}
		head.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		head.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		Grid.SetColumn(right, 1);
		foreach (TextBlock item in head.Children.OfType<TextBlock>())
		{
			if (item != right)
			{
				item.TextTrimming = TextTrimming.CharacterEllipsis;
				item.Margin = new Thickness(item.Margin.Left, item.Margin.Top, Math.Max(12.0, item.Margin.Right), item.Margin.Bottom);
			}
		}
		if (text != null)
		{
			text.TextTrimming = TextTrimming.CharacterEllipsis;
			head.SizeChanged += delegate
			{
				text.MaxWidth = Math.Max(80.0, head.ActualWidth * 0.62 - (double)((text != right) ? 16 : 0));
			};
		}
	}

	private void InitTitleFit()
	{
		titleLeft = LogoImg.Parent as StackPanel;
		titleRight = F<System.Windows.Controls.Button>("BtnClose").Parent as StackPanel;
		if (titleLeft == null || titleRight == null)
		{
			return;
		}
		TextBlock[] array = new TextBlock[3] { ServerText, FamilyText, LicText };
		foreach (TextBlock textBlock in array)
		{
			if (textBlock != null)
			{
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			}
		}
		SizeChangedEventHandler value = delegate
		{
			FitTitle();
		};
		TitleBar.SizeChanged += value;
		titleLeft.SizeChanged += value;
		titleRight.SizeChanged += value;
	}

	private void FitTitle()
	{
		if (titleFitting || titleRight == null || TitleBar.ActualWidth < 50.0)
		{
			return;
		}
		titleFitting = true;
		try
		{
			double num = TitleBar.ActualWidth - 16.0;
			System.Windows.Size availableSize = new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity);
			titleLevel = 0;
			while (titleLevel < 13)
			{
				ApplyTitleLevel(titleLevel);
				DirtyMeasure(titleLeft);
				DirtyMeasure(titleRight);
				titleLeft.Measure(availableSize);
				titleRight.Measure(availableSize);
				if (!(titleLeft.DesiredSize.Width + titleRight.DesiredSize.Width <= num))
				{
					titleLevel++;
					continue;
				}
				break;
			}
		}
		finally
		{
			titleFitting = false;
		}
	}

	private void ApplyTitleLevel(int lv)
	{
		double num = double.PositiveInfinity;
		foreach (System.Windows.Controls.Button item in titleRight.Children.OfType<System.Windows.Controls.Button>())
		{
			TextBlock textBlock = BarLabel(item);
			if (textBlock != null)
			{
				if (!barMargin.TryGetValue(item, out var value))
				{
					value = (barMargin[item] = item.Margin);
				}
				item.Margin = new Thickness(value.Left, value.Top, (lv >= 5) ? Math.Min(value.Right, 12.0) : value.Right, value.Bottom);
				bool hide = false;
				double maxWidth = num;
				if (textBlock.Text == "Калибровка")
				{
					hide = lv >= 1;
				}
				else if (textBlock.Text == "Telegram")
				{
					hide = lv >= 1;
				}
				else if (textBlock.Text == "Карта")
				{
					hide = lv >= 2;
				}
				else if (textBlock == ServerText)
				{
					maxWidth = ((lv >= 7) ? 100.0 : ((lv >= 3) ? 150.0 : num));
					hide = lv >= 11;
				}
				else if (textBlock == FamilyText)
				{
					maxWidth = ((lv >= 7) ? 90.0 : ((lv >= 4) ? 120.0 : num));
					hide = lv >= 10;
				}
				else if (textBlock == netText)
				{
					hide = lv >= 6;
				}
				else if (textBlock == LicText)
				{
					maxWidth = ((lv >= 9) ? 62.0 : num);
				}
				FitHide(textBlock, hide);
				textBlock.MaxWidth = maxWidth;
			}
		}
		if (modeBtn != null)
		{
			if (!barMargin.TryGetValue(modeBtn, out var value2))
			{
				value2 = (barMargin[modeBtn] = modeBtn.Margin);
			}
			modeBtn.Margin = new Thickness((lv >= 5) ? Math.Min(value2.Left, 10.0) : value2.Left, value2.Top, value2.Right, value2.Bottom);
			if (modeText != null)
			{
				FitHide(modeText, lv >= 12);
			}
		}
		TextBlock textBlock2 = titleLeft.Children.OfType<TextBlock>().FirstOrDefault((TextBlock t) => t.Text == "TTC");
		if (textBlock2 != null)
		{
			FitHide(textBlock2, lv >= 8);
		}
		TextBlock textBlock3 = titleLeft.Children.OfType<TextBlock>().FirstOrDefault((TextBlock t) => t.Text != "TTC");
		if (textBlock3 != null)
		{
			FitHide(textBlock3, lv >= 1);
		}
	}

	private static void DirtyMeasure(DependencyObject d)
	{
		if (d is UIElement uIElement)
		{
			uIElement.InvalidateMeasure();
		}
		int childrenCount = VisualTreeHelper.GetChildrenCount(d);
		for (int i = 0; i < childrenCount; i++)
		{
			DirtyMeasure(VisualTreeHelper.GetChild(d, i));
		}
	}

	private static TextBlock BarLabel(System.Windows.Controls.Button b)
	{
		if (b.Content is System.Windows.Controls.Panel panel)
		{
			return panel.Children.OfType<TextBlock>().LastOrDefault((TextBlock t) => t.Text != "●" && t.Text != "▾");
		}
		return null;
	}

	private void FitHide(UIElement e, bool hide)
	{
		if (hide && e.Visibility == Visibility.Visible)
		{
			e.Visibility = Visibility.Collapsed;
			fitHidden.Add(e);
		}
		else if (!hide && fitHidden.Remove(e))
		{
			e.Visibility = Visibility.Visible;
		}
	}

	private void InitNet()
	{
		if (demo || MapDemo)
		{
			return;
		}
		if (ServerBtn.Parent is System.Windows.Controls.Panel panel)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			StackPanel stackPanel2 = stackPanel;
			netDot = new TextBlock
			{
				Text = "●",
				FontSize = 11.0,
				Foreground = Dim,
				VerticalAlignment = VerticalAlignment.Center
			};
			netText = new TextBlock
			{
				Text = "Игра",
				FontSize = 13.0,
				Margin = new Thickness(6.0, 0.0, 0.0, 0.0),
				Foreground = Chalk
			};
			stackPanel2.Children.Add(netDot);
			stackPanel2.Children.Add(netText);
			netBtn = new System.Windows.Controls.Button
			{
				Style = St("Plain"),
				Content = stackPanel2,
				Margin = new Thickness(0.0, 0.0, 20.0, 0.0)
			};
			netBtn.Click += delegate
			{
				NetClick();
			};
			panel.Children.Insert(panel.Children.IndexOf(ServerBtn), netBtn);
		}
		NetWatch.Now = () => Clock.UtcNow();
		NetWatch.StorePath = System.IO.Path.Combine(AppPaths.Data, "netdelay.txt");
		NetWatch.Note += delegate(string s)
		{
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					Log(s);
				});
			}
			catch
			{
			}
		};
		NetWatch.OwnFamilyChanged += delegate(string n)
		{
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					OnGameFamily(n);
				});
			}
			catch
			{
			}
		};
		netStarted = NetWatch.Start();
		if (netStarted)
		{
			Log("Капты из игры: TTC читает входящий трафик своей игры (только чтение, в игру ничего не отправляет) — капты уходят на общую карту TTC. Всю карту сервер игры присылает, когда в планшете открывают карту территорий: откройте её один раз после входа.");
		}
		else
		{
			Log("Капты из игры не включились: " + NetWatch.Error + ". Нажмите «Игра» вверху, чтобы попробовать снова.");
		}
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromSeconds(1.0);
		DispatcherTimer dispatcherTimer2 = dispatcherTimer;
		dispatcherTimer2.Tick += delegate
		{
			NetTick();
		};
		dispatcherTimer2.Start();
		w.Closing += delegate(object s, CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				NetWatch.Stop();
			}
		};
	}

	private void NetTick()
	{
		UpdateNetBadge();
		if (!netStarted || netSending || closing)
		{
			return;
		}
		string srv = (cfg.Server ?? "").Trim();
		if (srv.Length == 0 || License.SavedKey().Length == 0)
		{
			return;
		}
		string gameServer = NetWatch.GameServer;
		if (gameServer != null && !string.Equals(gameServer, srv, StringComparison.OrdinalIgnoreCase))
		{
			if (netMismatchLogged != gameServer + "|" + srv)
			{
				netMismatchLogged = gameServer + "|" + srv;
				Log("Капты из игры: в игре сервер " + gameServer + ", а в TTC выбран " + srv.ToUpperInvariant() + " — не отправляю, чтобы не смешать карты. Выберите сервер " + gameServer + ".");
			}
			return;
		}
		List<NetCapt> batch = NetWatch.TakeOutbox();
		bool ownersFull;
		List<NetOwner> owners = NetWatch.TakeOwners(out ownersFull);
		NetLimits lim = NetWatch.TakeLimits();
		string fam = ((lim == null) ? null : ((!string.IsNullOrEmpty(lim.Family)) ? lim.Family : (cfg.Family ?? "").Trim()));
		if (batch.Count == 0 && owners.Count == 0 && lim == null)
		{
			return;
		}
		netSending = true;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err;
			try
			{
				err = War.ReportNet(srv, batch, owners, ownersFull, lim, fam);
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}
			if (err != null)
			{
				NetWatch.Requeue(batch);
				NetWatch.RequeueOwners(owners, ownersFull);
				if (lim != null)
				{
					NetWatch.RequeueLimits();
				}
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					netSending = false;
					if (err != null && err != netLastErr)
					{
						Log("Капты из игры: не отправлено на карту TTC: " + err + " — повторю.");
					}
					if (err == null && netLastErr != null)
					{
						Log("Капты из игры снова уходят на карту TTC.");
					}
					netLastErr = err;
				});
			}
			catch
			{
				netSending = false;
			}
		});
	}

	private void UpdateNetBadge()
	{
		if (netBtn != null)
		{
			Clock.UtcNow();
			System.Windows.Media.Brush foreground;
			string text;
			if (!netStarted)
			{
				foreground = StopB;
				text = "Капты из игры не включились: " + NetWatch.Error + "\nНажмите, чтобы попробовать снова.";
			}
			else if (!NetWatch.GameRunning)
			{
				foreground = Dim;
				text = "Капты из игры: игра не запущена.";
			}
			else if (NetWatch.LastFullUtc == DateTime.MinValue)
			{
				foreground = WarnB;
				text = "Капты из игры: игра запущена, всей карты ещё нет.\nОткройте в планшете карту территорий один раз — сервер игры пришлёт всю карту.";
			}
			else
			{
				foreground = ((netLastErr != null) ? WarnB : GoB);
				text = "Капты из игры: " + NetWatch.ZonesKnown + " зон, вся карта получена в " + Clock.Msk(NetWatch.LastFullUtc).ToString("HH:mm") + ", последнее событие в " + Clock.Msk(NetWatch.LastUpdateUtc).ToString("HH:mm:ss") + "." + ((netLastErr != null) ? ("\nНе отправляется на карту TTC: " + netLastErr) : "\nУходят на общую карту TTC.") + "\nНажмите — список каптов в журнал.";
			}
			if (NetWatch.Running && NetWatch.RttStats(15.0, out var median, out var min, out var max, out var n) && n >= 5)
			{
				text = text + $"\nПинг по трафику: {median:0} мс (разброс {min:0}–{max:0}, замеров {n})" + ((NetWatch.WireDelayCount > 0) ? $", задержка игры {NetWatch.WireDelayMs:0} мс по {NetWatch.WireDelayCount} нажатиям" : "");
			}
			netDot.Foreground = foreground;
			netBtn.ToolTip = text;
		}
	}

	private void NetClick()
	{
		if (!netStarted)
		{
			netStarted = NetWatch.Start();
			Log(netStarted ? "Капты из игры включены." : ("Капты из игры не включились: " + NetWatch.Error + "."));
			return;
		}
		List<NetCapt> list = NetWatch.Active();
		long num = War.NowUnix();
		Log("Капты из игры: " + (NetWatch.GameRunning ? "игра запущена" : "игра не запущена") + ", зон " + NetWatch.ZonesKnown + ((NetWatch.LastFullUtc == DateTime.MinValue) ? ", всей карты ещё нет (откройте карту территорий в планшете)" : (", вся карта в " + Clock.Msk(NetWatch.LastFullUtc).ToString("HH:mm"))) + ", каптов идёт или впереди: " + list.Count + ".");
		foreach (NetCapt c in list)
		{
			List<Zone> all = Zones.All;
			Func<Zone, bool> predicate = (Zone x) => x.Id == c.Zone;
			Zone zone = all.FirstOrDefault(predicate);
			string text = ((c.Start > num) ? ("начнётся в " + War.Hm(c.Start) + " (через " + (c.Start - num + 59) / 60 + " мин)") : ("идёт с " + War.Hm(c.Start) + ", кончится " + War.Hm(c.Start + 600) + "–" + War.Hm(c.Start + 1200)));
			Log("  " + ((zone != null) ? zone.Label : ("зона " + c.Zone)) + ": " + c.A + " → " + (c.D ?? "?") + ", " + text + ".");
		}
	}

	private void InitPrep()
	{
		if (!demo && !MapDemo && ServerBtn.Parent is System.Windows.Controls.Panel panel)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			StackPanel stackPanel2 = stackPanel;
			stackPanel2.Children.Add(Icons.Make("scan", 15.0, SignalB));
			stackPanel2.Children.Add(new TextBlock
			{
				Text = "Калибровка",
				FontSize = 13.0,
				Margin = new Thickness(6.0, 0.0, 0.0, 0.0),
				Foreground = Chalk
			});
			prepBtn = new System.Windows.Controls.Button
			{
				Style = St("Plain"),
				Content = stackPanel2,
				Margin = new Thickness(0.0, 0.0, 20.0, 0.0),
				ToolTip = "Подготовить TTC под текущий размер окна игры: раскладка планшета, карта сверху и снизу, поля «Часы»/«Минуты», панель пинга — всё заранее, без забива (около 20–40 с).\nКнопку «Запланировать атаку» бот не нажмёт. Квадрат — из поля цели."
			};
			prepBtn.Click += delegate
			{
				StartPrepare();
			};
			panel.Children.Insert(panel.Children.IndexOf(ServerBtn), prepBtn);
		}
	}

	private void StartPrepare()
	{
		if (running)
		{
			Log("Сейчас идёт забив или калибровка — калибровка после неё.");
			return;
		}
		if (lastW <= 0)
		{
			Log("Игра не найдена — запустите GTA 5 в режиме «Оконный без рамки».");
			return;
		}
		Log("Калибровка: бот откроет планшет и карту, померит всё под окно " + lastW + "×" + lastH + " и откроет форму на " + (SquareBox.Text ?? "").Trim().ToUpperInvariant() + ". Окно игры не трогайте.");
		Start(dry: true, prepare: true);
	}

	private void ShowGameMenu()
	{
		if (lastW <= 0)
		{
			Log("Игра не найдена — запустите GTA 5 в режиме «Оконный без рамки».");
			return;
		}
		if (running)
		{
			Log("Сейчас идёт забив или калибровка — меню калибровки откроется после неё.");
			return;
		}
		string text = lastW + "×" + lastH;
		StackPanel stackPanel = new StackPanel();
		stackPanel.MinWidth = 330.0;
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "Окно игры " + text,
			FontSize = 12.5,
			Foreground = Fog,
			Margin = new Thickness(12.0, 6.0, 12.0, 4.0)
		});
		stackPanel2.Children.Add(MenuButton(MenuItem("Подготовить под это окно", "все замеры заранее, без забива (~20–40 с): планшет, карта сверху и снизу, поля времени, пинг. Кнопку атаки бот не нажмёт", SignalB), StartPrepare));
		stackPanel2.Children.Add(MenuButton(MenuItem("Откалибровать заново", "забыть замеры этого размера окна — подберутся снова при открытии планшета", Chalk), AskRecalibrate));
		ShowPopup(GameBtn, stackPanel2, above: true);
	}

	private UIElement MenuItem(string title, string sub, System.Windows.Media.Brush color)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.MaxWidth = 360.0;
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(new TextBlock
		{
			Text = title,
			FontSize = 13.5,
			FontWeight = FontWeights.SemiBold,
			Foreground = color
		});
		stackPanel2.Children.Add(new TextBlock
		{
			Text = sub,
			FontSize = 12.0,
			Foreground = Fog,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 2.0, 0.0, 0.0)
		});
		return stackPanel2;
	}

	private void PrepHint(ProbeResult r)
	{
		string text = prepLastSize;
		prepLastSize = ((r != null && r.Found && r.W >= 200 && r.H >= 200) ? r.Size : null);
		if (r != null && r.Found && !r.Black && r.W >= 200 && r.H >= 200 && !string.IsNullOrEmpty(r.Size) && !(r.Size == prepHintSize) && !(r.Size != text))
		{
			prepHintSize = r.Size;
			if (!r.Calibrated)
			{
				Log("Окно игры " + r.Size + ": под этот размер TTC ещё не подготовлена. Нажмите «Калибровка» вверху — бот заранее померит планшет, карту и поля формы (кнопку атаки не нажмёт), и первый забив не будет тратить на это время.");
			}
		}
	}

	private void LoadRosterCfg()
	{
		try
		{
			if (File.Exists(RosterCfgPath))
			{
				rcfg = json.Deserialize<RosterCfg>(File.ReadAllText(RosterCfgPath)) ?? new RosterCfg();
			}
		}
		catch
		{
			rcfg = new RosterCfg();
		}
	}

	private void SaveRosterCfg()
	{
		try
		{
			File.WriteAllText(RosterCfgPath, json.Serialize(rcfg));
		}
		catch
		{
		}
	}

	private void InitRoster()
	{
		if (demo || MapDemo)
		{
			return;
		}
		LoadRosterCfg();
		Roster.Armed = rcfg.Armed;
		if (BuildModeMenu() && BuildListPanel())
		{
			SetMode(rcfg.ListsMode);
			InitVzz();
			InitPlan();
			InitPriority();
			InitIcons();
			DispatcherTimer dispatcherTimer = new DispatcherTimer();
			dispatcherTimer.Interval = TimeSpan.FromSeconds(1.0);
			DispatcherTimer dispatcherTimer2 = dispatcherTimer;
			dispatcherTimer2.Tick += delegate
			{
				listGoBtn.IsEnabled = !running;
			};
			dispatcherTimer2.Start();
			Thread thread = new Thread(RosterLoop);
			thread.IsBackground = true;
			thread.Name = "TTC roster";
			Thread thread2 = thread;
			thread2.Start();
		}
	}

	private bool BuildModeMenu()
	{
		if (!(LogoImg.Parent is StackPanel stackPanel))
		{
			return false;
		}
		TextBlock textBlock = stackPanel.Children.OfType<TextBlock>().LastOrDefault();
		if (textBlock != null && stackPanel.Children.IndexOf(textBlock) > 1)
		{
			textBlock.Visibility = Visibility.Collapsed;
		}
		modeText = new TextBlock
		{
			FontSize = 13.5,
			Foreground = Chalk,
			VerticalAlignment = VerticalAlignment.Center
		};
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel3 = stackPanel2;
		stackPanel3.Children.Add(modeText);
		stackPanel3.Children.Add(new TextBlock
		{
			Text = "▾",
			FontSize = 11.0,
			Margin = new Thickness(6.0, 1.0, 0.0, 0.0),
			Foreground = Fog,
			VerticalAlignment = VerticalAlignment.Center
		});
		modeBtn = new System.Windows.Controls.Button
		{
			Style = St("Plain"),
			Content = stackPanel3,
			Margin = new Thickness(16.0, 2.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			ToolTip = "Режим: «Забив» — забить капт; «Списки» — список людей из Discord, TTC собирает его в форме атаки; «Забив ВЗЗ» — код с картинки в канале Discord"
		};
		modeBtn.Click += delegate
		{
			ShowModeMenu();
		};
		stackPanel.Children.Add(modeBtn);
		return true;
	}

	private void ShowModeMenu()
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.MinWidth = 320.0;
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(MenuButton(ModeItem("strike", "Забив", "забить капт ко времени или как только станет можно", (!vzzOn && !planOn && !prioOn && !rcfg.ListsMode) ? SignalB : Chalk), delegate
		{
			SetMode(lists: false);
		}));
		stackPanel2.Children.Add(MenuButton(ModeItem("lists", "Списки", "список людей из Discord: бот семьи присылает его, TTC добавляет людей в форму атаки поиском", (!vzzOn && !planOn && !prioOn && rcfg.ListsMode) ? SignalB : Chalk), delegate
		{
			SetMode(lists: true);
		}));
		if (vzBody != null)
		{
			stackPanel2.Children.Add(MenuButton(ModeItem("vzz", "Забив ВЗЗ", "код с картинки в канале Discord: TTC читает его и сама отправляет сообщение в канал", vzzOn ? SignalB : Chalk), delegate
			{
				ShowVzz(on: true);
			}));
		}
		if (planBody != null)
		{
			stackPanel2.Children.Add(MenuButton(ModeItem("plan", "План", "квадраты по очереди с ботом семьи: ждёт конца своего капта, перебивает, мало людей — слабая семья", planOn ? SignalB : Chalk), delegate
			{
				ShowPlan(on: true);
			}));
		}
		if (PriorityAllowed)
		{
			stackPanel2.Children.Add(MenuButton(ModeItem("priority", "Приоритет", "только у владельца: чья семья бьёт первой, остальные копии TTC ждут её забива", prioOn ? SignalB : Chalk), delegate
			{
				ShowPriority(show: true);
			}));
		}
		ShowPopup(modeBtn, stackPanel2);
	}

	private void SetMode(bool lists)
	{
		if (vzzOn)
		{
			ShowVzz(on: false);
		}
		if (planOn)
		{
			ShowPlan(on: false);
		}
		if (prioOn)
		{
			ShowPriority(show: false);
		}
		rcfg.ListsMode = lists;
		SaveRosterCfg();
		modeText.Text = (lists ? "Списки" : "Забив");
		Inputs.Visibility = (lists ? Visibility.Collapsed : Visibility.Visible);
		listPanel.Visibility = ((!lists) ? Visibility.Collapsed : Visibility.Visible);
		BtnStart.Visibility = (lists ? Visibility.Collapsed : Visibility.Visible);
		BtnTest.Visibility = (lists ? Visibility.Collapsed : Visibility.Visible);
		listGoBtn.Visibility = ((!lists) ? Visibility.Collapsed : Visibility.Visible);
	}

	private bool BuildListPanel()
	{
		Grid grid = Inputs.Parent as Grid;
		System.Windows.Controls.Panel panel = BtnStart.Parent as System.Windows.Controls.Panel;
		if (grid == null || panel == null)
		{
			return false;
		}
		listPanel = new StackPanel
		{
			Visibility = Visibility.Collapsed
		};
		Grid.SetColumn(listPanel, Grid.GetColumn(Inputs));
		grid.Children.Add(listPanel);
		listPanel.Children.Add(new TextBlock
		{
			Text = "Список из Discord",
			Style = St("Label")
		});
		rosterHead = new TextBlock
		{
			FontSize = 17.0,
			Foreground = Chalk,
			TextTrimming = TextTrimming.CharacterEllipsis,
			Margin = new Thickness(0.0, 0.0, 0.0, 2.0)
		};
		rosterSub = new TextBlock
		{
			Style = St("Hint"),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		};
		listPanel.Children.Add(rosterHead);
		listPanel.Children.Add(rosterSub);
		rosterRows = new StackPanel
		{
			Margin = new Thickness(10.0, 6.0, 10.0, 6.0)
		};
		Border border = new Border();
		border.Background = B("#101215");
		border.CornerRadius = new CornerRadius(6.0);
		border.Height = 236.0;
		border.Child = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = rosterRows
		};
		Border element = border;
		listPanel.Children.Add(element);
		rosterRun = new TextBlock
		{
			Style = St("Hint"),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0)
		};
		listPanel.Children.Add(rosterRun);
		listPanel.Children.Add(new TextBlock
		{
			Text = "Квадрат формы атаки",
			Style = St("Label"),
			Margin = new Thickness(0.0, 14.0, 0.0, 8.0)
		});
		Grid grid2 = new Grid();
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(150.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(16.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		listSquareBox = new System.Windows.Controls.TextBox
		{
			Style = St("Field"),
			CharacterCasing = System.Windows.Controls.CharacterCasing.Upper,
			MaxLength = 5,
			Text = (rcfg.Square ?? ""),
			ToolTip = "На какой квадрат открыть форму атаки, если она ещё не открыта. Пусто — собрать в уже открытой форме.\nБот может назвать квадрат сам."
		};
		listSquareBox.TextChanged += delegate
		{
			rcfg.Square = listSquareBox.Text.Trim().ToUpperInvariant();
			SaveRosterCfg();
		};
		grid2.Children.Add(listSquareBox);
		TextBlock textBlock = new TextBlock();
		textBlock.Style = St("Hint");
		textBlock.Margin = new Thickness(0.0);
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		textBlock.TextWrapping = TextWrapping.Wrap;
		textBlock.Text = "пусто — в уже открытой форме";
		TextBlock element2 = textBlock;
		Grid.SetColumn(element2, 2);
		grid2.Children.Add(element2);
		listPanel.Children.Add(grid2);
		armedBox = new System.Windows.Controls.CheckBox
		{
			Style = St("Switch"),
			Content = "Собирать по кнопке из Discord",
			IsChecked = rcfg.Armed,
			Margin = new Thickness(0.0, 16.0, 0.0, 2.0),
			ToolTip = "Бот семьи жмёт «Собрать в игре» — TTC выводит игру вперёд, открывает форму и добавляет людей поиском. Кнопку атаки не нажимает. F10 — стоп."
		};
		armedBox.Checked += delegate
		{
			SetArmed(on: true);
		};
		armedBox.Unchecked += delegate
		{
			SetArmed(on: false);
		};
		listPanel.Children.Add(armedBox);
		listPanel.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 0.0, 0.0, 0.0),
			TextWrapping = TextWrapping.Wrap,
			Text = "TTC сама откроет форму и добавит людей, когда бот попросит. Атаку не нажимает."
		});
		strikeBox = new System.Windows.Controls.CheckBox
		{
			Style = St("Switch"),
			Content = "При забиве брать людей из списка",
			IsChecked = rcfg.UseForStrike,
			Margin = new Thickness(0.0, 12.0, 0.0, 2.0),
			ToolTip = "Вместо «первых N сверху» при забиве каждого ищу по имени: в участники попадут те, кто записался в Discord"
		};
		strikeBox.Checked += delegate
		{
			rcfg.UseForStrike = true;
			SaveRosterCfg();
			ShowRoster(Roster.Last);
		};
		strikeBox.Unchecked += delegate
		{
			rcfg.UseForStrike = false;
			SaveRosterCfg();
			ShowRoster(Roster.Last);
		};
		listPanel.Children.Add(strikeBox);
		listPanel.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0),
			TextWrapping = TextWrapping.Wrap,
			Text = "Вместо «первых N сверху»: поиск по имени, около секунды на человека."
		});
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		StackPanel stackPanel2 = stackPanel;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Quick");
		button.Content = Icons.Label("external", "Токен для бота", 13.0);
		button.ToolTip = "Личный кабинет, вкладка «API для Discord»: токен для бота семьи";
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			AppPaths.OpenUrl("https://timetocapture.ru/cabinet?tab=api");
		};
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = St("Quick");
		button3.Content = Icons.Label("bot", "Как подключить бота", 13.0);
		button3.ToolTip = "Документация API и готовый бот для Discord";
		System.Windows.Controls.Button button4 = button3;
		button4.Click += delegate
		{
			AppPaths.OpenUrl("https://timetocapture.ru/api-docs");
		};
		stackPanel2.Children.Add(button2);
		stackPanel2.Children.Add(button4);
		listPanel.Children.Add(stackPanel2);
		listGoBtn = new System.Windows.Controls.Button
		{
			Style = St("Primary"),
			Height = 50.0,
			Width = 214.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
			Content = "Собрать в игре",
			Visibility = Visibility.Collapsed,
			ToolTip = "Добавить людей из списка в форму атаки поиском (открытую или на квадрате выше). Кнопку атаки не нажимаю."
		};
		listGoBtn.Click += delegate
		{
			StartList(rcfg.Square, 0L, squareFromBot: false);
		};
		panel.Children.Add(listGoBtn);
		strikeHint = new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			TextWrapping = TextWrapping.Wrap,
			Visibility = Visibility.Collapsed
		};
		Inputs.Children.Add(strikeHint);
		ShowRoster(null);
		return true;
	}

	private void SetArmed(bool on)
	{
		rcfg.Armed = on;
		Roster.Armed = on;
		SaveRosterCfg();
		Log(on ? "Сбор по кнопке из Discord включён: когда бот семьи попросит, TTC сама откроет форму атаки и добавит людей (атаку не нажимает)." : "Сбор по кнопке из Discord выключен.");
	}

	private static string Hm(long unix)
	{
		if (unix <= 0)
		{
			return "—";
		}
		return Clock.Msk(War.FromUnix(unix)).ToString("HH:mm");
	}

	private void ShowRoster(RosterInfo r)
	{
		if (rosterRows == null)
		{
			return;
		}
		List<RosterResult> lastResults = Roster.LastResults;
		Dictionary<string, RosterResult> dictionary = new Dictionary<string, RosterResult>();
		if (lastResults != null)
		{
			foreach (RosterResult item in lastResults)
			{
				dictionary[FormRows.Key(item.Entry.Name)] = item;
			}
		}
		int num = r?.Entries.Count ?? 0;
		if (r == null)
		{
			rosterHead.Text = ((rosterError != null) ? "Нет связи с сайтом" : "Загружаю…");
			rosterSub.Text = rosterError ?? "";
		}
		else if (!r.Linked)
		{
			rosterHead.Text = "Бот семьи не подключён";
			rosterSub.Text = "Создайте токен в личном кабинете (вкладка «API для Discord») и вставьте его в бота семьи. Работает, пока ключ действует.";
		}
		else
		{
			rosterHead.Text = ((num == 0) ? "Список пуст" : ((string.IsNullOrEmpty(r.Title) ? "" : ("«" + r.Title + "», ")) + num + " " + People(num)));
			rosterSub.Text = ((num == 0) ? "Участники записываются кнопкой в Discord." : ("Обновлён в " + Hm(r.UpdatedAt) + " МСК" + ((r.ExpiresAt > 0) ? (", действует до " + Hm(r.ExpiresAt)) : "") + "."));
		}
		rosterRows.Children.Clear();
		for (int i = 0; i < num; i++)
		{
			RosterEntry rosterEntry = r.Entries[i];
			Grid grid = new Grid();
			grid.Margin = new Thickness(0.0, 3.0, 0.0, 3.0);
			Grid grid2 = grid;
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(26.0)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = GridLength.Auto
			});
			grid2.Children.Add(new TextBlock
			{
				Text = (i + 1).ToString(),
				Foreground = Dim,
				FontSize = 13.0
			});
			TextBlock textBlock = new TextBlock();
			textBlock.Text = rosterEntry.Name + ((rosterEntry.Uid > 0) ? ("  #" + rosterEntry.Uid) : "");
			textBlock.Foreground = Chalk;
			textBlock.FontSize = 13.5;
			textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			TextBlock element = textBlock;
			Grid.SetColumn(element, 1);
			grid2.Children.Add(element);
			if (dictionary.TryGetValue(FormRows.Key(rosterEntry.Name), out var value))
			{
				bool flag = value.Status == "added";
				TextBlock textBlock2 = new TextBlock();
				textBlock2.Text = (flag ? "✓ " : "✗ ") + Roster.StatusText(value.Status) + ((flag && value.Online == 0) ? ", не в игре" : "");
				textBlock2.Foreground = ((!flag) ? StopB : ((value.Online == 0) ? WarnB : GoB));
				textBlock2.FontSize = 12.5;
				textBlock2.Margin = new Thickness(8.0, 0.0, 0.0, 0.0);
				textBlock2.ToolTip = value.Note;
				TextBlock element2 = textBlock2;
				Grid.SetColumn(element2, 2);
				grid2.Children.Add(element2);
			}
			rosterRows.Children.Add(grid2);
		}
		if (num == 0)
		{
			rosterRows.Children.Add(new TextBlock
			{
				Text = "Здесь появятся люди, записавшиеся в Discord.",
				Foreground = Dim,
				FontSize = 13.0
			});
		}
		if (lastResults != null && Roster.LastResultsAt > DateTime.MinValue)
		{
			int num2 = lastResults.Count((RosterResult x) => x.Status == "added");
			rosterRun.Text = "Последний сбор в " + Clock.Msk(Roster.LastResultsAt).ToString("HH:mm") + ": добавлено " + num2 + " из " + lastResults.Count + (lastResults.Any((RosterResult x) => x.Status == "added" && x.Online == 0) ? ", часть не в игре" : "") + ".";
		}
		else
		{
			rosterRun.Text = "";
		}
		bool useForStrike = rcfg.UseForStrike;
		strikeHint.Visibility = ((!useForStrike) ? Visibility.Collapsed : Visibility.Visible);
		strikeHint.Text = ((num > 0) ? ("Вместо этого — список Discord: " + num + " " + People(num) + ".") : "Список Discord пуст — возьму первых сверху.");
		strikeHint.ToolTip = "Включено в режиме «Списки»: «При забиве брать людей из списка»";
	}

	private static string People(int n)
	{
		int num = n % 100;
		int num2 = n % 10;
		if (num < 11 || num > 14)
		{
			switch (num2)
			{
			default:
				return "человек";
			case 2:
			case 3:
			case 4:
				return "человека";
			case 1:
				return "человек";
			}
		}
		return "человек";
	}

	private void RosterLoop()
	{
		long ver = -1L;
		long fillId = 0L;
		int num = 0;
		bool flag = true;
		while (!closing)
		{
			if (string.IsNullOrEmpty(License.SavedKey()))
			{
				Thread.Sleep(10000);
				continue;
			}
			RosterInfo last = Roster.Last;
			int wait = ((!flag && (last == null || last.Linked)) ? 25 : 0);
			RosterFill fill;
			string error;
			RosterInfo r = Roster.Pull(ver, fillId, wait, out fill, out error);
			if (r == null)
			{
				num++;
				rosterError = "Список не получить: " + error;
				UiDo(delegate
				{
					ShowRoster(Roster.Last);
				});
				Thread.Sleep(Math.Min(60, 5 * num) * 1000);
				continue;
			}
			num = 0;
			flag = false;
			rosterError = null;
			ver = r.Ver;
			if (fill != null)
			{
				fillId = fill.Id;
				RosterFill f = fill;
				UiDo(delegate
				{
					OnBotFill(f);
				});
			}
			UiDo(delegate
			{
				ShowRoster(r);
			});
			if (!r.Linked)
			{
				Thread.Sleep(60000);
			}
		}
	}

	private void UiDo(Action a)
	{
		try
		{
			if (!closing)
			{
				w.Dispatcher.BeginInvoke(a);
			}
		}
		catch
		{
		}
	}

	private void OnBotFill(RosterFill f)
	{
		long ver = Roster.Last?.Ver ?? 0;
		string text = (string.IsNullOrEmpty(f.Square) ? "" : (" на " + f.Square));
		if (!rcfg.Armed)
		{
			Log("Бот семьи просит собрать список в игре" + text + ", но «Собирать по кнопке из Discord» выключено (режим «Списки»).");
			Roster.Report(f.Id, ver, "failed", f.Square, null, "в TTC выключено «Собирать по кнопке из Discord»");
		}
		else if (running)
		{
			Log("Бот семьи просит собрать список" + text + ", но сейчас идёт забив или калибровка.");
			Roster.Report(f.Id, ver, "failed", f.Square, null, "TTC занята: идёт забив или калибровка");
		}
		else if (!License.UsableNow())
		{
			Roster.Report(f.Id, ver, "failed", f.Square, null, "ключ в TTC не действует");
		}
		else
		{
			Log("Бот семьи просит собрать список в игре" + text + ".");
			bool flag = !string.IsNullOrEmpty(f.Square);
			StartList(flag ? f.Square : rcfg.Square, f.Id, flag);
		}
	}

	private void StartList(string square, long fillId, bool squareFromBot)
	{
		RosterInfo last = Roster.Last;
		if (running)
		{
			Log("Сейчас идёт забив или калибровка — соберу список после неё.");
		}
		else
		{
			if (fillId == 0 && !LicenseGate())
			{
				return;
			}
			if (last == null || last.Entries.Count == 0)
			{
				Log("Список из Discord пуст — собирать некого.");
				if (fillId > 0)
				{
					Roster.Report(fillId, last?.Ver ?? 0, "failed", square, null, "список пуст");
				}
				return;
			}
			square = (square ?? "").Trim().ToUpperInvariant();
			if (square.Length > 0 && !Runner.ParseSquare(square, out var _, out var _))
			{
				Log("Неверный квадрат «" + square + "».");
				if (fillId > 0)
				{
					Roster.Report(fillId, last.Ver, "failed", square, null, "неверный квадрат " + square);
				}
				return;
			}
			Job j = new Job
			{
				ListOnly = true,
				Roster = last,
				RosterFillId = fillId,
				SquareFromBot = squareFromBot,
				Square = square,
				DryRun = true,
				Family = cfg.Family,
				People = last.Entries.Count
			};
			dryRun = true;
			fireUtc = DateTime.MinValue;
			startUtc = Clock.UtcNow();
			runner = new Runner();
			runner.ShotDir = null;
			runner.Log = delegate(string m)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					Log(m);
				});
			};
			runner.Status = delegate(string k, string m)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					SetStatus(k, m);
				});
			};
			runner.StepUpdate = delegate(int i, StepState st, string d)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					SetStep(i, st, d);
				});
			};
			runner.PingUpdate = delegate(int r, int f, double l)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					OnPing(r, f, l);
				});
			};
			runner.WantFamily = familyWanted;
			runner.FamilyUpdate = delegate(FamilyInfo fi)
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					OnFamily(fi);
				});
			};
			Runner rn = runner;
			Telemetry.Begin("list", square, last.Entries.Count);
			running = true;
			Lock(r: true);
			ShowTab(log: true);
			SetStatus("test", "");
			Thread thread = new Thread((ThreadStart)delegate
			{
				try
				{
					rn.Run(j);
				}
				catch (Exception ex2)
				{
					Exception ex = ex2;
					w.Dispatcher.BeginInvoke((Action)delegate
					{
						Log("Ошибка: " + ex.Message);
					});
				}
				finally
				{
					running = false;
					try
					{
						w.Dispatcher.BeginInvoke((Action)delegate
						{
							Lock(r: false);
							ShowRoster(Roster.Last);
						});
					}
					catch
					{
					}
				}
			});
			thread.IsBackground = true;
			thread.Start();
		}
	}

	private void RosterForStrike(Job j)
	{
		if (!rcfg.UseForStrike || j.Prepare)
		{
			return;
		}
		RosterInfo last = Roster.Last;
		if (last == null || last.Entries.Count == 0)
		{
			Log("Список из Discord пуст — людей беру первых " + j.People + " сверху.");
			return;
		}
		j.Roster = last;
		int num = 12 + last.Entries.Count;
		if (!cfg.AfterCapt && j.PrepSec < num)
		{
			j.PrepSec = num;
			Log("Людей по списку из Discord: " + last.Entries.Count + " — начну готовить форму за " + num + " с (поиск около секунды на человека).");
		}
		else
		{
			Log("Людей по списку из Discord: " + last.Entries.Count + ".");
		}
	}

	private void LoadVzzCfg()
	{
		try
		{
			if (File.Exists(VzzCfgPath))
			{
				vcfg = json.Deserialize<VzzCfg>(File.ReadAllText(VzzCfgPath)) ?? new VzzCfg();
			}
			else
			{
				ImportVzzCfg();
			}
		}
		catch
		{
			vcfg = new VzzCfg();
		}
		vcfg.Color = VzzColors.Canon(vcfg.Color ?? "") ?? "Случайный";
		if (string.IsNullOrEmpty(vcfg.Line1) && !string.IsNullOrEmpty(cfg.Family))
		{
			vcfg.Line1 = cfg.Family;
		}
	}

	private void ImportVzzCfg()
	{
		vcfg = new VzzCfg();
		string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiscordImageReader\\settings.json");
		if (File.Exists(path) && json.DeserializeObject(File.ReadAllText(path)) is Dictionary<string, object> dictionary)
		{
			if (dictionary.TryGetValue("channel", out var value) && value is string)
			{
				vcfg.Channel = (string)value;
			}
			if (dictionary.TryGetValue("line1", out value) && value is string)
			{
				vcfg.Line1 = (string)value;
			}
			if (dictionary.TryGetValue("color", out value) && value is string)
			{
				vcfg.Color = (string)value;
			}
			if (dictionary.TryGetValue("copy", out value) && value is bool)
			{
				vcfg.Copy = (bool)value;
			}
			if (dictionary.TryGetValue("send", out value) && value is bool)
			{
				vcfg.Send = (bool)value;
			}
			if (dictionary.TryGetValue("check", out value) && value is bool)
			{
				vcfg.Check = (bool)value;
			}
			if (dictionary.TryGetValue("sound", out value) && value is bool)
			{
				vcfg.Sound = (bool)value;
			}
		}
	}

	private void SaveVzzCfg()
	{
		if (demo)
		{
			return;
		}
		try
		{
			File.WriteAllText(VzzCfgPath, json.Serialize(vcfg));
		}
		catch
		{
		}
	}

	private VzzSettings VzSettings()
	{
		VzzSettings vzzSettings = new VzzSettings();
		vzzSettings.Line1 = (vcfg.Line1 ?? "").Trim();
		vzzSettings.Color = vcfg.Color;
		vzzSettings.AutoSend = vcfg.Send;
		vzzSettings.Check = vcfg.Check;
		return vzzSettings;
	}

	private void VzEdited()
	{
		RefreshVzPreview();
		if (vzWatcher != null)
		{
			vzWatcher.Settings = VzSettings();
		}
		vzSaver.Stop();
		vzSaver.Start();
	}

	private void InitVzz()
	{
		mainBody = Inputs.Parent as Grid;
		mainBar = F<System.Windows.Controls.Button>("GameBtn").Parent as System.Windows.Controls.Panel;
		mainButtons = BtnStart.Parent as System.Windows.Controls.Panel;
		Grid grid = ((mainButtons != null) ? (mainButtons.Parent as Grid) : null);
		if (mainBody == null || mainBar == null || grid == null || modeBtn == null)
		{
			return;
		}
		LoadVzzCfg();
		vzSaver = new DispatcherTimer
		{
			Interval = TimeSpan.FromMilliseconds(400.0)
		};
		vzSaver.Tick += delegate
		{
			vzSaver.Stop();
			SaveVzzCfg();
		};
		BuildVzBody();
		BuildVzBar(grid);
		StackPanel stackPanel = modeBtn.Content as StackPanel;
		vzModeDot = new System.Windows.Shapes.Rectangle
		{
			Width = 7.0,
			Height = 7.0,
			RadiusX = 1.5,
			RadiusY = 1.5,
			Fill = SignalB,
			Margin = new Thickness(0.0, 1.0, 8.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center,
			Visibility = Visibility.Collapsed
		};
		stackPanel?.Children.Insert(0, vzModeDot);
		w.Closing += delegate(object s, CancelEventArgs e)
		{
			if (!e.Cancel)
			{
				SaveVzzCfg();
				StopVzz(wait: true);
			}
		};
		RefreshVzPreview();
		ShowVzCode("", animate: false);
		SetVzMeta("Здесь появится код с последней картинки в канале.", Fog);
		SetVzState("off", VzIdleText());
		if (vcfg.Active)
		{
			ShowVzz(on: true);
		}
	}

	private void ShowVzz(bool on)
	{
		if (vzBody != null)
		{
			if (on && planOn)
			{
				ShowPlan(on: false);
			}
			if (on && prioOn)
			{
				ShowPriority(show: false);
			}
			vzzOn = on;
			mainBody.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
			mainBar.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
			mainButtons.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
			Grid grid = vzBody;
			System.Windows.Controls.Panel panel = vzBar;
			Visibility visibility2 = (vzButtons.Visibility = ((!on) ? Visibility.Collapsed : Visibility.Visible));
			Visibility visibility4 = (panel.Visibility = visibility2);
			grid.Visibility = visibility4;
			if (on)
			{
				modeText.Text = "Забив ВЗЗ";
			}
			if (vcfg.Active != on)
			{
				vcfg.Active = on;
				SaveVzzCfg();
			}
			UpdateVzModeDot();
		}
	}

	private void UpdateVzModeDot()
	{
		if (vzModeDot != null)
		{
			vzModeDot.Visibility = ((!vzRunning || vzzOn) ? Visibility.Collapsed : Visibility.Visible);
			vzModeDot.ToolTip = (vzRunning ? "Забив ВЗЗ включён: TTC следит за каналом Discord" : null);
		}
	}

	private void BuildVzBody()
	{
		vzBody = new Grid
		{
			Margin = mainBody.Margin,
			Visibility = Visibility.Collapsed
		};
		Grid.SetRow(vzBody, Grid.GetRow(mainBody));
		vzBody.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(372.0)
		});
		vzBody.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(40.0)
		});
		vzBody.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		((System.Windows.Controls.Panel)mainBody.Parent).Children.Add(vzBody);
		StackPanel stackPanel = new StackPanel();
		vzBody.Children.Add(stackPanel);
		stackPanel.Children.Add(new TextBlock
		{
			Text = "Сообщение в канал",
			Style = St("Label")
		});
		StackPanel stackPanel2 = new StackPanel();
		vzPrevLine1 = new TextBlock
		{
			FontSize = 18.0,
			Foreground = Chalk,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel3.Margin = new Thickness(0.0, 5.0, 0.0, 0.0);
		StackPanel stackPanel4 = stackPanel3;
		vzPrevSwatch = new Border
		{
			Width = 12.0,
			Height = 12.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		vzPrevColor = new TextBlock
		{
			FontSize = 14.5,
			Foreground = Chalk,
			Margin = new Thickness(9.0, 0.0, 0.0, 0.0),
			VerticalAlignment = VerticalAlignment.Center
		};
		stackPanel4.Children.Add(vzPrevSwatch);
		stackPanel4.Children.Add(vzPrevColor);
		vzPrevCode = new TextBlock
		{
			Style = St("Num"),
			FontSize = 26.0,
			Margin = new Thickness(0.0, 4.0, 0.0, 0.0)
		};
		stackPanel2.Children.Add(vzPrevLine1);
		stackPanel2.Children.Add(stackPanel4);
		stackPanel2.Children.Add(vzPrevCode);
		stackPanel.Children.Add(new Border
		{
			Background = WellB,
			CornerRadius = new CornerRadius(6.0),
			Padding = new Thickness(18.0, 14.0, 18.0, 12.0),
			Child = stackPanel2,
			MinHeight = 118.0
		});
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 16.0, 0.0, 0.0);
		Grid grid2 = grid;
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(14.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(166.0)
		});
		StackPanel stackPanel5 = new StackPanel();
		stackPanel5.Children.Add(new TextBlock
		{
			Text = "Первая строка",
			Style = St("Label")
		});
		vzLine1 = VzField(17.0, 44.0, strong: true);
		vzLine1.MaxLength = 60;
		vzLine1.Text = vcfg.Line1 ?? "";
		stackPanel5.Children.Add(WithHint(vzLine1, "семья", out vzLine1Hint));
		vzLine1.TextChanged += delegate
		{
			vcfg.Line1 = vzLine1.Text;
			vzLine1Hint.Visibility = ((vzLine1.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			VzEdited();
		};
		grid2.Children.Add(stackPanel5);
		StackPanel stackPanel6 = new StackPanel();
		Grid.SetColumn(stackPanel6, 2);
		stackPanel6.Children.Add(new TextBlock
		{
			Text = "Цвет",
			Style = St("Label")
		});
		Grid grid3 = new Grid();
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid3.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = GridLength.Auto
		});
		vzColorSwatch = new Border
		{
			Width = 14.0,
			Height = 14.0,
			VerticalAlignment = VerticalAlignment.Center
		};
		vzColorText = new TextBlock
		{
			FontSize = 15.0,
			Foreground = Chalk,
			Margin = new Thickness(10.0, 0.0, 6.0, 1.0),
			VerticalAlignment = VerticalAlignment.Center,
			TextTrimming = TextTrimming.CharacterEllipsis
		};
		FrameworkElement element = Icons.Make("chevron-down", 12.0, Fog);
		Grid.SetColumn(vzColorText, 1);
		Grid.SetColumn(element, 2);
		grid3.Children.Add(vzColorSwatch);
		grid3.Children.Add(vzColorText);
		grid3.Children.Add(element);
		vzColorBtn = new System.Windows.Controls.Button
		{
			Style = St("Plain"),
			HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch,
			Content = new Border
			{
				Background = Slate,
				BorderBrush = RuleB,
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(8.0),
				Height = 44.0,
				Padding = new Thickness(13.0, 0.0, 13.0, 0.0),
				Child = grid3
			},
			ToolTip = "Вторая строка сообщения. «Случайный» — новый цвет для каждого кода"
		};
		vzColorBtn.Click += delegate
		{
			ShowVzColors();
		};
		stackPanel6.Children.Add(vzColorBtn);
		grid2.Children.Add(stackPanel6);
		stackPanel.Children.Add(grid2);
		vzSend = VzSwitch("Отправлять в канал сам", vcfg.Send, new Thickness(0.0, 22.0, 0.0, 2.0), "Только для картинок, пришедших после «Включить» и не старше " + 60 + " секунд.\nЕсли чат закрыт для записи, сообщение уйдёт, как только его откроют.\nЕсли чат откроют, а картинки с кодом не было, уйдут первые две строки.");
		vzSend.Checked += delegate
		{
			vcfg.Send = true;
			vzCheck.IsEnabled = true;
			VzEdited();
		};
		vzSend.Unchecked += delegate
		{
			vcfg.Send = false;
			vzCheck.IsEnabled = false;
			VzEdited();
		};
		stackPanel.Children.Add(vzSend);
		stackPanel.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(46.0, 2.0, 0.0, 0.0),
			Text = "Как только код прочитан: клик по полю, вставка и Enter. Закрытый чат — в миг, когда откроют."
		});
		vzCheck = VzSwitch("Сверять код с ответами и исправлять", vcfg.Check, new Thickness(0.0, 14.0, 0.0, 2.0), "Если после нашего сообщения хотя бы двое написали один и тот же другой код,\nнаше удаляется и отправляется заново с их кодом");
		vzCheck.IsEnabled = vcfg.Send;
		vzCheck.Checked += delegate
		{
			vcfg.Check = true;
			VzEdited();
		};
		vzCheck.Unchecked += delegate
		{
			vcfg.Check = false;
			VzEdited();
		};
		stackPanel.Children.Add(vzCheck);
		stackPanel.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(46.0, 2.0, 0.0, 0.0),
			Text = "Двое и больше после нас написали другой одинаковый код — удалю наше и отправлю с их кодом."
		});
		Grid grid4 = new Grid();
		grid4.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		Grid grid5 = grid4;
		grid5.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		grid5.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		vzCopy = VzSwitch("Копировать сразу", vcfg.Copy, new Thickness(0.0), "Сообщение с новым кодом сразу в буфере обмена — вставить в Discord самому");
		vzCopy.Checked += delegate
		{
			vcfg.Copy = true;
			VzEdited();
		};
		vzCopy.Unchecked += delegate
		{
			vcfg.Copy = false;
			VzEdited();
		};
		vzSound = VzSwitch("Звук и мигание", vcfg.Sound, new Thickness(0.0), "Звук Windows и мигание TTC на панели задач, когда пришёл код или открыли чат");
		vzSound.Checked += delegate
		{
			vcfg.Sound = true;
			VzEdited();
		};
		vzSound.Unchecked += delegate
		{
			vcfg.Sound = false;
			VzEdited();
		};
		Grid.SetColumn(vzSound, 1);
		grid5.Children.Add(vzCopy);
		grid5.Children.Add(vzSound);
		stackPanel.Children.Add(grid5);
		stackPanel.Children.Add(new TextBlock
		{
			Text = "Канал Discord",
			Style = St("Label"),
			Margin = new Thickness(0.0, 22.0, 0.0, 8.0)
		});
		vzChannel = VzField(13.5, 40.0, strong: false);
		vzChannel.Text = vcfg.Channel ?? "";
		stackPanel.Children.Add(WithHint(vzChannel, "https://discord.com/channels/…", out vzChannelHint));
		vzChannel.TextChanged += delegate
		{
			vcfg.Channel = vzChannel.Text.Trim();
			vzChannelHint.Visibility = ((vzChannel.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			vzSaver.Stop();
			vzSaver.Start();
			if (!vzRunning)
			{
				SetVzState("off", VzIdleText());
			}
		};
		vzChannel.KeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Return)
			{
				ToggleVzz();
			}
		};
		vzDetail = new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			MinHeight = 34.0
		};
		stackPanel.Children.Add(vzDetail);
		Grid grid6 = new Grid();
		Grid.SetColumn(grid6, 2);
		vzBody.Children.Add(grid6);
		for (int i = 0; i < 5; i++)
		{
			grid6.RowDefinitions.Add(new RowDefinition
			{
				Height = ((i == 4) ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto)
			});
		}
		Grid grid7 = new Grid();
		grid7.Children.Add(new TextBlock
		{
			Text = "Код с последней картинки",
			Style = St("Label"),
			Margin = new Thickness(0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		StackPanel stackPanel7 = new StackPanel();
		stackPanel7.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel7.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		stackPanel7.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel8 = stackPanel7;
		vzStatusDot = new System.Windows.Shapes.Rectangle
		{
			Width = 8.0,
			Height = 8.0,
			RadiusX = 1.5,
			RadiusY = 1.5,
			Fill = Fog,
			VerticalAlignment = VerticalAlignment.Center
		};
		vzStatusText = new TextBlock
		{
			FontSize = 13.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			Foreground = Chalk
		};
		stackPanel8.Children.Add(vzStatusDot);
		stackPanel8.Children.Add(vzStatusText);
		grid7.Children.Add(stackPanel8);
		grid6.Children.Add(grid7);
		vzCells = new StackPanel
		{
			Orientation = System.Windows.Controls.Orientation.Horizontal,
			Height = 118.0
		};
		Border border = new Border();
		border.Background = System.Windows.Media.Brushes.Transparent;
		border.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		border.Child = vzCells;
		border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		Border border2 = border;
		border2.MouseLeftButtonUp += delegate
		{
			CopyVzMessage(manual: true);
		};
		Grid.SetRow(border2, 1);
		grid6.Children.Add(border2);
		vzMeta = new TextBlock
		{
			Foreground = Fog,
			FontSize = 14.0,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(0.0, 14.0, 0.0, 0.0),
			MinHeight = 38.0,
			LineHeight = 19.0
		};
		Grid.SetRow(vzMeta, 2);
		grid6.Children.Add(vzMeta);
		Grid grid8 = new Grid();
		grid8.Margin = new Thickness(0.0, 22.0, 0.0, 0.0);
		Grid grid9 = grid8;
		for (int j = 0; j < 3; j++)
		{
			grid9.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
		}
		grid9.Children.Add(VzStat(0, "После картинки", "с", "наше сообщение в канале, по часам Discord", out vzAfter, out vzAfterSub));
		grid9.Children.Add(VzStat(1, "Место в канале", "", "каким по счёту ответили кодом", out vzPlace, out vzPlaceSub));
		grid9.Children.Add(VzStat(2, "Распознано", "%", "уверенность сети на сайте TTC", out vzSure, out vzSureSub));
		Grid.SetRow(grid9, 3);
		grid6.Children.Add(grid9);
		Grid grid10 = new Grid();
		grid10.Margin = new Thickness(0.0, 22.0, 0.0, 0.0);
		Grid grid11 = grid10;
		grid11.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid11.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid11.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Height = 1.0,
			Fill = RuleB,
			VerticalAlignment = VerticalAlignment.Top
		});
		Grid grid12 = new Grid();
		grid12.Margin = new Thickness(0.0, 14.0, 0.0, 10.0);
		Grid grid13 = grid12;
		grid13.Children.Add(new TextBlock
		{
			Text = "Ход",
			Style = St("Label"),
			Margin = new Thickness(0.0)
		});
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Plain");
		button.Content = new TextBlock
		{
			Text = "Очистить",
			FontSize = 13.0,
			Foreground = Fog
		};
		button.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		System.Windows.Controls.Button button2 = button;
		button2.Click += delegate
		{
			vzLog.Children.Clear();
			vzLog.Children.Add(vzLogEmpty);
		};
		grid13.Children.Add(button2);
		grid11.Children.Add(grid13);
		vzLog = new StackPanel();
		vzLogEmpty = new TextBlock
		{
			Style = St("Hint"),
			Foreground = Dim,
			Text = "Здесь будет всё, что TTC делает в канале: картинки, коды, отправка и сверка с ответами."
		};
		vzLog.Children.Add(vzLogEmpty);
		vzLogScroll = new ScrollViewer
		{
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
			Content = vzLog
		};
		Grid.SetRow(vzLogScroll, 1);
		grid11.Children.Add(vzLogScroll);
		Grid.SetRow(grid11, 4);
		grid6.Children.Add(grid11);
	}

	private UIElement VzStat(int col, string cap, string unit, string sub, out TextBlock value, out TextBlock subText)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(0.0, 0.0, 18.0, 0.0);
		StackPanel stackPanel2 = stackPanel;
		Grid.SetColumn(stackPanel2, col);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = cap,
			Style = St("Label")
		});
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
		StackPanel stackPanel4 = stackPanel3;
		value = new TextBlock
		{
			Style = St("Num"),
			FontSize = 40.0,
			Text = "—",
			Foreground = Dim
		};
		stackPanel4.Children.Add(value);
		if (unit.Length > 0)
		{
			stackPanel4.Children.Add(new TextBlock
			{
				Text = unit,
				Foreground = Fog,
				FontSize = 14.0,
				VerticalAlignment = VerticalAlignment.Bottom,
				Margin = new Thickness(6.0, 0.0, 0.0, 9.0)
			});
		}
		stackPanel2.Children.Add(stackPanel4);
		subText = new TextBlock
		{
			Style = St("Hint"),
			FontSize = 12.0,
			Text = sub
		};
		stackPanel2.Children.Add(subText);
		return stackPanel2;
	}

	private System.Windows.Controls.TextBox VzField(double size, double height, bool strong)
	{
		System.Windows.Controls.TextBox textBox = new System.Windows.Controls.TextBox();
		textBox.Style = St("Field");
		textBox.FontSize = size;
		textBox.Height = height;
		System.Windows.Controls.TextBox textBox2 = textBox;
		if (!strong)
		{
			textBox2.FontStretch = FontStretches.Normal;
			textBox2.FontWeight = FontWeights.Normal;
			textBox2.Padding = new Thickness(11.0, 2.0, 11.0, 2.0);
		}
		return textBox2;
	}

	private static UIElement WithHint(System.Windows.Controls.TextBox box, string hint, out TextBlock h)
	{
		Grid grid = new Grid();
		grid.Children.Add(box);
		h = new TextBlock
		{
			Text = hint,
			Foreground = Dim,
			FontSize = Math.Min(15.0, box.FontSize),
			IsHitTestVisible = false,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(box.Padding.Left + 3.0, 0.0, 0.0, 0.0),
			Visibility = ((box.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible)
		};
		grid.Children.Add(h);
		return grid;
	}

	private System.Windows.Controls.CheckBox VzSwitch(string text, bool on, Thickness margin, string tip)
	{
		System.Windows.Controls.CheckBox checkBox = new System.Windows.Controls.CheckBox();
		checkBox.Style = St("Switch");
		checkBox.Content = text;
		checkBox.IsChecked = on;
		checkBox.Margin = margin;
		checkBox.ToolTip = tip;
		return checkBox;
	}

	private void BuildVzBar(Grid bottom)
	{
		WrapPanel wrapPanel = new WrapPanel();
		wrapPanel.VerticalAlignment = VerticalAlignment.Center;
		wrapPanel.Visibility = Visibility.Collapsed;
		WrapPanel wrapPanel2 = wrapPanel;
		Grid.SetColumn(wrapPanel2, Grid.GetColumn(mainBar));
		wrapPanel2.Children.Add(VzReady("Браузер", out vzBrowserDot, out vzBrowserText));
		wrapPanel2.Children.Add(VzReady("Вход в Discord", out vzLoginDot, out vzLoginText));
		wrapPanel2.Children.Add(VzReady("Чат", out vzChatDot, out vzChatText));
		wrapPanel2.Children.Add(VzReady("Распознавание", out vzOcrDot, out vzOcrText));
		vzBar = wrapPanel2;
		bottom.Children.Add(wrapPanel2);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Visibility = Visibility.Collapsed;
		StackPanel stackPanel2 = stackPanel;
		Grid.SetColumn(stackPanel2, Grid.GetColumn(mainButtons));
		vzCopyBtn = new System.Windows.Controls.Button
		{
			Style = St("Btn"),
			Height = 50.0,
			Width = 168.0,
			IsEnabled = false,
			ToolTip = "Сообщение с последним кодом — в буфер обмена (Ctrl+V в Discord)"
		};
		Icons.Set(vzCopyBtn, "copy", "Скопировать");
		vzCopyBtn.Click += delegate
		{
			CopyVzMessage(manual: true);
		};
		vzToggle = new System.Windows.Controls.Button
		{
			Style = St("Primary"),
			Height = 50.0,
			Width = 214.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
		};
		Icons.Set(vzToggle, "power", "Включить");
		vzToggle.Click += delegate
		{
			ToggleVzz();
		};
		stackPanel2.Children.Add(vzCopyBtn);
		stackPanel2.Children.Add(vzToggle);
		vzButtons = stackPanel2;
		bottom.Children.Add(stackPanel2);
	}

	private static UIElement VzReady(string text, out Shape dot, out TextBlock label)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.Margin = new Thickness(0.0, 4.0, 22.0, 4.0);
		StackPanel stackPanel2 = stackPanel;
		dot = new System.Windows.Shapes.Rectangle
		{
			Width = 8.0,
			Height = 8.0,
			RadiusX = 1.5,
			RadiusY = 1.5,
			Fill = Dim,
			VerticalAlignment = VerticalAlignment.Center
		};
		label = new TextBlock
		{
			Text = text,
			FontSize = 13.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			Foreground = Chalk
		};
		stackPanel2.Children.Add(dot);
		stackPanel2.Children.Add(label);
		return stackPanel2;
	}

	private static void SetReady(Shape dot, TextBlock label, System.Windows.Media.Brush color, string text)
	{
		dot.Fill = color;
		label.Text = text;
	}

	private static Border Swatch(string name, double size)
	{
		string text = VzzColors.Hex(name);
		Border border = new Border();
		border.Width = size;
		border.Height = size;
		border.CornerRadius = new CornerRadius(2.0);
		border.BorderBrush = B("#3A3F48");
		border.BorderThickness = new Thickness(1.0);
		border.VerticalAlignment = VerticalAlignment.Center;
		Border border2 = border;
		if (text != null)
		{
			border2.Background = B(text);
			return border2;
		}
		UniformGrid uniformGrid = new UniformGrid();
		uniformGrid.Rows = 2;
		uniformGrid.Columns = 2;
		UniformGrid uniformGrid2 = uniformGrid;
		string[] array = new string[4] { "#E53935", "#FFD400", "#1E4FD8", "#2E9E44" };
		foreach (string hex in array)
		{
			uniformGrid2.Children.Add(new System.Windows.Shapes.Rectangle
			{
				Fill = B(hex)
			});
		}
		border2.Child = uniformGrid2;
		border2.ClipToBounds = true;
		return border2;
	}

	private static void PaintSwatch(Border target, string name)
	{
		Border border = Swatch(name, target.Width);
		UIElement child = border.Child;
		border.Child = null;
		target.Background = border.Background;
		target.Child = child;
		target.CornerRadius = border.CornerRadius;
		target.BorderBrush = border.BorderBrush;
		target.BorderThickness = border.BorderThickness;
		target.ClipToBounds = true;
	}

	private void ShowVzColors()
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Width = 270.0;
		StackPanel stackPanel2 = stackPanel;
		System.Windows.Controls.TextBox search = new System.Windows.Controls.TextBox
		{
			Style = St("Field"),
			FontSize = 14.0,
			Height = 36.0,
			FontStretch = FontStretches.Normal,
			FontWeight = FontWeights.Normal,
			Padding = new Thickness(10.0, 2.0, 10.0, 2.0)
		};
		TextBlock hint;
		UIElement uIElement = WithHint(search, "Найти цвет", out hint);
		((FrameworkElement)uIElement).Margin = new Thickness(6.0, 6.0, 6.0, 6.0);
		stackPanel2.Children.Add(uIElement);
		StackPanel items = new StackPanel();
		stackPanel2.Children.Add(new ScrollViewer
		{
			Content = items,
			MaxHeight = 330.0,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto
		});
		List<string> shown = new List<string>();
		Action fill = delegate
		{
			string text = search.Text.Trim();
			items.Children.Clear();
			shown.Clear();
			IEnumerable<string> enumerable = new string[1] { "Случайный" }.Concat(VzzColors.All.Select((string[] c) => c[0]));
			foreach (string item in enumerable)
			{
				if (text.Length <= 0 || item.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					string pick = item;
					shown.Add(item);
					StackPanel stackPanel3 = new StackPanel
					{
						Orientation = System.Windows.Controls.Orientation.Horizontal
					};
					stackPanel3.Children.Add(Swatch(item, 14.0));
					stackPanel3.Children.Add(new TextBlock
					{
						Text = ((item == "Случайный") ? "Случайный, новый для каждого кода" : item),
						FontSize = 13.5,
						Margin = new Thickness(10.0, 0.0, 0.0, 0.0),
						Foreground = ((item == vcfg.Color) ? SignalB : Chalk),
						FontWeight = ((item == vcfg.Color) ? FontWeights.SemiBold : FontWeights.Normal)
					});
					items.Children.Add(MenuButton(stackPanel3, delegate
					{
						SetVzColor(pick);
					}));
				}
			}
			if (shown.Count == 0)
			{
				items.Children.Add(new TextBlock
				{
					Text = "Такого цвета нет в списке.",
					Foreground = Fog,
					FontSize = 13.0,
					Margin = new Thickness(12.0, 7.0, 12.0, 7.0)
				});
			}
		};
		search.TextChanged += delegate
		{
			hint.Visibility = ((search.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			fill();
		};
		search.KeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Return && shown.Count > 0)
			{
				if (pop != null)
				{
					pop.IsOpen = false;
				}
				SetVzColor(shown[0]);
			}
			else if (e.Key == Key.Escape && pop != null)
			{
				pop.IsOpen = false;
			}
		};
		fill();
		ShowPopup(vzColorBtn, stackPanel2);
		w.Dispatcher.BeginInvoke((Action)delegate
		{
			search.Focus();
			Keyboard.Focus(search);
		}, DispatcherPriority.Input);
	}

	private void SetVzColor(string name)
	{
		vcfg.Color = VzzColors.Canon(name) ?? "Случайный";
		vzShownColor = null;
		if (vzCode.Length > 0)
		{
			vzShownColor = ((vcfg.Color == "Случайный") ? VzzColors.Pick(null) : vcfg.Color);
			vzMessage = VzCompose(vzCode, vzShownColor);
		}
		VzEdited();
	}

	private string VzCompose(string code, string color)
	{
		return (vcfg.Line1 ?? "").Trim() + "\n" + color + "\n" + code;
	}

	private void RefreshVzPreview()
	{
		if (vzPrevLine1 != null)
		{
			string text = (vcfg.Line1 ?? "").Trim();
			vzPrevLine1.Text = ((text.Length > 0) ? text : "Первая строка");
			vzPrevLine1.Foreground = ((text.Length > 0) ? Chalk : Dim);
			string text2 = vzShownColor ?? vcfg.Color;
			PaintSwatch(vzPrevSwatch, text2);
			bool flag = text2 == "Случайный";
			vzPrevColor.Text = (flag ? "Случайный цвет, выберется для каждого кода" : text2);
			vzPrevColor.Foreground = (flag ? Fog : Chalk);
			vzPrevCode.Text = ((vzCode.Length > 0) ? vzCode : "код с картинки");
			vzPrevCode.Foreground = ((vzCode.Length > 0) ? Chalk : Dim);
			vzPrevCode.FontSize = ((vzCode.Length > 0) ? 26 : 15);
			vzPrevCode.FontWeight = ((vzCode.Length > 0) ? FontWeights.SemiBold : FontWeights.Normal);
			PaintSwatch(vzColorSwatch, vcfg.Color);
			vzColorText.Text = vcfg.Color;
			if (vzCode.Length > 0 && vzShownColor == null)
			{
				vzMessage = VzCompose(vzCode, text2);
			}
		}
	}

	private void ShowVzCode(string code, bool animate)
	{
		if (vzReveal != null)
		{
			vzReveal.Stop();
		}
		vzCells.Children.Clear();
		int num = ((code.Length > 0) ? code.Length : 6);
		double num2 = 600.0;
		double num3 = ((num <= 8) ? 10 : 6);
		double num4 = Math.Min(84.0, (num2 - num3 * (double)(num - 1)) / (double)num);
		double num5 = 118.0;
		vzCells.Cursor = ((code.Length > 0) ? System.Windows.Input.Cursors.Hand : null);
		vzCells.ToolTip = ((code.Length > 0) ? "Нажмите, чтобы скопировать сообщение" : null);
		if (code.Length > 0 && num4 < 30.0)
		{
			vzCells.Children.Add(new TextBlock
			{
				Text = code,
				Style = St("Num"),
				FontSize = 44.0,
				VerticalAlignment = VerticalAlignment.Center
			});
			return;
		}
		bool motion = animate && SystemParameters.ClientAreaAnimation;
		List<TextBlock> letters = new List<TextBlock>();
		List<SolidColorBrush> lines = new List<SolidColorBrush>();
		for (int i = 0; i < num; i++)
		{
			SolidColorBrush solidColorBrush = new SolidColorBrush((code.Length > 0) ? SignalC : Colors.Transparent);
			TextBlock textBlock = new TextBlock();
			textBlock.Text = ((code.Length > 0) ? code[i].ToString() : "");
			textBlock.Style = St("Num");
			textBlock.FontSize = Math.Min(num5 * 0.64, num4 * 0.98);
			textBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
			textBlock.Opacity = ((!motion) ? 1 : 0);
			TextBlock textBlock2 = textBlock;
			Border border = new Border();
			border.Width = num4;
			border.Height = num5;
			border.CornerRadius = new CornerRadius(8.0);
			border.Margin = new Thickness((i == 0) ? 0.0 : num3, 0.0, 0.0, 0.0);
			border.Background = ((code.Length > 0) ? Slate : CellOut);
			border.BorderBrush = solidColorBrush;
			border.BorderThickness = new Thickness((code.Length > 0) ? 1.5 : 0.0);
			border.Child = textBlock2;
			Border element = border;
			vzCells.Children.Add(element);
			letters.Add(textBlock2);
			lines.Add(solidColorBrush);
		}
		if (code.Length == 0)
		{
			return;
		}
		Action settle = delegate
		{
			foreach (SolidColorBrush item in lines)
			{
				item.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(SignalC, C("#2B2F37"), TimeSpan.FromMilliseconds((!motion) ? 1 : 700))
				{
					BeginTime = TimeSpan.FromMilliseconds(motion ? 700 : 1200)
				});
			}
		};
		if (!motion)
		{
			settle();
			return;
		}
		int shown = 0;
		vzReveal = new DispatcherTimer(DispatcherPriority.Render)
		{
			Interval = TimeSpan.FromMilliseconds(45.0)
		};
		vzReveal.Tick += delegate
		{
			if (shown < letters.Count)
			{
				letters[shown++].Opacity = 1.0;
			}
			if (shown >= letters.Count)
			{
				vzReveal.Stop();
				settle();
			}
		};
		letters[shown++].Opacity = 1.0;
		vzReveal.Start();
	}

	private string VzIdleText()
	{
		if ((vcfg.Channel ?? "").Length <= 0)
		{
			return "Вставьте ссылку на канал и нажмите «Включить»: откроется отдельное окно браузера с Discord.";
		}
		return "Нажмите «Включить»: откроется отдельное окно браузера с Discord.";
	}

	private void SetVzState(string tone, string text)
	{
		SetVzState(tone, text, error: false);
	}

	private void SetVzState(string tone, string text, bool error)
	{
		string text2 = tone + "|" + text + "|" + error;
		if (!(vzState == text2))
		{
			vzState = text2;
			string text3;
			System.Windows.Media.Brush fill;
			switch (tone)
			{
			case "start":
				text3 = "Запускаю";
				fill = SignalB;
				break;
			case "login":
				text3 = "Нужен вход в Discord";
				fill = WarnB;
				break;
			case "opening":
				text3 = "Открываю канал";
				fill = SignalB;
				break;
			case "live":
				text3 = "Слежу за каналом";
				fill = SignalB;
				break;
			case "busy":
				text3 = "Распознаю";
				fill = WarnB;
				break;
			case "sending":
				text3 = "Отправляю";
				fill = WarnB;
				break;
			case "stopping":
				text3 = "Выключаю";
				fill = Fog;
				break;
			case "error":
				text3 = "Ошибка";
				fill = StopB;
				break;
			default:
				text3 = "Выключено";
				fill = Fog;
				break;
			}
			vzStatusText.Text = text3;
			vzStatusDot.Fill = fill;
			vzDetail.Text = text ?? "";
			vzDetail.Foreground = ((error || tone == "error") ? StopB : Fog);
			if (tone == "error" && !string.IsNullOrEmpty(text))
			{
				VzLog(text, StopB);
			}
			switch (tone)
			{
			case "login":
				SetReady(vzLoginDot, vzLoginText, WarnB, "Нужен вход в Discord");
				break;
			case "opening":
			case "live":
			case "busy":
			case "sending":
				SetReady(vzLoginDot, vzLoginText, GoB, "Вход в Discord выполнен");
				break;
			}
			switch (tone)
			{
			case "live":
			{
				bool flag = text?.StartsWith("Чат закрыт") ?? false;
				SetReady(vzChatDot, vzChatText, flag ? WarnB : GoB, flag ? "Чат закрыт для записи" : "Чат открыт");
				break;
			}
			case "login":
			case "opening":
				SetReady(vzChatDot, vzChatText, Dim, "Чат");
				break;
			}
		}
	}

	private void SetVzControls(bool active)
	{
		vzRunning = active;
		vzChannel.IsEnabled = !active;
		vzToggle.IsEnabled = true;
		Icons.Set(vzToggle, "power", active ? "Выключить" : "Включить");
		vzToggle.Style = St(active ? "StopBtn" : "Primary");
		if (!active)
		{
			SetReady(vzBrowserDot, vzBrowserText, Dim, "Браузер");
			SetReady(vzLoginDot, vzLoginText, Dim, "Вход в Discord");
			SetReady(vzChatDot, vzChatText, Dim, "Чат");
		}
		UpdateVzModeDot();
	}

	private void ToggleVzz()
	{
		if (vzRunning)
		{
			HaltVzz();
		}
		else
		{
			StartVzz();
		}
	}

	private void StartVzz()
	{
		string url = (vzChannel.Text ?? "").Trim().TrimEnd('/');
		if (!VzChannelRx.IsMatch(url))
		{
			SetVzState("off", "Нужна ссылка на канал. В Discord нажмите правой кнопкой на канал и выберите «Копировать ссылку».", error: true);
			vzChannel.Focus();
			return;
		}
		if ((vcfg.Line1 ?? "").Trim().Length == 0)
		{
			SetVzState("off", "Заполните первую строку: с неё начинается сообщение.", error: true);
			vzLine1.Focus();
			return;
		}
		if (!LicenseGate())
		{
			SetVzState("off", "Нужен действующий ключ TTC: распознавание кода идёт на сайте TTC.", error: true);
			return;
		}
		vzChannel.Text = url;
		vcfg.Channel = url;
		SaveVzzCfg();
		vzFailed = false;
		vzNote = null;
		VzzWatcher wt = new VzzWatcher
		{
			Root = System.IO.Path.Combine(dir, "vzz"),
			Settings = VzSettings()
		};
		wt.Emit = delegate(VzzEvent ev)
		{
			try
			{
				if (!closing)
				{
					w.Dispatcher.BeginInvoke((Action)delegate
					{
						OnVzz(wt, ev);
					});
				}
			}
			catch
			{
			}
		};
		vzWatcher = wt;
		SetVzControls(active: true);
		SetVzState("start", "Открываю отдельное окно браузера…");
		SetReady(vzOcrDot, vzOcrText, Dim, "Распознавание");
		VzLog("Включено: слежу за каналом " + url.Substring(url.LastIndexOf('/') + 1) + ".", Chalk);
		Log("Забив ВЗЗ: включено, канал " + url + ".");
		vzThread = new Thread((ThreadStart)delegate
		{
			wt.Run(url);
		})
		{
			IsBackground = true,
			Name = "TTC vzz"
		};
		vzThread.Start();
	}

	private void HaltVzz()
	{
		if (vzWatcher != null)
		{
			vzWatcher.Stop();
			vzToggle.IsEnabled = false;
			Icons.Set(vzToggle, "power", "Выключаю…");
			SetVzState("stopping", "Закрываю окно браузера…");
		}
	}

	private void StopVzz(bool wait)
	{
		VzzWatcher vzzWatcher = vzWatcher;
		if (vzzWatcher != null)
		{
			vzzWatcher.Stop();
			if (wait && vzThread != null)
			{
				vzThread.Join(3000);
			}
		}
	}

	private void OnVzz(VzzWatcher from, VzzEvent e)
	{
		if (from != vzWatcher)
		{
			return;
		}
		switch (e.Kind)
		{
		case "done":
			vzWatcher = null;
			SetVzControls(active: false);
			if (!vzFailed)
			{
				SetVzState("off", vzNote ?? VzIdleText());
			}
			VzLog("Выключено." + ((vzNote != null) ? (" " + vzNote) : ""), Fog);
			Log("Забив ВЗЗ: выключено.");
			return;
		case "error":
			vzFailed = true;
			SetVzState("error", e.Text);
			Log("Забив ВЗЗ: " + e.Text);
			return;
		case "note":
			vzNote = e.Text;
			return;
		}
		if (from.Stopping)
		{
			return;
		}
		string text = MskNow();
		switch (e.Kind)
		{
		case "state":
			SetVzState(e.Tone, e.Text);
			break;
		case "ocr":
			if (e.Ok)
			{
				SetReady(vzOcrDot, vzOcrText, GoB, "Распознавание " + e.OcrMs + " мс");
				break;
			}
			SetReady(vzOcrDot, vzOcrText, StopB, "Распознавание: " + e.Text);
			VzLog("Сайт TTC не распознаёт: " + e.Text + ".", StopB);
			break;
		case "browser":
			if (e.Ok)
			{
				SetReady(vzBrowserDot, vzBrowserText, GoB, e.Text + " открыт");
				VzLog("Открыто окно " + e.Text + " с профилем TTC.", Fog);
			}
			else
			{
				SetReady(vzBrowserDot, vzBrowserText, StopB, "Браузер не открылся");
			}
			break;
		case "result":
			ShowVzResult(e, text);
			break;
		case "warn":
			SetVzMeta("Картинку в " + text + " не получилось прочитать: " + e.Text, StopB);
			VzLog("Картинку не прочитать: " + e.Text, StopB);
			break;
		case "sent":
		{
			vzSentAt = text;
			string text2 = ((e.After >= 0.0) ? (", через " + Sec(e.After) + " с после картинки") : "");
			string text3 = ((e.Place == 1) ? ", первыми" : ((e.Place > 1) ? (", " + e.Place + "-ми по счёту") : ""));
			SetVzMeta("Отправлено в " + text + text2 + text3 + "." + (vzChecking ? " Сверяю код с ответами после нашего…" : "") + vzDoubt, GoB);
			if (e.After >= 0.0)
			{
				vzAfter.Text = Sec(e.After);
				vzAfter.Foreground = Chalk;
				vzAfterSub.Text = "наше сообщение в канале, по часам Discord";
			}
			if (e.Place > 0)
			{
				vzPlace.Text = e.Place.ToString();
				vzPlace.Foreground = ((e.Place == 1) ? GoB : Chalk);
				vzPlaceSub.Text = ((e.Place == 1) ? "до нас кодом не ответил никто" : ("до нас кодом ответили " + (e.Place - 1)));
			}
			VzLog("Отправлено, Discord доставил" + text2 + text3 + ".", GoB);
			break;
		}
		case "unconfirmed":
			vzSentAt = text;
			SetVzMeta(e.Text + "В " + text + " сообщение ушло из поля ввода, но Discord за " + 10 + " с не показал его доставленным. Проверьте его в канале." + ((vzChecking && string.IsNullOrEmpty(e.Text)) ? " Сверяю код с ответами после нашего…" : "") + vzDoubt, WarnB);
			VzLog("Ушло из поля ввода, но Discord не подтвердил доставку.", WarnB);
			break;
		case "unsent":
			CopyVzMessage(manual: false);
			SetVzMeta("Не получилось отправить в " + text + ": " + e.Text + ". Сообщение скопировано, вставьте его сами.", StopB);
			VzLog("Не отправлено: " + e.Text, StopB);
			break;
		case "waiting":
			SetVzMeta("Получено в " + vzGotAt + ". Чат пока закрыт: отправлю, как только откроют." + vzDoubt, Fog);
			VzLog("Чат закрыт для записи — сообщение ждёт открытия.", WarnB);
			break;
		case "opened":
			vzCode = "";
			vzDoubt = "";
			vzChecking = false;
			vzShownColor = ((vcfg.Color == "Случайный") ? e.Color : null);
			vzMessage = "";
			ShowVzCode("", animate: false);
			vzCopyBtn.IsEnabled = false;
			RefreshVzPreview();
			if (!string.IsNullOrEmpty(e.Message))
			{
				SetClipboard(e.Message);
			}
			SetVzMeta("Чат открыли в " + text + ", картинки с кодом не было: отправляю 2 строки…", Fog);
			VzLog("Чат открыли, картинки не было — отправляю первые 2 строки.", Chalk);
			VzAlert();
			break;
		case "checked":
			SetVzMeta("Отправлено в канал в " + vzSentAt + ". Код верный: так же написали " + e.Votes + " из " + e.Total + " ответов после нашего.", GoB);
			vzSureSub.Text = "сверено: так же у " + e.Votes + " из " + e.Total;
			vzSureSub.Foreground = GoB;
			VzLog("Код верный: так же написали " + e.Votes + " из " + e.Total + ".", GoB);
			break;
		case "unchecked":
			SetVzMeta("Отправлено в канал в " + vzSentAt + ". Код не сверен: " + e.Text + ".", Fog);
			VzLog("Код не сверен: " + e.Text + ".", Fog);
			break;
		case "fixed":
			TakeVzCode(e.Old, e.New, e.Message);
			if (vcfg.Copy)
			{
				CopyVzMessage(manual: false);
			}
			SetVzMeta("Исправлено в " + text + ": " + e.Old + " на " + e.New + ", так написали " + e.Votes + " из " + e.Total + " ответов после нашего. Старое сообщение удалено, новое отправлено.", GoB);
			vzSureSub.Text = "исправлено по ответам: " + e.Votes + " из " + e.Total;
			vzSureSub.Foreground = WarnB;
			VzLog("Исправлено: " + e.Old + " на " + e.New + " (" + e.Votes + " из " + e.Total + "), старое удалено.", WarnB);
			break;
		case "unfixed":
			TakeVzCode(e.Old, e.New, null);
			CopyVzMessage(manual: false);
			SetVzMeta("По ответам в канале (" + e.Votes + " из " + e.Total + ") код " + e.New + ", а не " + e.Old + ", но исправить не вышло: " + e.Text + ". Сообщение с новым кодом скопировано, поправьте сами.", StopB);
			VzLog("Не исправлено: " + e.Text, StopB);
			VzAlert();
			break;
		}
	}

	private void ShowVzResult(VzzEvent e, string now)
	{
		vzGotAt = now;
		vzChecking = false;
		bool flag = e.Code.Length > 0 && e.Sure >= 0.0 && e.Sure < 0.85;
		vzDoubt = (flag ? (" Распознано неуверенно (" + Pct(e.Sure) + " %), проверьте.") : "");
		string text = ((e.OcrMs >= 0) ? ("прочитан на сайте за " + Sec((double)e.OcrMs / 1000.0) + " с") : "");
		vzAfter.Text = "—";
		vzAfter.Foreground = Dim;
		vzPlace.Text = "—";
		vzPlace.Foreground = Dim;
		vzPlaceSub.Text = "каким по счёту ответили кодом";
		vzAfterSub.Text = "наше сообщение в канале, по часам Discord";
		if (e.Code.Length == 0)
		{
			vzCode = "";
			vzMessage = "";
			vzShownColor = null;
			ShowVzCode("", animate: false);
			vzCopyBtn.IsEnabled = false;
			RefreshVzPreview();
			vzSure.Text = "—";
			vzSure.Foreground = Dim;
			vzSureSub.Text = "текста на картинке не нашлось";
			vzSureSub.Foreground = Fog;
			SetVzMeta("В " + now + " пришла картинка, но текст на ней не распознан.", WarnB);
			VzLog("Картинка в канале, текст не распознан.", WarnB);
			return;
		}
		vzCode = e.Code;
		vzMessage = e.Message ?? VzCompose(e.Code, e.Color);
		vzShownColor = e.Color;
		RefreshVzPreview();
		ShowVzCode(e.Code, animate: true);
		vzCopyBtn.IsEnabled = true;
		vzSure.Text = Pct(e.Sure);
		vzSure.Foreground = (flag ? WarnB : Chalk);
		vzSureSub.Text = (flag ? "неуверенно, проверьте код" : "уверенность сети на сайте TTC");
		vzSureSub.Foreground = (flag ? WarnB : Fog);
		string text2 = ((!string.IsNullOrEmpty(e.Hold) && vcfg.Send) ? (" " + e.Hold) : "");
		if (e.Sending)
		{
			vzChecking = vcfg.Check;
			if (vcfg.Copy)
			{
				CopyVzMessage(manual: false);
			}
			SetVzMeta("Получено в " + now + ". Отправляю в канал…" + vzDoubt, Fog);
		}
		else if (vcfg.Copy)
		{
			CopyVzMessage(manual: false);
			SetVzMeta("Скопировано в " + now + ". Осталось вставить в Discord и отправить." + text2 + vzDoubt, Fog);
		}
		else
		{
			SetVzMeta("Получено в " + now + "." + text2 + vzDoubt, Fog);
		}
		VzLog("Код " + e.Code + ((e.Sure >= 0.0) ? (", уверенно на " + Pct(e.Sure) + " %") : "") + ((text.Length > 0) ? ("; " + text) : "") + "." + ((text2.Length > 0) ? text2 : ""), flag ? WarnB : Chalk);
		VzAlert();
	}

	private void TakeVzCode(string old, string fresh, string message)
	{
		if (!(vzCode != old))
		{
			vzCode = fresh;
			vzMessage = message ?? VzCompose(fresh, vzShownColor ?? vcfg.Color);
			ShowVzCode(fresh, animate: true);
			RefreshVzPreview();
		}
	}

	private void SetVzMeta(string text, System.Windows.Media.Brush color)
	{
		vzMeta.Text = text;
		vzMeta.Foreground = color;
	}

	private void VzLog(string text, System.Windows.Media.Brush color)
	{
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 0.0, 0.0, 7.0);
		Grid grid2 = grid;
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(92.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		DateTime dateTime = Clock.Msk(Clock.UtcNow());
		grid2.Children.Add(new TextBlock
		{
			Text = dateTime.ToString("HH:mm:ss") + "," + dateTime.Millisecond.ToString("000"),
			Style = St("Num"),
			FontWeight = FontWeights.Normal,
			FontSize = 13.0,
			Foreground = Fog
		});
		TextBlock textBlock = new TextBlock();
		textBlock.Text = text;
		textBlock.FontSize = 13.0;
		textBlock.Foreground = color;
		textBlock.TextWrapping = TextWrapping.Wrap;
		textBlock.LineHeight = 17.0;
		TextBlock element = textBlock;
		Grid.SetColumn(element, 1);
		grid2.Children.Add(element);
		vzLog.Children.Remove(vzLogEmpty);
		vzLog.Children.Add(grid2);
		while (vzLog.Children.Count > 200)
		{
			vzLog.Children.RemoveAt(0);
		}
		vzLogScroll.ScrollToEnd();
	}

	private void CopyVzMessage(bool manual)
	{
		if (!string.IsNullOrEmpty(vzCode) && !string.IsNullOrEmpty(vzMessage) && SetClipboard(vzMessage.Replace("\n", "\r\n")) && manual)
		{
			Icons.Set(vzCopyBtn, "check", "Скопировано");
			DispatcherTimer t = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(1500.0)
			};
			t.Tick += delegate
			{
				t.Stop();
				Icons.Set(vzCopyBtn, "copy", "Скопировать");
			};
			t.Start();
		}
	}

	private static bool SetClipboard(string text)
	{
		for (int i = 0; i < 5; i++)
		{
			try
			{
				System.Windows.Clipboard.SetDataObject(text, copy: true);
				return true;
			}
			catch
			{
				Thread.Sleep(30);
			}
		}
		return false;
	}

	private void VzAlert()
	{
		if (!vcfg.Sound)
		{
			return;
		}
		try
		{
			SystemSounds.Asterisk.Play();
		}
		catch
		{
		}
		if (w.IsActive)
		{
			return;
		}
		try
		{
			VzFlash.FLASHWINFO fi = default(VzFlash.FLASHWINFO);
			fi.cbSize = (uint)Marshal.SizeOf(typeof(VzFlash.FLASHWINFO));
			fi.hwnd = new WindowInteropHelper(w).Handle;
			fi.dwFlags = 15u;
			fi.uCount = 0u;
			fi.dwTimeout = 0u;
			VzFlash.FlashWindowEx(ref fi);
		}
		catch
		{
		}
	}

	private static string MskNow()
	{
		return Clock.Msk(Clock.UtcNow()).ToString("HH:mm:ss");
	}

	private static string Sec(double s)
	{
		return Math.Max(0.0, s).ToString("0.00", Ru);
	}

	private static string Pct(double sure)
	{
		return Math.Round(Math.Max(0.0, Math.Min(1.0, sure)) * 100.0).ToString("0", Ru);
	}

	private static bool CaptTimeOk(TimeSpan t)
	{
		if (t >= CaptFrom)
		{
			return t <= CaptTo;
		}
		return false;
	}

	private void LoadPlanCfg()
	{
		try
		{
			if (File.Exists(PlanCfgPath))
			{
				pcfg = json.Deserialize<PlanCfg>(File.ReadAllText(PlanCfgPath)) ?? new PlanCfg();
			}
		}
		catch
		{
			pcfg = new PlanCfg();
		}
		if (pcfg.Entries == null)
		{
			pcfg.Entries = new List<PlanEntry>();
		}
		pcfg.MinPeople = Math.Max(1, Math.Min(60, pcfg.MinPeople));
	}

	private void SavePlanCfg()
	{
		if (demo)
		{
			return;
		}
		try
		{
			PlanCfg planCfg = new PlanCfg();
			planCfg.Active = pcfg.Active;
			planCfg.Smart = pcfg.Smart;
			planCfg.MinPeople = pcfg.MinPeople;
			planCfg.Neutral = pcfg.Neutral;
			planCfg.Rebid = pcfg.Rebid;
			planCfg.Entries = pcfg.Entries.Where((PlanEntry e) => !e.Retry).ToList();
			PlanCfg obj = planCfg;
			File.WriteAllText(PlanCfgPath, json.Serialize(obj));
		}
		catch
		{
		}
	}

	private void InitPlan()
	{
		if (mainBody != null && mainBar != null && mainButtons != null && mainButtons.Parent is Grid bottom)
		{
			LoadPlanCfg();
			DarkScrollbars();
			BuildPlanBody();
			BuildPlanBar(bottom);
			DispatcherTimer dispatcherTimer = new DispatcherTimer();
			dispatcherTimer.Interval = TimeSpan.FromSeconds(1.0);
			DispatcherTimer dispatcherTimer2 = dispatcherTimer;
			dispatcherTimer2.Tick += delegate
			{
				PlanTick();
			};
			dispatcherTimer2.Start();
			RenderPlan();
			if (pcfg.Active)
			{
				ShowPlan(on: true);
			}
		}
	}

	private void DarkScrollbars()
	{
		string xamlText = "<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"ScrollBar\"><Setter Property=\"Width\" Value=\"8\"/><Setter Property=\"MinWidth\" Value=\"8\"/><Setter Property=\"Background\" Value=\"Transparent\"/><Setter Property=\"Template\"><Setter.Value><ControlTemplate TargetType=\"ScrollBar\"><Track x:Name=\"PART_Track\" IsDirectionReversed=\"True\"><Track.Thumb>" + string.Format("<Thumb><Thumb.Template><ControlTemplate TargetType=\"Thumb\"><Border Background=\"#3A3F48\" CornerRadius=\"3\" Margin=\"{0}\"/></ControlTemplate></Thumb.Template></Thumb>", "1,2") + "</Track.Thumb></Track></ControlTemplate></Setter.Value></Setter><Style.Triggers><Trigger Property=\"Orientation\" Value=\"Horizontal\"><Setter Property=\"Width\" Value=\"Auto\"/><Setter Property=\"MinWidth\" Value=\"0\"/><Setter Property=\"Height\" Value=\"8\"/><Setter Property=\"MinHeight\" Value=\"8\"/><Setter Property=\"Template\"><Setter.Value><ControlTemplate TargetType=\"ScrollBar\"><Track x:Name=\"PART_Track\"><Track.Thumb>" + string.Format("<Thumb><Thumb.Template><ControlTemplate TargetType=\"Thumb\"><Border Background=\"#3A3F48\" CornerRadius=\"3\" Margin=\"{0}\"/></ControlTemplate></Thumb.Template></Thumb>", "2,1") + "</Track.Thumb></Track></ControlTemplate></Setter.Value></Setter></Trigger></Style.Triggers></Style>";
		try
		{
			Style value = (Style)XamlReader.Parse(xamlText);
			w.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = value;
			RootGrid.Resources[typeof(System.Windows.Controls.Primitives.ScrollBar)] = value;
		}
		catch
		{
		}
	}

	private void ShowPlan(bool on)
	{
		if (planBody != null)
		{
			if (on && vzzOn)
			{
				ShowVzz(on: false);
			}
			if (on && prioOn)
			{
				ShowPriority(show: false);
			}
			planOn = on;
			mainBody.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
			mainBar.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
			mainButtons.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
			Grid grid = planBody;
			System.Windows.Controls.Panel panel = planBar;
			Visibility visibility2 = (planButtons.Visibility = ((!on) ? Visibility.Collapsed : Visibility.Visible));
			Visibility visibility4 = (panel.Visibility = visibility2);
			grid.Visibility = visibility4;
			if (on)
			{
				modeText.Text = "План";
			}
			else if (!vzzOn && !prioOn)
			{
				modeText.Text = (rcfg.ListsMode ? "Списки" : "Забив");
			}
			if (pcfg.Active != on)
			{
				pcfg.Active = on;
				SavePlanCfg();
			}
			if (on)
			{
				PlanHints();
				RenderPlan();
				RefreshPlanData(force: false);
			}
		}
	}

	private void UpdatePlanLock()
	{
		if (planLock == null)
		{
			return;
		}
		bool flag = !PlanLinked && !demo;
		if (flag != planLockShown)
		{
			planLockShown = flag;
			planLock.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
			planMain.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			if (planUpd != null)
			{
				planUpd.Visibility = (flag ? Visibility.Collapsed : Visibility.Visible);
			}
			RenderPlan();
		}
	}

	private void BuildPlanBody()
	{
		planBody = new Grid
		{
			Margin = mainBody.Margin,
			Visibility = Visibility.Collapsed
		};
		Grid.SetRow(planBody, Grid.GetRow(mainBody));
		((System.Windows.Controls.Panel)mainBody.Parent).Children.Add(planBody);
		planMain = new Grid();
		planMain.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(372.0)
		});
		planMain.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(40.0)
		});
		planMain.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		planBody.Children.Add(planMain);
		planBody.Children.Add(planLock = BuildPlanLock());
		StackPanel stackPanel = new StackPanel();
		planMain.Children.Add(stackPanel);
		stackPanel.Children.Add(new TextBlock
		{
			Text = "Цели плана",
			Style = St("Label")
		});
		planRows = new StackPanel
		{
			Margin = new Thickness(14.0, 10.0, 10.0, 10.0)
		};
		stackPanel.Children.Add(new Border
		{
			Background = WellB,
			CornerRadius = new CornerRadius(6.0),
			Height = 196.0,
			Child = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Content = planRows
			}
		});
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 12.0, 0.0, 0.0);
		Grid grid2 = grid;
		double[] array = new double[6] { 76.0, 8.0, 66.0, 8.0, 100.0, 8.0 };
		foreach (double pixels in array)
		{
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(pixels)
			});
		}
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		planSqBox = VzField(17.0, 40.0, strong: true);
		planSqBox.MaxLength = 5;
		planCaptBox = VzField(17.0, 40.0, strong: true);
		planCaptBox.MaxLength = 5;
		planFireBox = VzField(17.0, 40.0, strong: true);
		planFireBox.MaxLength = 12;
		UIElement element = WithHint(planSqBox, "AI40", out planSqHint);
		UIElement element2 = WithHint(planCaptBox, "20:00", out planCaptHint);
		UIElement element3 = WithHint(planFireBox, "19:59:58", out planFireHint);
		planSqBox.TextChanged += delegate
		{
			planSqHint.Visibility = ((planSqBox.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			planSqBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
		};
		planCaptBox.TextChanged += delegate
		{
			planCaptHint.Visibility = ((planCaptBox.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			planCaptBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
		};
		planFireBox.TextChanged += delegate
		{
			planFireHint.Visibility = ((planFireBox.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			planFireBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
		};
		planSqBox.ToolTip = "Квадрат цели. Пусто — из вкладки «Забив». Русская раскладка не мешает: «ФШ40» станет AI40";
		planCaptBox.ToolTip = "Время капта в форме атаки, шаг 5 минут. Пусто — из вкладки «Забив»";
		planFireBox.ToolTip = "Когда нажать «Запланировать атаку», МСК. Пусто — из вкладки «Забив»";
		Grid.SetColumn(element2, 2);
		Grid.SetColumn(element3, 4);
		grid2.Children.Add(element);
		grid2.Children.Add(element2);
		grid2.Children.Add(element3);
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Btn");
		button.Height = 40.0;
		button.FontSize = 13.5;
		button.ToolTip = "Добавить цель в план (Enter). Пустые поля — как во вкладке «Забив»";
		System.Windows.Controls.Button button2 = button;
		Icons.Set(button2, "plus", "Добавить");
		button2.Click += delegate
		{
			AddPlanEntry();
		};
		Grid.SetColumn(button2, 6);
		grid2.Children.Add(button2);
		System.Windows.Controls.TextBox[] array2 = new System.Windows.Controls.TextBox[3] { planSqBox, planCaptBox, planFireBox };
		foreach (System.Windows.Controls.TextBox textBox in array2)
		{
			textBox.KeyDown += delegate(object s, System.Windows.Input.KeyEventArgs e)
			{
				if (e.Key == Key.Return)
				{
					AddPlanEntry();
				}
			};
		}
		stackPanel.Children.Add(grid2);
		planNote = new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(planNote);
		PlanNote(null, error: false);
		planSmart = VzSwitch("Умная цель, если мало людей", pcfg.Smart, new Thickness(0.0, 16.0, 0.0, 2.0), "Перед забивом считаю людей в списке из Discord.\nМеньше порога — бью цель, которую таким числом людей берут чаще всего.");
		planSmart.Checked += delegate
		{
			pcfg.Smart = true;
			SavePlanCfg();
			RenderPlan();
			RefreshPlanData(force: true);
		};
		planSmart.Unchecked += delegate
		{
			pcfg.Smart = false;
			SavePlanCfg();
			RenderPlan();
		};
		stackPanel.Children.Add(planSmart);
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel2.Margin = new Thickness(46.0, 6.0, 0.0, 0.0);
		StackPanel stackPanel3 = stackPanel2;
		stackPanel3.Children.Add(new TextBlock
		{
			Text = "Порог",
			Foreground = Fog,
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(0.0, 0.0, 10.0, 0.0)
		});
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = St("Small");
		button3.Content = Icons.Make("minus", 12.0);
		button3.Width = 28.0;
		button3.Height = 26.0;
		System.Windows.Controls.Button button4 = button3;
		planMinText = new TextBlock
		{
			Style = St("Num"),
			FontSize = 18.0,
			Width = 44.0,
			TextAlignment = TextAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		};
		System.Windows.Controls.Button button5 = new System.Windows.Controls.Button();
		button5.Style = St("Small");
		button5.Content = Icons.Make("plus", 12.0);
		button5.Width = 28.0;
		button5.Height = 26.0;
		System.Windows.Controls.Button button6 = button5;
		button4.Click += delegate
		{
			SetPlanMin(pcfg.MinPeople - 1);
		};
		button6.Click += delegate
		{
			SetPlanMin(pcfg.MinPeople + 1);
		};
		planMinText.MouseWheel += delegate(object s, MouseWheelEventArgs e)
		{
			SetPlanMin(pcfg.MinPeople + Math.Sign(e.Delta));
		};
		stackPanel3.Children.Add(button4);
		stackPanel3.Children.Add(planMinText);
		stackPanel3.Children.Add(button6);
		stackPanel3.Children.Add(new TextBlock
		{
			Text = "человек в списке",
			Foreground = Fog,
			FontSize = 13.0,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
		});
		stackPanel.Children.Add(stackPanel3);
		planRebid = VzSwitch("Перебивать", pcfg.Rebid, new Thickness(0.0, 14.0, 0.0, 2.0), "Игра поставила на цель капт другой семьи раньше нашего — сразу бью следующую цель с лучшим шансом (до " + 2 + " раз).\nНужно чтение игры (TTC от имени администратора): по нему видно, кто успел.");
		planRebid.Checked += delegate
		{
			pcfg.Rebid = true;
			SavePlanCfg();
		};
		planRebid.Unchecked += delegate
		{
			pcfg.Rebid = false;
			SavePlanCfg();
		};
		stackPanel.Children.Add(planRebid);
		stackPanel.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(46.0, 2.0, 0.0, 0.0),
			Text = "Перебили — сразу бью следующую разрешённую цель. Свой капт в момент нажатия — жду его конца."
		});
		planNeutral = VzSwitch("Можно в ничью зону", pcfg.Neutral, new Thickness(0.0, 14.0, 0.0, 2.0), "В истории сервера каптов на ничьи зоны нет — проверьте в игре, что их можно бить");
		planNeutral.Checked += delegate
		{
			pcfg.Neutral = true;
			SavePlanCfg();
			RefreshPlanData(force: true);
		};
		planNeutral.Unchecked += delegate
		{
			pcfg.Neutral = false;
			SavePlanCfg();
			RefreshPlanData(force: true);
		};
		stackPanel.Children.Add(planNeutral);
		SetPlanMin(pcfg.MinPeople);
		Grid grid3 = new Grid();
		Grid.SetColumn(grid3, 2);
		planMain.Children.Add(grid3);
		for (int j = 0; j < 5; j++)
		{
			grid3.RowDefinitions.Add(new RowDefinition
			{
				Height = ((j == 4) ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto)
			});
		}
		Grid grid4 = new Grid();
		grid4.Children.Add(new TextBlock
		{
			Text = "Записалось в Discord",
			Style = St("Label"),
			Margin = new Thickness(0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		StackPanel stackPanel4 = new StackPanel();
		stackPanel4.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel4.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		stackPanel4.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel5 = stackPanel4;
		planStatusDot = new System.Windows.Shapes.Rectangle
		{
			Width = 8.0,
			Height = 8.0,
			RadiusX = 1.5,
			RadiusY = 1.5,
			Fill = Fog,
			VerticalAlignment = VerticalAlignment.Center
		};
		planStatusText = new TextBlock
		{
			FontSize = 13.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			Foreground = Chalk
		};
		stackPanel5.Children.Add(planStatusDot);
		stackPanel5.Children.Add(planStatusText);
		grid4.Children.Add(stackPanel5);
		grid3.Children.Add(grid4);
		StackPanel stackPanel6 = new StackPanel();
		stackPanel6.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel6.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		StackPanel stackPanel7 = stackPanel6;
		planPeople = new TextBlock
		{
			Style = St("Num"),
			FontSize = 72.0,
			Text = "—",
			Margin = new Thickness(-3.0, -4.0, 0.0, -6.0)
		};
		planPeopleSub = new TextBlock
		{
			Foreground = Fog,
			FontSize = 16.0,
			VerticalAlignment = VerticalAlignment.Bottom,
			Margin = new Thickness(12.0, 0.0, 0.0, 14.0)
		};
		stackPanel7.Children.Add(planPeople);
		stackPanel7.Children.Add(planPeopleSub);
		Grid.SetRow(stackPanel7, 1);
		grid3.Children.Add(stackPanel7);
		StackPanel stackPanel8 = new StackPanel();
		stackPanel8.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
		StackPanel stackPanel9 = stackPanel8;
		planNow = new TextBlock
		{
			FontSize = 14.0,
			Foreground = SignalB,
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 19.0,
			Margin = new Thickness(0.0, 0.0, 0.0, 6.0)
		};
		planMainText = new TextBlock
		{
			FontSize = 14.0,
			Foreground = Chalk,
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 19.0
		};
		planBest = new TextBlock
		{
			FontSize = 14.0,
			Foreground = GoB,
			TextWrapping = TextWrapping.Wrap,
			LineHeight = 19.0,
			Margin = new Thickness(0.0, 6.0, 0.0, 0.0)
		};
		planOptRows = new StackPanel
		{
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0)
		};
		stackPanel9.Children.Add(planNow);
		stackPanel9.Children.Add(planMainText);
		stackPanel9.Children.Add(planBest);
		stackPanel9.Children.Add(planOptRows);
		Grid.SetRow(stackPanel9, 2);
		grid3.Children.Add(stackPanel9);
		Grid grid5 = new Grid();
		grid5.Margin = new Thickness(0.0, 18.0, 0.0, 0.0);
		Grid grid6 = grid5;
		grid6.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Height = 1.0,
			Fill = RuleB,
			VerticalAlignment = VerticalAlignment.Top
		});
		Grid grid7 = new Grid();
		grid7.Margin = new Thickness(0.0, 14.0, 0.0, 8.0);
		Grid grid8 = grid7;
		grid8.Children.Add(new TextBlock
		{
			Text = "Семьи сервера по силе",
			Style = St("Label"),
			Margin = new Thickness(0.0)
		});
		planFamHead = new TextBlock
		{
			FontSize = 12.5,
			Foreground = Fog,
			HorizontalAlignment = System.Windows.HorizontalAlignment.Right
		};
		grid8.Children.Add(planFamHead);
		grid6.Children.Add(grid8);
		Grid.SetRow(grid6, 3);
		grid3.Children.Add(grid6);
		Grid grid9 = new Grid();
		grid9.RowDefinitions.Add(new RowDefinition
		{
			Height = GridLength.Auto
		});
		grid9.RowDefinitions.Add(new RowDefinition
		{
			Height = new GridLength(1.0, GridUnitType.Star)
		});
		grid9.Children.Add(FamRow(new string[5] { "Семья", "в защите", "отбилась", "за 7 дней", "" }, head: true, null, null));
		planFamRows = new StackPanel();
		ScrollViewer scrollViewer = new ScrollViewer();
		scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		scrollViewer.Content = planFamRows;
		ScrollViewer element4 = scrollViewer;
		Grid.SetRow(element4, 1);
		grid9.Children.Add(element4);
		Grid.SetRow(grid9, 4);
		grid3.Children.Add(grid9);
	}

	private Grid BuildPlanLock()
	{
		Grid grid = new Grid();
		grid.Visibility = Visibility.Collapsed;
		Grid grid2 = grid;
		StackPanel stackPanel = new StackPanel();
		stackPanel.MaxWidth = 520.0;
		stackPanel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 60.0);
		StackPanel stackPanel2 = stackPanel;
		Border border = new Border();
		border.Width = 64.0;
		border.Height = 64.0;
		border.CornerRadius = new CornerRadius(16.0);
		border.Background = Slate;
		border.BorderBrush = RuleB;
		border.BorderThickness = new Thickness(1.0);
		border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		border.Child = Icons.Make("bot", 30.0, SignalB);
		Border element = border;
		stackPanel2.Children.Add(element);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "План работает с ботом семьи в Discord",
			FontSize = 24.0,
			FontWeight = FontWeights.SemiBold,
			Foreground = Chalk,
			Margin = new Thickness(0.0, 22.0, 0.0, 10.0),
			TextWrapping = TextWrapping.Wrap
		});
		stackPanel2.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			FontSize = 14.0,
			LineHeight = 20.0,
			Text = "Бот присылает список тех, кто идёт на капт. По нему TTC решает, хватит ли людей на цель, и выбирает другую, если нет. Подключите бота — создайте токен в личном кабинете и вставьте его в бота семьи. План откроется сам, как только бот подключится."
		});
		StackPanel stackPanel3 = new StackPanel();
		stackPanel3.Margin = new Thickness(0.0, 18.0, 0.0, 0.0);
		StackPanel stackPanel4 = stackPanel3;
		string[] array = new string[3] { "Личный кабинет, вкладка «API для Discord»: «Создать токен».", "Токен — в бота семьи (готовый бот и инструкция — по второй кнопке).", "Бот записывает людей кнопкой в Discord — TTC видит список во вкладке «Списки»." };
		for (int i = 0; i < array.Length; i++)
		{
			Grid grid3 = new Grid();
			grid3.Margin = new Thickness(0.0, 0.0, 0.0, 8.0);
			Grid grid4 = grid3;
			grid4.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(30.0)
			});
			grid4.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid4.Children.Add(new TextBlock
			{
				Text = (i + 1).ToString(),
				Style = St("Num"),
				FontSize = 16.0,
				Foreground = SignalB
			});
			TextBlock textBlock = new TextBlock();
			textBlock.Text = array[i];
			textBlock.FontSize = 14.0;
			textBlock.Foreground = Chalk;
			textBlock.TextWrapping = TextWrapping.Wrap;
			TextBlock element2 = textBlock;
			Grid.SetColumn(element2, 1);
			grid4.Children.Add(element2);
			stackPanel4.Children.Add(grid4);
		}
		stackPanel2.Children.Add(stackPanel4);
		StackPanel stackPanel5 = new StackPanel();
		stackPanel5.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel5.Margin = new Thickness(0.0, 14.0, 0.0, 0.0);
		StackPanel stackPanel6 = stackPanel5;
		System.Windows.Controls.Button button = new System.Windows.Controls.Button();
		button.Style = St("Primary");
		button.Height = 44.0;
		button.Padding = new Thickness(18.0, 0.0, 18.0, 0.0);
		button.FontSize = 14.5;
		System.Windows.Controls.Button button2 = button;
		Icons.Set(button2, "external", "Создать токен");
		button2.Click += delegate
		{
			AppPaths.OpenUrl("https://timetocapture.ru/cabinet?tab=api");
		};
		System.Windows.Controls.Button button3 = new System.Windows.Controls.Button();
		button3.Style = St("Btn");
		button3.Height = 44.0;
		button3.Padding = new Thickness(18.0, 0.0, 18.0, 0.0);
		button3.Margin = new Thickness(10.0, 0.0, 0.0, 0.0);
		button3.FontSize = 14.5;
		System.Windows.Controls.Button button4 = button3;
		Icons.Set(button4, "bot", "Как подключить бота");
		button4.Click += delegate
		{
			AppPaths.OpenUrl("https://timetocapture.ru/api-docs");
		};
		stackPanel6.Children.Add(button2);
		stackPanel6.Children.Add(button4);
		stackPanel2.Children.Add(stackPanel6);
		grid2.Children.Add(stackPanel2);
		return grid2;
	}

	private FrameworkElement PlanBadge(string name, long id, double size)
	{
		WarFamily warFamily = ((war != null && name != null) ? war.Family(name) : null);
		if (warFamily != null)
		{
			return FamBadge(warFamily, size);
		}
		string path = ((id > 0 && !string.IsNullOrEmpty(cfg.Server)) ? ("/war/logo/" + cfg.Server.ToUpperInvariant() + "/" + id + ".png") : null);
		return Images.Badge(path, Dim, null, size);
	}

	private Grid FamRow(string[] cells, bool head, System.Windows.Media.Brush nameColor, System.Windows.Media.Brush tierColor)
	{
		return FamRow(cells, head, nameColor, tierColor, null);
	}

	private Grid FamRow(string[] cells, bool head, System.Windows.Media.Brush nameColor, System.Windows.Media.Brush tierColor, FrameworkElement badge)
	{
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 0.0, 12.0, head ? 6 : 5);
		Grid grid2 = grid;
		double[] array = new double[5] { 0.0, 92.0, 86.0, 82.0, 86.0 };
		foreach (double num in array)
		{
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = ((num == 0.0) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(num))
			});
		}
		for (int j = 0; j < cells.Length; j++)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Text = cells[j];
			textBlock.FontSize = (head ? 12.5 : 13.5);
			textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			textBlock.Foreground = (head ? Fog : (j switch
			{
				4 => tierColor ?? Fog, 
				0 => nameColor ?? Chalk, 
				_ => Chalk, 
			}));
			textBlock.TextAlignment = ((j != 0) ? TextAlignment.Right : TextAlignment.Left);
			TextBlock textBlock2 = textBlock;
			if (!head && j >= 1 && j <= 3)
			{
				textBlock2.FontStretch = FontStretches.Condensed;
				textBlock2.FontWeight = FontWeights.SemiBold;
				textBlock2.FontSize = 15.0;
			}
			FrameworkElement element = textBlock2;
			if (j == 0 && badge != null)
			{
				StackPanel stackPanel = new StackPanel();
				stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
				StackPanel stackPanel2 = stackPanel;
				badge.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
				badge.VerticalAlignment = VerticalAlignment.Center;
				textBlock2.VerticalAlignment = VerticalAlignment.Center;
				stackPanel2.Children.Add(badge);
				stackPanel2.Children.Add(textBlock2);
				element = stackPanel2;
			}
			Grid.SetColumn(element, j);
			grid2.Children.Add(element);
		}
		return grid2;
	}

	private void BuildPlanBar(Grid bottom)
	{
		WrapPanel wrapPanel = new WrapPanel();
		wrapPanel.VerticalAlignment = VerticalAlignment.Center;
		wrapPanel.Visibility = Visibility.Collapsed;
		WrapPanel wrapPanel2 = wrapPanel;
		Grid.SetColumn(wrapPanel2, Grid.GetColumn(mainBar));
		wrapPanel2.Children.Add(VzReady("Бот семьи", out planRosterDot, out planRosterText));
		wrapPanel2.Children.Add(VzReady("Каптов сервера", out planDataDot, out planDataText));
		planBar = wrapPanel2;
		bottom.Children.Add(wrapPanel2);
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		stackPanel.Visibility = Visibility.Collapsed;
		StackPanel stackPanel2 = stackPanel;
		Grid.SetColumn(stackPanel2, Grid.GetColumn(mainButtons));
		planUpd = new System.Windows.Controls.Button
		{
			Style = St("Btn"),
			Height = 50.0,
			Width = 168.0,
			ToolTip = "Заново взять с сайта силу семей и выбор цели"
		};
		Icons.Set(planUpd, "refresh", "Пересчитать");
		planUpd.Click += delegate
		{
			RefreshPlanData(force: true);
		};
		planGo = new System.Windows.Controls.Button
		{
			Style = St("Primary"),
			Height = 50.0,
			Width = 214.0,
			Margin = new Thickness(10.0, 0.0, 0.0, 0.0)
		};
		planGo.Click += delegate
		{
			if (planRunning)
			{
				StopPlan("План остановлен.");
			}
			else
			{
				StartPlan();
			}
		};
		stackPanel2.Children.Add(planUpd);
		stackPanel2.Children.Add(planGo);
		planButtons = stackPanel2;
		bottom.Children.Add(stackPanel2);
	}

	private void SetPlanMin(int n)
	{
		pcfg.MinPeople = Math.Max(1, Math.Min(60, n));
		planMinText.Text = pcfg.MinPeople.ToString();
		SavePlanCfg();
		RenderDecision();
	}

	private void PlanHints()
	{
		if (planSqHint != null)
		{
			string text = (SquareBox.Text ?? "").Trim().ToUpperInvariant();
			planSqHint.Text = (Runner.ParseSquare(text, out var _, out var _) ? text : "AI40");
			planCaptHint.Text = cfg.CaptH.ToString("00") + ":" + cfg.CaptM.ToString("00");
			planFireHint.Text = (Clock.ParseTime(FireBox.Text, out var _) ? FireBox.Text.Trim() : "19:59:58");
		}
	}

	private static string NormSquare(string s)
	{
		char[] array = (s ?? "").Trim().ToUpperInvariant().ToCharArray();
		for (int i = 0; i < array.Length; i++)
		{
			int num = "ФИСВУАПРШО".IndexOf(array[i]);
			if (num >= 0)
			{
				array[i] = "ABCDEFGHIJ"[num];
			}
		}
		return new string(array).Replace(" ", "");
	}

	private void AddPlanEntry()
	{
		PlanHints();
		string text = NormSquare((planSqBox.Text.Length > 0) ? planSqBox.Text : planSqHint.Text);
		string s = ((planCaptBox.Text.Length > 0) ? planCaptBox.Text : planCaptHint.Text);
		string s2 = ((planFireBox.Text.Length > 0) ? planFireBox.Text : planFireHint.Text);
		if (!Runner.ParseSquare(text, out var _, out var _))
		{
			PlanNote("Квадрата «" + text + "» нет на карте — нужен код вроде AI40.", error: true);
			planSqBox.BorderBrush = StopB;
			planSqBox.Focus();
			return;
		}
		if (!Clock.ParseTime(s, out var t) || t.Minutes % 5 != 0 || t.Seconds != 0)
		{
			PlanNote("Время капта — часы и минуты с шагом 5, например 20:00.", error: true);
			planCaptBox.BorderBrush = StopB;
			planCaptBox.Focus();
			return;
		}
		if (!Clock.ParseTime(s2, out var t2))
		{
			PlanNote("Когда нажать — время МСК, например 19:59:58.", error: true);
			planFireBox.BorderBrush = StopB;
			planFireBox.Focus();
			return;
		}
		if (!CaptTimeOk(t))
		{
			PlanNote("Капты — с 15:00 до 21:59 МСК: на " + t.ToString("hh\\:mm") + " капт не поставить.", error: true);
			planCaptBox.BorderBrush = StopB;
			planCaptBox.Focus();
			return;
		}
		if (t2 > CaptTo)
		{
			PlanNote("Забить можно до 21:59:59 МСК — после этого капты не ставятся.", error: true);
			planFireBox.BorderBrush = StopB;
			planFireBox.Focus();
			return;
		}
		PlanEntry pe = new PlanEntry
		{
			Square = text,
			Capt = t.ToString("hh\\:mm"),
			Fire = t2.ToString((t2.Milliseconds > 0) ? "hh\\:mm\\:ss\\.fff" : "hh\\:mm\\:ss")
		};
		if (pcfg.Entries.Any((PlanEntry e) => !e.Retry && e.Square == pe.Square && e.Fire == pe.Fire))
		{
			PlanNote(pe.Square + " в " + pe.Fire + " уже есть в плане.", error: true);
			return;
		}
		pcfg.Entries.Add(pe);
		if (!planRunning)
		{
			pcfg.Entries = pcfg.Entries.OrderBy((PlanEntry e) => PlanFireUtc(e)).ToList();
		}
		SavePlanCfg();
		planSqBox.Text = "";
		planCaptBox.Text = "";
		planFireBox.Text = "";
		planSqBox.Focus();
		PlanNote("Добавлено: " + pe.Square + ", капт на " + pe.Capt + ", нажать в " + pe.Fire + ".", error: false);
		RenderPlan();
		RefreshPlanData(force: true);
	}

	private void PlanNote(string text, bool error)
	{
		planNote.Text = text ?? "Квадрат, время капта и когда нажать (МСК). Пустые поля — как во вкладке «Забив».";
		planNote.Foreground = (error ? StopB : Fog);
	}

	private static DateTime PlanFireUtc(PlanEntry e)
	{
		if (e.FireUtc != DateTime.MinValue)
		{
			return e.FireUtc;
		}
		if (!Clock.ParseTime(e.Fire, out var t))
		{
			return DateTime.MaxValue;
		}
		return Clock.NextMskUtc(t);
	}

	private static string PlanLeft(DateTime utc)
	{
		TimeSpan timeSpan = utc - Clock.UtcNow();
		if (timeSpan < TimeSpan.Zero)
		{
			timeSpan = TimeSpan.Zero;
		}
		if (!(timeSpan.TotalHours >= 1.0))
		{
			return $"{timeSpan.Minutes}:{timeSpan.Seconds:00}";
		}
		return $"{(int)timeSpan.TotalHours}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
	}

	private void RenderPlan()
	{
		if (planRows == null)
		{
			return;
		}
		planRows.Children.Clear();
		foreach (PlanEntry entry in pcfg.Entries)
		{
			PlanEntry it = entry;
			Grid grid = new Grid();
			grid.Margin = new Thickness(entry.Retry ? 18 : 0, 0.0, 0.0, 8.0);
			Grid grid2 = grid;
			double[] array = new double[5] { 28.0, 66.0, 100.0, 0.0, 26.0 };
			foreach (double num in array)
			{
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = ((num == 0.0) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(num))
				});
			}
			string text = ((entry.Target != null && entry.Target != entry.Square) ? entry.Target : entry.Square);
			Zone zone = Zones.ByCode(entry.Retry ? (entry.Target ?? "") : text);
			WarFamily warFamily = ((zone != null && war != null) ? war.OwnerOf(zone) : null);
			object obj;
			if (warFamily == null)
			{
				Border border = new Border();
				border.Width = 20.0;
				border.Height = 20.0;
				obj = border;
			}
			else
			{
				obj = FamBadge(warFamily, 20.0);
			}
			FrameworkElement frameworkElement = (FrameworkElement)obj;
			frameworkElement.VerticalAlignment = VerticalAlignment.Center;
			frameworkElement.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
			if (warFamily != null)
			{
				frameworkElement.ToolTip = warFamily.Title + ((ZoneTypes.Of(zone.Sub) != null) ? ("\n" + ZoneTypes.Describe(zone.Sub)) : "");
			}
			grid2.Children.Add(frameworkElement);
			StackPanel stackPanel = new StackPanel();
			stackPanel.VerticalAlignment = VerticalAlignment.Center;
			StackPanel stackPanel2 = stackPanel;
			stackPanel2.Children.Add(new TextBlock
			{
				Text = (entry.Retry ? (entry.Target ?? "?") : text),
				Style = St("Num"),
				FontSize = 20.0
			});
			if (!entry.Retry && entry.Target != null && entry.Target != entry.Square)
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = "вместо " + entry.Square,
					FontSize = 11.5,
					Foreground = Fog
				});
			}
			if (entry.Retry)
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = "перебиваем",
					FontSize = 11.5,
					Foreground = WarnB
				});
			}
			Grid.SetColumn(stackPanel2, 1);
			grid2.Children.Add(stackPanel2);
			StackPanel stackPanel3 = new StackPanel();
			stackPanel3.VerticalAlignment = VerticalAlignment.Center;
			StackPanel stackPanel4 = stackPanel3;
			stackPanel4.Children.Add(new TextBlock
			{
				Text = "капт " + (entry.CaptNow ?? entry.Capt),
				FontSize = 13.5,
				Foreground = ((entry.CaptNow != null && entry.CaptNow != entry.Capt) ? WarnB : Chalk)
			});
			DateTime utc = PlanFireUtc(entry);
			stackPanel4.Children.Add(new TextBlock
			{
				Text = "нажать " + ((entry.FireUtc != DateTime.MinValue) ? Clock.Msk(entry.FireUtc).ToString("HH:mm:ss") : entry.Fire),
				FontSize = 12.0,
				Foreground = Fog
			});
			Grid.SetColumn(stackPanel4, 2);
			grid2.Children.Add(stackPanel4);
			string text2 = entry.Status ?? (planRunning ? ("ждёт, через " + PlanLeft(utc)) : "");
			System.Windows.Media.Brush foreground = ((entry.State == "done" && entry.Confirm == "accepted") ? GoB : ((entry.State == "running" || entry.State == "confirm") ? SignalB : ((entry.State == "outbid" || entry.State == "failed") ? StopB : ((entry.State == "blocked" || entry.State == "skipped") ? WarnB : Fog))));
			TextBlock textBlock = new TextBlock();
			textBlock.Text = text2;
			textBlock.FontSize = 12.5;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.TextAlignment = TextAlignment.Right;
			textBlock.TextWrapping = TextWrapping.Wrap;
			textBlock.Foreground = foreground;
			TextBlock element = textBlock;
			Grid.SetColumn(element, 3);
			grid2.Children.Add(element);
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = Icons.Make("close", 13.0, Fog);
			button.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
			button.VerticalAlignment = VerticalAlignment.Center;
			button.ToolTip = "Убрать из плана";
			button.IsEnabled = !planRunning || it != planCurrent;
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				pcfg.Entries.Remove(it);
				SavePlanCfg();
				RenderPlan();
			};
			Grid.SetColumn(button2, 4);
			grid2.Children.Add(button2);
			planRows.Children.Add(grid2);
		}
		if (pcfg.Entries.Count == 0)
		{
			planRows.Children.Add(new TextBlock
			{
				Style = St("Hint"),
				Foreground = Dim,
				Text = "Добавьте квадраты ниже: TTC забьёт их по очереди, каждый в своё время, и будет следить за войной."
			});
		}
		bool flag = PlanLinked || demo;
		Icons.Set(planGo, planRunning ? "stop" : "play", planRunning ? "Остановить план" : "Запустить план");
		planGo.Style = St(planRunning ? "StopBtn" : "Primary");
		planGo.IsEnabled = planRunning || (pcfg.Entries.Count > 0 && flag);
		planGo.ToolTip = ((!flag && !planRunning) ? "План работает только с подключённым ботом семьи в Discord" : null);
		PlanEntry planEntry = pcfg.Entries.FirstOrDefault((PlanEntry e) => e.State == null || e.State == "blocked" || e.State == "running" || e.State == "confirm");
		planStatusText.Text = ((!flag) ? "Нужен бот семьи" : ((!planRunning) ? (pcfg.Smart ? "Умная цель включена" : "Умная цель выключена") : ((planEntry == null) ? "План идёт" : ((planEntry.State == "blocked") ? "Ждёт конца нашего капта" : ((planEntry.State == "running" || planEntry.State == "confirm") ? ("Бью " + (planEntry.Target ?? planEntry.Square)) : ("Следующая " + planEntry.Square + " через " + PlanLeft(PlanFireUtc(planEntry))))))));
		planStatusDot.Fill = ((!flag) ? WarnB : (planRunning ? SignalB : (pcfg.Smart ? GoB : Fog)));
		PushPlanState(force: false);
		RenderDecision();
	}

	private void StartPlan()
	{
		if (running)
		{
			Log("Сейчас идёт забив или калибровка — план можно запустить после него.");
			return;
		}
		if (!PlanLinked)
		{
			Log("План работает только с подключённым ботом семьи в Discord (вкладка «Списки»).");
			return;
		}
		pcfg.Entries.RemoveAll((PlanEntry e) => e.Retry);
		if (pcfg.Entries.Count == 0 || !LicenseGate())
		{
			return;
		}
		DateTime dateTime = Clock.UtcNow();
		foreach (PlanEntry entry in pcfg.Entries)
		{
			entry.State = null;
			entry.Status = null;
			entry.Target = null;
			entry.CaptNow = null;
			entry.Attempt = 0;
			entry.Tried = new List<string>();
			entry.Blocked = false;
			entry.Confirm = null;
			entry.ConfirmDetail = null;
			entry.FireUtc = DateTime.MinValue;
			DateTime dateTime2 = PlanFireUtc(entry);
			if ((dateTime2 - dateTime).TotalHours > 20.0)
			{
				entry.State = "skipped";
				entry.Status = "время прошло";
			}
			else
			{
				entry.FireUtc = dateTime2;
			}
		}
		pcfg.Entries = pcfg.Entries.OrderBy((PlanEntry e) => (!(e.FireUtc == DateTime.MinValue)) ? e.FireUtc : DateTime.MaxValue).ToList();
		planRunning = true;
		planCurrent = null;
		planSawRun = false;
		Log("План запущен: " + string.Join(", ", (from e in pcfg.Entries
			where e.State == null
			select e.Square + " в " + e.Fire).ToArray()) + "." + (pcfg.Smart ? (" Умная цель: меньше " + pcfg.MinPeople + " человек в списке — другая цель.") : "") + (pcfg.Rebid ? " Перебили — бью следующую цель." : ""));
		RenderPlan();
		PushPlanState(force: true);
	}

	private void StopPlan(string why)
	{
		planRunning = false;
		if (planCurrent != null && running && runner != null)
		{
			runner.Stop = true;
		}
		planCurrent = null;
		foreach (PlanEntry entry in pcfg.Entries)
		{
			if (entry.State == null || entry.State == "running" || entry.State == "blocked" || entry.State == "confirm")
			{
				if (entry.State != null)
				{
					entry.Status = "остановлено";
				}
				entry.State = ((entry.State == null) ? null : "stopped");
			}
		}
		Log(why);
		RenderPlan();
		PushPlanState(force: true);
	}

	private void PlanTick()
	{
		UpdatePlanLock();
		QueuePriority();
		if (!planRunning)
		{
			if (planOn && (DateTime.UtcNow - planLoadedAt).TotalSeconds > 60.0)
			{
				RefreshPlanData(force: false);
			}
			return;
		}
		if (running)
		{
			if (planCurrent != null)
			{
				planSawRun = true;
			}
			RenderPlanLight();
			return;
		}
		if (planCurrent != null && !FinishPlanEntry())
		{
			RenderPlanLight();
			return;
		}
		PlanEntry planEntry = pcfg.Entries.FirstOrDefault((PlanEntry e) => e.State == null || e.State == "blocked");
		if (planEntry == null)
		{
			StopPlan("План выполнен.");
			return;
		}
		if (!PlanLinked)
		{
			StopPlan("Бот семьи в Discord отключён — план остановлен (без списка не понять, сколько людей придёт).");
			return;
		}
		DateTime dateTime = Clock.UtcNow();
		if (planEntry.FireUtc == DateTime.MinValue)
		{
			planEntry.FireUtc = PlanFireUtc(planEntry);
		}
		if ((planEntry.FireUtc - dateTime).TotalSeconds > (double)(cfg.PrepSec + 15))
		{
			RenderPlanLight();
			return;
		}
		string text = AttacksGone();
		if (text != null)
		{
			planEntry.State = "skipped";
			planEntry.Status = text;
			Log("План: " + planEntry.Square + " — " + text + ".");
			RenderPlan();
			return;
		}
		string text2 = OurCaptAt(Math.Max(Unix(planEntry.FireUtc), Unix(dateTime)));
		if (text2 != null)
		{
			if (planEntry.State != "blocked")
			{
				Log("План: " + (planEntry.Target ?? planEntry.Square) + " — " + text2 + ". Нажму сразу после конца нашего капта.");
			}
			planEntry.State = "blocked";
			planEntry.Blocked = true;
			planEntry.Status = "ждёт: " + text2;
			RenderPlan();
			return;
		}
		DateTime dateTime2 = dateTime.AddSeconds(cfg.PrepSec + 5);
		if (planEntry.FireUtc < dateTime2)
		{
			if ((dateTime2 - planEntry.FireUtc).TotalMinutes > 30.0 && !planEntry.Retry)
			{
				planEntry.State = "skipped";
				planEntry.Status = "опоздали больше чем на " + 30 + " мин";
				RenderPlan();
				return;
			}
			ShiftPlanEntry(planEntry, dateTime2);
			Log("План: " + (planEntry.Target ?? planEntry.Square) + " — нажму сразу, в " + Clock.Fmt(planEntry.FireUtc) + ", капт на " + planEntry.CaptNow + (planEntry.Blocked ? " (наш капт кончился)." : "."));
		}
		planEntry.Blocked = false;
		if (Clock.ParseTime(planEntry.CaptNow ?? planEntry.Capt, out var t) && !CaptTimeOk(t))
		{
			planEntry.State = "skipped";
			planEntry.Status = "капты — до 21:59: на " + (planEntry.CaptNow ?? planEntry.Capt) + " уже нельзя";
			Log("План: " + (planEntry.Target ?? planEntry.Square) + " — капт на " + (planEntry.CaptNow ?? planEntry.Capt) + " не поставить: капты идут с 15:00 до 21:59 МСК.");
			RenderPlan();
		}
		else if (Clock.Msk(planEntry.FireUtc).TimeOfDay > CaptTo)
		{
			planEntry.State = "skipped";
			planEntry.Status = "после 21:59 забить нельзя";
			Log("План: " + (planEntry.Target ?? planEntry.Square) + " — нажать пришлось бы в " + Clock.Fmt(planEntry.FireUtc) + ", а после 21:59:59 капты не ставятся.");
			RenderPlan();
		}
		else
		{
			StartPlanEntry(planEntry);
		}
	}

	private bool FinishPlanEntry()
	{
		PlanEntry planEntry = planCurrent;
		if (!planSawRun)
		{
			planEntry.State = "failed";
			planEntry.Status = "не запустился";
			planCurrent = null;
			RenderPlan();
			return true;
		}
		if (statusKind == "stop")
		{
			planEntry.State = "stopped";
			planEntry.Status = "остановлено";
			StopPlan("План остановлен вместе с забивом.");
			return false;
		}
		if (planEntry.EndedUtc == DateTime.MinValue)
		{
			planEntry.EndedUtc = DateTime.UtcNow;
		}
		if (planJob != null && planJob.Cancel)
		{
			planEntry.State = "skipped";
			planEntry.Status = planEntry.Status ?? "некуда бить";
			planCurrent = null;
			RenderPlan();
			return true;
		}
		if (statusKind == "error")
		{
			planEntry.State = "failed";
			planEntry.Status = "ошибка забива, подробности в журнале";
			planCurrent = null;
			RenderPlan();
			return true;
		}
		if (statusKind == "testok" || dryRun)
		{
			planEntry.State = "done";
			planEntry.Status = "тест";
			planCurrent = null;
			RenderPlan();
			return true;
		}
		if (planEntry.Confirm == null && (DateTime.UtcNow - planEntry.EndedUtc).TotalSeconds < 9.0)
		{
			planEntry.State = "confirm";
			planEntry.Status = "жду итога из игры";
			return false;
		}
		planCurrent = null;
		if (planEntry.Confirm == "accepted")
		{
			planEntry.State = "done";
			planEntry.Status = "принят" + (string.IsNullOrEmpty(planEntry.ConfirmDetail) ? "" : (", капт в " + planEntry.ConfirmDetail));
		}
		else if (planEntry.Confirm == "outbid")
		{
			planEntry.State = "outbid";
			planEntry.Status = "перебили: " + planEntry.ConfirmDetail;
			string text = ((planJob != null) ? planJob.Square : (planEntry.Target ?? planEntry.Square));
			if (!planEntry.Tried.Contains(text))
			{
				planEntry.Tried.Add(text);
			}
			if (pcfg.Rebid && planEntry.Attempt < 2)
			{
				PlanEntry planEntry2 = new PlanEntry();
				planEntry2.Square = planEntry.Square;
				planEntry2.Capt = planEntry.CaptNow ?? planEntry.Capt;
				planEntry2.Fire = planEntry.Fire;
				planEntry2.CaptNow = planEntry.CaptNow ?? planEntry.Capt;
				planEntry2.Retry = true;
				planEntry2.Attempt = planEntry.Attempt + 1;
				planEntry2.Tried = new List<string>(planEntry.Tried);
				planEntry2.FireUtc = planEntry.FireUtc;
				PlanEntry planEntry3 = planEntry2;
				ShiftPlanEntry(planEntry3, Clock.UtcNow().AddSeconds(cfg.PrepSec + 5));
				pcfg.Entries.Insert(pcfg.Entries.IndexOf(planEntry) + 1, planEntry3);
				Log("План: " + text + " перебили (" + planEntry.ConfirmDetail + ") — перебиваю: сразу бью следующую цель с лучшим шансом, кроме " + string.Join(", ", planEntry3.Tried.ToArray()) + ".");
			}
			else if (pcfg.Rebid)
			{
				Log("План: " + text + " перебили — повторов больше не делаю (уже " + 2 + ").");
			}
		}
		else
		{
			planEntry.State = "done";
			planEntry.Status = "нажато, игра не подтвердила" + (string.IsNullOrEmpty(planEntry.ConfirmDetail) ? "" : (" (" + planEntry.ConfirmDetail + ")"));
		}
		RenderPlan();
		PushPlanState(force: true);
		return true;
	}

	private void ShiftPlanEntry(PlanEntry e, DateTime at)
	{
		DateTime dateTime = ((e.FireUtc != DateTime.MinValue) ? e.FireUtc : PlanFireUtc(e));
		if (dateTime == DateTime.MaxValue)
		{
			dateTime = at;
		}
		if (!Clock.ParseTime(e.CaptNow ?? e.Capt, out var t))
		{
			t = new TimeSpan(cfg.CaptH, cfg.CaptM, 0);
		}
		DateTime dateTime2 = Clock.Msk(dateTime);
		DateTime dateTime3 = dateTime2.Date + t;
		if (dateTime3 < dateTime2.AddHours(-1.0))
		{
			dateTime3 = dateTime3.AddDays(1.0);
		}
		double value = Math.Max(0.0, (at - dateTime).TotalMinutes);
		DateTime dateTime4 = dateTime3.AddMinutes(value);
		DateTime dateTime5 = Clock.Msk(at);
		if (dateTime4 < dateTime5)
		{
			dateTime4 = dateTime5;
		}
		int num = (5 - dateTime4.Minute % 5) % 5;
		if (dateTime4.Second > 0 && num == 0 && dateTime4.Minute % 5 == 0 && dateTime4 > dateTime3)
		{
			num = 5;
		}
		e.CaptNow = new DateTime(dateTime4.Year, dateTime4.Month, dateTime4.Day, dateTime4.Hour, dateTime4.Minute, 0).AddMinutes(num).ToString("HH:mm");
		e.FireUtc = at;
	}

	private void StartPlanEntry(PlanEntry e)
	{
		if (!Clock.ParseTime(e.CaptNow ?? e.Capt, out var t))
		{
			e.State = "failed";
			e.Status = "неверное время капта";
			RenderPlan();
			return;
		}
		if (cfg.AfterCapt)
		{
			SetSnipeMode(after: false);
			Log("План: забив ко времени (режим «как только можно» выключен).");
		}
		string text = (e.Retry ? (e.Tried.FirstOrDefault() ?? e.Square) : e.Square);
		SquareBox.Text = text;
		SetCapt(t.Hours, t.Minutes);
		FireBox.Text = Clock.Msk(e.FireUtc).ToString("HH:mm:ss.fff");
		planCurrent = e;
		e.State = "running";
		e.Status = (e.Retry ? "перебиваю" : "забиваю");
		e.Target = (e.Retry ? null : text);
		e.Confirm = null;
		e.ConfirmDetail = null;
		e.EndedUtc = DateTime.MinValue;
		planSawRun = false;
		planJob = null;
		Log("План: " + (e.Retry ? ("перебиваю (попытка " + (e.Attempt + 1) + ")") : text) + ", капт на " + (e.CaptNow ?? e.Capt) + ", нажать в " + Clock.Fmt(e.FireUtc) + ".");
		Start(dry: false);
		if (running)
		{
			planSawRun = true;
		}
		RenderPlan();
		PushPlanState(force: true);
	}

	private void RenderPlanLight()
	{
		if (planOn)
		{
			RenderPlan();
		}
	}

	private static long Unix(DateTime utc)
	{
		return (long)(utc - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	private string OurCaptAt(long t)
	{
		string text = Zones.Key((!string.IsNullOrEmpty(NetWatch.OwnFamily)) ? NetWatch.OwnFamily : cfg.Family);
		if (text.Length == 0 || war == null)
		{
			return null;
		}
		foreach (WarCapt capt in war.Capts)
		{
			bool flag = Zones.Key(capt.A) == text;
			bool flag2 = Zones.Key(capt.D) == text;
			if ((flag || flag2) && (capt.Winner == null || !capt.ExactEnd || capt.EndHi > t) && capt.Start - 5 <= t && t < capt.EndHi)
			{
				return (flag2 ? "у нас защита" : "у нас атака") + (string.IsNullOrEmpty(capt.Square) ? "" : (" на " + capt.Square)) + " до " + (capt.ExactEnd ? War.Hm(capt.EndHi) : ("≈" + War.Hm(capt.EndLo) + "–" + War.Hm(capt.EndHi)));
			}
		}
		WarFamily warFamily = war.Family(text);
		if (warFamily != null && warFamily.InCaptEnd != null && warFamily.InCaptEnd[1] > t)
		{
			return ((warFamily.InCaptRole == "d") ? "у нас защита" : "у нас капт") + " до ≈" + War.Hm(warFamily.InCaptEnd[0]) + "–" + War.Hm(warFamily.InCaptEnd[1]);
		}
		return null;
	}

	private string AttacksGone()
	{
		if (war == null || string.IsNullOrEmpty(cfg.Family))
		{
			return null;
		}
		long[] array = war.IngameOf(cfg.Family);
		if (array != null && array[1] > 0 && array[0] >= array[1] && War.NowUnix() - array[4] < 10800)
		{
			return "атаки на сегодня кончились (" + array[0] + "/" + array[1] + " по планшету)";
		}
		WarFamily warFamily = war.Family(cfg.Family);
		if (warFamily != null && warFamily.AtkMax > 0 && warFamily.AtkLeft == 0)
		{
			return "атаки на сегодня кончились (" + warFamily.AtkUsed + " из " + warFamily.AtkMax + ")";
		}
		return null;
	}

	private void PlanForStrike(Job j)
	{
		if (j.Prepare || cfg.AfterCapt)
		{
			return;
		}
		j.Server = cfg.Server;
		PlanEntry pe = (planRunning ? planCurrent : null);
		if (pe != null)
		{
			planJob = j;
			j.Confirm = delegate(string st, string d)
			{
				UiDo(delegate
				{
					pe.Confirm = st;
					pe.ConfirmDetail = d;
				});
			};
			List<string> tried = new List<string>(pe.Tried);
			if (pe.Retry)
			{
				j.BeforePrep = delegate(Job jj)
				{
					SmartTarget(jj, force: true, tried, pe);
				};
				return;
			}
		}
		if (pcfg.Smart)
		{
			j.BeforePrep = delegate(Job jj)
			{
				SmartTarget(jj, force: false, null, pe);
			};
			Log("Умная цель включена: перед подготовкой посчитаю людей в списке из Discord (порог " + pcfg.MinPeople + ").");
		}
	}

	private static long CaptUnix(Job j)
	{
		DateTime utc = ((j.FireUtc == DateTime.MinValue) ? Clock.UtcNow() : j.FireUtc);
		DateTime dateTime = Clock.Msk(utc);
		DateTime dateTime2 = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, j.CaptH, j.CaptM, 0);
		if (dateTime2 < dateTime)
		{
			dateTime2 = dateTime2.AddDays(1.0);
		}
		return (long)(DateTime.SpecifyKind(dateTime2.AddHours(-3.0), DateTimeKind.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	private void SmartTarget(Job j, bool force, List<string> exclude, PlanEntry pe)
	{
		Action<string> action = delegate(string m)
		{
			UiDo(delegate
			{
				Log(m);
			});
		};
		RosterInfo rosterInfo = Roster.Fresh(Roster.Last, 3000);
		int minPeople = pcfg.MinPeople;
		int num = ((rosterInfo != null && rosterInfo.Linked) ? rosterInfo.Entries.Count : (-1));
		if (!force)
		{
			if (num < 0)
			{
				action("Умная цель: бот семьи не подключён (вкладка «Списки») — бью " + j.Square + ".");
				return;
			}
			if (num >= minPeople)
			{
				action("Умная цель: записалось " + num + " из " + minPeople + " — бью основную цель " + j.Square + ".");
				return;
			}
		}
		if (string.IsNullOrEmpty(j.Server))
		{
			action("Умная цель: не выбран сервер вверху окна — бью " + j.Square + ".");
			if (force)
			{
				j.Cancel = true;
			}
			return;
		}
		string error;
		PlanSuggest s = WarPlan.Suggest(j.Server, j.Family, Math.Max(0, num), j.Square, CaptUnix(j), pcfg.Neutral, exclude, 5000, out error);
		if (s == null)
		{
			action("Умная цель: сайт не ответил (" + error + ")" + (force ? " — перебивать некуда." : (" — бью " + j.Square + ".")));
			if (!force)
			{
				return;
			}
			j.Cancel = true;
			UiDo(delegate
			{
				if (pe != null)
				{
					pe.Status = "сайт не ответил";
				}
			});
			return;
		}
		UiDo(delegate
		{
			planSug = s;
			RenderDecision();
		});
		PlanOption planOption = s.Options.FirstOrDefault((PlanOption o) => !string.IsNullOrEmpty(o.Square) && (exclude == null || !exclude.Contains(o.Square)) && (force || o.Square != j.Square));
		if (force)
		{
			if (planOption == null)
			{
				action("Перебиваю: подходящих целей нет (соседние и у края карты заняты, без защит или в КД).");
				j.Cancel = true;
				UiDo(delegate
				{
					if (pe != null)
					{
						pe.Status = "некуда перебить";
					}
				});
				return;
			}
		}
		else
		{
			double num2 = ((s.Main != null && s.Main.Blocked == null) ? s.Main.Chance : 0.0);
			string text = ((s.Main == null) ? "" : (" (" + (s.Main.Blocked ?? ("шанс " + Pct(s.Main.Chance) + " %")) + ")"));
			if (planOption == null || planOption.Chance < num2 + 0.05)
			{
				action("Умная цель: записалось " + num + " из " + minPeople + ", но лучше основной цели " + j.Square + text + " ничего нет — бью её.");
				return;
			}
			action("Умная цель: записалось " + num + " из " + minPeople + " — вместо " + j.Square + text + " бью " + planOption.Square + ". " + planOption.Reason + ".");
		}
		if (!Runner.ParseSquare(planOption.Square, out var col, out var row))
		{
			if (force)
			{
				j.Cancel = true;
			}
			return;
		}
		if (force)
		{
			action("Перебиваю: бью " + planOption.Square + ". " + planOption.Reason + ".");
		}
		j.Square = planOption.Square;
		j.Col = col;
		j.Row = row;
		string sq = planOption.Square;
		UiDo(delegate
		{
			SquareBox.Text = sq;
			if (pe != null)
			{
				pe.Target = sq;
				RenderPlan();
			}
		});
	}

	private void PushPlanState(bool force)
	{
		if (demo || MapDemo || string.IsNullOrEmpty(License.SavedKey()) || (!force && (DateTime.UtcNow - planPushAt).TotalSeconds < 20.0))
		{
			return;
		}
		List<object> list = new List<object>();
		foreach (PlanEntry entry in pcfg.Entries)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["square"] = entry.Square;
			dictionary["capt"] = entry.CaptNow ?? entry.Capt;
			dictionary["fire_at"] = ((entry.FireUtc != DateTime.MinValue) ? Unix(entry.FireUtc) : Unix((PlanFireUtc(entry) == DateTime.MaxValue) ? Clock.UtcNow() : PlanFireUtc(entry)));
			dictionary["target"] = entry.Target;
			dictionary["state"] = entry.State ?? "waiting";
			dictionary["status"] = entry.Status;
			dictionary["retry"] = entry.Retry;
			dictionary["attempt"] = entry.Attempt;
			dictionary["confirm"] = entry.Confirm;
			dictionary["by"] = ((entry.Confirm == "outbid") ? entry.ConfirmDetail : null);
			list.Add(dictionary);
		}
		Dictionary<string, object> plan = new Dictionary<string, object>();
		plan["running"] = planRunning;
		plan["smart"] = pcfg.Smart;
		plan["min_people"] = pcfg.MinPeople;
		plan["rebid"] = pcfg.Rebid;
		plan["server"] = cfg.Server ?? "";
		plan["family"] = cfg.Family ?? "";
		plan["entries"] = list;
		string text = json.Serialize(plan);
		if (force || !(text == planPushed))
		{
			planPushed = text;
			planPushAt = DateTime.UtcNow;
			ThreadPool.QueueUserWorkItem(delegate
			{
				WarPlan.PushPlan(plan, out var _);
			});
		}
	}

	private void RefreshPlanData(bool force)
	{
		if (planLoading || demo || MapDemo)
		{
			return;
		}
		if (!force && (DateTime.UtcNow - planLoadedAt).TotalSeconds < 50.0)
		{
			RenderDecision();
			return;
		}
		string server = cfg.Server;
		string family = cfg.Family;
		RosterInfo last = Roster.Last;
		int i = ((last != null && last.Linked) ? last.Entries.Count : (-1));
		PlanEntry planEntry = pcfg.Entries.FirstOrDefault((PlanEntry e) => e.State == null || e.State == "blocked" || e.State == "running");
		string square = ((planEntry != null) ? (planEntry.Target ?? planEntry.Square) : (SquareBox.Text ?? "").Trim().ToUpperInvariant());
		long captAt = 0L;
		if (planEntry != null && Clock.ParseTime(planEntry.CaptNow ?? planEntry.Capt, out var t))
		{
			captAt = CaptUnix(new Job
			{
				FireUtc = PlanFireUtc(planEntry),
				CaptH = t.Hours,
				CaptM = t.Minutes
			});
		}
		if (string.IsNullOrEmpty(server))
		{
			planError = "Выберите сервер вверху окна.";
			RenderDecision();
			return;
		}
		planLoading = true;
		bool neutral = pcfg.Neutral;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string e3 = null;
			int capts;
			int med;
			string e2;
			List<PlanFamily> fams = WarPlan.Power(server, out capts, out med, out e2);
			PlanSuggest sug = ((i >= 0) ? WarPlan.Suggest(server, family, i, square, captAt, neutral, 15000, out e3) : null);
			UiDo(delegate
			{
				planLoading = false;
				planLoadedAt = DateTime.UtcNow;
				if (fams != null)
				{
					planFams = fams;
					planCapts = capts;
					planMedian = med;
				}
				planSug = sug ?? ((i < 0) ? null : planSug);
				planError = ((fams == null) ? ("Сила семей не загрузилась: " + e2) : ((sug == null && i >= 0) ? ("Выбор цели не загрузился: " + e3) : null));
				RenderPlanFamilies();
				RenderDecision();
			});
		});
	}

	private void RenderDecision()
	{
		if (planPeople == null)
		{
			return;
		}
		RosterInfo last = Roster.Last;
		bool flag = last?.Linked ?? false;
		int num = (flag ? last.Entries.Count : (-1));
		int minPeople = pcfg.MinPeople;
		planPeople.Text = ((num >= 0) ? num.ToString() : "—");
		planPeople.Foreground = ((num < 0) ? Dim : ((num >= minPeople) ? GoB : WarnB));
		planPeopleSub.Text = ((num >= 0) ? ("из " + minPeople + ((num >= minPeople) ? ", хватает на основную цель" : ", меньше порога")) : "бот семьи не подключён");
		SetReady(planRosterDot, planRosterText, flag ? GoB : WarnB, flag ? ("Бот семьи: " + num + " " + People(num) + " в списке") : "Бот семьи не подключён");
		SetReady(planDataDot, planDataText, (planFams != null) ? GoB : ((planError != null) ? StopB : Dim), (planFams != null) ? ("Каптов сервера за 30 дней: " + planCapts) : ((planError != null) ? "Данные сервера не загрузились" : "Каптов сервера"));
		PlanEntry planEntry = pcfg.Entries.FirstOrDefault((PlanEntry e) => e.State == "running" || e.State == "confirm" || e.State == "blocked" || (planRunning && e.State == null));
		planNow.Text = ((!planRunning || planEntry == null) ? "" : ((planEntry.State == "blocked") ? ("Ждёт: " + planEntry.Status.Replace("ждёт: ", "") + ". Нажму сразу после конца, капт сдвину.") : ((planEntry.State == "running") ? ("Бью " + (planEntry.Target ?? (planEntry.Retry ? "следующую цель" : planEntry.Square)) + ", нажатие в " + Clock.Fmt(planEntry.FireUtc) + ".") : ((planEntry.State == "confirm") ? ("Жду итога из игры по " + (planEntry.Target ?? planEntry.Square) + "…") : ("Следующая цель " + planEntry.Square + ": нажатие через " + PlanLeft(PlanFireUtc(planEntry)) + ", капт на " + (planEntry.CaptNow ?? planEntry.Capt) + ".")))));
		planNow.Visibility = ((planNow.Text.Length <= 0) ? Visibility.Collapsed : Visibility.Visible);
		planOptRows.Children.Clear();
		if (!flag)
		{
			planMainText.Text = "";
			planBest.Text = "";
			return;
		}
		PlanSuggest s = planSug;
		if (planError != null && s == null)
		{
			planMainText.Text = planError;
			planMainText.Foreground = StopB;
			planBest.Text = "";
			return;
		}
		if (s == null)
		{
			planMainText.Text = (planLoading ? "Считаю шансы…" : "");
			planMainText.Foreground = Fog;
			planBest.Text = "";
			return;
		}
		if (s.Main != null && !string.IsNullOrEmpty(s.Main.Square))
		{
			planMainText.Text = "Основная цель " + s.Main.Square + ((s.Main.Family != null) ? (", семья " + s.Main.Family + ((s.Main.TheirN > 0.0) ? (" (выводит около " + s.Main.TheirN.ToString("0") + ")") : "")) : "") + ": " + ((s.Main.Blocked != null) ? ("нельзя — " + s.Main.Blocked) : ("шанс " + Pct(s.Main.Chance) + " % при " + s.People + " " + People(s.People))) + ".";
			planMainText.Foreground = ((s.Main.Blocked != null) ? WarnB : Chalk);
		}
		else
		{
			planMainText.Text = "Основная цель не выбрана: шансы ниже — для " + s.People + " " + People(s.People) + ".";
			planMainText.Foreground = Fog;
		}
		double num2 = ((s.Main != null && s.Main.Blocked == null) ? s.Main.Chance : 0.0);
		PlanOption planOption = s.Options.FirstOrDefault((PlanOption o) => s.Main == null || o.Square != s.Main.Square);
		if (planOption != null && planOption.Chance >= num2 + 0.05)
		{
			planBest.Text = ((num < minPeople && pcfg.Smart) ? "Бью вместо неё " : "Лучше по шансу: ") + planOption.Square + ((planOption.Family != null) ? (" — " + planOption.Family) : " — ничья зона") + ", шанс " + Pct(planOption.Chance) + " %." + ((num >= minPeople) ? " Сейчас людей хватает — умная цель не сработает." : ((!pcfg.Smart) ? " Умная цель выключена." : ""));
			planBest.Foreground = ((num < minPeople && pcfg.Smart) ? GoB : Fog);
		}
		else
		{
			planBest.Text = "Лучше основной цели вариантов нет.";
			planBest.Foreground = Fog;
		}
		foreach (PlanOption item in s.Options.Take(4))
		{
			Grid grid = new Grid();
			grid.Margin = new Thickness(0.0, 0.0, 0.0, 4.0);
			grid.ToolTip = item.Reason;
			Grid grid2 = grid;
			double[] array = new double[4] { 64.0, 0.0, 130.0, 60.0 };
			foreach (double num3 in array)
			{
				grid2.ColumnDefinitions.Add(new ColumnDefinition
				{
					Width = ((num3 == 0.0) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(num3))
				});
			}
			grid2.Children.Add(new TextBlock
			{
				Text = item.Square,
				Style = St("Num"),
				FontSize = 16.0
			});
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel.VerticalAlignment = VerticalAlignment.Center;
			StackPanel stackPanel2 = stackPanel;
			if (item.Family != null)
			{
				FrameworkElement frameworkElement = PlanBadge(item.Family, item.FamilyId, 18.0);
				frameworkElement.Margin = new Thickness(0.0, 0.0, 8.0, 0.0);
				stackPanel2.Children.Add(frameworkElement);
			}
			stackPanel2.Children.Add(new TextBlock
			{
				Text = (item.Family ?? "ничья зона") + ((item.Type != null) ? "  " : ""),
				FontSize = 13.5,
				Foreground = Chalk,
				TextTrimming = TextTrimming.CharacterEllipsis,
				VerticalAlignment = VerticalAlignment.Center
			});
			if (item.Type != null)
			{
				stackPanel2.Children.Add(new TextBlock
				{
					Text = item.Type,
					FontSize = 12.0,
					Foreground = WarnB,
					VerticalAlignment = VerticalAlignment.Center,
					TextTrimming = TextTrimming.CharacterEllipsis
				});
			}
			Grid.SetColumn(stackPanel2, 1);
			grid2.Children.Add(stackPanel2);
			string text = ((item.Reason != null && item.Reason.EndsWith("у края карты")) ? ", у края" : ((item.Reason != null && item.Reason.EndsWith("граничит с нашей территорией")) ? ", рядом" : ((item.Reason != null && item.Reason.Contains("наших квадратов")) ? ", 7+ вокруг" : "")));
			TextBlock textBlock = new TextBlock();
			textBlock.Text = ((item.TheirN > 0.0) ? ("около " + item.TheirN.ToString("0")) : "без защиты") + text;
			textBlock.FontSize = 12.5;
			textBlock.Foreground = Fog;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.TextAlignment = TextAlignment.Right;
			textBlock.ToolTip = ((item.TheirN > 0.0) ? "Столько людей семья обычно выводит в защите" : null);
			TextBlock element = textBlock;
			Grid.SetColumn(element, 2);
			grid2.Children.Add(element);
			TextBlock textBlock2 = new TextBlock();
			textBlock2.Text = Pct(item.Chance) + " %";
			textBlock2.Style = St("Num");
			textBlock2.FontSize = 16.0;
			textBlock2.TextAlignment = TextAlignment.Right;
			textBlock2.Foreground = ((item.Confident || item.Family == null) ? Chalk : Fog);
			TextBlock element2 = textBlock2;
			Grid.SetColumn(element2, 3);
			grid2.Children.Add(element2);
			planOptRows.Children.Add(grid2);
		}
	}

	private void RenderPlanFamilies()
	{
		if (planFamRows == null)
		{
			return;
		}
		planFamRows.Children.Clear();
		planFamHead.Text = ((planFams != null) ? ("по " + planCapts + " каптам за 30 дней, обычно выходит " + planMedian) : "");
		if (planFams == null)
		{
			return;
		}
		string text = Zones.Key(cfg.Family);
		foreach (PlanFamily item in planFams.Where((PlanFamily x) => x.Tier != null).Take(60))
		{
			string text2 = ((item.Tier == "strong") ? "сильная" : ((item.Tier == "weak") ? "слабая" : "средняя"));
			System.Windows.Media.Brush tierColor = ((item.Tier == "strong") ? WarnB : ((item.Tier == "weak") ? GoB : Fog));
			int num = item.DefW + item.DefL;
			Grid grid = FamRow(new string[5]
			{
				item.Name,
				item.DefN.ToString("0"),
				(num > 0) ? (item.DefW + " из " + num) : "—",
				item.Capts7d.ToString(),
				text2
			}, head: false, (Zones.Key(item.Name) == text) ? SignalB : null, tierColor, PlanBadge(item.Name, item.Id, 18.0));
			grid.ToolTip = item.Name + ": зон " + item.Zones + (item.Mansion ? ", особняк" : "") + ". В защите выводит около " + item.DefN.ToString("0") + " (каптов " + item.DefSamples + "), в атаке — около " + item.AtkN.ToString("0") + " (каптов " + item.AtkSamples + "). Атаки: взяла " + item.AtkW + " из " + (item.AtkW + item.AtkL) + ".";
			planFamRows.Children.Add(grid);
		}
		int num2 = planFams.Count((PlanFamily x) => x.Tier == null);
		if (num2 > 0)
		{
			planFamRows.Children.Add(new TextBlock
			{
				Style = St("Hint"),
				Foreground = Dim,
				Margin = new Thickness(0.0, 6.0, 0.0, 0.0),
				Text = "Ещё " + num2 + " семей с зонами, но без каптов за 30 дней — их силу не оценить."
			});
		}
	}

	private void InitIcons()
	{
		SetIcon(F<System.Windows.Controls.Button>("BtnMin"), "minimize", 13.0);
		SetIcon(F<System.Windows.Controls.Button>("BtnClose"), "close", 13.0);
		string[] array = new string[2] { "CaptHUp", "CaptMUp" };
		foreach (string n in array)
		{
			SetIcon(F<System.Windows.Controls.Button>(n), "chevron-up", 12.0);
		}
		string[] array2 = new string[2] { "CaptHDown", "CaptMDown" };
		foreach (string n2 in array2)
		{
			SetIcon(F<System.Windows.Controls.Button>(n2), "chevron-down", 12.0);
		}
		string[] array3 = new string[3] { "PeopleMinus", "PrepMinus", "FineMinus" };
		foreach (string n3 in array3)
		{
			SetIcon(F<System.Windows.Controls.Button>(n3), "minus", 12.0);
		}
		string[] array4 = new string[3] { "PeoplePlus", "PrepPlus", "FinePlus" };
		foreach (string n4 in array4)
		{
			SetIcon(F<System.Windows.Controls.Button>(n4), "plus", 12.0);
		}
		ReplaceChevron(ServerBtnContent());
		Icons.Set(BtnTest, "eye", "Тест без нажатия");
		Icons.Set(BtnStop, "stop", "Стоп (F10)");
		Icons.Set(BtnStart, cfg.AfterCapt ? "clock" : "strike", cfg.AfterCapt ? "Ждать и забить" : "Запустить забив");
		StackPanel stackPanel = ((modeBtn != null) ? (modeBtn.Content as StackPanel) : null);
		if (stackPanel != null)
		{
			ReplaceChevron(stackPanel);
			modeIconHost = new ContentControl
			{
				Margin = new Thickness(0.0, 0.0, 8.0, 0.0),
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = SignalB,
				Focusable = false
			};
			stackPanel.Children.Insert(stackPanel.Children.IndexOf(modeText), modeIconHost);
			DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock)).AddValueChanged(modeText, delegate
			{
				UpdateModeIcon();
			});
			UpdateModeIcon();
		}
		if (listGoBtn != null)
		{
			Icons.Set(listGoBtn, "users", "Собрать в игре");
		}
	}

	private static string ModeIcon(string mode)
	{
		return mode switch
		{
			"Списки" => "lists", 
			"Забив ВЗЗ" => "vzz", 
			"План" => "plan", 
			"Приоритет" => "priority", 
			_ => "strike", 
		};
	}

	private void UpdateModeIcon()
	{
		if (modeIconHost is ContentControl contentControl)
		{
			contentControl.Content = Icons.Make(ModeIcon(modeText.Text), 15.0);
		}
	}

	private UIElement ModeItem(string icon, string title, string sub, System.Windows.Media.Brush color)
	{
		Grid grid = new Grid();
		grid.MaxWidth = 400.0;
		Grid grid2 = grid;
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(34.0)
		});
		grid2.ColumnDefinitions.Add(new ColumnDefinition
		{
			Width = new GridLength(1.0, GridUnitType.Star)
		});
		Border border = new Border();
		border.Width = 26.0;
		border.Height = 26.0;
		border.CornerRadius = new CornerRadius(6.0);
		border.Background = ((color == SignalB) ? B("#2A2240") : CellBg);
		border.VerticalAlignment = VerticalAlignment.Top;
		border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
		border.Margin = new Thickness(0.0, 1.0, 0.0, 0.0);
		border.Child = Icons.Make(icon, 15.0, (color == SignalB) ? SignalB : Fog);
		Border element = border;
		grid2.Children.Add(element);
		FrameworkElement element2 = (FrameworkElement)MenuItem(title, sub, color);
		Grid.SetColumn(element2, 1);
		grid2.Children.Add(element2);
		return grid2;
	}

	private void SetIcon(System.Windows.Controls.Button b, string icon, double size)
	{
		if (b != null)
		{
			b.Content = Icons.Make(icon, size);
		}
	}

	private System.Windows.Controls.Panel ServerBtnContent()
	{
		if (!(w.FindName("ServerBtn") is System.Windows.Controls.Button button))
		{
			return null;
		}
		return button.Content as System.Windows.Controls.Panel;
	}

	private static void ReplaceChevron(System.Windows.Controls.Panel p)
	{
		if (p != null)
		{
			TextBlock textBlock = p.Children.OfType<TextBlock>().FirstOrDefault((TextBlock t) => t.Text == "▾");
			if (textBlock != null)
			{
				int index = p.Children.IndexOf(textBlock);
				FrameworkElement frameworkElement = Icons.Make("chevron-down", 12.0);
				frameworkElement.Margin = new Thickness(5.0, 1.0, 0.0, 0.0);
				TextElementForeground(frameworkElement, textBlock.Foreground);
				p.Children.RemoveAt(index);
				p.Children.Insert(index, frameworkElement);
			}
		}
	}

	private static void TextElementForeground(FrameworkElement e, System.Windows.Media.Brush b)
	{
		if (b != null)
		{
			TextElement.SetForeground(e, b);
		}
	}

	private void InitTray()
	{
		if (demo || MapDemo)
		{
			return;
		}
		w.Closing += delegate(object s, CancelEventArgs e)
		{
			if (!quitting && cfg.StartWithWindows && tray != null)
			{
				e.Cancel = true;
				HideToTray();
			}
		};
		w.Closed += delegate
		{
			DisposeTray();
		};
		tray = new NotifyIcon
		{
			Text = "TTC — капты из игры на карту TTC"
		};
		try
		{
			using Stream stream = Res("app.ico");
			if (stream != null)
			{
				tray.Icon = new Icon(stream, SystemInformation.SmallIconSize);
			}
		}
		catch
		{
		}
		if (tray.Icon == null)
		{
			tray.Icon = SystemIcons.Application;
		}
		ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
		ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem("Открыть TTC");
		toolStripMenuItem.Font = new Font(toolStripMenuItem.Font, System.Drawing.FontStyle.Bold);
		toolStripMenuItem.Click += delegate
		{
			ShowFromTray();
		};
		ToolStripMenuItem toolStripMenuItem2 = new ToolStripMenuItem("Выход");
		toolStripMenuItem2.Click += delegate
		{
			ExitApp();
		};
		contextMenuStrip.Items.Add(toolStripMenuItem);
		contextMenuStrip.Items.Add(new ToolStripSeparator());
		contextMenuStrip.Items.Add(toolStripMenuItem2);
		tray.ContextMenuStrip = contextMenuStrip;
		tray.MouseClick += delegate(object s, System.Windows.Forms.MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				ShowFromTray();
			}
		};
		tray.Visible = cfg.StartWithWindows;
		w.IsVisibleChanged += delegate
		{
			UpdateTray();
		};
		System.Windows.Controls.Button button = F<System.Windows.Controls.Button>("BtnMin");
		if (button.Parent is System.Windows.Controls.Panel panel)
		{
			System.Windows.Controls.Button gear = new System.Windows.Controls.Button
			{
				Style = St("WinBtn"),
				Content = Icons.Make("sliders", 14.0),
				ToolTip = "Настройки TTC"
			};
			gear.Click += delegate
			{
				ShowSettings(gear);
			};
			panel.Children.Insert(panel.Children.IndexOf(button), gear);
		}
		SingleInstance.OnShow = delegate
		{
			try
			{
				w.Dispatcher.BeginInvoke(new Action(ShowFromTray));
			}
			catch
			{
			}
		};
		if (!cfg.StartWithWindows)
		{
			return;
		}
		string exe = Updater.ExePath;
		ThreadPool.QueueUserWorkItem(delegate
		{
			bool changed = false;
			string err;
			try
			{
				err = LogonTask.Sync(exe, out changed);
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					if (err != null)
					{
						Log("Запуск с Windows: не удалось обновить задачу планировщика: " + err + ".");
					}
					else if (changed)
					{
						Log("Запуск с Windows: задача планировщика теперь запускает " + exe + ".");
					}
				});
			}
			catch
			{
			}
		});
	}

	private void StartHidden()
	{
		if (tray == null)
		{
			return;
		}
		hiddenStart = true;
		w.ShowActivated = false;
		w.ShowInTaskbar = false;
		w.Opacity = 0.0;
		tray.Visible = true;
		EventHandler h = null;
		h = delegate
		{
			w.ContentRendered -= h;
			if (hiddenStart)
			{
				hiddenStart = false;
				w.Hide();
				RestoreLook();
				Log("TTC запущена вместе с Windows и работает в трее: капты из игры уходят на общую карту TTC. Открыть окно — щелчок по значку TTC в трее.");
			}
		};
		w.ContentRendered += h;
	}

	private void RestoreLook()
	{
		w.Opacity = 1.0;
		w.ShowInTaskbar = true;
		w.ShowActivated = true;
	}

	private void UpdateTray()
	{
		if (tray == null)
		{
			return;
		}
		try
		{
			tray.Visible = cfg.StartWithWindows || hiddenStart || !w.IsVisible;
		}
		catch
		{
		}
	}

	private void HideToTray()
	{
		if (tray == null)
		{
			return;
		}
		if (pop != null)
		{
			pop.IsOpen = false;
		}
		foreach (Window ownedWindow in w.OwnedWindows)
		{
			if (ownedWindow.IsVisible)
			{
				ownedWindow.Hide();
				trayHidden.Add(ownedWindow);
			}
		}
		w.Hide();
		Log("Окно TTC спрятано в трей: капты из игры по-прежнему уходят на общую карту TTC. Выход — правой кнопкой по значку TTC в трее.");
		if (cfg.TrayTipShown)
		{
			return;
		}
		cfg.TrayTipShown = true;
		SaveCfg();
		try
		{
			tray.ShowBalloonTip(6000, "TTC работает в трее", "Капты из игры уходят на общую карту TTC. Открыть — щелчок по значку, выйти — правой кнопкой → «Выход».", ToolTipIcon.Info);
		}
		catch
		{
		}
	}

	private void ShowFromTray()
	{
		if (closing || quitting)
		{
			return;
		}
		if (hiddenStart)
		{
			hiddenStart = false;
			RestoreLook();
		}
		if (!w.IsVisible)
		{
			w.Show();
		}
		if (w.WindowState == WindowState.Minimized)
		{
			w.WindowState = WindowState.Normal;
		}
		foreach (Window item in trayHidden)
		{
			try
			{
				item.Show();
			}
			catch
			{
			}
		}
		trayHidden.Clear();
		w.Activate();
		w.Topmost = true;
		w.Topmost = false;
		UpdateTray();
	}

	private void ExitApp()
	{
		quitting = true;
		hiddenStart = false;
		w.Close();
	}

	private void DisposeTray()
	{
		if (tray != null)
		{
			try
			{
				tray.Visible = false;
				tray.Dispose();
			}
			catch
			{
			}
			tray = null;
		}
	}

	private void ShowSettings(System.Windows.Controls.Button gear)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Margin = new Thickness(12.0, 10.0, 12.0, 10.0);
		stackPanel.MaxWidth = 390.0;
		StackPanel stackPanel2 = stackPanel;
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "Настройки",
			FontSize = 12.5,
			Foreground = Fog,
			Margin = new Thickness(0.0, 0.0, 0.0, 10.0)
		});
		autostartSw = new System.Windows.Controls.CheckBox
		{
			Style = St("Switch"),
			Content = "Запускать с Windows (в трее)",
			IsChecked = cfg.StartWithWindows,
			IsEnabled = !autostartBusy
		};
		autostartSw.Checked += delegate
		{
			SetAutostart(on: true);
		};
		autostartSw.Unchecked += delegate
		{
			SetAutostart(on: false);
		};
		stackPanel2.Children.Add(autostartSw);
		stackPanel2.Children.Add(new TextBlock
		{
			Text = "TTC сама запустится при входе в Windows — без окна, значком в трее у часов — и будет читать капты из игры и отправлять их на общую карту TTC. Крестик прячет окно в трей, выход — правой кнопкой по значку → «Выход».",
			FontSize = 12.0,
			Foreground = Fog,
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(46.0, 4.0, 0.0, 0.0)
		});
		ShowPopup(gear, stackPanel2);
	}

	private void SetAutostart(bool on)
	{
		if (autostartQuiet || autostartBusy || on == cfg.StartWithWindows)
		{
			return;
		}
		autostartBusy = true;
		if (autostartSw != null)
		{
			autostartSw.IsEnabled = false;
		}
		string exe = Updater.ExePath;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err;
			try
			{
				err = (on ? LogonTask.Enable(exe) : LogonTask.Disable());
			}
			catch (Exception ex)
			{
				err = ex.Message;
			}
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					AutostartDone(on, err);
				});
			}
			catch
			{
			}
		});
	}

	private void AutostartDone(bool on, string err)
	{
		autostartBusy = false;
		if (err == null)
		{
			cfg.StartWithWindows = on;
			cfg.StartWithWindowsChosen = true;
			SaveCfg();
		}
		if (autostartSw != null)
		{
			autostartSw.IsEnabled = true;
			autostartQuiet = true;
			autostartSw.IsChecked = cfg.StartWithWindows;
			autostartQuiet = false;
		}
		UpdateTray();
		if (err != null)
		{
			Log("«Запускать с Windows» не " + (on ? "включилось" : "выключилось") + ": " + err + ".");
		}
		else if (on)
		{
			Log("Запуск с Windows включён: при входе в Windows TTC запустится в трее (задача планировщика «" + LogonTask.Name + "», с правами администратора — без окна UAC). Крестик теперь прячет окно в трей, выход — правой кнопкой по значку TTC → «Выход».");
		}
		else
		{
			Log("Запуск с Windows выключен: задача планировщика удалена, крестик снова закрывает TTC.");
		}
	}

	private void InitPromo()
	{
		if (RootGrid != null && promoText == null)
		{
			RootGrid.RowDefinitions.Add(new RowDefinition
			{
				Height = new GridLength(30.0)
			});
			Grid grid = new Grid();
			grid.Background = PromoBg;
			Grid grid2 = grid;
			grid2.Children.Add(new System.Windows.Shapes.Rectangle
			{
				Height = 1.0,
				Fill = RuleB,
				VerticalAlignment = VerticalAlignment.Top
			});
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			StackPanel stackPanel2 = stackPanel;
			stackPanel2.Children.Add(PromoGift());
			TextBlock textBlock = new TextBlock();
			textBlock.FontSize = 13.0;
			textBlock.Foreground = Chalk;
			textBlock.VerticalAlignment = VerticalAlignment.Center;
			textBlock.Margin = new Thickness(9.0, 0.0, 0.0, 1.0);
			TextBlock textBlock2 = textBlock;
			textBlock2.Inlines.Add(new Run("Вводи промокод "));
			textBlock2.Inlines.Add(new Run("Skye")
			{
				Foreground = PromoCodeB,
				FontWeight = FontWeights.SemiBold
			});
			textBlock2.Inlines.Add(new Run(" при регистрации на Majestic RP и получай бонусы!"));
			stackPanel2.Children.Add(textBlock2);
			StackPanel stackPanel3 = new StackPanel();
			stackPanel3.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel3.Margin = new Thickness(14.0, 0.0, 0.0, 0.0);
			StackPanel stackPanel4 = stackPanel3;
			stackPanel4.Children.Add(Icons.Make("copy", 13.0, Fog));
			stackPanel4.Children.Add(new TextBlock
			{
				Text = "скопировать",
				FontSize = 12.0,
				Foreground = Fog,
				Margin = new Thickness(5.0, 0.0, 0.0, 1.0),
				VerticalAlignment = VerticalAlignment.Center
			});
			stackPanel2.Children.Add(stackPanel4);
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = stackPanel2;
			button.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			button.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
			button.VerticalAlignment = VerticalAlignment.Stretch;
			button.ToolTip = "Промокод Skye — нажмите, чтобы скопировать, и вставьте его при регистрации на Majestic RP";
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				PromoCopy();
			};
			promoText = button2;
			grid2.Children.Add(button2);
			StackPanel stackPanel5 = new StackPanel();
			stackPanel5.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel5.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
			stackPanel5.VerticalAlignment = VerticalAlignment.Center;
			stackPanel5.Visibility = Visibility.Hidden;
			stackPanel5.IsHitTestVisible = false;
			StackPanel stackPanel6 = stackPanel5;
			stackPanel6.Children.Add(Icons.Make("check", 14.0, GoB));
			stackPanel6.Children.Add(new TextBlock
			{
				Text = "Промокод Skye скопирован — вставь его при регистрации на Majestic RP",
				FontSize = 13.0,
				Foreground = Chalk,
				Margin = new Thickness(8.0, 0.0, 0.0, 1.0),
				VerticalAlignment = VerticalAlignment.Center
			});
			promoDone = stackPanel6;
			grid2.Children.Add(stackPanel6);
			Grid.SetRow(grid2, RootGrid.RowDefinitions.Count - 1);
			RootGrid.Children.Add(grid2);
			promoTimer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(2.0)
			};
			promoTimer.Tick += delegate
			{
				promoTimer.Stop();
				promoText.Visibility = Visibility.Visible;
				promoDone.Visibility = Visibility.Hidden;
			};
		}
	}

	private void PromoCopy()
	{
		if (SetClipboard("Skye"))
		{
			promoText.Visibility = Visibility.Hidden;
			promoDone.Visibility = Visibility.Visible;
			promoTimer.Stop();
			promoTimer.Start();
		}
	}

	private static FrameworkElement PromoGift()
	{
		System.Windows.Shapes.Path path = new System.Windows.Shapes.Path();
		path.Data = Geometry.Parse("M2.5,5.2 H13.5 V7.8 H2.5 Z M3.5,7.8 V13.5 H12.5 V7.8 M8,5.2 V13.5 M8,5.2 C7,3 4.4,2.4 4.6,4 C4.8,5 8,5.2 8,5.2 M8,5.2 C9,3 11.6,2.4 11.4,4 C11.2,5 8,5.2 8,5.2");
		path.Stroke = PromoCodeB;
		path.StrokeThickness = 1.5;
		path.StrokeLineJoin = PenLineJoin.Round;
		path.StrokeStartLineCap = PenLineCap.Round;
		path.StrokeEndLineCap = PenLineCap.Round;
		System.Windows.Shapes.Path element = path;
		Canvas canvas = new Canvas();
		canvas.Width = 16.0;
		canvas.Height = 16.0;
		Canvas canvas2 = canvas;
		canvas2.Children.Add(element);
		Viewbox viewbox = new Viewbox();
		viewbox.Width = 16.0;
		viewbox.Height = 16.0;
		viewbox.Child = canvas2;
		viewbox.VerticalAlignment = VerticalAlignment.Center;
		viewbox.IsHitTestVisible = false;
		return viewbox;
	}

	private void InitPresence()
	{
		if (demo || MapDemo)
		{
			return;
		}
		System.Windows.Controls.Panel panel = ((LicBtn != null) ? (LicBtn.Parent as System.Windows.Controls.Panel) : null);
		if (panel != null)
		{
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			StackPanel stackPanel2 = stackPanel;
			tgDot = new System.Windows.Shapes.Rectangle
			{
				Width = 8.0,
				Height = 8.0,
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = Dim,
				VerticalAlignment = VerticalAlignment.Center
			};
			stackPanel2.Children.Add(tgDot);
			stackPanel2.Children.Add(new TextBlock
			{
				Text = "Telegram",
				FontSize = 13.0,
				Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
				Foreground = Chalk
			});
			tgBtn = new System.Windows.Controls.Button
			{
				Style = St("Plain"),
				Content = stackPanel2,
				Margin = new Thickness(0.0, 0.0, 20.0, 0.0)
			};
			tgBtn.Click += delegate
			{
				TgClick();
			};
			panel.Children.Insert(panel.Children.IndexOf(LicBtn), tgBtn);
			UpdateTgBtn();
		}
		System.Windows.Controls.Panel panel2 = F<System.Windows.Controls.Panel>("StatusChip");
		if (panel2 != null)
		{
			reportBtn = new System.Windows.Controls.Button
			{
				Style = St("Plain"),
				Margin = new Thickness(14.0, 0.0, 0.0, 0.0),
				Visibility = Visibility.Collapsed,
				Content = new TextBlock
				{
					Text = "Отправить отчёт",
					FontSize = 13.0,
					Foreground = SignalB
				},
				ToolTip = "Отправить в поддержку журнал и снимок окна игры в момент ошибки.\nОтвет придёт в личный кабинет на сайте (и в Telegram, если он привязан)."
			};
			reportBtn.Click += delegate
			{
				SendReport();
			};
			panel2.Children.Add(reportBtn);
		}
		BuildOnboarding();
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromSeconds(2.0);
		DispatcherTimer dispatcherTimer2 = dispatcherTimer;
		dispatcherTimer2.Tick += delegate
		{
			PresenceTick();
		};
		dispatcherTimer2.Start();
		presNextUtc = DateTime.UtcNow.AddSeconds(8.0);
		InitPower();
	}

	private void InitPower()
	{
		try
		{
			SystemEvents.PowerModeChanged += delegate(object s, PowerModeChangedEventArgs e)
			{
				if (e.Mode == PowerModes.Suspend)
				{
					Farewell("sleep");
				}
				else if (e.Mode == PowerModes.Resume)
				{
					farewell = null;
					presSig = "";
					presNextUtc = DateTime.UtcNow.AddSeconds(3.0);
				}
			};
		}
		catch
		{
		}
		w.Closed += delegate
		{
			Farewell("exit");
		};
	}

	private void Farewell(string kind)
	{
		LicenseState current = License.Current;
		if (demo || MapDemo || farewell != null || current == null || !current.Ok || current.Key.Length == 0)
		{
			return;
		}
		farewell = kind;
		farewellUtc = DateTime.UtcNow;
		try
		{
			Dictionary<string, object> dictionary = PresenceState();
			dictionary["power"] = kind;
			dictionary["state"] = "idle";
			dictionary.Remove("target");
			dictionary.Remove("fire_at");
			dictionary.Remove("capt");
			dictionary.Remove("mode");
			Dictionary<string, object> body = new Dictionary<string, object>();
			body["key"] = current.Key;
			body["hwid"] = License.Hwid();
			body["ver"] = "2.9.13";
			body["build"] = "user";
			body["st"] = dictionary;
			Thread thread = new Thread((ThreadStart)delegate
			{
				PostApi("/api/v1/status", body, 1500, out var _);
			});
			thread.IsBackground = true;
			thread.Name = "TTC farewell";
			Thread thread2 = thread;
			thread2.Start();
			thread2.Join(1500);
		}
		catch
		{
		}
	}

	private void PresenceBegin(string mode, string square)
	{
		presMode = mode;
		presTarget = square;
		presNextUtc = DateTime.UtcNow;
	}

	private void PresenceEnd()
	{
		if (presMode == null)
		{
			return;
		}
		string text = statusKind;
		switch (text)
		{
		case "done":
		case "testok":
		case "error":
		{
			bool flag = text != "error";
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			dictionary["at"] = War.NowUnix();
			dictionary["ok"] = flag;
			dictionary["mode"] = presMode;
			dictionary["target"] = presTarget ?? "";
			if (!flag)
			{
				dictionary["text"] = ((statusMsg.Length > 190) ? statusMsg.Substring(0, 190) : statusMsg);
			}
			presResult = dictionary;
			if (flag && !cfg.OnboardDone)
			{
				cfg.OnboardDone = true;
				SaveCfg();
			}
			break;
		}
		}
		presMode = null;
		presTarget = null;
		presNextUtc = DateTime.UtcNow;
	}

	private Dictionary<string, object> PresenceState()
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["power"] = "on";
		dictionary["state"] = (running ? statusKind : "idle");
		if (running && presMode != null)
		{
			dictionary["mode"] = presMode;
			dictionary["target"] = presTarget ?? "";
			if (fireUtc > Clock.UtcNow())
			{
				dictionary["fire_at"] = War.Unix(fireUtc);
			}
			if (presMode != "calib")
			{
				dictionary["capt"] = cfg.CaptH.ToString("00") + ":" + cfg.CaptM.ToString("00");
			}
		}
		dictionary["admin"] = NetWatch.IsAdmin;
		dictionary["net"] = TrafficSeen();
		ProbeResult probeResult = lastProbeR;
		dictionary["game"] = probeResult?.Found ?? false;
		if (probeResult != null && probeResult.Found)
		{
			dictionary["cal"] = probeResult.Calibrated;
			dictionary["size"] = probeResult.Size ?? "";
		}
		if (!string.IsNullOrEmpty(NetWatch.OwnFamily))
		{
			dictionary["fam"] = NetWatch.OwnFamily;
		}
		return dictionary;
	}

	private static bool TrafficSeen()
	{
		if (NetWatch.Running && NetWatch.LastEventUtc > DateTime.MinValue)
		{
			return (DateTime.UtcNow - NetWatch.LastEventUtc).TotalMinutes < 10.0;
		}
		return false;
	}

	private void PresenceTick()
	{
		UpdateReportBtn();
		UpdateOnboarding();
		LicenseState current = License.Current;
		if (current == null || !current.Ok || current.Key.Length == 0 || presSending || closing)
		{
			return;
		}
		if (farewell != null)
		{
			if ((DateTime.UtcNow - farewellUtc).TotalSeconds < 60.0)
			{
				return;
			}
			farewell = null;
			presSig = "";
		}
		Dictionary<string, object> dictionary = PresenceState();
		string text = PJson.Serialize(dictionary);
		DateTime utcNow = DateTime.UtcNow;
		if ((text == presSig && presResult == null && utcNow < presNextUtc) || (text != presSig && utcNow < presNextUtc.AddSeconds(-58.0)))
		{
			return;
		}
		if (presResult != null)
		{
			dictionary["res"] = presResult;
		}
		Dictionary<string, object> body = new Dictionary<string, object>();
		body["key"] = current.Key;
		body["hwid"] = License.Hwid();
		body["ver"] = "2.9.13";
		body["build"] = "user";
		body["st"] = dictionary;
		Dictionary<string, object> sentRes = presResult;
		presSending = true;
		presSig = text;
		presNextUtc = utcNow.AddSeconds(60.0);
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err;
			Dictionary<string, object> d = PostApi("/api/v1/status", body, 10000, out err);
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					presSending = false;
					if (d == null || !PBool(d, "ok"))
					{
						presNextUtc = DateTime.UtcNow.AddSeconds((err != null && err.Contains("часто")) ? 60 : 30);
					}
					else
					{
						if (object.ReferenceEquals(presResult, sentRes))
						{
							presResult = null;
						}
						PresenceReply(d);
					}
				});
			}
			catch
			{
				presSending = false;
			}
		});
	}

	private void PresenceReply(Dictionary<string, object> d)
	{
		if (d.TryGetValue("tg", out var value) && value is bool)
		{
			tgLinked = (bool)value;
			UpdateTgBtn();
		}
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		if (d.TryGetValue("family", out var value2) && value2 is IEnumerable)
		{
			foreach (object item2 in (IEnumerable)value2)
			{
				if (item2 is Dictionary<string, object> item)
				{
					list.Add(item);
				}
			}
		}
		famBoard = list;
		UpdateWarInfo();
	}

	private string FamilyTargetNote(string code)
	{
		if (famBoard.Count == 0)
		{
			return "";
		}
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (Dictionary<string, object> item in famBoard)
		{
			if (!PBool(item, "online"))
			{
				continue;
			}
			string text = PStr(item, "target");
			if (text.Length != 0)
			{
				string text2 = "ключ " + PStr(item, "seat") + ((PStr(item, "holder").Length > 0) ? (" (" + PStr(item, "holder") + ")") : "");
				if (text == code)
				{
					list.Add(text2);
				}
				else
				{
					list2.Add(text + ((PStr(item, "capt").Length > 0) ? (" в " + PStr(item, "capt")) : "") + " — " + text2);
				}
			}
		}
		string text3 = "";
		if (list.Count > 0)
		{
			string text4 = text3;
			text3 = text4 + " " + code + " бьёт и " + string.Join(", ", list) + ".";
		}
		if (list2.Count > 0)
		{
			text3 = text3 + " В семье бьют: " + string.Join("; ", list2) + ".";
		}
		return text3;
	}

	private bool? TargetAdjacent(string code)
	{
		Zone zone = Zones.ByCode((code ?? "").Trim().ToUpperInvariant());
		string ownKey = Zones.Key(cfg.Family);
		if (zone == null || war == null || ownKey.Length == 0)
		{
			return null;
		}
		WarFamily warFamily = war.OwnerOf(zone);
		if (warFamily != null && warFamily.Key == ownKey)
		{
			return null;
		}
		if (!war.OwnerIdx.Any((KeyValuePair<int, int> kv) => kv.Value >= 0 && kv.Value < war.Families.Count && war.Families[kv.Value].Key == ownKey))
		{
			return null;
		}
		return Zones.Neighbors(zone).Any(delegate(Zone n)
		{
			WarFamily warFamily2 = war.OwnerOf(n);
			return warFamily2 != null && warFamily2.Key == ownKey;
		});
	}

	private bool ConfirmTarget(string code)
	{
		if (TargetAdjacent(code) != false)
		{
			return true;
		}
		string text = "Квадрат " + code + " не граничит с территорией " + FamilyPhrase() + " — форма атаки, скорее всего, не откроется.";
		Log("Внимание: " + text);
		if (!w.IsActive || planCurrent != null)
		{
			return true;
		}
		return System.Windows.MessageBox.Show(w, text + "\n\nВсё равно запустить?", "TTC", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes;
	}

	private void UpdateTgBtn()
	{
		if (tgBtn != null)
		{
			tgDot.Fill = ((tgLinked == true) ? GoB : Dim);
			tgBtn.ToolTip = ((tgLinked == true) ? "Telegram привязан к этому ключу: итоги забивов, срок ключа, перевыпуск.\nНажмите, чтобы отвязать или открыть бота." : "Уведомления в Telegram: итоги забивов, срок ключа, перевыпуск.\nНажмите — откроется бот, в нём нажмите «Запустить».");
		}
	}

	private void TgClick()
	{
		if (tgLinked == true)
		{
			System.Windows.Controls.ContextMenu contextMenu = new System.Windows.Controls.ContextMenu();
			System.Windows.Controls.MenuItem menuItem = new System.Windows.Controls.MenuItem();
			menuItem.Header = "Открыть бота (привязать ещё раз)";
			System.Windows.Controls.MenuItem menuItem2 = menuItem;
			menuItem2.Click += delegate
			{
				TgRequest(unlink: false);
			};
			System.Windows.Controls.MenuItem menuItem3 = new System.Windows.Controls.MenuItem();
			menuItem3.Header = "Отвязать Telegram от этого ключа";
			System.Windows.Controls.MenuItem menuItem4 = menuItem3;
			menuItem4.Click += delegate
			{
				TgRequest(unlink: true);
			};
			contextMenu.Items.Add(menuItem2);
			contextMenu.Items.Add(menuItem4);
			contextMenu.PlacementTarget = tgBtn;
			contextMenu.IsOpen = true;
		}
		else
		{
			TgRequest(unlink: false);
		}
	}

	private void TgRequest(bool unlink)
	{
		LicenseState current = License.Current;
		if (current == null || !current.Ok)
		{
			System.Windows.MessageBox.Show(w, "Нужен действующий ключ TTC.", "TTC");
			return;
		}
		Dictionary<string, object> body = new Dictionary<string, object>();
		body["key"] = current.Key;
		body["hwid"] = License.Hwid();
		body["unlink"] = unlink;
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err;
			Dictionary<string, object> d = PostApi("/api/v1/tg/link", body, 12000, out err);
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					if (d == null || !PBool(d, "ok"))
					{
						System.Windows.MessageBox.Show(w, "Telegram: " + (err ?? "сайт не ответил") + ".", "TTC");
					}
					else if (unlink)
					{
						tgLinked = false;
						UpdateTgBtn();
						Log("Telegram отвязан от ключа.");
					}
					else
					{
						string text = PStr(d, "url");
						if (text.StartsWith("https://t.me/") && AppPaths.OpenUrl(text))
						{
							Log("Открыл бота TTC в Telegram: нажмите в нём «Запустить», и уведомления придут туда.");
							presNextUtc = DateTime.UtcNow.AddSeconds(20.0);
						}
						else
						{
							System.Windows.MessageBox.Show(w, "Откройте ссылку в Telegram:\n" + text, "TTC");
						}
					}
				});
			}
			catch
			{
			}
		});
	}

	private void UpdateReportBtn()
	{
		if (reportBtn != null)
		{
			LicenseState current = License.Current;
			bool flag = !running && statusKind == "error" && statusMsg != reportedMsg && current != null && current.Ok && !reportSending;
			reportBtn.Visibility = ((!flag) ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	private void SendReport()
	{
		LicenseState current = License.Current;
		if (current == null || !current.Ok || reportSending || System.Windows.MessageBox.Show(w, "Отправить в поддержку журнал TTC и снимок окна игры в момент ошибки?\n\nПоддержка ответит в личном кабинете на сайте" + (current.FamilySeat ? " владельца семейной подписки" : "") + " (и в Telegram, если он привязан к аккаунту).", "TTC: отчёт об ошибке", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
		{
			return;
		}
		Dictionary<string, object> body = new Dictionary<string, object>();
		body["key"] = current.Key;
		body["hwid"] = License.Hwid();
		body["ver"] = "2.9.13";
		body["place"] = ((statusMsg.Length <= 0) ? "ошибка забива" : ((statusMsg.Length > 100) ? statusMsg.Substring(0, 100) : statusMsg));
		string text = LogBox.Text ?? "";
		body["log"] = ((text.Length > 200000) ? text.Substring(text.Length - 200000) : text);
		byte[] array = ((runner != null) ? runner.FailShot : null);
		if (array != null && array.Length < 4194304)
		{
			body["shot"] = Convert.ToBase64String(array);
		}
		ProbeResult probeResult = lastProbeR;
		if (probeResult != null && probeResult.Found)
		{
			body["size"] = probeResult.Size ?? "";
		}
		body["mode"] = (cfg.AfterCapt ? "asap" : "strike");
		body["target"] = cfg.Square ?? "";
		reportSending = true;
		UpdateReportBtn();
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err;
			Dictionary<string, object> d = PostApi("/api/v1/report", body, 30000, out err);
			try
			{
				w.Dispatcher.BeginInvoke((Action)delegate
				{
					reportSending = false;
					if (d == null || !PBool(d, "ok"))
					{
						System.Windows.MessageBox.Show(w, "Отчёт не отправлен: " + (err ?? "сайт не ответил") + ".", "TTC");
						UpdateReportBtn();
					}
					else
					{
						reportedMsg = statusMsg;
						UpdateReportBtn();
						Log("Отчёт отправлен в поддержку, обращение №" + PStr(d, "ticket") + ".");
						if (System.Windows.MessageBox.Show(w, "Отчёт отправлен, обращение №" + PStr(d, "ticket") + ". Открыть его на сайте?", "TTC", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) == MessageBoxResult.Yes)
						{
							AppPaths.OpenUrl(PStr(d, "url"));
						}
					}
				});
			}
			catch
			{
				reportSending = false;
			}
		});
	}

	private void BuildOnboarding()
	{
		if (!cfg.OnboardDone && StepsPanel != null)
		{
			onbList = new StackPanel();
			StackPanel stackPanel = new StackPanel();
			Grid grid = new Grid();
			grid.Children.Add(new TextBlock
			{
				Text = "Первый запуск",
				FontSize = 14.0,
				FontWeight = FontWeights.SemiBold,
				Foreground = Chalk
			});
			System.Windows.Controls.Button button = new System.Windows.Controls.Button();
			button.Style = St("Plain");
			button.Content = new TextBlock
			{
				Text = "Скрыть",
				FontSize = 12.5,
				Foreground = Fog
			};
			button.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
			System.Windows.Controls.Button button2 = button;
			button2.Click += delegate
			{
				cfg.OnboardDone = true;
				SaveCfg();
				onbCard.Visibility = Visibility.Collapsed;
			};
			grid.Children.Add(button2);
			stackPanel.Children.Add(grid);
			stackPanel.Children.Add(onbList);
			onbCard = new Border
			{
				Background = Slate,
				BorderBrush = RuleB,
				BorderThickness = new Thickness(1.0),
				CornerRadius = new CornerRadius(8.0),
				Padding = new Thickness(14.0, 10.0, 14.0, 10.0),
				Margin = new Thickness(0.0, 0.0, 0.0, 10.0),
				Child = stackPanel,
				Visibility = Visibility.Collapsed
			};
			StepsPanel.Children.Insert(0, onbCard);
		}
	}

	private void UpdateOnboarding()
	{
		if (onbCard == null)
		{
			return;
		}
		if (cfg.OnboardDone || running)
		{
			onbCard.Visibility = Visibility.Collapsed;
			return;
		}
		ProbeResult probeResult = lastProbeR;
		bool[] array = new bool[5]
		{
			NetWatch.IsAdmin,
			probeResult != null && probeResult.Found && !probeResult.Black && !probeResult.Admin,
			TrafficSeen(),
			!string.IsNullOrEmpty(NetWatch.OwnFamily),
			probeResult != null && probeResult.Found && probeResult.Calibrated
		};
		string[] array2 = new string[5] { "TTC запущена от имени администратора", "Игра найдена в режиме «Оконный без рамки»", "TTC видит трафик игры", "Семья видна: откройте в планшете приложение семьи", "Калибровка под это окно: кнопка «Калибровка» вверху" };
		string[] array3 = new string[5]
		{
			"Закройте TTC и запустите снова, в окне Windows нажмите «Да».",
			(probeResult != null && probeResult.Black) ? "Сейчас полноэкранный режим: переключите в настройках игры." : "Запустите GTA 5 и зайдите на сервер.",
			"Нужны права администратора. Если включён VPN или прокси, попробуйте без них.",
			"Там же карта каптов. Ключ семейной подписки без этого забив не начнёт.",
			"Бот откроет планшет и карту и всё померит. Окно игры в это время не трогайте."
		};
		if (array.All((bool x) => x))
		{
			cfg.OnboardDone = true;
			SaveCfg();
			onbCard.Visibility = Visibility.Collapsed;
			return;
		}
		onbList.Children.Clear();
		for (int i = 0; i < array.Length; i++)
		{
			Grid grid = new Grid();
			grid.Margin = new Thickness(0.0, 6.0, 0.0, 0.0);
			Grid grid2 = grid;
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(22.0)
			});
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			TextBlock textBlock = new TextBlock();
			textBlock.Text = (array[i] ? "✓" : (i + 1).ToString());
			textBlock.FontSize = 13.0;
			textBlock.FontWeight = FontWeights.SemiBold;
			textBlock.Foreground = (array[i] ? GoB : WarnB);
			TextBlock element = textBlock;
			StackPanel stackPanel = new StackPanel();
			stackPanel.Children.Add(new TextBlock
			{
				Text = array2[i],
				FontSize = 13.0,
				Foreground = (array[i] ? Fog : Chalk),
				TextWrapping = TextWrapping.Wrap
			});
			if (!array[i])
			{
				stackPanel.Children.Add(new TextBlock
				{
					Text = array3[i],
					FontSize = 12.0,
					Foreground = Fog,
					TextWrapping = TextWrapping.Wrap
				});
			}
			Grid.SetColumn(stackPanel, 1);
			grid2.Children.Add(element);
			grid2.Children.Add(stackPanel);
			onbList.Children.Add(grid2);
		}
		onbCard.Visibility = Visibility.Visible;
	}

	private static Dictionary<string, object> PostApi(string path, Dictionary<string, object> body, int timeoutMs, out string err)
	{
		err = null;
		try
		{
			ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
			HttpWebRequest httpWebRequest = SiteNet.Create("https://timetocapture.ru" + path);
			httpWebRequest.Method = "POST";
			httpWebRequest.ContentType = "application/json; charset=utf-8";
			httpWebRequest.UserAgent = "TTC/2.9.13";
			httpWebRequest.Timeout = timeoutMs;
			httpWebRequest.ReadWriteTimeout = timeoutMs;
			httpWebRequest.Proxy = WebRequest.DefaultWebProxy;
			byte[] bytes = Encoding.UTF8.GetBytes(PJson.Serialize(body));
			using (Stream stream = httpWebRequest.GetRequestStream())
			{
				stream.Write(bytes, 0, bytes.Length);
			}
			string input;
			try
			{
				using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
				using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
				input = streamReader.ReadToEnd();
			}
			catch (WebException ex)
			{
				if (!(ex.Response is HttpWebResponse httpWebResponse2))
				{
					throw;
				}
				using StreamReader streamReader2 = new StreamReader(httpWebResponse2.GetResponseStream(), Encoding.UTF8);
				input = streamReader2.ReadToEnd();
			}
			Dictionary<string, object> dictionary = PJson.DeserializeObject(input) as Dictionary<string, object>;
			if (dictionary != null && !PBool(dictionary, "ok"))
			{
				err = ((PStr(dictionary, "error").Length > 0) ? PStr(dictionary, "error") : "сайт отказал");
			}
			return dictionary;
		}
		catch (Exception ex2)
		{
			err = ex2.Message;
			return null;
		}
	}

	private static bool PBool(Dictionary<string, object> d, string k)
	{
		if (d != null && d.TryGetValue(k, out var value) && value is bool)
		{
			return (bool)value;
		}
		return false;
	}

	private static string PStr(Dictionary<string, object> d, string k)
	{
		if (d == null || !d.TryGetValue(k, out var value) || value == null)
		{
			return "";
		}
		return Convert.ToString(value, CultureInfo.InvariantCulture);
	}

	private void InitPriority()
	{
		if (demo || MapDemo)
		{
			return;
		}
		DispatcherTimer dispatcherTimer = new DispatcherTimer();
		dispatcherTimer.Interval = TimeSpan.FromSeconds(3.0);
		DispatcherTimer dispatcherTimer2 = dispatcherTimer;
		dispatcherTimer2.Tick += delegate
		{
			if (prioOn)
			{
				if (!PriorityAllowed)
				{
					ShowPriority(show: false);
				}
				else if (w.IsVisible && w.WindowState != WindowState.Minimized)
				{
					LoadPriority();
				}
			}
		};
		dispatcherTimer2.Start();
	}

	private void ShowPriority(bool show)
	{
		if (show && (!PriorityAllowed || mainBody == null))
		{
			return;
		}
		if (show && vzzOn)
		{
			ShowVzz(on: false);
		}
		if (show && planOn)
		{
			ShowPlan(on: false);
		}
		if (show && prioBody == null)
		{
			BuildPriority();
		}
		if (prioBody != null)
		{
			prioOn = show;
			Grid grid = mainBody;
			System.Windows.Controls.Panel panel = mainBar;
			Visibility visibility2 = (mainButtons.Visibility = (show ? Visibility.Collapsed : Visibility.Visible));
			Visibility visibility4 = (panel.Visibility = visibility2);
			grid.Visibility = visibility4;
			Grid grid2 = prioBody;
			System.Windows.Controls.Panel panel2 = prioBar;
			Visibility visibility6 = (prioButtons.Visibility = ((!show) ? Visibility.Collapsed : Visibility.Visible));
			Visibility visibility8 = (panel2.Visibility = visibility6);
			grid2.Visibility = visibility8;
			if (show)
			{
				modeText.Text = "Приоритет";
			}
			else if (!vzzOn && !planOn)
			{
				modeText.Text = (rcfg.ListsMode ? "Списки" : "Забив");
			}
			if (show)
			{
				RenderPriority();
				LoadPriority();
			}
		}
	}

	private void BuildPriority()
	{
		Grid grid = mainButtons.Parent as Grid;
		prioBody = new Grid
		{
			Margin = mainBody.Margin,
			Visibility = Visibility.Collapsed
		};
		Grid.SetRow(prioBody, Grid.GetRow(mainBody));
		GridLength[] array = new GridLength[3]
		{
			new GridLength(372.0),
			new GridLength(40.0),
			new GridLength(1.0, GridUnitType.Star)
		};
		foreach (GridLength width in array)
		{
			prioBody.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = width
			});
		}
		((System.Windows.Controls.Panel)mainBody.Parent).Children.Add(prioBody);
		StackPanel stackPanel = new StackPanel();
		prioBody.Children.Add(stackPanel);
		stackPanel.Children.Add(new TextBlock
		{
			Text = "Семья, которая бьёт первой",
			Style = St("Label")
		});
		prioFam = VzField(24.0, 52.0, strong: true);
		prioFam.MaxLength = 64;
		UIElement element = WithHint(prioFam, "название, как в игре", out prioFamHint);
		prioFam.TextChanged += delegate
		{
			prioFamHint.Visibility = ((prioFam.Text.Length != 0) ? Visibility.Collapsed : Visibility.Visible);
			prioFam.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
			RenderPriority();
		};
		stackPanel.Children.Add(element);
		prioTake = new System.Windows.Controls.Button
		{
			Style = St("Quick"),
			Margin = new Thickness(0.0, 10.0, 0.0, 0.0),
			HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
			ToolTip = "Вписать семью, которую TTC видит в игре"
		};
		prioTake.Click += delegate
		{
			prioFam.Text = OwnFamilyName() ?? "";
		};
		stackPanel.Children.Add(prioTake);
		prioServer = new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0)
		};
		stackPanel.Children.Add(prioServer);
		prioNote = new TextBlock
		{
			Style = St("Hint"),
			Foreground = WarnB,
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			Visibility = Visibility.Collapsed
		};
		stackPanel.Children.Add(prioNote);
		stackPanel.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Height = 1.0,
			Fill = RuleB,
			Margin = new Thickness(0.0, 22.0, 0.0, 18.0)
		});
		stackPanel.Children.Add(new TextBlock
		{
			Text = "Когда другая копия TTC ждёт",
			Style = St("Label")
		});
		string[] array2 = new string[4] { "бьёт тот же квадрат, что и ваша семья;", "бьёт семью, которую бьёте вы: она может быть только в одном капте;", "нападает на вашу семью: капт с вами закроет вам атаку;", "она сама — семья вашей цели: её капт закроет её для вас." };
		string[] array3 = array2;
		foreach (string text in array3)
		{
			Grid grid2 = new Grid();
			grid2.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
			Grid grid3 = grid2;
			grid3.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(16.0)
			});
			grid3.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = new GridLength(1.0, GridUnitType.Star)
			});
			grid3.Children.Add(new System.Windows.Shapes.Rectangle
			{
				Width = 6.0,
				Height = 6.0,
				RadiusX = 1.5,
				RadiusY = 1.5,
				Fill = SignalB,
				HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(0.0, 7.0, 0.0, 0.0)
			});
			TextBlock textBlock = new TextBlock();
			textBlock.Text = text;
			textBlock.FontSize = 14.0;
			textBlock.Foreground = Chalk;
			textBlock.TextWrapping = TextWrapping.Wrap;
			textBlock.LineHeight = 19.0;
			TextBlock element2 = textBlock;
			Grid.SetColumn(element2, 1);
			grid3.Children.Add(element2);
			stackPanel.Children.Add(grid3);
		}
		stackPanel.Children.Add(new TextBlock
		{
			Style = St("Hint"),
			Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
			Text = "Ждёт вашего нажатия, если оно в ближайший час, и бьёт через секунду после него. Считаются запущенный забив и цели плана. Остановили забив или пропала связь — не ждёт. Приоритет у ваших забивов и у игроков этой семьи, которых TTC узнала по игре."
		});
		if (Turn.Enabled)
		{
			stackPanel.Children.Add(new TextBlock
			{
				Style = St("Hint"),
				Margin = new Thickness(0.0, 8.0, 0.0, 0.0),
				Text = "У игрока в TTC написано «Жду момент», про очередь — нет. Чья это семья и что она бьёт, ему не показывается. Правило описано на сайте: пункт 7.4 соглашения."
			});
		}
		Grid grid4 = new Grid();
		Grid.SetColumn(grid4, 2);
		prioBody.Children.Add(grid4);
		for (int k = 0; k < 4; k++)
		{
			grid4.RowDefinitions.Add(new RowDefinition
			{
				Height = ((k == 3) ? new GridLength(1.0, GridUnitType.Star) : GridLength.Auto)
			});
		}
		Grid grid5 = new Grid();
		grid5.Children.Add(new TextBlock
		{
			Text = "Заявлено сейчас",
			Style = St("Label"),
			Margin = new Thickness(0.0),
			VerticalAlignment = VerticalAlignment.Center
		});
		StackPanel stackPanel2 = new StackPanel();
		stackPanel2.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel2.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
		stackPanel2.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel3 = stackPanel2;
		prioStatusDot = new System.Windows.Shapes.Rectangle
		{
			Width = 8.0,
			Height = 8.0,
			RadiusX = 1.5,
			RadiusY = 1.5,
			Fill = Fog,
			VerticalAlignment = VerticalAlignment.Center
		};
		prioStatusText = new TextBlock
		{
			FontSize = 13.0,
			Margin = new Thickness(8.0, 0.0, 0.0, 0.0),
			Foreground = Chalk
		};
		stackPanel3.Children.Add(prioStatusDot);
		stackPanel3.Children.Add(prioStatusText);
		grid5.Children.Add(stackPanel3);
		grid4.Children.Add(grid5);
		prioClaims = new StackPanel
		{
			Margin = new Thickness(0.0, 12.0, 0.0, 0.0),
			MinHeight = 60.0
		};
		Grid.SetRow(prioClaims, 1);
		grid4.Children.Add(prioClaims);
		Grid grid6 = new Grid();
		grid6.Margin = new Thickness(0.0, 18.0, 0.0, 0.0);
		Grid grid7 = grid6;
		grid7.Children.Add(new System.Windows.Shapes.Rectangle
		{
			Height = 1.0,
			Fill = RuleB,
			VerticalAlignment = VerticalAlignment.Top
		});
		grid7.Children.Add(new TextBlock
		{
			Text = "Кто ждал вашего забива",
			Style = St("Label"),
			Margin = new Thickness(0.0, 14.0, 0.0, 8.0)
		});
		Grid.SetRow(grid7, 2);
		grid4.Children.Add(grid7);
		prioLog = new StackPanel();
		ScrollViewer scrollViewer = new ScrollViewer();
		scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		scrollViewer.Content = prioLog;
		ScrollViewer element3 = scrollViewer;
		Grid.SetRow(element3, 3);
		grid4.Children.Add(element3);
		WrapPanel wrapPanel = new WrapPanel();
		wrapPanel.VerticalAlignment = VerticalAlignment.Center;
		wrapPanel.Visibility = Visibility.Collapsed;
		WrapPanel wrapPanel2 = wrapPanel;
		Grid.SetColumn(wrapPanel2, Grid.GetColumn(mainBar));
		wrapPanel2.Children.Add(VzReady("Сайт", out prioSiteDot, out prioSiteText));
		wrapPanel2.Children.Add(VzReady("Копии TTC", out prioCopiesDot, out prioCopiesText));
		prioBar = wrapPanel2;
		grid.Children.Add(wrapPanel2);
		StackPanel stackPanel4 = new StackPanel();
		stackPanel4.Orientation = System.Windows.Controls.Orientation.Horizontal;
		stackPanel4.VerticalAlignment = VerticalAlignment.Center;
		stackPanel4.Visibility = Visibility.Collapsed;
		StackPanel stackPanel5 = stackPanel4;
		Grid.SetColumn(stackPanel5, Grid.GetColumn(mainButtons));
		prioGo = new System.Windows.Controls.Button
		{
			Style = St("Primary"),
			Height = 50.0,
			Width = 214.0
		};
		prioGo.Click += delegate
		{
			SavePriority();
		};
		stackPanel5.Children.Add(prioGo);
		prioButtons = stackPanel5;
		grid.Children.Add(stackPanel5);
		ApplyLayoutMode(narrowLayout);
	}

	private void LoadPriority()
	{
		PostPriority(null);
	}

	private void PostPriority(Dictionary<string, object> set)
	{
		LicenseState current = License.Current;
		if (current == null || !current.Ok || (set == null && prioLoading))
		{
			return;
		}
		prioLoading = true;
		if (set != null)
		{
			prioSaving = true;
			RenderPriority();
		}
		Dictionary<string, object> body = new Dictionary<string, object>();
		body["key"] = current.Key;
		body["hwid"] = License.Hwid();
		body["ver"] = "2.9.13";
		if (set != null)
		{
			body["set"] = set;
		}
		ThreadPool.QueueUserWorkItem(delegate
		{
			string err;
			Dictionary<string, object> d = PostApi("/api/v1/priority", body, 10000, out err);
			UiDo(delegate
			{
				prioLoading = false;
				if (set != null)
				{
					prioSaving = false;
				}
				bool flag = d != null && PBool(d, "ok");
				if (flag)
				{
					prioState = d;
					if (!prioFilled || set != null)
					{
						prioFilled = true;
						if (PStr(d, "family").Length > 0)
						{
							prioFam.Text = PStr(d, "family");
						}
					}
					if (set != null)
					{
						Log(PBool(d, "on") ? ("Приоритет включён: " + PStr(d, "family") + " (" + PStr(d, "server") + ")." + (Turn.Enabled ? "" : " Очередь пока выключена — другие копии TTC не ждут.")) : "Приоритет выключен: копии TTC бьют без очереди.");
						Turn.Refresh();
					}
				}
				SetReady(prioSiteDot, prioSiteText, flag ? GoB : StopB, flag ? "Сайт на связи" : ("Сайт: " + (err ?? "не ответил")));
				PrioNote(flag ? null : ((set != null) ? ("Не сохранено: " + (err ?? "сайт не ответил") + ".") : null));
				RenderPriority();
			});
		});
	}

	private string PrioServerId()
	{
		return ((!string.IsNullOrEmpty(NetWatch.GameServer)) ? NetWatch.GameServer : (cfg.Server ?? "")).ToUpperInvariant();
	}

	private bool PrioChanged()
	{
		if (prioState != null)
		{
			if (!(Zones.Key(prioFam.Text) != Zones.Key(PStr(prioState, "family"))))
			{
				return PrioServerId() != PStr(prioState, "server");
			}
			return true;
		}
		return false;
	}

	private void SavePriority()
	{
		if (!prioSaving)
		{
			bool flag = prioState != null && PBool(prioState, "on") && !PrioChanged();
			string text = (prioFam.Text ?? "").Trim();
			if (!flag && Zones.Key(text).Length == 0)
			{
				PrioNote("Впишите семью.");
				prioFam.BorderBrush = StopB;
				prioFam.Focus();
			}
			else if (!flag && PrioServerId().Length == 0)
			{
				PrioNote("Выберите сервер игры вверху окна.");
			}
			else
			{
				Dictionary<string, object> dictionary = new Dictionary<string, object>();
				dictionary["on"] = !flag;
				dictionary["family"] = text;
				dictionary["server"] = (flag ? PStr(prioState, "server") : PrioServerId());
				PostPriority(dictionary);
			}
		}
	}

	private void PrioNote(string text)
	{
		prioNote.Text = text ?? "";
		prioNote.Visibility = (string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible);
	}

	private static List<Dictionary<string, object>> PList(Dictionary<string, object> d, string k)
	{
		List<Dictionary<string, object>> list = new List<Dictionary<string, object>>();
		if (d != null && d.TryGetValue(k, out var value) && value is IEnumerable && !(value is string))
		{
			foreach (object item2 in (IEnumerable)value)
			{
				if (item2 is Dictionary<string, object> item)
				{
					list.Add(item);
				}
			}
		}
		return list;
	}

	private static long PLong(Dictionary<string, object> d, string k)
	{
		if (!long.TryParse(PStr(d, k), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0L;
		}
		return result;
	}

	private void RenderPriority()
	{
		if (prioBody == null)
		{
			return;
		}
		Dictionary<string, object> dictionary = prioState;
		bool flag = dictionary != null;
		bool flag2 = flag && PBool(dictionary, "on");
		bool flag3 = PrioChanged();
		string text = PrioServerId();
		string text2 = OwnFamilyName();
		prioTake.Visibility = ((string.IsNullOrEmpty(text2) || !(Zones.Key(text2) != Zones.Key(prioFam.Text))) ? Visibility.Collapsed : Visibility.Visible);
		prioTake.Content = "Взять из игры: " + text2;
		ServerInfo serverInfo = CurrentServer();
		string text3 = ((serverInfo != null && string.Equals(serverInfo.Id, text, StringComparison.OrdinalIgnoreCase)) ? serverInfo.Title : text);
		prioServer.Text = ((text.Length == 0) ? "Сервер игры не выбран: выберите его вверху окна." : ((flag2 && PStr(dictionary, "server") != text) ? ("Сейчас приоритет на сервере " + PStr(dictionary, "server") + ", а выбран " + text3 + ". «Сохранить» перенесёт его сюда.") : ("Сервер " + text3 + (string.IsNullOrEmpty(NetWatch.GameServer) ? "." : ", по игре."))));
		prioStatusDot.Fill = ((!flag) ? Dim : ((!Turn.Enabled) ? WarnB : (flag2 ? GoB : Fog)));
		prioStatusText.Text = ((!flag) ? "Узнаю у сайта…" : ((!Turn.Enabled) ? "Очередь пока выключена" : (flag2 ? ("Включён: " + PStr(dictionary, "family") + ", " + PStr(dictionary, "server")) : "Выключен")));
		Icons.Set(prioGo, (flag2 && flag3) ? "check" : "power", prioSaving ? "Сохраняю…" : ((!flag2) ? "Включить" : (flag3 ? "Сохранить" : "Выключить")));
		prioGo.IsEnabled = flag && !prioSaving;
		prioClaims.Children.Clear();
		List<Dictionary<string, object>> list = PList(dictionary, "claims");
		if (list.Count == 0)
		{
			prioClaims.Children.Add(new TextBlock
			{
				Style = St("Hint"),
				FontSize = 14.0,
				Text = ((!flag) ? "" : ((!Turn.Enabled) ? "Очередь пока выключена: все копии TTC бьют без ожидания." : ((!flag2) ? "Приоритет выключен: все копии TTC бьют без очереди." : "Целей нет. Запустите забив или план — цель появится здесь, и остальные будут её ждать.")))
			});
		}
		foreach (Dictionary<string, object> item in list)
		{
			long num = PLong(item, "fire_at");
			StackPanel stackPanel = new StackPanel();
			stackPanel.Orientation = System.Windows.Controls.Orientation.Horizontal;
			stackPanel.Margin = new Thickness(0.0, 0.0, 0.0, 6.0);
			StackPanel stackPanel2 = stackPanel;
			stackPanel2.Children.Add(new TextBlock
			{
				Style = St("Num"),
				FontSize = 28.0,
				Text = PStr(item, "square"),
				Foreground = SignalB,
				Width = 96.0
			});
			stackPanel2.Children.Add(new TextBlock
			{
				FontSize = 14.0,
				Foreground = Chalk,
				VerticalAlignment = VerticalAlignment.Center,
				Text = ((num > 0) ? ("нажатие в " + War.Hms(num)) : "как только станет можно") + ((PStr(item, "kind") == "plan") ? ", по плану" : "") + ((PStr(item, "who").Length > 0) ? (", " + PStr(item, "who")) : "")
			});
			prioClaims.Children.Add(stackPanel2);
		}
		prioLog.Children.Clear();
		List<Dictionary<string, object>> list2 = PList(dictionary, "log");
		if (list2.Count == 0)
		{
			prioLog.Children.Add(new TextBlock
			{
				Style = St("Hint"),
				Text = (flag ? "Пока никто не ждал." : "")
			});
		}
		else
		{
			prioLog.Children.Add(PrioRow(new string[5] { "Когда", "Семья", "Квадрат", "Ждал", "Чем кончилось" }, head: true, null));
		}
		foreach (Dictionary<string, object> item2 in list2)
		{
			string text4 = PStr(item2, "how");
			string text5 = PStr(item2, "mine");
			bool flag4 = text4.Length == 0;
			string text6 = (flag4 ? ("ждёт вашего " + text5) : ((text4 == "fired") ? ("после вашего " + text5) : ((text4 == "stop") ? "остановлен" : ("ваш " + text5 + " снят"))));
			prioLog.Children.Add(PrioRow(new string[5]
			{
				Clock.Msk(War.FromUnix(PLong(item2, "at"))).ToString("dd.MM HH:mm:ss"),
				(PStr(item2, "fam").Length > 0) ? PStr(item2, "fam") : "не видна",
				PStr(item2, "square"),
				(PStr(item2, "sec").Length > 0) ? (PStr(item2, "sec") + " с") : "…",
				text6
			}, head: false, flag4 ? WarnB : ((text4 == "fired") ? GoB : null)));
		}
		long num2 = PLong(dictionary, "online");
		long num3 = PLong(dictionary, "old");
		SetReady(prioCopiesDot, prioCopiesText, (!flag) ? Dim : ((num3 > 0) ? WarnB : GoB), (!flag) ? "Копии TTC" : ("Копий TTC на связи: " + num2 + ((num3 > 0) ? (", из них без очереди (до " + PStr(dictionary, "min_ver") + "): " + num3) : "")));
		prioCopiesText.ToolTip = ((num3 > 0) ? "Старые версии TTC про очередь не знают и бьют как раньше, пока их не обновят" : null);
	}

	private static Grid PrioRow(string[] cells, bool head, System.Windows.Media.Brush last)
	{
		Grid grid = new Grid();
		grid.Margin = new Thickness(0.0, 0.0, 12.0, head ? 6 : 5);
		Grid grid2 = grid;
		double[] array = new double[5] { 118.0, 0.0, 74.0, 62.0, 158.0 };
		foreach (double num in array)
		{
			grid2.ColumnDefinitions.Add(new ColumnDefinition
			{
				Width = ((num == 0.0) ? new GridLength(1.0, GridUnitType.Star) : new GridLength(num))
			});
		}
		for (int j = 0; j < cells.Length; j++)
		{
			TextBlock textBlock = new TextBlock();
			textBlock.Text = cells[j];
			textBlock.FontSize = (head ? 12.5 : 13.5);
			textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			textBlock.ToolTip = (head ? null : cells[j]);
			textBlock.Foreground = (head ? Fog : (j switch
			{
				0 => Fog, 
				4 => last ?? Fog, 
				_ => Chalk, 
			}));
			TextBlock element = textBlock;
			Grid.SetColumn(element, j);
			grid2.Children.Add(element);
		}
		return grid2;
	}

	private void QueuePriority()
	{
		List<KeyValuePair<string, long>> list = new List<KeyValuePair<string, long>>();
		if (planRunning)
		{
			foreach (PlanEntry entry in pcfg.Entries)
			{
				if (entry == planCurrent || (entry.State != null && entry.State != "blocked"))
				{
					continue;
				}
				DateTime dateTime = PlanFireUtc(entry);
				if (!(dateTime == DateTime.MaxValue))
				{
					long num = Unix(dateTime);
					long num2 = War.NowUnix();
					if (num < num2)
					{
						num = (num2 / 300 + 1) * 300;
					}
					list.Add(new KeyValuePair<string, long>(entry.Target ?? entry.Square, num));
				}
			}
		}
		Turn.SetQueue(list, cfg.Server);
	}
}
