using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CaptBot;

internal static class AppPaths
{
	public const bool Debug = false;

	private static string data;

	private static readonly string[] MoveFiles = new string[4] { "license.dat", "captbot.json", "calib.json", "servers.json" };

	private static readonly string[] MoveDirs = new string[4] { "calib", "badges", "logos", "maptiles" };

	private static readonly string[] JunkFiles = new string[4] { "captbot.log", "test_run.log", "update_check.log", "license_check.log" };

	public static string Data
	{
		get
		{
			if (data == null)
			{
				data = Init();
			}
			return data;
		}
	}

	public static string ExeDir => AppDomain.CurrentDomain.BaseDirectory;

	public static bool OpenUrl(string url)
	{
		if (string.IsNullOrEmpty(url) || (!url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)))
		{
			return false;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo("explorer.exe", "\"" + url.Replace("\"", "%22") + "\"");
			processStartInfo.UseShellExecute = false;
			Process.Start(processStartInfo);
			return true;
		}
		catch
		{
			try
			{
				ProcessStartInfo processStartInfo2 = new ProcessStartInfo(url);
				processStartInfo2.UseShellExecute = true;
				Process.Start(processStartInfo2);
				return true;
			}
			catch
			{
				return false;
			}
		}
	}

	private static string Init()
	{
		try
		{
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TTC");
			Directory.CreateDirectory(text);
			Migrate(ExeDir, text);
			return text;
		}
		catch
		{
			return ExeDir;
		}
	}

	private static void Migrate(string from, string to)
	{
		if (string.Equals(Path.GetFullPath(from).TrimEnd('\\'), Path.GetFullPath(to).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
		{
			return;
		}
		string[] moveFiles = MoveFiles;
		foreach (string path in moveFiles)
		{
			try
			{
				string text = Path.Combine(from, path);
				string text2 = Path.Combine(to, path);
				if (File.Exists(text) && !File.Exists(text2))
				{
					File.Copy(text, text2);
					File.Delete(text);
				}
			}
			catch
			{
			}
		}
		string[] moveDirs = MoveDirs;
		foreach (string path2 in moveDirs)
		{
			try
			{
				string text3 = Path.Combine(from, path2);
				string text4 = Path.Combine(to, path2);
				if (Directory.Exists(text3) && !Directory.Exists(text4))
				{
					try
					{
						Directory.Move(text3, text4);
					}
					catch (IOException)
					{
						goto IL_00d9;
					}
				}
				goto end_IL_00a5;
				IL_00d9:
				CopyDir(text3, text4);
				Directory.Delete(text3, recursive: true);
				end_IL_00a5:;
			}
			catch
			{
			}
		}
		string[] junkFiles = JunkFiles;
		foreach (string path3 in junkFiles)
		{
			try
			{
				string path4 = Path.Combine(from, path3);
				if (File.Exists(path4))
				{
					File.Delete(path4);
				}
			}
			catch
			{
			}
		}
		try
		{
			string path5 = Path.Combine(from, "screens");
			if (!Directory.Exists(path5))
			{
				return;
			}
			string[] files = Directory.GetFiles(path5, "*.png");
			foreach (string path6 in files)
			{
				if (Regex.IsMatch(Path.GetFileName(path6), "^\\d{8}_\\d{6}_.+\\.png$"))
				{
					try
					{
						File.Delete(path6);
					}
					catch
					{
					}
				}
			}
			if (Directory.GetFileSystemEntries(path5).Length == 0)
			{
				Directory.Delete(path5);
			}
		}
		catch
		{
		}
	}

	private static void CopyDir(string s, string t)
	{
		Directory.CreateDirectory(t);
		string[] files = Directory.GetFiles(s);
		foreach (string text in files)
		{
			File.Copy(text, Path.Combine(t, Path.GetFileName(text)));
		}
		string[] directories = Directory.GetDirectories(s);
		foreach (string text2 in directories)
		{
			CopyDir(text2, Path.Combine(t, Path.GetFileName(text2)));
		}
	}
}
