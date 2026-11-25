using System;
using System.IO;
using System.Security;
using System.Security.Principal;

namespace CaptBot;

internal static class LogonTask
{
	public const string TrayArg = "--tray";

	public static readonly string Name = "TTC Autostart";

	public static string Enable(string exe)
	{
		return Enable(exe, highest: true);
	}

	public static string Enable(string exe, bool highest)
	{
		try
		{
			dynamic val = Folder();
			val.RegisterTask(Name, Xml(exe, highest), 6, null, null, 3, null);
			return null;
		}
		catch (Exception ex)
		{
			return Denied(ex) ? "Windows не дала создать задачу планировщика: нужны права администратора" : ex.Message;
		}
	}

	public static string Disable()
	{
		try
		{
			Folder().DeleteTask(Name, 0);
			return null;
		}
		catch (Exception ex)
		{
			if (NotFound(ex))
			{
				return null;
			}
			return Denied(ex) ? "Windows не дала удалить задачу планировщика: нужны права администратора" : ex.Message;
		}
	}

	public static string Sync(string exe, out bool changed)
	{
		changed = false;
		try
		{
			if (LogonTask.Matches(Folder().GetTask(Name), exe))
			{
				return null;
			}
		}
		catch
		{
		}
		changed = true;
		return Enable(exe);
	}

	public static string Describe()
	{
		try
		{
			dynamic task = Folder().GetTask(Name);
			dynamic val = task.Definition;
			string text = "";
			foreach (dynamic item in val.Actions)
			{
				if ((int)item.Type == 0)
				{
					text = (string)item.Path + " | " + (string)item.Arguments;
				}
			}
			return text + " | " + (((int)val.Principal.RunLevel == 1) ? "highest" : "limited") + " | " + (((bool)task.Enabled) ? "enabled" : "disabled");
		}
		catch
		{
			return null;
		}
	}

	private static bool Matches(dynamic task, string exe)
	{
		dynamic val = task.Definition;
		if ((int)val.Principal.RunLevel != 1)
		{
			return false;
		}
		int num = 0;
		bool result = false;
		foreach (dynamic item in val.Actions)
		{
			num++;
			if ((int)item.Type == 0)
			{
				string a = (((string)item.Path) ?? "").Trim().Trim('"');
				string text = ((string)item.Arguments) ?? "";
				result = string.Equals(a, exe, StringComparison.OrdinalIgnoreCase) && text.Contains("--tray");
			}
		}
		if (num == 1)
		{
			return result;
		}
		return false;
	}

	private static dynamic Folder()
	{
		dynamic val = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service"));
		val.Connect();
		return val.GetFolder("\\");
	}

	private static bool NotFound(Exception ex)
	{
		if (!(ex is FileNotFoundException))
		{
			return ex.HResult == -2147024894;
		}
		return true;
	}

	private static bool Denied(Exception ex)
	{
		if (!(ex is UnauthorizedAccessException))
		{
			return ex.HResult == -2147024891;
		}
		return true;
	}

	private static string Xml(string exe, bool highest)
	{
		WindowsIdentity current = WindowsIdentity.GetCurrent();
		string directoryName = Path.GetDirectoryName(exe);
		return "<?xml version=\"1.0\" encoding=\"UTF-16\"?><Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"><RegistrationInfo><Author>TTC</Author><Description>" + X("Запуск TTC в трее при входе в Windows: капты из игры уходят на общую карту TTC. Выключается в TTC: шестерёнка → «Запускать с Windows».") + "</Description></RegistrationInfo><Triggers><LogonTrigger><Enabled>true</Enabled><UserId>" + X(current.Name) + "</UserId><Delay>PT15S</Delay></LogonTrigger></Triggers><Principals><Principal id=\"Author\"><UserId>" + X(current.User.Value) + "</UserId><LogonType>InteractiveToken</LogonType><RunLevel>" + (highest ? "HighestAvailable" : "LeastPrivilege") + "</RunLevel></Principal></Principals><Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><AllowHardTerminate>true</AllowHardTerminate><StartWhenAvailable>false</StartWhenAvailable><RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable><IdleSettings><StopOnIdleEnd>false</StopOnIdleEnd><RestartOnIdle>false</RestartOnIdle></IdleSettings><AllowStartOnDemand>true</AllowStartOnDemand><Enabled>true</Enabled><Hidden>false</Hidden><RunOnlyIfIdle>false</RunOnlyIfIdle><WakeToRun>false</WakeToRun><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><Priority>4</Priority></Settings><Actions Context=\"Author\"><Exec><Command>" + X("\"" + exe + "\"") + "</Command><Arguments>--tray</Arguments><WorkingDirectory>" + X(directoryName) + "</WorkingDirectory></Exec></Actions></Task>";
	}

	private static string X(string s)
	{
		return SecurityElement.Escape(s ?? "");
	}
}
