using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Microsoft.Win32;

namespace CaptBot;

internal class CdpBrowser
{
	private const string Flags = "--no-first-run --no-default-browser-check --disable-search-engine-choice-screen --disable-background-timer-throttling --disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling";

	public string Name;

	public string Exe;

	public string Profile;

	public int Port;

	public bool Reused;

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 16777216
	};

	public static bool Find(out string name, out string exe)
	{
		string path = Environment.GetEnvironmentVariable("ProgramFiles") ?? "C:\\Program Files";
		string path2 = Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? "C:\\Program Files (x86)";
		string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string[][] array = new string[3][]
		{
			new string[5]
			{
				"Edge",
				"msedge.exe",
				Path.Combine(path2, "Microsoft\\Edge\\Application\\msedge.exe"),
				Path.Combine(path, "Microsoft\\Edge\\Application\\msedge.exe"),
				Path.Combine(folderPath, "Microsoft\\Edge\\Application\\msedge.exe")
			},
			new string[5]
			{
				"Chrome",
				"chrome.exe",
				Path.Combine(path, "Google\\Chrome\\Application\\chrome.exe"),
				Path.Combine(path2, "Google\\Chrome\\Application\\chrome.exe"),
				Path.Combine(folderPath, "Google\\Chrome\\Application\\chrome.exe")
			},
			new string[3]
			{
				"Яндекс",
				"browser.exe",
				Path.Combine(folderPath, "Yandex\\YandexBrowser\\Application\\browser.exe")
			}
		};
		string[][] array2 = array;
		foreach (string[] array3 in array2)
		{
			for (int j = 2; j < array3.Length; j++)
			{
				if (File.Exists(array3[j]))
				{
					name = array3[0];
					exe = array3[j];
					return true;
				}
			}
			string text = AppPath(array3[1]);
			if (text != null && File.Exists(text))
			{
				name = array3[0];
				exe = text;
				return true;
			}
		}
		name = (exe = null);
		return false;
	}

	private static string AppPath(string file)
	{
		RegistryKey[] array = new RegistryKey[2]
		{
			Registry.LocalMachine,
			Registry.CurrentUser
		};
		foreach (RegistryKey registryKey in array)
		{
			try
			{
				using RegistryKey registryKey2 = registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + file);
				if (registryKey2 != null && registryKey2.GetValue(null) is string)
				{
					return ((string)registryKey2.GetValue(null)).Trim('"');
				}
			}
			catch
			{
			}
		}
		return null;
	}

	public static CdpBrowser Open(string root, string url, bool headless, out string error)
	{
		error = null;
		if (!Find(out var name, out var exe))
		{
			error = "Не нашёл браузер: нужен Microsoft Edge (есть в каждой Windows 10/11), Google Chrome или Яндекс Браузер.";
			return null;
		}
		string text = Path.Combine(root, (name == "Яндекс") ? "yandex" : name.ToLowerInvariant());
		bool flag = !Directory.Exists(text);
		Directory.CreateDirectory(text);
		if (flag && name == "Edge" && !headless)
		{
			ImportOld(text);
		}
		CdpBrowser cdpBrowser = new CdpBrowser();
		cdpBrowser.Name = name;
		cdpBrowser.Exe = exe;
		cdpBrowser.Profile = text;
		CdpBrowser cdpBrowser2 = cdpBrowser;
		if (cdpBrowser2.ReadPort() && cdpBrowser2.Alive())
		{
			cdpBrowser2.Reused = true;
			return cdpBrowser2;
		}
		try
		{
			File.Delete(Path.Combine(text, "DevToolsActivePort"));
		}
		catch
		{
		}
		string text2 = "--remote-debugging-port=0 --user-data-dir=\"" + text + "\" --no-first-run --no-default-browser-check --disable-search-engine-choice-screen --disable-background-timer-throttling --disable-backgrounding-occluded-windows --disable-renderer-backgrounding --disable-features=CalculateNativeWinOcclusion,IntensiveWakeUpThrottling" + (headless ? " --headless=new" : "") + " \"" + url + "\"";
		string directoryName = Path.GetDirectoryName(exe);
		bool flag2 = false;
		if (!headless && NetWatch.IsAdmin)
		{
			try
			{
				flag2 = ShellRun.Unelevated(exe, text2, directoryName);
			}
			catch
			{
				flag2 = false;
			}
		}
		if (!flag2)
		{
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(exe, text2);
				processStartInfo.UseShellExecute = false;
				processStartInfo.WorkingDirectory = directoryName;
				Process.Start(processStartInfo);
			}
			catch (Exception ex)
			{
				error = "Не запустился " + name + ": " + ex.Message;
				return null;
			}
		}
		for (int i = 0; i < 200; i++)
		{
			Thread.Sleep(100);
			if (cdpBrowser2.ReadPort() && cdpBrowser2.Alive())
			{
				return cdpBrowser2;
			}
		}
		error = name + " открылся, но не дал подключиться. Закройте его окна с профилем TTC и нажмите «Включить» ещё раз.";
		return null;
	}

	private static void ImportOld(string profile)
	{
		try
		{
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DiscordImageReader\\browser-edge");
			if (File.Exists(Path.Combine(path, "Local State")) && Directory.Exists(Path.Combine(path, "Default")))
			{
				string[] skip = new string[11]
				{
					"Cache", "Code Cache", "GPUCache", "DawnGraphiteCache", "DawnWebGPUCache", "CacheStorage", "ScriptCache", "EdgeCoupons", "Crashpad", "ShaderCache",
					"GrShaderCache"
				};
				File.Copy(Path.Combine(path, "Local State"), Path.Combine(profile, "Local State"), overwrite: true);
				CopyDir(Path.Combine(path, "Default"), Path.Combine(profile, "Default"), skip);
			}
		}
		catch
		{
			try
			{
				Directory.Delete(profile, recursive: true);
				Directory.CreateDirectory(profile);
			}
			catch
			{
			}
		}
	}

	private static void CopyDir(string from, string to, string[] skip)
	{
		Directory.CreateDirectory(to);
		string[] files = Directory.GetFiles(from);
		foreach (string text in files)
		{
			if (!text.EndsWith("LOCK", StringComparison.OrdinalIgnoreCase))
			{
				File.Copy(text, Path.Combine(to, Path.GetFileName(text)), overwrite: true);
			}
		}
		string[] directories = Directory.GetDirectories(from);
		foreach (string text2 in directories)
		{
			if (!skip.Contains(Path.GetFileName(text2), StringComparer.OrdinalIgnoreCase))
			{
				CopyDir(text2, Path.Combine(to, Path.GetFileName(text2)), skip);
			}
		}
	}

	private bool ReadPort()
	{
		try
		{
			string path = Path.Combine(Profile, "DevToolsActivePort");
			if (!File.Exists(path))
			{
				return false;
			}
			string text;
			using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
			{
				using StreamReader streamReader = new StreamReader(stream);
				text = streamReader.ReadLine();
			}
			if (int.TryParse((text ?? "").Trim(), out var result) && result > 0 && result < 65536)
			{
				Port = result;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	public bool Alive()
	{
		try
		{
			return Port > 0 && Http("/json/version", "GET", 1500).Contains("webSocketDebuggerUrl");
		}
		catch
		{
			return false;
		}
	}

	public List<CdpTarget> Pages()
	{
		List<CdpTarget> list = new List<CdpTarget>();
		if (!(Json.DeserializeObject(Http("/json/list", "GET", 3000)) is object[] array))
		{
			return list;
		}
		object[] array2 = array;
		foreach (object obj in array2)
		{
			if (obj is Dictionary<string, object> d && !(S(d, "type") != "page") && !string.IsNullOrEmpty(S(d, "webSocketDebuggerUrl")))
			{
				string text = S(d, "url") ?? "";
				if (!text.StartsWith("devtools:") && !text.StartsWith("chrome-extension:") && !text.StartsWith("edge-extension:"))
				{
					list.Add(new CdpTarget
					{
						Id = S(d, "id"),
						Type = "page",
						Url = text,
						Ws = S(d, "webSocketDebuggerUrl")
					});
				}
			}
		}
		return list;
	}

	public CdpTarget NewPage(string url)
	{
		if (!(Json.DeserializeObject(Http("/json/new?" + Uri.EscapeDataString(url), "PUT", 5000)) is Dictionary<string, object> d))
		{
			return null;
		}
		CdpTarget cdpTarget = new CdpTarget();
		cdpTarget.Id = S(d, "id");
		cdpTarget.Type = "page";
		cdpTarget.Url = S(d, "url");
		cdpTarget.Ws = S(d, "webSocketDebuggerUrl");
		return cdpTarget;
	}

	public void Close()
	{
		try
		{
			string text = ((Json.DeserializeObject(Http("/json/version", "GET", 1500)) is Dictionary<string, object> d) ? S(d, "webSocketDebuggerUrl") : null);
			if (text == null)
			{
				return;
			}
			CdpTarget cdpTarget = new CdpTarget();
			cdpTarget.Ws = text;
			using CdpPage cdpPage = CdpPage.Connect(cdpTarget, page: false);
			try
			{
				cdpPage.Call("Browser.close", null, 3000);
			}
			catch
			{
			}
		}
		catch
		{
		}
	}

	private string Http(string path, string method, int timeoutMs)
	{
		HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + Port + path);
		httpWebRequest.Method = method;
		httpWebRequest.Timeout = timeoutMs;
		httpWebRequest.ReadWriteTimeout = timeoutMs;
		httpWebRequest.Proxy = null;
		httpWebRequest.KeepAlive = false;
		if (method == "PUT")
		{
			httpWebRequest.ContentLength = 0L;
		}
		using HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
		using StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream(), Encoding.UTF8);
		return streamReader.ReadToEnd();
	}

	private static string S(Dictionary<string, object> d, string k)
	{
		if (!d.TryGetValue(k, out var value) || value == null)
		{
			return null;
		}
		return Convert.ToString(value);
	}
}
