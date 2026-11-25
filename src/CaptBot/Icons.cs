using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CaptBot;

internal static class Icons
{
	private static readonly Dictionary<string, string> D = new Dictionary<string, string>
	{
		{ "chevron-down", "M4.5,6.5 L8,10 L11.5,6.5" },
		{ "chevron-up", "M4.5,9.5 L8,6 L11.5,9.5" },
		{ "chevron-right", "M6.5,4.5 L10,8 L6.5,11.5" },
		{ "close", "M4.5,4.5 L11.5,11.5 M11.5,4.5 L4.5,11.5" },
		{ "minimize", "M4,8.5 L12,8.5" },
		{ "maximize", "M4.5,4.5 H11.5 V11.5 H4.5 Z" },
		{ "restore", "M6.5,4.5 H11.5 V9.5 M4.5,6.5 H9.5 V11.5 H4.5 Z" },
		{ "plus", "M8,4 V12 M4,8 H12" },
		{ "minus", "M4,8 H12" },
		{ "map", "M2.5,4.2 L6,2.8 L10,4.2 L13.5,2.8 V11.8 L10,13.2 L6,11.8 L2.5,13.2 Z M6,2.8 V11.8 M10,4.2 V13.2" },
		{ "strike", "M3,8 A5,5 0 1 1 13,8 A5,5 0 1 1 3,8 Z M8,1.5 V4.5 M8,11.5 V14.5 M1.5,8 H4.5 M11.5,8 H14.5 M7.3,8 A0.7,0.7 0 1 1 8.7,8 A0.7,0.7 0 1 1 7.3,8 Z" },
		{ "lists", "M7,4.5 H13.5 M7,8 H13.5 M7,11.5 H13.5 M2.5,4.5 L3.4,5.4 L5,3.6 M2.5,8 L3.4,8.9 L5,7.1 M2.5,11.5 L3.4,12.4 L5,10.6" },
		{ "vzz", "M2.5,3.5 H13.5 V10.5 H7.5 L4.5,13 V10.5 H2.5 Z M6.3,5.6 L5,7 L6.3,8.4 M9.7,5.6 L11,7 L9.7,8.4" },
		{ "plan", "M3.5,14 V2.5 M3.5,3 H12 L10,5.75 L12,8.5 H3.5" },
		{ "priority", "M3,2.5 H13 M8,13.5 V5.5 M4.5,9 L8,5.5 L11.5,9" },
		{ "scan", "M2.5,5.5 V2.5 H5.5 M10.5,2.5 H13.5 V5.5 M13.5,10.5 V13.5 H10.5 M5.5,13.5 H2.5 V10.5 M8,5.5 V10.5 M5.5,8 H10.5" },
		{ "eye", "M1.5,8 C3.5,4.6 12.5,4.6 14.5,8 C12.5,11.4 3.5,11.4 1.5,8 Z M6.2,8 A1.8,1.8 0 1 1 9.8,8 A1.8,1.8 0 1 1 6.2,8 Z" },
		{ "stop", "M5,5 H11 V11 H5 Z" },
		{ "play", "M5.5,3.8 L12,8 L5.5,12.2 Z" },
		{ "power", "M8,2.5 V7.5 M5.1,4.3 A5,5 0 1 0 10.9,4.3" },
		{ "copy", "M5.5,5.5 H12.5 V12.5 H5.5 Z M3.5,10.5 V3.5 H10.5" },
		{ "refresh", "M12.8,9.2 A5,5 0 1 1 11.6,4.6 M12,2 V4.9 H9.1" },
		{ "users", "M6,7 A2.25,2.25 0 1 1 6,2.5 A2.25,2.25 0 1 1 6,7 Z M1.8,13.2 C2.2,10.4 3.8,9.2 6,9.2 C8.2,9.2 9.8,10.4 10.2,13.2 M10.6,6.6 A1.9,1.9 0 1 0 10.6,2.8 M11.6,9.4 C13.1,9.8 14,11 14.3,13" },
		{ "external", "M6.5,3.5 H12.5 V9.5 M12.5,3.5 L5,11 M10.5,12.5 H3.5 V5.5" },
		{ "link", "M6.8,9.2 L9.2,6.8 M7.6,4.6 L8.8,3.4 A2.6,2.6 0 0 1 12.6,7.2 L11.4,8.4 M8.4,11.4 L7.2,12.6 A2.6,2.6 0 0 1 3.4,8.8 L4.6,7.6" },
		{ "bot", "M3.5,6 H12.5 V12.5 H3.5 Z M8,6 V3.8 M8,2.5 A0.6,0.6 0 1 1 7.99,2.5 M6,9 V9.6 M10,9 V9.6 M1.8,8.5 V10.5 M14.2,8.5 V10.5" },
		{ "shield", "M8,1.8 L13,3.8 V7.5 C13,10.8 10.8,13.2 8,14.2 C5.2,13.2 3,10.8 3,7.5 V3.8 Z" },
		{ "swords", "M2.5,2.5 L9.5,9.5 M2.5,2.5 H5 M2.5,2.5 V5 M13.5,2.5 L6.5,9.5 M13.5,2.5 H11 M13.5,2.5 V5 M8,11 L10.5,13.5 M8,11 L5.5,13.5 M11.8,10.2 L10,12 M4.2,10.2 L6,12" },
		{ "clock", "M2.5,8 A5.5,5.5 0 1 1 13.5,8 A5.5,5.5 0 1 1 2.5,8 Z M8,4.8 V8 L10.3,9.4" },
		{ "check", "M3.5,8.5 L6.5,11.5 L12.5,4.5" },
		{ "sliders", "M2.5,5 H5.5 M8.5,5 H13.5 M5.5,5 A1.5,1.5 0 1 1 8.5,5 A1.5,1.5 0 1 1 5.5,5 Z M2.5,11 H7.5 M10.5,11 H13.5 M7.5,11 A1.5,1.5 0 1 1 10.5,11 A1.5,1.5 0 1 1 7.5,11 Z" }
	};

