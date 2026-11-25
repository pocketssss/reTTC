using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CaptBot;

internal static class Program
{
	[STAThread]
	private static void Main(string[] args)
	{
		bool flag = args.Contains("--elevated");
		if (flag)
		{
			args = args.Where((string a) => a != "--elevated").ToArray();
		}
		bool flag2 = !TestMode(args);
		Telemetry.Disabled = !flag2;
		AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e)
		{
			Telemetry.Crash(e.ExceptionObject as Exception, "поток");
		};
		TaskScheduler.UnobservedTaskException += delegate(object s, UnobservedTaskExceptionEventArgs e)
		{
			Telemetry.Caught(e.Exception, "задача");
		};
		if (!NetWatch.IsAdmin)
		{
			if (!flag2 || !SingleInstance.ShowRunning())
			{
				if (flag)
				{
					MessageBox.Show("TTC работает только от имени администратора, а Windows не дала ей эти права. Запустите TTC от имени администратора.", "TTC");
				}
				else
				{
					RelaunchElevated(args);
				}
			}
			return;
		}
		bool flag3 = args.Contains("--tray");
		args = args.Where((string a) => a != "--tray").ToArray();
		if (flag2 && !SingleInstance.Acquire())
		{
			return;
		}
		try
		{
			Ocr.Init();
			PingOcr.Init();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Не удалось загрузить образцы цифр: " + ex.Message, "TTC");
			return;
		}
		bool flag4 = false;
		try
		{
			if (flag4 || Ui.MapDemo || (flag3 ? QuietLicense() : LicenseDialog.Show(null, auto: true, null)))
			{
				if (flag4 || Ui.MapDemo)
				{
					Telemetry.Disabled = true;
				}
				new Ui(flag4).Run(flag3);
			}
		}
		catch (Exception ex2)
		{
			Telemetry.Crash(ex2, "Main");
			MessageBox.Show(ex2.ToString(), "TTC: ошибка");
		}
	}

	private static bool TestMode(string[] args)
	{
		return false;
	}

	private static bool QuietLicense()
	{
		string text = License.Normalize(License.SavedKey());
		int num = 0;
		while (text.Length >= 10 && num < 12)
		{
			LicenseState licenseState = License.Check(text);
			if (licenseState.Ok)
			{
				License.Current = licenseState;
				return true;
			}
			if (licenseState.Code != "network")
			{
				break;
			}
			Thread.Sleep(5000);
			num++;
		}
		return LicenseDialog.Show(null, auto: true, null);
	}

	private static void RelaunchElevated(string[] args)
	{
		try
		{
			string arguments = string.Join(" ", args.Select((string x) => "\"" + x.Replace("\"", "\\\"") + "\"").Concat(new string[1] { "--elevated" }).ToArray());
			ProcessStartInfo processStartInfo = new ProcessStartInfo(Updater.ExePath, arguments);
			processStartInfo.Verb = "runas";
			processStartInfo.UseShellExecute = true;
			processStartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
			Process.Start(processStartInfo);
		}
		catch (Win32Exception)
		{
			MessageBox.Show("TTC работает только от имени администратора: без этих прав Windows не даёт читать капты из игры.\n\nЗапустите TTC снова и нажмите «Да» в окне Windows.", "TTC");
		}
		catch (Exception ex2)
		{
			MessageBox.Show("Не удалось запустить TTC от имени администратора: " + ex2.Message, "TTC");
		}
	}
}
