using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;

namespace CaptBot;

internal class CdpPage : IDisposable
{
	private class Pending
	{
		public readonly ManualResetEvent Done = new ManualResetEvent(initialState: false);

		public Dictionary<string, object> Result;

		public string Error;

		public bool Lost;
	}

	private static readonly JavaScriptSerializer Json = new JavaScriptSerializer
	{
		MaxJsonLength = 33554432
	};

	private readonly ClientWebSocket ws = new ClientWebSocket();

	private readonly object sendLock = new object();

	private readonly Dictionary<int, Pending> calls = new Dictionary<int, Pending>();

	private int next;

	public volatile bool Closed;

	public string Id;

	public static CdpPage Connect(CdpTarget t)
	{
		return Connect(t, page: true);
	}

	public static CdpPage Connect(CdpTarget t, bool page)
	{
		CdpPage cdpPage = new CdpPage();
		cdpPage.Id = t.Id;
		CdpPage cdpPage2 = cdpPage;
		cdpPage2.ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(20.0);
		try
		{
			if (!cdpPage2.ws.ConnectAsync(new Uri(t.Ws), CancellationToken.None).Wait(5000))
			{
				throw new CdpError("вкладка браузера не отвечает");
			}
		}
		catch (AggregateException ex)
		{
			throw new CdpError("к вкладке браузера не подключиться: " + ex.InnerException.Message);
		}
		Thread thread = new Thread(cdpPage2.ReadLoop);
		thread.IsBackground = true;
		thread.Name = "TTC cdp";
		Thread thread2 = thread;
		thread2.Start();
		if (page)
		{
			try
			{
				cdpPage2.Call("Page.enable", null, 5000);
			}
			catch
			{
			}
			try
			{
				cdpPage2.Call("Emulation.setFocusEmulationEnabled", new Dictionary<string, object> { { "enabled", true } }, 5000);
			}
			catch
			{
			}
		}
		return cdpPage2;
	}

