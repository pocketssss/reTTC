using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;

namespace CaptBot;

internal static class Patrol
{
	private const double VX = 52.0;

	private const double VY = 150.0;

	private const double VW = 1180.0;

	private const double VH = 795.0;

	private static MapGrid lastGrid;

	private static DateTime lastAt;

	private static Point lastCursor;

	private static readonly Regex Slash = new Regex("(?<![\\d.,%])(\\d)\\s*[/lIi1|\\\\]\\s*(\\d)(?![\\d%])");

	public static PatrolScan Scan(out string why)
	{
		why = null;
		if (!WinOcr.Available)
		{
			why = "нет распознавания текста";
			return null;
		}
		Game game = new Game();
		if (!game.Find())
		{
			return null;
		}
		game.Refresh();
		if (Native.IsIconic(game.H) || game.Client.Width < 640 || game.Client.Height < 480)
		{
			return null;
		}
		Frame f = game.Shot();
		if (Game.IsBlack(f) || game.Detect(f) != Scr.Map)
		{
			return null;
		}
		Rectangle rectangle = View(game.L, f);
		if (rectangle.Width < 200 || rectangle.Height < 150)
		{
			return null;
		}
		Point[] array = new Point[5]
		{
			new Point(rectangle.Left + 4, rectangle.Top + 4),
			new Point(rectangle.Right - 4, rectangle.Top + 4),
			new Point(rectangle.Left + 4, rectangle.Bottom - 4),
			new Point(rectangle.Right - 4, rectangle.Bottom - 4),
			new Point(rectangle.Left + rectangle.Width / 2, rectangle.Top + rectangle.Height / 2)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Point point = array[i];
			if (!game.VisibleScreen(game.Client.X + point.X, game.Client.Y + point.Y))
			{
				why = "карта перекрыта окном";
				return null;
			}
		}
		DateTime dateTime = Clock.UtcNow();
		Native.GetCursorPos(out var p);
		Point pt = new Point(p.X, p.Y);
		bool flag = new Rectangle(game.Client.X + rectangle.X - 40, game.Client.Y + rectangle.Y - 40, rectangle.Width + 80, rectangle.Height + 80).Contains(pt);
		MapGrid grid;
		PatrolScan patrolScan = Analyze(f, game.L, out grid, out why);
		if (patrolScan == null)
		{
			lastGrid = null;
			return null;
		}
		patrolScan.At = dateTime;
		bool flag2 = lastGrid != null && (dateTime - lastAt).TotalSeconds < 8.0 && Math.Abs(lastGrid.P - grid.P) < grid.P * 0.01 && Math.Abs(lastGrid.Ax - grid.Ax) < 2.0 && Math.Abs(lastGrid.Ay - grid.Ay) < 2.0 && Math.Abs(lastCursor.X - pt.X) + Math.Abs(lastCursor.Y - pt.Y) < 4;
		patrolScan.MayEnd = flag2 && !flag;
		lastGrid = grid;
		lastAt = dateTime;
		lastCursor = pt;
		patrolScan.Limits = ReadLimits(f, game.L);
		return patrolScan;
	}

	public static Rectangle View(Layout l, Frame f)
	{
		Rectangle result = new Rectangle((int)Math.Round(l.X(52.0)), (int)Math.Round(l.Y(150.0)), (int)Math.Round(1180.0 * l.Sx), (int)Math.Round(795.0 * l.Sy));
		result.Intersect(new Rectangle(0, 0, f.W, f.H));
		return result;
	}

	public static PatrolScan Analyze(Frame f, Layout l, out MapGrid grid, out string why)
	{
		why = null;
		Rectangle rectangle = View(l, f);
		double scale = Math.Max(1.0, Math.Min(2.5, 2.0 / Math.Max(0.1, l.Sy)));
		using (Bitmap src = f.ToBitmap(rectangle))
		{
			grid = MapLabels.Read(src, scale, out var _);
		}
		if (grid == null)
		{
			why = "подписи квадратов мелкие — приблизьте карту";
			return null;
		}
		grid = grid.Shift(rectangle.X, rectangle.Y, 1.0);
		if (grid.P < 14.0)
		{
			why = "квадраты слишком мелкие";
			return null;
		}
		PatrolScan patrolScan = FromGrid(f, rectangle, grid);
		if (patrolScan == null)
		{
			why = "видно слишком мало квадратов";
		}
		return patrolScan;
	}

