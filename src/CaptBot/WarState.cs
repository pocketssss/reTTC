using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptBot;

internal class WarState
{
	public string Server;

	public string OwnersSource;

	public string OwnersTitle;

	public string OwnersError;

	public string OwnersVer;

	public string ServerIcon;

	public string HistorySource;

	public long HistorySynced;

	public long Now;

	public long DayStart;

	public long OwnersUpdated;

	public long OwnersFetched;

	public long OwnersNetAt;

	public int Attacks = 2;

	public int Defenses = 2;

	public int MansionDefenses = 3;

	public int PrepMin = 10;

	public int FightMin = 10;

	public int AtkCdMin;

	public int DefCdMin = 15;

	public int NeutralAtkCdMin = 5;

	public List<WarFamily> Families = new List<WarFamily>();

	public Dictionary<int, int> OwnerIdx = new Dictionary<int, int>();

	public Dictionary<int, long> Since = new Dictionary<int, long>();

	public List<WarCapt> Capts = new List<WarCapt>();

	public List<LiveCapt> Live = new List<LiveCapt>();

	public string StateVer;

	public Dictionary<string, long[]> Ingame = new Dictionary<string, long[]>();

	public DateTime ReceivedUtc;

	public string SourceText
	{
		get
		{
			string text = ((!string.IsNullOrEmpty(OwnersTitle)) ? OwnersTitle : ((OwnersSource == "mitsuki") ? "Mitsuki Hub" : ((OwnersSource == "majesticmcl") ? "Majestic Rating" : ((OwnersSource == "fletcher") ? "fletcher-wiki" : OwnersSource))));
			if (text == null)
			{
				if (OwnersError == null)
				{
					return "владельцев зон пока нет";
				}
				return "владельцы зон не загрузились: " + OwnersError;
			}
			long num = ((OwnersUpdated > 0) ? OwnersUpdated : OwnersFetched);
			return "владельцы зон: " + text + ((num > 0) ? (", " + War.Ago(num, Now)) : "") + ((OwnersError != null) ? " (последнее обновление не удалось)" : "") + ((OwnersNetAt > 0) ? ("; из игры — " + War.Ago(OwnersNetAt, Now)) : "");
		}
	}

	public long[] IngameOf(string family)
	{
		if (family == null || !Ingame.TryGetValue(Zones.Key(family), out var value))
		{
			return null;
		}
		return value;
	}

	public LiveCapt LiveAt(Zone z)
	{
		if (z != null)
		{
			return (from l in Live
				where l.ZoneId == z.Id
				orderby l.FirstSeen descending
				select l).FirstOrDefault();
		}
		return null;
	}

	public WarFamily Family(string name)
	{
		string text = Zones.Key(name);
		if (text.Length == 0)
		{
			return null;
		}
		foreach (WarFamily family in Families)
		{
			if (family.Key == text)
			{
				return family;
			}
		}
		return null;
	}

	public WarFamily OwnerOf(Zone z)
	{
		if (z == null || !OwnerIdx.TryGetValue(z.Id, out var value) || value < 0 || value >= Families.Count)
		{
			return null;
		}
		return Families[value];
	}

	public IEnumerable<WarCapt> Active(long now, Zone z)
	{
		return Capts.Where((WarCapt c) => c.EndHi > now && (z == null || c.ZoneId == z.Id));
	}
}
