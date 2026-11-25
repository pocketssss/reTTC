using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CaptBot;

internal static class Images
{
	private static readonly Dictionary<string, ImageSource> mem = new Dictionary<string, ImageSource>();

	private static readonly Dictionary<string, List<Action<ImageSource>>> waiting = new Dictionary<string, List<Action<ImageSource>>>();

	private static readonly Dictionary<string, DateTime> failed = new Dictionary<string, DateTime>();

	private static string Dir => System.IO.Path.Combine(AppPaths.Data, "logos");

	public static void Get(string path, Action<ImageSource> onReady)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}
		Dispatcher ui = Dispatcher.CurrentDispatcher;
		if (mem.TryGetValue(path, out var value))
		{
			if (value != null)
			{
				onReady(value);
			}
		}
		else
		{
			if (failed.TryGetValue(path, out var value2) && (DateTime.UtcNow - value2).TotalMinutes < 5.0)
			{
				return;
			}
			if (waiting.TryGetValue(path, out var value3))
			{
				value3.Add(onReady);
				return;
			}
			waiting[path] = new List<Action<ImageSource>> { onReady };
			ThreadPool.QueueUserWorkItem(delegate
			{
				ImageSource res = null;
				try
				{
					res = Load(path);
				}
				catch
				{
				}
				ui.BeginInvoke((Action)delegate
				{
					List<Action<ImageSource>> list = waiting[path];
					waiting.Remove(path);
					if (res == null)
					{
						failed[path] = DateTime.UtcNow;
						return;
					}
					mem[path] = res;
					foreach (Action<ImageSource> item in list)
					{
						try
						{
							item(res);
						}
						catch
						{
						}
					}
				});
			});
		}
	}

	public static FrameworkElement Badge(string path, Brush fallback, Brush stroke, double size)
	{
		Grid grid = new Grid();
		grid.Width = size;
		grid.Height = size;
		grid.VerticalAlignment = VerticalAlignment.Center;
		Grid grid2 = grid;
		Rectangle r = new Rectangle
		{
			Width = Math.Round(size * 0.7),
			Height = Math.Round(size * 0.7),
			RadiusX = 2.0,
			RadiusY = 2.0,
			Fill = fallback,
			Stroke = stroke,
			StrokeThickness = ((stroke != null) ? 1 : 0)
		};
		grid2.Children.Add(r);
		if (!string.IsNullOrEmpty(path))
		{
			Image im = new Image
			{
				Width = size,
				Height = size,
				Stretch = Stretch.Uniform
			};
			RenderOptions.SetBitmapScalingMode(im, BitmapScalingMode.HighQuality);
			grid2.Children.Add(im);
			Get(path, delegate(ImageSource src)
			{
				im.Source = src;
				r.Visibility = Visibility.Collapsed;
			});
		}
		return grid2;
	}

	private static ImageSource Load(string path)
	{
		string path2 = Regex.Replace(path.TrimStart('/'), "[^A-Za-z0-9._-]", "_");
		string path3 = System.IO.Path.Combine(Dir, path2);
		byte[] array = null;
		if (File.Exists(path3) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(path3)).TotalDays < 3.0)
		{
			array = File.ReadAllBytes(path3);
		}
		if (array == null)
		{
			try
			{
				array = SiteNet.Get("https://timetocapture.ru" + path, 100000);
				if (array.Length > 0 && array.Length < 2097152)
				{
					Directory.CreateDirectory(Dir);
					File.WriteAllBytes(path3, array);
				}
			}
			catch
			{
				if (!File.Exists(path3))
				{
					throw;
				}
				array = File.ReadAllBytes(path3);
			}
		}
		if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
		{
			return SvgIcon.Render(Encoding.UTF8.GetString(array));
		}
		BitmapImage bitmapImage = new BitmapImage();
		bitmapImage.BeginInit();
		bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
		bitmapImage.StreamSource = new MemoryStream(array);
		bitmapImage.DecodePixelWidth = 64;
		bitmapImage.EndInit();
		bitmapImage.Freeze();
		return bitmapImage;
	}
}
