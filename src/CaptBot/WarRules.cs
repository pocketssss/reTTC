using System;
using System.Linq;

namespace CaptBot;

internal static class WarRules
{
	public static SlotCheck Check(WarState s, string own, Zone z, long start)
	{
		SlotCheck slotCheck = new SlotCheck();
		slotCheck.Level = 0;
		slotCheck.Text = "можно";
		SlotCheck slotCheck2 = slotCheck;
		if (s == null)
		{
			slotCheck2.Level = -1;
			slotCheck2.Text = "нет данных с сервера";
			return slotCheck2;
		}
		WarFamily target = s.OwnerOf(z);
		string ownKey = Zones.Key(own);
		if (target != null && target.Key == ownKey && ownKey.Length > 0)
		{
			slotCheck2.Level = 2;
			slotCheck2.Text = "это ваша зона";
			return slotCheck2;
		}
		long dayStart = DayStartOf(s, start);
		long dayEnd = dayStart + 86400;
		bool flag = dayStart > s.DayStart;
		long durMax = (long)(s.PrepMin + s.FightMin) * 60L;
		long durMin = (long)s.PrepMin * 60L;
		if (target != null)
		{
			int num = s.Capts.Count((WarCapt c) => Zones.Key(c.D) == target.Key && c.Start >= dayStart && c.Start < dayEnd);
			int num2 = ((target.Mansion || (target.MansionSoon && flag)) ? s.MansionDefenses : s.Defenses);
			if (num >= num2)
			{
				Worse(slotCheck2, 2, target.Name + ": защит уже " + num + " из " + num2 + (flag ? " на завтра" : " за сегодня"));
			}
			foreach (WarCapt item in s.Capts.Where((WarCapt c) => Zones.Key(c.D) == target.Key))
			{
				Overlap(slotCheck2, item, start, durMin, durMax, s.DefCdMin, target.Name + " защищается");
			}
			long now = War.NowUnix();
			foreach (LiveCapt item2 in s.Live)
			{
				WarFamily warFamily = s.OwnerOf(Zones.ById(item2.ZoneId));
				if (warFamily != null && !(warFamily.Key != target.Key))
				{
					Zone zone = Zones.ById(item2.ZoneId);
					if (item2.Active(now))
					{
						Worse(slotCheck2, 1, target.Name + ": на " + zone.Label + " у них капт (видят игроки TTC), КД будет после его конца");
					}
					else if (item2.EndedAt > 0 && start < item2.EndedAt + (long)s.DefCdMin * 60L)
					{
						Worse(slotCheck2, 2, target.Name + ": капт на " + zone.Label + " кончился в " + War.Hms(item2.EndedAt) + ", КД до " + War.Hm(item2.EndedAt + (long)s.DefCdMin * 60L));
					}
				}
			}
		}
		if (ownKey.Length > 0)
		{
			WarFamily warFamily2 = s.Family(own);
			string text = ((warFamily2 != null) ? warFamily2.Name : own);
			int num3 = s.Capts.Count((WarCapt c) => Zones.Key(c.A) == ownKey && c.Start >= dayStart && c.Start < dayEnd && !c.Neutral);
			if (num3 >= s.Attacks)
			{
				Worse(slotCheck2, 2, "у вашей семьи атак уже " + num3 + " из " + s.Attacks + (flag ? " на завтра" : " за сегодня"));
			}
			foreach (WarCapt item3 in s.Capts.Where((WarCapt c) => Zones.Key(c.A) == ownKey))
			{
				Overlap(slotCheck2, item3, start, durMin, durMax, item3.Neutral ? s.NeutralAtkCdMin : s.AtkCdMin, text + (item3.Neutral ? " атакует ничейную зону" : " атакует"));
			}
		}
		if (slotCheck2.Level == 0 && target == null)
		{
			slotCheck2.Text = "зона свободна или владелец неизвестен";
		}
		return slotCheck2;
	}

	private static void Overlap(SlotCheck res, WarCapt c, long start, long durMin, long durMax, int cdMin, string who)
	{
		long num = (long)cdMin * 60L;
		string text = "капт в " + War.Hm(c.Start) + ((c.Square != null) ? (" (" + c.Square + ")") : "");
		if (start < c.EndHi + num && start + durMax + num > c.Start)
		{
			if (start >= c.EndLo + num)
			{
				Worse(res, 1, who + ": " + text + ", КД кончится в " + War.Window(new long[2]
				{
					c.EndLo + num,
					c.EndHi + num
				}));
			}
			else if (start + durMin + num <= c.Start)
			{
				Worse(res, 1, who + ": " + text + " — успеет, только если ваш капт кончится рано");
			}
			else
			{
				Worse(res, 2, who + ": " + text + ", КД до " + War.Window(new long[2]
				{
					c.EndLo + num,
					c.EndHi + num
				}));
			}
		}
	}

	private static void Worse(SlotCheck res, int level, string text)
	{
		if (level > res.Level)
		{
			res.Level = level;
			res.Text = text;
		}
	}

	private static long DayStartOf(WarState s, long t)
	{
		long num;
		for (num = ((s.DayStart > 0) ? s.DayStart : War.Unix(Clock.Msk(Clock.UtcNow()).Date.AddHours(-3.0))); t >= num + 86400; num += 86400)
		{
		}
		while (t < num)
		{
			num -= 86400;
		}
		return num;
	}

	public static long NextFree(WarState s, string own, Zone z, long from, out SlotCheck check)
	{
		long num = (from + 299) / 300 * 300;
		long num2 = 0L;
		SlotCheck slotCheck = null;
		int num3 = 0;
		while (num3 < 288)
		{
			SlotCheck slotCheck2 = Check(s, own, z, num);
			if (slotCheck2.Level == 0)
			{
				check = slotCheck2;
				return num;
			}
			if (slotCheck2.Level == 1 && num2 == 0)
			{
				num2 = num;
				slotCheck = slotCheck2;
			}
			num3++;
			num += 300;
		}
		check = slotCheck;
		return num2;
	}

	public static DateTime CaptUtc(int h, int m, DateTime afterUtc)
	{
		DateTime dateTime = Clock.Msk(afterUtc);
		DateTime dateTime2 = dateTime.Date.AddHours(h).AddMinutes(m);
		if (dateTime2 <= dateTime)
		{
			dateTime2 = dateTime2.AddDays(1.0);
		}
		return DateTime.SpecifyKind(dateTime2.AddHours(-3.0), DateTimeKind.Utc);
	}
}
