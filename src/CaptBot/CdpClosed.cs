namespace CaptBot;

internal class CdpClosed : CdpError
{
	public CdpClosed()
		: base("вкладка браузера закрыта")
	{
	}
}
