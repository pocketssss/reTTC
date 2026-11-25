namespace CaptBot;

internal class VzzCfg
{
	public bool Active { get; set; }

	public string Channel { get; set; }

	public string Line1 { get; set; }

	public string Color { get; set; }

	public bool Copy { get; set; }

	public bool Send { get; set; }

	public bool Check { get; set; }

	public bool Sound { get; set; }

	public VzzCfg()
	{
		Channel = "";
		Line1 = "";
		Color = "Случайный";
		Copy = (Send = (Check = (Sound = true)));
	}
}
