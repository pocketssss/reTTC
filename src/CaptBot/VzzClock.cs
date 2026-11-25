using System;
using System.Globalization;

namespace CaptBot;

internal class VzzClock
{
	public double Offset;

	public void Learn(string date)
	{
		if (!string.IsNullOrEmpty(date) && DateTime.TryParseExact(date, "r", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result))
		{
			Offset = (result - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds + 0.5 - LocalNow();
		}
	}

	private static double LocalNow()
	{
		return (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	public double Now()
	{
		return LocalNow() + Offset;
	}
}
