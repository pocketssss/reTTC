namespace CaptBot;

public class Settings
{
	public string Square { get; set; }

	public int CaptH { get; set; }

	public int CaptM { get; set; }

	public string FireTime { get; set; }

	public int People { get; set; }

	public int PrepSec { get; set; }

	public bool AutoPing { get; set; }

	public int FineMs { get; set; }

	public int WinLeft { get; set; }

	public int WinTop { get; set; }

	public string Server { get; set; }

	public string Family { get; set; }

	public bool AfterCapt { get; set; }

	public int AfterMin { get; set; }

	public int WinW { get; set; }

	public int WinH { get; set; }

	public bool WinMax { get; set; }

	public bool StartWithWindows { get; set; }

	public bool StartWithWindowsChosen { get; set; }

	public bool TrayTipShown { get; set; }

	public bool OnboardDone { get; set; }

	public Settings()
	{
		Square = "AI40";
		CaptH = 20;
		CaptM = 0;
		FireTime = "19:00:00";
		People = 10;
		PrepSec = 25;
		AutoPing = true;
		FineMs = 0;
		WinLeft = int.MinValue;
		WinTop = int.MinValue;
		Server = "";
		Family = "";
		AfterCapt = false;
		AfterMin = 15;
	}
}
