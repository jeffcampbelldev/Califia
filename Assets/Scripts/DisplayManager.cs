using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class DisplayManager : MonoBehaviour
{
	//cycles through the connected displays by calling the ChangeDisplayClicked() method
	//does not work with windowed mode

	List<DisplayInfo> myDisplays = new List<DisplayInfo>();
	List<MyMonitor> myMonitors = new List<MyMonitor>();
	System.IntPtr _windowPtr;

    public static readonly System.IntPtr HWND_TOPMOST = new System.IntPtr(-1);
    public static readonly System.IntPtr HWND_NOTTOPMOST = new System.IntPtr(-2);

	public int _monitorCache=-10;

    const System.UInt32 SWP_SHOWWINDOW = 0x0040;

	[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
	internal static extern void MoveWindow(System.IntPtr hwnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

	[DllImport("user32.dll")]
	private static extern bool SetWindowPos(System.IntPtr hwnd, System.IntPtr hwndAfter, int X, int Y, int cx, int cy, uint uFlags);

	[DllImport("user32.dll")]
	private static extern System.IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern System.IntPtr GetTopWindow(System.IntPtr hwnd);

	[DllImport("user32.dll")]
	private static extern System.IntPtr FindWindow(string className, string windowName);

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(System.IntPtr hwnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern bool SetForegroundWindow(System.IntPtr hwnd);

	[DllImport("user32.dll")]
	private static extern bool LockSetForegroundWindow(uint uLockCode);
	
	[DllImport("user32.dll")]
	private static extern bool SetWindowLongW(System.IntPtr hwnd, int nIndex, long dwNewLong);

	[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, ExactSpelling = true, SetLastError = true)]
	internal static extern bool GetWindowRect(System.IntPtr hWnd, ref RECT rect);

	[DllImport("user32.dll")]
	static extern bool EnumDisplayMonitors(System.IntPtr hdc, System.IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, System.IntPtr dwData);

	delegate bool MonitorEnumDelegate(System.IntPtr hMonitor, System.IntPtr hdcMonitor, ref RECT lprcMonitor, System.IntPtr dwData);

	static ConfirmMenu _confirm;
	public static bool _forceClose;

	[StructLayout(LayoutKind.Sequential)]
	public struct RECT
	{
		public int left;
		public int top;
		public int right;
		public int bottom;
	}

	public class DisplayInfo
	{
		public string Availability { get; set; }
		public string ScreenHeight { get; set; }
		public string ScreenWidth { get; set; }
		public RECT MonitorArea { get; set; }
		public RECT WorkArea { get; set; }
	}

	public class DisplayInfoCollection : List<DisplayInfo>
	{
	}

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	public static extern bool GetMonitorInfo(System.IntPtr hmonitor, [In, Out] MONITORINFOEX info);
	[DllImport("User32.dll", ExactSpelling = true)]
	public static extern System.IntPtr MonitorFromPoint(POINTSTRUCT pt, int flags);

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
	public class MONITORINFOEX
	{
		public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
		public RECT rcMonitor = new RECT();
		public RECT rcWork = new RECT();
		public int dwFlags = 0;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		public char[] szDevice = new char[32];
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct POINTSTRUCT
	{
		public int x;
		public int y;
		public POINTSTRUCT(int x, int y)
		{
			this.x = x;
			this.y = y;
		}
	}

	public DisplayInfoCollection GetDisplays()
	{
		DisplayInfoCollection col = new DisplayInfoCollection();

		EnumDisplayMonitors(System.IntPtr.Zero, System.IntPtr.Zero,
		delegate (System.IntPtr hMonitor, System.IntPtr hdcMonitor, ref RECT lprcMonitor, System.IntPtr dwData)
		{
			MONITORINFOEX mi = new MONITORINFOEX();
			mi.cbSize = (int)Marshal.SizeOf(mi);
			bool success = GetMonitorInfo(hMonitor, mi);
			if (success)
			{
				DisplayInfo di = new DisplayInfo();
				di.ScreenWidth = (mi.rcMonitor.right - mi.rcMonitor.left).ToString();
				di.ScreenHeight = (mi.rcMonitor.bottom - mi.rcMonitor.top).ToString();
				di.MonitorArea = mi.rcMonitor;
				di.WorkArea = mi.rcWork;
				di.Availability = mi.dwFlags.ToString();
				col.Add(di);
			}
			return true;
		}, System.IntPtr.Zero);
		return col;
	}

	public class MyMonitor
	{
		public int targetX;
		public int monitorNumber;
		public int height;
		public int width;

		public MyMonitor(int targetX, int monitorNumber, int height, int width)
		{
			this.targetX = targetX;
			this.monitorNumber = monitorNumber;
			this.height = height;
			this.width = width;
		}
	}

	private void Start(){
		myDisplays = GetDisplays();
		for (int i = 0; i < myDisplays.Count; i++)
		{
			myMonitors.Add(new MyMonitor(myDisplays[i].WorkArea.left, i, System.Convert.ToInt32(myDisplays[i].ScreenHeight), System.Convert.ToInt32(myDisplays[i].ScreenWidth)));
		}
		GetActiveWindowPointer();
		_confirm = FindObjectOfType<ConfirmMenu>();
		Application.wantsToQuit += WantsToQuit;
	}

	static void ReallyWantsToQuit(){
		_forceClose=true;
		Application.Quit();
	}

	[ContextMenu("hm")]
	public void Hmm(){
		ConfirmMenu f = FindObjectOfType<ConfirmMenu>();
		_confirm.ConfirmRequest("What's going on here");
	}

	static bool WantsToQuit(){
		_confirm.ConfirmRequest("Are you sure you'd like to exit?");
		_confirm._confirm.onClick.AddListener(delegate {ReallyWantsToQuit();});
		return _forceClose;
	}

	public void SetWindowAsTopmost(bool onTop){
#if UNITY_EDITOR
#else
		RECT rect = new RECT();
		GetWindowRect(_windowPtr, ref rect);
		SetWindowPos(_windowPtr,onTop ? HWND_TOPMOST: HWND_NOTTOPMOST,rect.left,rect.top,rect.right-rect.left,rect.bottom-rect.top,0);
#endif
	}

	public void SetStyle(long style){
		SetWindowLongW(_windowPtr,-16,style);
	}

	public int TrySetDisplay(int desiredMonitor){
		if((int)_windowPtr==0)
			GetActiveWindowPointer();
		if((int)_windowPtr==0)
			return -10;
		Debug.Log("Trying set display to: "+desiredMonitor);
		Debug.Log("Monitor cache: "+_monitorCache);
		if(desiredMonitor==_monitorCache)
			return _monitorCache;
		else
		{
			if(desiredMonitor==99)
				desiredMonitor=_monitorCache;
			else
				_monitorCache=desiredMonitor;
		}
		if(desiredMonitor<0)
		{
			Minimize();
			return _monitorCache;
		}
		Restore();
		if(desiredMonitor < myDisplays.Count){
			MyMonitor mym = myMonitors[desiredMonitor];
			MyMoveWindow(mym.height,mym.width,mym.targetX);
		}
		//if desired monitor is 9, then we don't want window as topmost
		SetWindowAsTopmost(desiredMonitor!=9);
		Maximize();
		Debug.Log("Setting foreground window: "+_windowPtr);
		SetForegroundWindow(_windowPtr);
		return _monitorCache;
	}

	public void MyMoveWindow(int newHeight, int newWidth, int targetX)
	{
		RECT Rect = new RECT();
		//yield return new WaitForSeconds(2);
		GetWindowRect(_windowPtr, ref Rect);
		MoveWindow(_windowPtr, targetX, Rect.top, newWidth, newHeight, true);
	}

	public void Minimize(){
		//Debug.Log("minimizing: "+_windowPtr);
		ShowWindow(_windowPtr, 2);
	}
	public void Restore(){
		//Debug.Log("restoring: "+_windowPtr);
		ShowWindow(_windowPtr, 9);
	}
	public void Maximize(){
		//Debug.Log("maximizing: "+_windowPtr);
		ShowWindow(_windowPtr, 3);
	}
	public void GetActiveWindowPointer(){
#if UNITY_EDITOR
	   	_windowPtr=GetActiveWindow();
#else
		_windowPtr = FindWindow(null, "Califia3D");
#endif
	}
}
