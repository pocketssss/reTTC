using System;

namespace CaptBot;

internal class LicenseState
{
	public bool Ok;

	public bool Lifetime;

	public bool Offline;

	public string Code = "";

	public string Message = "";

	public string Key = "";

	public string Latest = "";

	public string Role = "";

	public long Expires;

	public long ServerTime;

	public int GraceHours = 24;

	public bool FamilySeat;

	public bool FamilyNeed;

	public string Family = "";

	public string FamilyServer = "";

	internal static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	public DateTime ExpiresUtc
	{
		get
		{
			if (!Lifetime)
			{
				return Epoch.AddSeconds(Expires);
			}
			return DateTime.MaxValue;
		}
	}

	public DateTime ServerUtc => Epoch.AddSeconds(ServerTime);

	public bool Definitive
	{
		get
		{
			if (!Ok && Code != "network" && Code != "bad_signature")
			{
				return Code != "rate_limited";
			}
			return false;
		}
	}

	public string Term()
	{
		if (Lifetime)
		{
			return "навсегда";
		}
		DateTime dateTime = ExpiresUtc.AddHours(3.0);
		TimeSpan timeSpan = ExpiresUtc - Clock.UtcNow();
		if (timeSpan <= TimeSpan.Zero)
		{
			return "истёк";
		}
		string text = ((timeSpan.TotalDays >= 1.0) ? ((int)timeSpan.TotalDays + " д " + timeSpan.Hours + " ч") : ((timeSpan.TotalHours >= 1.0) ? ((int)timeSpan.TotalHours + " ч " + timeSpan.Minutes + " мин") : (Math.Max(1, (int)timeSpan.TotalMinutes) + " мин")));
		return "до " + dateTime.ToString("dd.MM.yyyy HH:mm") + " · " + text;
	}
}
