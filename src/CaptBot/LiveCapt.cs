namespace CaptBot;

internal class LiveCapt
{
	public int ZoneId;

	public long FirstSeen;

	public long LastSeen;

	public long EndedAt;

	public bool Planned;

	public long StartedAt;

	public string StateText(long now)
	{
		if (EndedAt > 0)
		{
			return "капт кончился в " + War.Hms(EndedAt);
		}
		if (!Active(now))
		{
			return "капт замечен в " + War.Hm(FirstSeen) + ", с тех пор никто не видел";
		}
		if (StartedAt > 0)
		{
			return "капт идёт" + (Planned ? (" с " + War.Hm(StartedAt)) : (", замечен в " + War.Hm(StartedAt)));
		}
		return "капт запланирован, замечен в " + War.Hm(FirstSeen);
	}

	public bool Active(long now)
	{
		if (EndedAt == 0)
		{
			return now - LastSeen < 900;
		}
		return false;
	}
}
