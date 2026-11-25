namespace CaptBot;

internal class ProbeResult
{
	public bool Found;

	public bool Covered;

	public bool Admin;

	public bool Black;

	public bool Calibrated;

	public bool PingKnown;

	public bool Unavailable;

	public int Rtt = -1;

	public int Fps = -1;

	public int W;

	public int H;

	public string Size;

	public string Tablet;

	public string Model;

	public string Note;

	public FamilyInfo Family;
}