	public static PatrolScan FromGrid(Frame f, Rectangle v, MapGrid grid)
	{
		PatrolScan patrolScan = new PatrolScan();
		patrolScan.P = grid.P;
		PatrolScan patrolScan2 = patrolScan;
		patrolScan2.C0 = Math.Max(1, (int)Math.Ceiling(grid.Col(v.Left + 1) + 0.5));
		patrolScan2.C1 = Math.Min(58, (int)Math.Floor(grid.Col(v.Right - 1) - 0.5));
		patrolScan2.R0 = Math.Max(1, (int)Math.Ceiling(grid.Row(v.Top + 1) + 0.5));
		patrolScan2.R1 = Math.Min(84, (int)Math.Floor(grid.Row(v.Bottom - 1) - 0.5));
		if (patrolScan2.C1 - patrolScan2.C0 < 1 || patrolScan2.R1 - patrolScan2.R0 < 1)
		{
			return null;
		}
		foreach (Zone item in Zones.All)
		{
			if (item.C0 < patrolScan2.C0 || item.C1 > patrolScan2.C1 || item.R0 < patrolScan2.R0 || item.R1 > patrolScan2.R1)
			{
				continue;
			}
			PointF pointF = grid.Cell((double)item.C0 - 0.5, (double)item.R0 - 0.5);
			PointF pointF2 = grid.Cell((double)item.C1 + 0.5, (double)item.R1 + 0.5);
			Rectangle r = Rectangle.FromLTRB((int)Math.Round(pointF.X), (int)Math.Round(pointF.Y), (int)Math.Round(pointF2.X), (int)Math.Round(pointF2.Y));
			if (r.Width >= 10 && r.Height >= 10 && r.Left >= 0 && r.Top >= 0 && r.Right <= f.W && r.Bottom <= f.H)
			{
				int num = Runner.MarkerState(f, r);
				if (num != 0)
				{
					patrolScan2.Marks.Add(new int[2] { item.Id, num });
				}
			}
		}
		return patrolScan2;
	}

	public static int[] ReadLimits(Frame f, Layout l)
	{
		Rectangle r = new Rectangle((int)Math.Round(l.X(120.0)), (int)Math.Round(l.Y(106.0)), (int)Math.Round(900.0 * l.Sx), (int)Math.Round(44.0 * l.Sy));
		r.Intersect(new Rectangle(0, 0, f.W, f.H));
		if (r.Width < 50 || r.Height < 10)
		{
			return null;
		}
		double num = Math.Max(2.0, Math.Min(4.0, Math.Round(2.2 / Math.Max(0.1, l.Sy))));
		List<OcrLine> source;
		using (Bitmap bitmap = f.ToBitmap(r))
		{
			using Bitmap bitmap2 = new Bitmap((int)((double)bitmap.Width * num), (int)((double)bitmap.Height * num));
			using (Graphics graphics = Graphics.FromImage(bitmap2))
			{
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				graphics.DrawImage(bitmap, 0, 0, bitmap2.Width, bitmap2.Height);
			}
			source = WinOcr.Recognize(bitmap2);
		}
		float after = (from ln in source
			where ln.Text.Contains("%")
			select ln.Box.Right).DefaultIfEmpty(0f).Max();
		List<int> list = new List<int>();
		foreach (OcrLine item in from x in source
			where x.Box.X >= after - 2f
			orderby x.Box.X
			select x)
		{
			Match match = Slash.Match(item.Text);
			if (match.Success)
			{
				list.Add(int.Parse(match.Groups[1].Value));
				list.Add(int.Parse(match.Groups[2].Value));
				if (list.Count == 4)
				{
					break;
				}
			}
		}
		if (list.Count != 4 || list[1] < 1 || list[1] > 3 || list[3] < 1 || list[3] > 3 || list[0] > list[1] || list[2] > list[3])
		{
			return null;
		}
		return list.ToArray();
	}
}
