using System.Collections.Generic;

namespace CaptBot;

internal class PlanCfg
{
	public bool Active { get; set; }

	public bool Smart { get; set; }

	public int MinPeople { get; set; }

	public bool Neutral { get; set; }

	public bool Rebid { get; set; }

	public List<PlanEntry> Entries { get; set; }

	public PlanCfg()
	{
		MinPeople = 30;
		Rebid = true;
		Entries = new List<PlanEntry>();
	}
}
