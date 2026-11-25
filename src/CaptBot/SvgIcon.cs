using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace CaptBot;

internal static class SvgIcon
{
	private static readonly Color Default = Color.FromRgb(236, 231, 220);

	public static ImageSource Render(string svg)
	{
		XmlDocument xmlDocument = new XmlDocument();
		xmlDocument.XmlResolver = null;
		XmlDocument xmlDocument2 = xmlDocument;
		xmlDocument2.LoadXml(svg);
		XmlElement documentElement = xmlDocument2.DocumentElement;
		double num = Num(documentElement.GetAttribute("width"), 24.0);
		double num2 = Num(documentElement.GetAttribute("height"), 24.0);
		double x = 0.0;
		double y = 0.0;
		double width = num;
		double height = num2;
		string[] array = documentElement.GetAttribute("viewBox").Split(new char[2] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
		if (array.Length == 4)
		{
			x = Num(array[0], 0.0);
			y = Num(array[1], 0.0);
			width = Num(array[2], num);
			height = Num(array[3], num2);
		}
		DrawingGroup drawingGroup = new DrawingGroup();
		drawingGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(x, y, width, height))));
		Walk(documentElement, drawingGroup, Fill(documentElement.GetAttribute("fill"), Default));
		drawingGroup.Freeze();
		DrawingImage drawingImage = new DrawingImage(drawingGroup);
		drawingImage.Freeze();
		return drawingImage;
	}

	private static void Walk(XmlElement el, DrawingGroup g, Color? inherited)
	{
		foreach (XmlNode childNode in el.ChildNodes)
		{
			if (!(childNode is XmlElement xmlElement))
			{
				continue;
			}
			Color? inherited2 = (xmlElement.HasAttribute("fill") ? Fill(xmlElement.GetAttribute("fill"), inherited ?? Default) : inherited);
			double op = (xmlElement.HasAttribute("opacity") ? Num(xmlElement.GetAttribute("opacity"), 1.0) : (xmlElement.HasAttribute("fill-opacity") ? Num(xmlElement.GetAttribute("fill-opacity"), 1.0) : 1.0));
			try
			{
				switch (xmlElement.LocalName)
				{
				case "g":
					Walk(xmlElement, g, inherited2);
					break;
				case "path":
					if (inherited2.HasValue && xmlElement.HasAttribute("d"))
					{
						Geometry geometry = Geometry.Parse(((xmlElement.GetAttribute("fill-rule") == "evenodd") ? "F0 " : "F1 ") + xmlElement.GetAttribute("d"));
						g.Children.Add(new GeometryDrawing(Brush(inherited2.Value, op), null, geometry));
					}
					break;
				case "rect":
					if (inherited2.HasValue)
					{
						g.Children.Add(new GeometryDrawing(Brush(inherited2.Value, op), null, new RectangleGeometry(new Rect(Num(xmlElement.GetAttribute("x"), 0.0), Num(xmlElement.GetAttribute("y"), 0.0), Num(xmlElement.GetAttribute("width"), 0.0), Num(xmlElement.GetAttribute("height"), 0.0)), Num(xmlElement.GetAttribute("rx"), 0.0), Num(xmlElement.GetAttribute("ry"), Num(xmlElement.GetAttribute("rx"), 0.0)))));
					}
					break;
				case "circle":
					if (inherited2.HasValue)
					{
						double num2 = Num(xmlElement.GetAttribute("r"), 0.0);
						g.Children.Add(new GeometryDrawing(Brush(inherited2.Value, op), null, new EllipseGeometry(new Point(Num(xmlElement.GetAttribute("cx"), 0.0), Num(xmlElement.GetAttribute("cy"), 0.0)), num2, num2)));
					}
					break;
				case "image":
				{
					string attribute = xmlElement.GetAttribute("href");
					if (string.IsNullOrEmpty(attribute))
					{
						attribute = xmlElement.GetAttribute("href", "http://www.w3.org/1999/xlink");
					}
					int num = attribute.IndexOf(',');
					if (attribute.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase) && num >= 0 && attribute.IndexOf(";base64", 0, num, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						BitmapImage bitmapImage = new BitmapImage();
						bitmapImage.BeginInit();
						bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
						bitmapImage.StreamSource = new MemoryStream(Convert.FromBase64String(attribute.Substring(num + 1)));
						bitmapImage.DecodePixelWidth = 64;
						bitmapImage.EndInit();
						bitmapImage.Freeze();
						g.Children.Add(new ImageDrawing(bitmapImage, new Rect(Num(xmlElement.GetAttribute("x"), 0.0), Num(xmlElement.GetAttribute("y"), 0.0), Num(xmlElement.GetAttribute("width"), 0.0), Num(xmlElement.GetAttribute("height"), 0.0))));
					}
					break;
				}
				}
			}
			catch
			{
			}
		}
	}

	private static Brush Brush(Color c, double op)
	{
		SolidColorBrush solidColorBrush = new SolidColorBrush(c);
		solidColorBrush.Opacity = Math.Max(0.0, Math.Min(1.0, op));
		SolidColorBrush solidColorBrush2 = solidColorBrush;
		solidColorBrush2.Freeze();
		return solidColorBrush2;
	}

	private static Color? Fill(string v, Color inherited)
	{
		v = (v ?? "").Trim();
		if (v.Length == 0)
		{
			return inherited;
		}
		switch (v)
		{
		case "none":
		case "transparent":
			return null;
		default:
			if (!v.StartsWith("url("))
			{
				try
				{
					return (Color)ColorConverter.ConvertFromString(v);
				}
				catch
				{
					return inherited;
				}
			}
			goto case "currentColor";
		case "currentColor":
			return Default;
		}
	}

	private static double Num(string s, double def)
	{
		s = (s ?? "").Trim().Replace("px", "");
		if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return def;
		}
		return result;
	}
}
