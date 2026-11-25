using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CaptBot;

internal class LicenseDialog
{
	private readonly Window w;

	private readonly TextBox KeyBox;

	private readonly TextBlock ErrText;

	private readonly TextBlock BusyText;

	private readonly TextBlock HwidText;

	private readonly TextBlock HintText;

	private readonly TextBlock SubTitle;

	private readonly StackPanel BusyPanel;

	private readonly StackPanel InputPanel;

	private readonly StackPanel ErrRow;

	private readonly Button BtnActivate;

	private readonly Button BtnPaste;

	private bool ok;

	private bool busy;

	private T F<T>(string n) where T : class
	{
		if (!(w.FindName(n) is T result))
		{
			throw new Exception("нет элемента " + n);
		}
		return result;
	}

	private LicenseDialog(Window owner, string message)
	{
		using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("LicenseWindow.xaml"))
		{
			w = (Window)XamlReader.Load(stream);
		}
		if (owner != null && owner.IsLoaded)
		{
			w.Owner = owner;
			w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
			w.ShowInTaskbar = false;
		}
		KeyBox = F<TextBox>("KeyBox");
		ErrText = F<TextBlock>("ErrText");
		BusyText = F<TextBlock>("BusyText");
		HwidText = F<TextBlock>("HwidText");
		HintText = F<TextBlock>("HintText");
		SubTitle = F<TextBlock>("SubTitle");
		BusyPanel = F<StackPanel>("BusyPanel");
		InputPanel = F<StackPanel>("InputPanel");
		ErrRow = F<StackPanel>("ErrRow");
		BtnActivate = F<Button>("BtnActivate");
		BtnPaste = F<Button>("BtnPaste");
		try
		{
			Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("logo.png");
			if (manifestResourceStream != null)
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.StreamSource = manifestResourceStream;
				bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapImage.EndInit();
				F<Image>("LogoImg").Source = bitmapImage;
			}
			Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico");
			if (manifestResourceStream2 != null)
			{
				w.Icon = BitmapFrame.Create(manifestResourceStream2, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
			}
		}
		catch
		{
		}
		HwidText.Text = "ПК " + License.Hwid().Substring(2, 6) + " · v2.9.13";
		F<Grid>("TitleBar").MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
		{
			if (e.ButtonState == MouseButtonState.Pressed)
			{
				w.DragMove();
			}
		};
		F<Button>("BtnClose").Click += delegate
		{
			w.Close();
		};
		F<Button>("BtnSite").Click += delegate
		{
			License.OpenSite("/cabinet");
		};
		BtnActivate.Click += delegate
		{
			Try(KeyBox.Text);
		};
		BtnPaste.Click += delegate
		{
			try
			{
				if (Clipboard.ContainsText())
				{
					KeyBox.Text = License.Normalize(Clipboard.GetText());
					KeyBox.CaretIndex = KeyBox.Text.Length;
				}
			}
			catch
			{
			}
		};
		KeyBox.TextChanged += delegate
		{
			if (!busy)
			{
				ErrRow.Visibility = Visibility.Collapsed;
			}
		};
		w.Loaded += delegate
		{
			KeyBox.Focus();
		};
		string text = License.SavedKey();
		KeyBox.Text = text;
		if (message != null)
		{
			Error(message);
		}
		DoubleAnimation animation = new DoubleAnimation(-90.0, 380.0, TimeSpan.FromMilliseconds(1100.0))
		{
			RepeatBehavior = RepeatBehavior.Forever
		};
		((TranslateTransform)F<Border>("BusyBar").RenderTransform).BeginAnimation(TranslateTransform.XProperty, animation);
	}

	private void Error(string msg)
	{
		ErrText.Text = msg;
		ErrRow.Visibility = Visibility.Visible;
	}

	private void Busy(bool on, string text)
	{
		busy = on;
		BusyText.Text = text ?? "Проверяю ключ";
		BusyPanel.Visibility = ((!on) ? Visibility.Collapsed : Visibility.Visible);
		InputPanel.Visibility = (on ? Visibility.Collapsed : Visibility.Visible);
	}

	private void Try(string key)
	{
		key = License.Normalize(key);
		if (key.Length < 10)
		{
			Error("Вставьте ключ полностью, вида CAPT-XXXX-XXXX-XXXX-XXXX.");
			KeyBox.Focus();
			return;
		}
		Busy(on: true, "Проверяю ключ");
		Thread thread = new Thread((ThreadStart)delegate
		{
			LicenseState st = License.Check(key);
			w.Dispatcher.BeginInvoke((Action)delegate
			{
				if (st.Ok)
				{
					License.Current = st;
					ok = true;
					BusyText.Text = "Ключ активен, " + st.Term();
					DispatcherTimerOnce(650, delegate
					{
						w.Close();
					});
				}
				else
				{
					Busy(on: false, null);
					Error(st.Message);
					KeyBox.Focus();
					KeyBox.SelectAll();
				}
			});
		});
		thread.IsBackground = true;
		thread.Start();
	}

	private void DispatcherTimerOnce(int ms, Action a)
	{
		DispatcherTimer tm = new DispatcherTimer();
		tm.Interval = TimeSpan.FromMilliseconds(ms);
		tm.Tick += delegate
		{
			tm.Stop();
			a();
		};
		tm.Start();
	}

	public static bool Show(Window owner, bool auto, string message)
	{
		LicenseDialog d = new LicenseDialog(owner, message);
		if (owner != null)
		{
			d.SubTitle.Text = "смена ключа";
		}
		string saved = License.SavedKey();
		if (auto && saved.Length > 0)
		{
			d.Busy(on: true, "Проверяю ключ");
			d.w.ContentRendered += delegate
			{
				d.Try(saved);
			};
		}
		d.w.ShowDialog();
		return d.ok;
	}
}
