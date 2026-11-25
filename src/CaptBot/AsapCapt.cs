namespace CaptBot;

internal class AsapCapt
{
	public int Zone;

	public string A;

	public string D;

	public long Start;

	public long EndLo;

	public long EndHi;

	public bool Ended;

	public bool Exact;

	public bool Own;

	public bool FromGame;

	public bool Live;

	public bool Neutral;

	public bool Gone;

	public string Square
	{
		get
		{
			Zone zone = Zones.ById(Zone);
			if (zone == null)
			{
				return "?";
			}
			return zone.Label;
		}
	}
}