	private static readonly HashSet<string> Filled = new HashSet<string> { "play", "stop" };

	public static bool Has(string name)
	{
		if (name != null)
		{
			return D.ContainsKey(name);
		}
		return false;
	}

	public static FrameworkElement Make(string name, double size)
	{
		return Make(name, size, null);
	}

	public static FrameworkElement Make(string name, double size, Brush brush)
	{
		if (name == null || !D.TryGetValue(name, out var value))
		{
			Border border = new Border();
			border.Width = size;
			border.Height = size;
			return border;
		}
		Path path = new Path();
		path.Data = Geometry.Parse(value);
		path.StrokeThickness = 1.5;
		path.StrokeLineJoin = PenLineJoin.Round;
		path.StrokeStartLineCap = PenLineCap.Round;
		path.StrokeEndLineCap = PenLineCap.Round;
		Path path2 = path;
		if (brush != null)
		{
			path2.Stroke = brush;
			if (Filled.Contains(name))
			{
				path2.Fill = brush;
			}
		}
		else
		{
			path2.SetBinding(Shape.StrokeProperty, new Binding("(TextElement.Foreground)")
			{
				RelativeSource = RelativeSource.Self
			});
			if (Filled.Contains(name))
			{
				path2.SetBinding(Shape.FillProperty, new Binding("(TextElement.Foreground)")
				{
					RelativeSource = RelativeSource.Self
				});
			}
		}
		Canvas canvas = new Canvas();
		canvas.Width = 16.0;
		canvas.Height = 16.0;
		Canvas canvas2 = canvas;
		canvas2.Children.Add(path2);
		Viewbox viewbox = new Viewbox();
		viewbox.Width = size;
		viewbox.Height = size;
		viewbox.Child = canvas2;
		viewbox.Stretch = Stretch.Uniform;
		viewbox.VerticalAlignment = VerticalAlignment.Center;
		viewbox.IsHitTestVisible = false;
		return viewbox;
	}

	public static StackPanel Label(string icon, string text, double size)
	{
		StackPanel stackPanel = new StackPanel();
		stackPanel.Orientation = Orientation.Horizontal;
		stackPanel.VerticalAlignment = VerticalAlignment.Center;
		StackPanel stackPanel2 = stackPanel;
		if (Has(icon))
		{
			stackPanel2.Children.Add(Make(icon, size));
		}
		stackPanel2.Children.Add(new TextBlock
		{
			Text = text,
			VerticalAlignment = VerticalAlignment.Center,
			Margin = new Thickness(Has(icon) ? (size * 0.55) : 0.0, 0.0, 0.0, 1.0)
		});
		return stackPanel2;
	}

	public static void Set(Button b, string icon, string text)
	{
		if (b != null)
		{
			string text2 = icon + "|" + text;
			if (!(b.Tag as string == text2))
			{
				b.Tag = text2;
				b.Content = Label(icon, text, (b.FontSize >= 15.0) ? 17 : ((b.FontSize >= 13.5) ? 15 : 13));
			}
		}
	}
}
