using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CaptBot;

internal static class ShellRun
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
	private interface IServiceProvider
	{
		[return: MarshalAs(UnmanagedType.IUnknown)]
		object QueryService(ref Guid service, ref Guid riid);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("000214E2-0000-0000-C000-000000000046")]
	private interface IShellBrowser
	{
		void GetWindow(out IntPtr hwnd);

		void ContextSensitiveHelp(int enter);

		void InsertMenusSB(IntPtr shared, IntPtr widths);

		void SetMenuSB(IntPtr shared, IntPtr olemenu, IntPtr active);

		void RemoveMenusSB(IntPtr shared);

		void SetStatusTextSB(IntPtr text);

		void EnableModelessSB(int enable);

		void TranslateAcceleratorSB(IntPtr msg, short id);

		void BrowseObject(IntPtr pidl, uint flags);

		void GetViewStateStream(uint mode, out IntPtr stream);

		void GetControlWindow(uint id, out IntPtr hwnd);

		void SendControlMsg(uint id, uint msg, IntPtr wp, IntPtr lp, out IntPtr ret);

		IShellView QueryActiveShellView();
	}

	[ComImport]
	[Guid("000214E3-0000-0000-C000-000000000046")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	private interface IShellView
	{
		void GetWindow(out IntPtr hwnd);

		void ContextSensitiveHelp(int enter);

		void TranslateAccelerator(IntPtr msg);

		void EnableModeless(int enable);

		void UIActivate(uint state);

		void Refresh();

		void CreateViewWindow(IntPtr prev, IntPtr fs, IntPtr browser, IntPtr rect, out IntPtr hwnd);

		void DestroyViewWindow();

		void GetCurrentInfo(IntPtr fs);

		void AddPropertySheetPages(uint reserved, IntPtr fn, IntPtr lp);

		void SaveViewState();

		void SelectItem(IntPtr pidl, uint flags);

		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetItemObject(uint item, ref Guid riid);
	}

	public static bool Unelevated(string file, string args, string dir)
	{
		Type typeFromCLSID = Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"));
		object obj = Activator.CreateInstance(typeFromCLSID);
		try
		{
			object[] args2 = new object[5] { 0, null, 8, 0, 1 };
			ParameterModifier parameterModifier = new ParameterModifier(5);
			parameterModifier[3] = true;
			object obj2 = typeFromCLSID.InvokeMember("FindWindowSW", BindingFlags.InvokeMethod, null, obj, args2, new ParameterModifier[1] { parameterModifier }, null, null);
			if (obj2 == null)
			{
				return false;
			}
			Guid service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
			Guid riid = typeof(IShellBrowser).GUID;
			Guid riid2 = new Guid("00020400-0000-0000-C000-000000000046");
			IShellBrowser shellBrowser = (IShellBrowser)((IServiceProvider)obj2).QueryService(ref service, ref riid);
			IShellView shellView = shellBrowser.QueryActiveShellView();
			object itemObject = shellView.GetItemObject(0u, ref riid2);
			object obj3 = itemObject.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, itemObject, null);
			obj3.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, obj3, new object[5] { file, args, dir, "open", 1 });
			return true;
		}
		finally
		{
			Marshal.ReleaseComObject(obj);
		}
	}
}
