using System.Globalization;
using System.Text.RegularExpressions;

namespace CaptBot;

public class ServerInfo
{
	public string Id { get; set; }

	public string Name { get; set; }

	public int Players { get; set; }

	public int Queued { get; set; }

	public bool Online { get; set; }

	public bool TechWorks { get; set; }

	public bool RegOpened { get; set; }

	public bool Live { get; set; }

	public string Code => (Id ?? "").ToUpperInvariant();

	public string Title => Code + " · " + Name;

	public string State
	{
		get
		{
			if (!Live)
			{
				return "";
			}
			if (TechWorks)
			{
				return "техработы";
			}
			if (!Online)
			{
				return "недоступен";
			}
			return Players.ToString("#,0", CultureInfo.GetCultureInfo("ru-RU")) + " онлайн" + ((Queued > 0) ? (", очередь " + Queued) : "");
		}
	}

	public int Order
	{
		get
		{
			Match match = Regex.Match(Id ?? "", "^ru(\\d+)$", RegexOptions.IgnoreCase);
			if (!match.Success)
			{
				return 1000;
			}
			return int.Parse(match.Groups[1].Value);
		}
	}
}
