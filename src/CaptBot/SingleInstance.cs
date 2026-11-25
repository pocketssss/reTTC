using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace CaptBot;

internal static class SingleInstance
{
	private static readonly string Prefix = "Local\\TTC";

	private static readonly string MutexName = Prefix + ".instance";

	private static readonly string EventName = Prefix + ".show";

	private static Mutex mutex;

	private static EventWaitHandle showEvent;

	private static bool owned;

	public static volatile bool ShowPending;

	public static volatile Action OnShow;

	private static SecurityIdentifier Me => WindowsIdentity.GetCurrent().User;

	[DllImport("user32.dll")]
	private static extern bool AllowSetForegroundWindow(int pid);

	public static bool Acquire()
	{
		try
		{
			EventWaitHandleSecurity eventWaitHandleSecurity = new EventWaitHandleSecurity();
			eventWaitHandleSecurity.AddAccessRule(new EventWaitHandleAccessRule(Me, EventWaitHandleRights.FullControl, AccessControlType.Allow));
			showEvent = new EventWaitHandle(initialState: false, EventResetMode.AutoReset, EventName, out var createdNew, eventWaitHandleSecurity);
			MutexSecurity mutexSecurity = new MutexSecurity();
			mutexSecurity.AddAccessRule(new MutexAccessRule(Me, MutexRights.FullControl, AccessControlType.Allow));
			mutex = new Mutex(initiallyOwned: false, MutexName, out createdNew, mutexSecurity);
			owned = Take(mutex);
		}
		catch (Exception)
		{
			return true;
		}
		if (!owned)
		{
			Poke(showEvent);
			return false;
		}
		Thread thread = new Thread(Listen);
		thread.IsBackground = true;
		thread.Name = "TTC show";
		Thread thread2 = thread;
		thread2.Start();
		return true;
	}

	public static bool ShowRunning()
	{
		try
		{
			using (Mutex mutex = Mutex.OpenExisting(MutexName, MutexRights.Modify | MutexRights.Synchronize))
			{
				if (Take(mutex))
				{
					mutex.ReleaseMutex();
					return false;
				}
			}
			using (EventWaitHandle e = EventWaitHandle.OpenExisting(EventName, EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize))
			{
				Poke(e);
			}
			return true;
		}
		catch (Exception)
		{
			return false;
		}
	}

	public static void Release()
	{
		if (mutex == null || !owned)
		{
			return;
		}
		try
		{
			mutex.ReleaseMutex();
			owned = false;
		}
		catch
		{
		}
	}

	public static void Reacquire()
	{
		if (mutex == null || owned)
		{
			return;
		}
		try
		{
			owned = Take(mutex);
		}
		catch
		{
		}
	}

	private static bool Take(Mutex m)
	{
		try
		{
			return m.WaitOne(0);
		}
		catch (AbandonedMutexException)
		{
			return true;
		}
	}

	private static void Poke(EventWaitHandle e)
	{
		try
		{
			AllowSetForegroundWindow(-1);
		}
		catch
		{
		}
		try
		{
			e.Set();
		}
		catch
		{
		}
	}

	private static void Listen()
	{
		while (true)
		{
			try
			{
				showEvent.WaitOne();
			}
			catch
			{
				break;
			}
			Action onShow = OnShow;
			if (onShow == null)
			{
				ShowPending = true;
				continue;
			}
			try
			{
				onShow();
			}
			catch
			{
			}
		}
	}
}
