namespace CaptBot;

internal class WarCapt
{
	public int Id;

	public int ZoneId;

	public long AId;

	public long DId;

	public string A;

	public string D;

	public string Square;

	public string Winner;

	public string Src;

	public long Start;

	public long EndLo;

	public long EndHi;

	public long Placed;

	public bool ExactEnd;

	public int ANum;

	public int DNum;

	public long ADmg;

	public long DDmg;

	public string Place;

	public bool Neutral
	{
		get
		{
			if (string.IsNullOrEmpty(D))
			{
				return DId == 0;
			}
			return false;
		}
	}
}
