using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CaptBot;

internal class MapView : FrameworkElement
{
	private const double ExtMinX = -7869.07;

	private const double ExtMaxY = 10553.85;

	private const double ExtW = 16759.41;

	private const string TilePath = "/public/master/wiki/map/black/{0}/{1}/{2}.png";

	public WarState State;

	public string OwnKey = "";

	public string HighlightKey = "";

	public Zone Selected;

	public Zone Hover;

	public Zone Target;

	public Zone Watch;

	public Brush TargetBrush;

	public double CenterCellPx = 36.0;

	private Zone pendingCenter;

	private double scale = 0.06;

	private double offX;

	private double offY;

	private bool fitted;

	private bool dragging;

	private bool moved;

	private Point dragFrom;

	private Point dragOff;

	private static readonly Brush Bg = Frozen(new SolidColorBrush(Color.FromRgb(14, 16, 19)));

	private static readonly Pen GridPen = FrozenPen(Color.FromArgb(36, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.6);

	private static readonly Pen OwnPen = FrozenPen(Color.FromRgb(139, 92, 246), 1.8);

	private static readonly Pen CaptPen = FrozenPen(Color.FromRgb(240, 80, 110), 2.2);

	private static readonly Pen LiveGoingPen = FrozenPen(Color.FromRgb(240, 80, 110), 2.6);

	private static readonly Pen LivePlannedPen = DashedPen(Color.FromRgb(242, 179, 61), 2.2);

	private static readonly Pen HoverPen = FrozenPen(Color.FromArgb(208, byte.MaxValue, byte.MaxValue, byte.MaxValue), 1.4);

	private static readonly Pen SelPen = FrozenPen(Color.FromRgb(236, 231, 220), 3.0);

	private static readonly Pen WatchPen = FrozenPen(Color.FromRgb(240, 80, 110), 2.6);

	private static readonly Brush WatchFill = Frozen(new SolidColorBrush(Color.FromArgb(80, 240, 80, 110)));

	private static readonly Brush MansionB = Frozen(new SolidColorBrush(Color.FromRgb(242, 179, 61)));

	private static readonly Brush BusinessB = Frozen(new SolidColorBrush(Color.FromRgb(70, 211, 154)));

	private static readonly Brush SpecialB = Frozen(new SolidColorBrush(Color.FromRgb(236, 231, 220)));

	private static readonly Pen MarkPen = FrozenPen(Color.FromArgb(208, 21, 23, 27), 1.1);

	private static readonly Brush LabelB = Frozen(new SolidColorBrush(Color.FromArgb(224, 236, 231, 220)));

	private static readonly Typeface Face = new Typeface(new FontFamily("Bahnschrift, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Condensed);

	private static readonly Typeface FaceName = new Typeface(new FontFamily("Bahnschrift, Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Condensed);

	private static readonly Brush DarkTextB = Frozen(new SolidColorBrush(Color.FromArgb(232, 21, 23, 27)));

	private readonly Dictionary<string, Brush> fills = new Dictionary<string, Brush>();

	private readonly Dictionary<string, Pen> edges = new Dictionary<string, Pen>();

	private static readonly Brush TimerBg = Frozen(new SolidColorBrush(Color.FromArgb(224, 14, 16, 19)));

	private static readonly Brush GoingTextB = Frozen(new SolidColorBrush(Color.FromRgb(240, 80, 110)));

	private static readonly Brush PlannedTextB = Frozen(new SolidColorBrush(Color.FromRgb(242, 179, 61)));

	private static readonly string[] TileHosts = new string[4] { "cdn-ru.majestic-files.net", "cdn-eu.majestic-files.net", "cdn.majestic-files.com", "cdn.majestic-files.net" };

	private static volatile int tileHost;

	private static readonly Dictionary<string, BitmapSource> tiles = new Dictionary<string, BitmapSource>();

	private static readonly HashSet<string> loading = new HashSet<string>();

	private static readonly Dictionary<string, DateTime> failed = new Dictionary<string, DateTime>();

	private static int inFlight;

	private static string TileDir => Path.Combine(AppPaths.Data, "maptiles");

	public event Action<Zone> ZoneClick = delegate
	{
	};

	public event Action<Zone> HoverChanged = delegate
	{
	};

	private static Pen DashedPen(Color c, double w)
	{
		Pen pen = new Pen(new SolidColorBrush(c), w);
		pen.DashStyle = new DashStyle(new double[2] { 2.0, 1.4 }, 0.0);
		Pen pen2 = pen;
		pen2.Freeze();
		return pen2;
	}

	private static Brush Frozen(Brush b)
	{
		b.Freeze();
		return b;
	}

	private static Pen FrozenPen(Color c, double w)
	{
		Pen pen = new Pen(new SolidColorBrush(c), w);
		pen.LineJoin = PenLineJoin.Round;
		pen.Freeze();
		return pen;
	}

	public MapView()
	{
		base.ClipToBounds = true;
		base.Focusable = true;
		base.Cursor = Cursors.Arrow;
		base.SnapsToDevicePixels = false;
	}

	private Point ToScreen(double gx, double gy)
	{
		return new Point(offX + (gx - -3570.0) * scale, offY + (8070.0 - gy) * scale);
	}

	private Rect ZoneRect(Zone z)
	{
		Point point = ToScreen(z.X0, z.YTop);
		Point point2 = ToScreen(z.X1, z.YBottom);
		return new Rect(point, point2);
	}

	private Zone ZoneAt(Point p)
	{
		double num = -3570.0 + (p.X - offX) / scale;
		double num2 = 8070.0 - (p.Y - offY) / scale;
		int col = (int)Math.Floor((num - -3570.0) / 140.0) + 1;
		int row = (int)Math.Floor((8070.0 - num2) / 140.0) + 1;
		return Zones.At(col, row);
	}

	public void Fit()
	{
		if (!(base.ActualWidth < 10.0) && !(base.ActualHeight < 10.0))
		{
			double num = 8120.0;
			double num2 = 11760.0;
			scale = Math.Min((base.ActualWidth - 24.0) / num, (base.ActualHeight - 24.0) / num2);
			offX = (base.ActualWidth - num * scale) / 2.0;
			offY = (base.ActualHeight - num2 * scale) / 2.0;
			fitted = true;
			InvalidateVisual();
		}
	}

	public void CenterOn(Zone z)
	{
		if (z != null)
		{
			if (base.ActualWidth < 10.0 || base.ActualHeight < 10.0)
			{
				pendingCenter = z;
				return;
			}
			pendingCenter = null;
			double val = Math.Max(scale, CenterCellPx / 140.0);
			scale = Math.Min(val, 1.2);
			double num = (z.X0 + z.X1) / 2.0;
			double num2 = (z.YTop + z.YBottom) / 2.0;
			offX = base.ActualWidth / 2.0 - (num - -3570.0) * scale;
			offY = base.ActualHeight / 2.0 - (8070.0 - num2) * scale;
			fitted = true;
			InvalidateVisual();
		}
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo info)
	{
		base.OnRenderSizeChanged(info);
		if (pendingCenter != null)
		{
			CenterOn(pendingCenter);
		}
		else if (!fitted)
		{
			Fit();
		}
		else if (info.PreviousSize.Width >= 10.0 && info.PreviousSize.Height >= 10.0)
		{
			offX += (info.NewSize.Width - info.PreviousSize.Width) / 2.0;
			offY += (info.NewSize.Height - info.PreviousSize.Height) / 2.0;
			InvalidateVisual();
		}
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		Point position = e.GetPosition(this);
		double num = ((e.Delta > 0) ? 1.25 : 0.8);
		double num2 = Math.Max(0.02, Math.Min(1.6, scale * num));
		num = num2 / scale;
		scale = num2;
		offX = position.X - (position.X - offX) * num;
		offY = position.Y - (position.Y - offY) * num;
		InvalidateVisual();
		e.Handled = true;
	}

	protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
	{
		Focus();
		dragging = true;
		moved = false;
		dragFrom = e.GetPosition(this);
		dragOff = new Point(offX, offY);
		CaptureMouse();
		e.Handled = true;
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		Point position = e.GetPosition(this);
		if (dragging)
		{
			Vector vector = position - dragFrom;
			if (!moved && vector.Length > 4.0)
			{
				moved = true;
				base.Cursor = Cursors.SizeAll;
			}
			if (moved)
			{
				offX = dragOff.X + vector.X;
				offY = dragOff.Y + vector.Y;
				InvalidateVisual();
				return;
			}
		}
		Zone zone = ZoneAt(position);
		if (zone != Hover)
		{
			Hover = zone;
			this.HoverChanged(zone);
			InvalidateVisual();
		}
	}

	protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
	{
		if (!dragging)
		{
			return;
		}
		dragging = false;
		ReleaseMouseCapture();
		base.Cursor = Cursors.Arrow;
		if (!moved)
		{
			Zone zone = ZoneAt(e.GetPosition(this));
			if (zone != null)
			{
				Selected = zone;
				InvalidateVisual();
				this.ZoneClick(zone);
			}
		}
	}

	protected override void OnMouseLeave(MouseEventArgs e)
	{
		if (Hover != null)
		{
			Hover = null;
			this.HoverChanged(null);
			InvalidateVisual();
		}
	}

	public static Color FamilyColor(WarFamily f)
	{
		if (f != null && TryColor(f.Color, out var c))
		{
			return c;
		}
		uint num = 2166136261u;
		string text = ((f == null) ? "" : f.Key);
		foreach (char c2 in text)
		{
			num = (num ^ c2) * 16777619;
		}
		return Hsl((double)(num % 360) / 360.0, 0.62, 0.58);
	}

	public static double Luma(Color c)
	{
		return (0.2126 * (double)(int)c.R + 0.7152 * (double)(int)c.G + 0.0722 * (double)(int)c.B) / 255.0;
	}

	public static Color OutlineColor(WarFamily f)
	{
		Color color = FamilyColor(f);
		if (!(Luma(color) < 0.16))
		{
			return color;
		}
		return Color.FromRgb(154, 158, 166);
	}

	private static bool TryColor(string s, out Color c)
	{
		c = Colors.Transparent;
		if (string.IsNullOrEmpty(s) || s.Length != 7 || s[0] != '#')
		{
			return false;
		}
		try
		{
			c = (Color)ColorConverter.ConvertFromString(s);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static Color Hsl(double h, double s, double l)
	{
		Func<double, double> func = delegate(double n)
		{
			double num = (n + h * 12.0) % 12.0;
			return l - s * Math.Min(l, 1.0 - l) * Math.Max(-1.0, Math.Min(num - 3.0, Math.Min(9.0 - num, 1.0)));
		};
		return Color.FromRgb((byte)(func(0.0) * 255.0), (byte)(func(8.0) * 255.0), (byte)(func(4.0) * 255.0));
	}

	private Brush Fill(WarFamily f, byte alpha)
	{
		string key = f.Key + ":" + alpha;
		if (!fills.TryGetValue(key, out var value))
		{
			Color color = FamilyColor(f);
			value = Frozen(new SolidColorBrush(Color.FromArgb(alpha, color.R, color.G, color.B)));
			fills[key] = value;
		}
		return value;
	}

	private Pen Edge(WarFamily f)
	{
		if (!edges.TryGetValue(f.Key, out var value))
		{
			Color color = OutlineColor(f);
			value = FrozenPen(Color.FromArgb(192, color.R, color.G, color.B), (Luma(FamilyColor(f)) < 0.16) ? 1.1 : 0.9);
			edges[f.Key] = value;
		}
		return value;
	}

	protected override void OnRender(DrawingContext dc)
	{
		Rect rect = new Rect(0.0, 0.0, base.ActualWidth, base.ActualHeight);
		dc.DrawRectangle(Bg, null, rect);
		DrawTiles(dc, rect);
		double num = 140.0 * scale;
		long num2 = War.NowUnix();
		Dictionary<int, WarCapt> dictionary = new Dictionary<int, WarCapt>();
		if (State != null)
		{
			foreach (WarCapt item in State.Active(num2, null))
			{
				if (item.ZoneId > 0)
				{
					bool flag = item.Start <= num2;
					if (!dictionary.TryGetValue(item.ZoneId, out var value) || (flag && value.Start > num2) || (flag == value.Start <= num2 && item.Start < value.Start))
					{
						dictionary[item.ZoneId] = item;
					}
				}
			}
		}
		double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
		List<Rect> list = new List<Rect>();
		foreach (Zone item2 in Zones.All)
		{
			Rect rect2 = ZoneRect(item2);
			if (rect2.IntersectsWith(rect))
			{
				WarFamily warFamily = ((State == null) ? null : State.OwnerOf(item2));
				byte alpha = (byte)((warFamily != null && HighlightKey.Length > 0 && warFamily.Key == HighlightKey) ? 216u : ((HighlightKey.Length > 0) ? 64u : ((num >= 30.0) ? 106u : 128u)));
				bool flag2 = item2 == Target && TargetBrush != null;
				Brush brush = (flag2 ? TargetBrush : ((warFamily == null) ? null : Fill(warFamily, alpha)));
				Pen pen = ((warFamily != null) ? Edge(warFamily) : ((num >= 6.0) ? GridPen : null));
				Rect rectangle = ((num >= 5.0) ? Inflate(rect2, -0.5) : rect2);
				dc.DrawRectangle(brush, pen, rectangle);
				if (warFamily != null && OwnKey.Length > 0 && warFamily.Key == OwnKey)
				{
					list.Add(rect2);
				}
				bool flag3 = rect2.Width >= 26.0 && rect2.Height >= 16.0;
				ZoneType zoneType = ZoneTypes.Of(item2.Sub);
				if (item2.Mansion)
				{
					DrawMansion(dc, rect2, num, flag3);
				}
				else if (zoneType != null && ZoneIcons.Has(item2.Sub) && !ZoneIcons.Draw(dc, item2, rect2, num, flag3) && (zoneType.Business || zoneType.Around))
				{
					DrawTypeMark(dc, rect2, num, flag3, zoneType.Business);
				}
				if (flag3)
				{
					DrawLabels(dc, item2, warFamily, rect2, alpha, pixelsPerDip, flag2);
				}
			}
		}
		foreach (Rect item3 in list)
		{
			dc.DrawRectangle(null, OwnPen, Inflate(item3, -1.0));
		}
		if (Watch != null)
		{
			dc.DrawRectangle(WatchFill, WatchPen, Inflate(ZoneRect(Watch), -1.0));
		}
		foreach (WarCapt value2 in dictionary.Values)
		{
			Zone zone = Zones.ById(value2.ZoneId);
			if (zone == null)
			{
				continue;
			}
			Rect r = ZoneRect(zone);
			if (r.IntersectsWith(rect))
			{
				bool flag4 = value2.Start <= num2;
				dc.DrawRectangle(null, flag4 ? CaptPen : LivePlannedPen, Inflate(r, -1.0));
				if (r.Width >= 40.0 && r.Height >= 44.0)
				{
					DrawTimer(dc, r, CaptTimer(value2, num2), flag4 ? "идёт" : Mins(value2.Start - num2), flag4, pixelsPerDip);
				}
			}
		}
		if (State != null)
		{
			foreach (LiveCapt item4 in State.Live)
			{
				if (item4.Active(num2))
				{
					Zone zone2 = Zones.ById(item4.ZoneId);
					if (zone2 != null)
					{
						dc.DrawRectangle(null, (item4.StartedAt > 0) ? LiveGoingPen : LivePlannedPen, Inflate(ZoneRect(zone2), -1.5));
					}
				}
			}
		}
		if (Hover != null && Hover != Selected)
		{
			dc.DrawRectangle(null, HoverPen, ZoneRect(Hover));
		}
		if (Selected != null)
		{
			dc.DrawRectangle(null, SelPen, Inflate(ZoneRect(Selected), 1.0));
		}
	}

	private void DrawLabels(DrawingContext dc, Zone z, WarFamily f, Rect r, byte alpha, double dpi, bool light)
	{
		double num = ((f == null) ? 0.08 : (Luma(FamilyColor(f)) * (double)(int)alpha / 255.0 + 0.08 * (1.0 - (double)(int)alpha / 255.0)));
		Brush foreground = ((!light && num > 0.42) ? DarkTextB : LabelB);
		double num2 = Math.Max(9.0, Math.Min(15.0, Math.Min(r.Width, r.Height) * 0.24));
		FormattedText formattedText = new FormattedText(z.Label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, num2, foreground, dpi);
		FormattedText formattedText2 = null;
		double num3 = r.Width - 6.0;
		if (f != null && r.Height >= num2 * 2.4 + 4.0)
		{
			double emSize = num2 * 0.86;
			formattedText2 = new FormattedText(f.Name, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, FaceName, emSize, foreground, dpi);
			if (formattedText2.Width > num3 && !string.IsNullOrEmpty(f.Tag))
			{
				formattedText2 = new FormattedText(f.Tag, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, FaceName, emSize, foreground, dpi);
			}
			if (formattedText2.Width > num3)
			{
				formattedText2.MaxTextWidth = Math.Max(4.0, num3);
				formattedText2.MaxLineCount = 1;
				formattedText2.Trimming = TextTrimming.CharacterEllipsis;
			}
		}
		double num4 = formattedText.Height + ((formattedText2 != null) ? (formattedText2.Height - 1.0) : 0.0);
		double num5 = r.Y + (r.Height - num4) / 2.0;
		dc.DrawText(formattedText, new Point(r.X + (r.Width - formattedText.Width) / 2.0, num5));
		if (formattedText2 != null)
		{
			dc.DrawText(formattedText2, new Point(r.X + (r.Width - Math.Min(formattedText2.Width, num3)) / 2.0, num5 + formattedText.Height - 1.0));
		}
	}

	public static string Mins(long s)
	{
		if (s < 60)
		{
			return "<1 мин";
		}
		long num = (s + 59) / 60;
		if (num >= 60)
		{
			return num / 60 + " ч " + (num % 60).ToString("00") + " мин";
		}
		return num + " мин";
	}

	public static string CaptTimer(WarCapt c, long now)
	{
		if (c.Start > now)
		{
			return "через " + Mins(c.Start - now);
		}
		if (c.ExactEnd || c.EndLo <= now || c.EndLo == c.EndHi)
		{
			return "до " + War.Hm(c.EndHi);
		}
		return "до " + War.Hm(c.EndLo) + "–" + War.Hm(c.EndHi);
	}

	private void DrawTimer(DrawingContext dc, Rect r, string text, string shortText, bool going, double dpi)
	{
		double emSize = Math.Max(9.0, Math.Min(12.5, r.Height * 0.2));
		Brush foreground = (going ? GoingTextB : PlannedTextB);
		FormattedText formattedText = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, emSize, foreground, dpi);
		if (formattedText.Width > r.Width - 8.0)
		{
			formattedText = new FormattedText(shortText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, emSize, foreground, dpi);
		}
		if (!(formattedText.Width > r.Width - 4.0))
		{
			Rect rectangle = new Rect(r.X + (r.Width - formattedText.Width) / 2.0 - 3.0, r.Bottom - formattedText.Height - 4.0, formattedText.Width + 6.0, formattedText.Height + 1.0);
			dc.DrawRoundedRectangle(TimerBg, null, rectangle, 3.0, 3.0);
			dc.DrawText(formattedText, new Point(rectangle.X + 3.0, rectangle.Y + 0.5));
		}
	}

	private static Rect Inflate(Rect r, double d)
	{
		r.Inflate(d, d);
		if (!(r.Width > 0.0) || !(r.Height > 0.0))
		{
			return new Rect(r.X, r.Y, Math.Max(r.Width, 0.5), Math.Max(r.Height, 0.5));
		}
		return r;
	}

	private static string Short(string s, int n)
	{
		if (s.Length > n)
		{
			return s.Substring(0, n - 1) + "…";
		}
		return s;
	}

	private void DrawTypeMark(DrawingContext dc, Rect r, double cellPx, bool corner, bool business)
	{
		double num = Math.Max(2.5, Math.Min(corner ? 6 : 8, cellPx * (corner ? 0.16 : 0.28)));
		Point center = (corner ? new Point(r.Right - num - 3.0, r.Y + num + 3.0) : new Point(r.X + r.Width / 2.0, r.Y + r.Height / 2.0));
		if (business)
		{
			dc.DrawEllipse(BusinessB, MarkPen, center, num * 0.85, num * 0.85);
			return;
		}
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(new Point(center.X, center.Y - num), isFilled: true, isClosed: true);
			streamGeometryContext.LineTo(new Point(center.X + num, center.Y), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(center.X, center.Y + num), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(center.X - num, center.Y), isStroked: true, isSmoothJoin: false);
		}
		streamGeometry.Freeze();
		dc.DrawGeometry(SpecialB, MarkPen, streamGeometry);
	}

	private void DrawMansion(DrawingContext dc, Rect r, double cellPx, bool corner)
	{
		double num = Math.Max(3.0, Math.Min(corner ? 7 : 10, cellPx * (corner ? 0.18 : 0.32)));
		Point point = (corner ? new Point(r.Right - num - 3.0, r.Y + num + 3.0) : new Point(r.X + r.Width / 2.0, r.Y + r.Height / 2.0));
		StreamGeometry streamGeometry = new StreamGeometry();
		using (StreamGeometryContext streamGeometryContext = streamGeometry.Open())
		{
			streamGeometryContext.BeginFigure(new Point(point.X, point.Y - num), isFilled: true, isClosed: true);
			streamGeometryContext.LineTo(new Point(point.X + num, point.Y - num * 0.1), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(point.X + num * 0.7, point.Y - num * 0.1), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(point.X + num * 0.7, point.Y + num * 0.8), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(point.X - num * 0.7, point.Y + num * 0.8), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(point.X - num * 0.7, point.Y - num * 0.1), isStroked: true, isSmoothJoin: false);
			streamGeometryContext.LineTo(new Point(point.X - num, point.Y - num * 0.1), isStroked: true, isSmoothJoin: false);
		}
		streamGeometry.Freeze();
		dc.DrawGeometry(MansionB, null, streamGeometry);
	}

	private void DrawTiles(DrawingContext dc, Rect view)
	{
		int i;
		for (i = 0; i < 7 && 256.0 * Math.Pow(2.0, i) / 16759.41 < scale * 0.9; i++)
		{
		}
		int num = 1 << i;
		double num2 = 16759.41 / (double)num;
		double num3 = -3570.0 + (0.0 - offX) / scale;
		double num4 = -3570.0 + (view.Width - offX) / scale;
		double num5 = 8070.0 - (0.0 - offY) / scale;
		double num6 = 8070.0 - (view.Height - offY) / scale;
		int num7 = Math.Max(0, (int)Math.Floor((num3 - -7869.07) / num2));
		int num8 = Math.Min(num - 1, (int)Math.Floor((num4 - -7869.07) / num2));
		int num9 = Math.Max(0, (int)Math.Floor((10553.85 - num5) / num2));
		int num10 = Math.Min(num - 1, (int)Math.Floor((10553.85 - num6) / num2));
		dc.PushOpacity(0.72);
		for (int j = num7; j <= num8; j++)
		{
			for (int k = num9; k <= num10; k++)
			{
				Point point = ToScreen(-7869.07 + (double)j * num2, 10553.85 - (double)k * num2);
				Point point2 = ToScreen(-7869.07 + (double)(j + 1) * num2, 10553.85 - (double)(k + 1) * num2);
				Rect rect = new Rect(point, point2);
				BitmapSource bitmapSource = Tile(i, j, k);
				if (bitmapSource == null && i > 0)
				{
					DrawParent(dc, i, j, k, rect);
				}
				else if (bitmapSource != null)
				{
					dc.DrawImage(bitmapSource, rect);
				}
			}
		}
		dc.Pop();
	}

	private void DrawParent(DrawingContext dc, int z, int tx, int ty, Rect r)
	{
		for (int i = 1; i <= z; i++)
		{
			if (tiles.TryGetValue(Key(z - i, tx >> i, ty >> i), out var value))
			{
				int num = 1 << i;
				double num2 = (double)value.PixelWidth / (double)num;
				CroppedBitmap imageSource = new CroppedBitmap(value, new Int32Rect((int)((double)(tx % num) * num2), (int)((double)(ty % num) * num2), Math.Max(1, (int)num2), Math.Max(1, (int)num2)));
				dc.DrawImage(imageSource, r);
				break;
			}
		}
	}

	private static string Key(int z, int x, int y)
	{
		return z + "/" + x + "/" + y;
	}

	private BitmapSource Tile(int z, int x, int y)
	{
		string i = Key(z, x, y);
		if (tiles.TryGetValue(i, out var value))
		{
			return value;
		}
		if (failed.TryGetValue(i, out var value2) && (DateTime.UtcNow - value2).TotalSeconds < 60.0)
		{
			return null;
		}
		if (loading.Contains(i) || inFlight >= 6)
		{
			return null;
		}
		loading.Add(i);
		inFlight++;
		ThreadPool.QueueUserWorkItem(delegate
		{
			BitmapSource img = null;
			try
			{
				string text = Path.Combine(TileDir, z.ToString(), x.ToString(), y + ".png");
				if (!File.Exists(text))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(text));
					ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
					byte[] array = null;
					int num = tileHost;
					for (int j = 0; j < TileHosts.Length; j++)
					{
						if (array != null)
						{
							break;
						}
						int num2 = (num + j) % TileHosts.Length;
						try
						{
							using (WebClient webClient = new WebClient())
							{
								webClient.Headers[HttpRequestHeader.UserAgent] = "TTC/2.9.13";
								array = webClient.DownloadData("https://" + TileHosts[num2] + $"/public/master/wiki/map/black/{z}/{x}/{y}.png");
							}
							if (array != null && array.Length > 8 && array[1] == 80 && array[2] == 78 && array[3] == 71)
							{
								tileHost = num2;
							}
							else
							{
								array = null;
							}
						}
						catch
						{
							array = null;
						}
					}
					if (array == null)
					{
						throw new IOException("тайл не скачался ни с одного зеркала");
					}
					File.WriteAllBytes(text + ".tmp", array);
					if (File.Exists(text))
					{
						File.Delete(text);
					}
					File.Move(text + ".tmp", text);
				}
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.UriSource = new Uri(text);
				bitmapImage.EndInit();
				bitmapImage.Freeze();
				img = bitmapImage;
			}
			catch
			{
			}
			base.Dispatcher.BeginInvoke((Action)delegate
			{
				loading.Remove(i);
				inFlight--;
				if (img != null)
				{
					tiles[i] = img;
					failed.Remove(i);
				}
				else
				{
					failed[i] = DateTime.UtcNow;
				}
				InvalidateVisual();
			});
		});
		return null;
	}
}