	private void ReadLoop()
	{
		byte[] array = new byte[65536];
		MemoryStream memoryStream = new MemoryStream();
		try
		{
			while (ws.State == WebSocketState.Open)
			{
				WebSocketReceiveResult result = ws.ReceiveAsync(new ArraySegment<byte>(array), CancellationToken.None).Result;
				if (result.MessageType != WebSocketMessageType.Close)
				{
					memoryStream.Write(array, 0, result.Count);
					if (result.EndOfMessage)
					{
						string @string = Encoding.UTF8.GetString(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
						memoryStream.SetLength(0L);
						Dispatch(@string);
					}
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		Closed = true;
		lock (calls)
		{
			foreach (Pending value in calls.Values)
			{
				value.Lost = true;
				value.Done.Set();
			}
			calls.Clear();
		}
	}

	private void Dispatch(string text)
	{
		Dictionary<string, object> dictionary;
		try
		{
			dictionary = Json.DeserializeObject(text) as Dictionary<string, object>;
		}
		catch
		{
			return;
		}
		if (dictionary == null)
		{
			return;
		}
		object value5;
		if (dictionary.TryGetValue("id", out var value))
		{
			int key = Convert.ToInt32(value);
			Pending value2;
			lock (calls)
			{
				if (calls.TryGetValue(key, out value2))
				{
					calls.Remove(key);
				}
			}
			if (value2 != null)
			{
				if (dictionary.TryGetValue("error", out var value3))
				{
					Dictionary<string, object> dictionary2 = value3 as Dictionary<string, object>;
					object value4 = null;
					value2.Error = ((dictionary2 != null && dictionary2.TryGetValue("message", out value4)) ? Convert.ToString(value4) : "ошибка браузера");
				}
				else
				{
					value2.Result = (dictionary.TryGetValue("result", out value5) ? value5 : null) as Dictionary<string, object>;
				}
				value2.Done.Set();
			}
		}
		else
		{
			if (!dictionary.TryGetValue("method", out value5))
			{
				return;
			}
			switch (value5 as string)
			{
			case "Inspector.detached":
			case "Inspector.targetCrashed":
				Closed = true;
				break;
			case "Page.javascriptDialogOpening":
				ThreadPool.QueueUserWorkItem(delegate
				{
					try
					{
						Call("Page.handleJavaScriptDialog", new Dictionary<string, object> { { "accept", true } }, 3000);
					}
					catch
					{
					}
				});
				break;
			}
		}
	}

	public Dictionary<string, object> Call(string method, Dictionary<string, object> args, int timeoutMs)
	{
		if (Closed)
		{
			throw new CdpClosed();
		}
		Pending pending = new Pending();
		int num;
		lock (calls)
		{
			num = ++next;
			calls[num] = pending;
		}
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["id"] = num;
		dictionary["method"] = method;
		dictionary["params"] = args ?? new Dictionary<string, object>();
		byte[] bytes = Encoding.UTF8.GetBytes(Json.Serialize(dictionary));
		try
		{
			lock (sendLock)
			{
				if (!ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None).Wait(timeoutMs))
				{
					throw new CdpError("браузер не принял команду");
				}
			}
		}
		catch (AggregateException)
		{
			lock (calls)
			{
				calls.Remove(num);
			}
			Closed = Closed || ws.State != WebSocketState.Open;
			if (Closed)
			{
				throw new CdpClosed();
			}
			throw new CdpError("браузер не принял команду");
		}
		if (!pending.Done.WaitOne(timeoutMs))
		{
			lock (calls)
			{
				calls.Remove(num);
			}
			if (Closed)
			{
				throw new CdpClosed();
			}
			throw new CdpError("браузер не ответил за " + timeoutMs / 1000 + " с");
		}
		if (pending.Lost)
		{
			throw new CdpClosed();
		}
		if (pending.Error != null)
		{
			throw new CdpError(pending.Error);
		}
		return pending.Result ?? new Dictionary<string, object>();
	}

	public object Eval(string func, object arg, int timeoutMs)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["expression"] = "(" + func + ")(" + Json.Serialize(arg) + ")";
		dictionary["returnByValue"] = true;
		dictionary["awaitPromise"] = true;
		dictionary["userGesture"] = true;
		Dictionary<string, object> dictionary2 = Call("Runtime.evaluate", dictionary, timeoutMs);
		if (dictionary2.TryGetValue("exceptionDetails", out var value))
		{
			Dictionary<string, object> dictionary3 = value as Dictionary<string, object>;
			object value2 = null;
			object value3 = null;
			object value4 = null;
			if (dictionary3 != null && dictionary3.TryGetValue("exception", out value2) && value2 is Dictionary<string, object>)
			{
				((Dictionary<string, object>)value2).TryGetValue("description", out value3);
			}
			dictionary3?.TryGetValue("text", out value4);
			throw new CdpError("скрипт в вкладке: " + Convert.ToString(value3 ?? value4 ?? "ошибка"));
		}
		if (!((dictionary2.TryGetValue("result", out var value5) ? value5 : null) is Dictionary<string, object> dictionary4) || !dictionary4.TryGetValue("value", out var value6))
		{
			return null;
		}
		return value6;
	}

	public object Eval(string func, object arg)
	{
		return Eval(func, arg, 10000);
	}

	public void Navigate(string url)
	{
		Call("Page.navigate", new Dictionary<string, object> { { "url", url } }, 30000);
	}

	public void Click(double x, double y, string button, int modifiers)
	{
		int buttons = ((!(button == "right")) ? 1 : 2);
		Call("Input.dispatchMouseEvent", Mouse("mouseMoved", x, y, "none", 0, modifiers), 5000);
		Call("Input.dispatchMouseEvent", Mouse("mousePressed", x, y, button, buttons, modifiers), 5000);
		Call("Input.dispatchMouseEvent", Mouse("mouseReleased", x, y, button, 0, modifiers), 5000);
	}

	private static Dictionary<string, object> Mouse(string type, double x, double y, string button, int buttons, int modifiers)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["type"] = type;
		dictionary["x"] = x;
		dictionary["y"] = y;
		dictionary["button"] = button;
		dictionary["buttons"] = buttons;
		dictionary["modifiers"] = modifiers;
		if (type != "mouseMoved")
		{
			dictionary["clickCount"] = 1;
		}
		return dictionary;
	}

	public void Key(string key, string code, int vk, int modifiers, string text, string[] commands)
	{
		Dictionary<string, object> dictionary = new Dictionary<string, object>();
		dictionary["type"] = (string.IsNullOrEmpty(text) ? "rawKeyDown" : "keyDown");
		dictionary["key"] = key;
		dictionary["code"] = code;
		dictionary["windowsVirtualKeyCode"] = vk;
		dictionary["nativeVirtualKeyCode"] = vk;
		dictionary["modifiers"] = modifiers;
		if (!string.IsNullOrEmpty(text))
		{
			dictionary["text"] = text;
			dictionary["unmodifiedText"] = text;
		}
		if (commands != null)
		{
			dictionary["commands"] = commands;
		}
		Call("Input.dispatchKeyEvent", dictionary, 5000);
		Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
		dictionary2["type"] = "keyUp";
		dictionary2["key"] = key;
		dictionary2["code"] = code;
		dictionary2["windowsVirtualKeyCode"] = vk;
		dictionary2["nativeVirtualKeyCode"] = vk;
		dictionary2["modifiers"] = modifiers;
		Call("Input.dispatchKeyEvent", dictionary2, 5000);
	}

	public void Enter(bool shift)
	{
		Key("Enter", "Enter", 13, shift ? 8 : 0, "\r", null);
	}

	public void SelectAll()
	{
		Key("a", "KeyA", 65, 2, null, new string[1] { "selectAll" });
	}

	public void Delete()
	{
		Key("Delete", "Delete", 46, 0, null, new string[1] { "deleteForward" });
	}

	public void Escape()
	{
		Key("Escape", "Escape", 27, 0, null, null);
	}

	public void InsertText(string text)
	{
		Call("Input.insertText", new Dictionary<string, object> { { "text", text } }, 5000);
	}

	public void Dispose()
	{
		try
		{
			if (ws.State == WebSocketState.Open)
			{
				ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait(1000);
			}
		}
		catch
		{
		}
		try
		{
			ws.Dispose();
		}
		catch
		{
		}
		Closed = true;
	}
}
