using System;
using System.Collections.Generic;

namespace CaptBot;

internal class RosterInfo
{
	public long Ver;

	public string Title;

	public List<RosterEntry> Entries = new List<RosterEntry>();

	public long UpdatedAt;

	public long ExpiresAt;

	public bool Linked;

	public DateTime ReceivedUtc;
}
