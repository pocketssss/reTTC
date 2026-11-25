namespace CaptBot;

internal class NetCapt
{
	public int Zone;

	public long Start;

	public long End;

	public long AId;

	public long DId;

	public string A;

	public string D;

	public string Winner;

	public long PrevStart;

	public NetCapt Copy()
	{
		return (NetCapt)MemberwiseClone();
	}
}
