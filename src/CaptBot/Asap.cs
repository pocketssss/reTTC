using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptBot;

internal static class Asap
{
	public const int OpenLeadSec = 75;

	public static readonly TimeSpan DayFrom = new TimeSpan(15, 0, 0);

	public static readonly TimeSpan DayLast = new TimeSpan(21, 55, 0);

	public static bool NetEvents
	{
		get
		{
			if (!NetWatch.Running || !NetWatch.GameRunning)
			{
				return false;
			}
			if (NetWatch.RttStats(60.0, out var _, out var _, out var _, out var n) && n > 0)
			{
				return true;
			}
			if (NetWatch.LastEventUtc != DateTime.MinValue)
			{
				return (NetWatch.Now() - NetWatch.LastEventUtc).TotalMinutes < 10.0;
			}
			return false;
		}
	}

	public static bool CaptTimeOk(int h, int m)
	{
		TimeSpan timeSpan = new TimeSpan(h, m, 0);
		if (timeSpan >= DayFrom)
		{
			return timeSpan <= DayLast;
		}
		return false;
	}

	private static string K(string s)
	{
		return Zones.Key(s);
	}

	private static bool Net(WarCapt c)
	{
		return (c.Src ?? "").Split(',').Contains("net");
	}

	public static long MskUnix(DateTime msk)
	{
		return War.Unix(DateTime.SpecifyKind(msk.AddHours(-3.0), DateTimeKind.Utc));
	}

	private static long DayStart(long t)
	{
		return MskUnix(Clock.Msk(War.FromUnix(t)).Date);
	}

	public static DateTime CaptAt(DateTime pressUtc, DateTime baseUtc, int afterMin, double fillSec)
	{
		return Runner.CaptWindow(Clock.Msk(pressUtc), Clock.Msk(baseUtc), afterMin, fillSec);
	}

	public static DateTime AtLeast(DateTime hm, long captFrom, DateTime nowUtc)
	{
		if (captFrom <= 0)
		{
			return hm;
		}
		DateTime dateTime = Clock.Msk(nowUtc);
		DateTime dateTime2 = dateTime.Date.AddHours(hm.Hour).AddMinutes(hm.Minute);
		if (dateTime2 < dateTime.AddHours(-12.0))
		{
			dateTime2 = dateTime2.AddDays(1.0);
		}
		DateTime dateTime3 = Clock.Msk(War.FromUnix(captFrom));
		if (dateTime2 >= dateTime3)
		{
			return hm;
		}
		DateTime dateTime4 = dateTime3.AddSeconds(59 - dateTime3.Second);
		dateTime4 = new DateTime(dateTime4.Year, dateTime4.Month, dateTime4.Day, dateTime4.Hour, dateTime4.Minute, 0);
		if (dateTime4.Minute % 5 != 0)
		{
			dateTime4 = dateTime4.AddMinutes(5 - dateTime4.Minute % 5);
		}
		if (!(dateTime4 <= dateTime.AddMinutes(60.0)))
		{
			return hm;
		}
		return dateTime4;
	}

	public static bool Same(AsapCapt a, AsapCapt b)
	{
		if (a.Zone != b.Zone)
		{
			return false;
		}
		if (!a.Live && !b.Live && Math.Abs(a.Start - b.Start) > 900)
		{
			return false;
		}
		string text = K(a.A);
		string text2 = K(b.A);
		if (text.Length != 0 && text2.Length != 0)
		{
			return text == text2;
		}
		return true;
	}

	public static string Squares(IEnumerable<AsapCapt> l)
	{
		return string.Join(", ", l.Select((AsapCapt c) => c.Square).Distinct().ToArray());
	}

	public static long EndedOnData(WarState st, int zone, long since, out string src)
	{
		src = null;
		if (st == null)
		{
			return 0L;
		}
		WarCapt warCapt = (from c in st.Capts
			where c.ZoneId == zone && c.ExactEnd && c.EndHi >= since
			orderby c.EndHi descending
			select c).FirstOrDefault();
		if (warCapt != null)
		{
			src = (Net(warCapt) ? "из игры, по карте TTC" : "по карте TTC");
			return warCapt.EndHi;
		}
		LiveCapt liveCapt = st.LiveAt(Zones.ById(zone));
		if (liveCapt != null && liveCapt.EndedAt >= since)
		{
			src = "по картам игроков TTC";
			return liveCapt.EndedAt;
		}
		return 0L;
	}

	public static List<AsapCapt> Merge(WarState st, long now, bool events, bool full, IList<long[]> seenEnds)
	{
		int num = ((st != null && st.PrepMin > 0) ? st.PrepMin : 10);
		int num2 = ((st != null && st.FightMin > 0) ? st.FightMin : 10);
		List<AsapCapt> list = new List<AsapCapt>();
		if (st != null)
		{
			foreach (WarCapt capt in st.Capts)
			{
				if (capt.Start < now - 108000)
				{
					continue;
				}
				AsapCapt asapCapt = new AsapCapt();
				asapCapt.Zone = capt.ZoneId;
				asapCapt.A = capt.A;
				asapCapt.D = capt.D;
				asapCapt.Start = capt.Start;
				asapCapt.EndLo = capt.EndLo;
				asapCapt.EndHi = capt.EndHi;
				asapCapt.Exact = capt.ExactEnd;
				asapCapt.FromGame = Net(capt);
				asapCapt.Neutral = capt.Neutral;
				AsapCapt asapCapt2 = asapCapt;
				if (asapCapt2.EndLo <= 0)
				{
					asapCapt2.EndLo = asapCapt2.Start + num * 60;
				}
				if (asapCapt2.EndHi < asapCapt2.EndLo)
				{
					asapCapt2.EndHi = Math.Max(asapCapt2.EndLo, asapCapt2.Start + (num + num2) * 60);
				}
				asapCapt2.Ended = capt.ExactEnd || capt.Winner != null || asapCapt2.EndHi <= now;
				if (asapCapt2.Ended && !asapCapt2.Exact && asapCapt2.EndHi > now)
				{
					asapCapt2.EndHi = now;
					if (asapCapt2.EndLo > now)
					{
						asapCapt2.EndLo = now;
					}
				}
				list.Add(asapCapt2);
			}
			foreach (LiveCapt lc in st.Live)
			{
				Func<AsapCapt, bool> predicate = (AsapCapt x) => x.Zone == lc.ZoneId && x.Start <= lc.LastSeen + 3600 && (!x.Ended || x.EndHi >= lc.FirstSeen - 600);
				AsapCapt asapCapt3 = (from x in list.Where(predicate)
					orderby x.Start descending
					select x).FirstOrDefault();
				if (lc.EndedAt > 0)
				{
					if (asapCapt3 != null && !asapCapt3.Exact && lc.EndedAt >= asapCapt3.Start)
					{
						asapCapt3.Ended = true;
						asapCapt3.Exact = true;
						asapCapt3.EndLo = (asapCapt3.EndHi = lc.EndedAt);
					}
				}
				else if (asapCapt3 == null && lc.Active(now))
				{
					Zone zone = Zones.ById(lc.ZoneId);
					WarFamily warFamily = ((zone != null) ? st.OwnerOf(zone) : null);
					long num3 = ((lc.StartedAt > 0) ? lc.StartedAt : lc.FirstSeen);
					list.Add(new AsapCapt
					{
						Zone = lc.ZoneId,
						D = warFamily?.Name,
						Start = num3,
						Live = true,
						EndLo = ((lc.StartedAt > 0) ? Math.Max(now, lc.StartedAt) : (lc.FirstSeen + num * 60)),
						EndHi = num3 + (num + num2) * 60 + ((lc.StartedAt <= 0) ? 3600 : 0)
					});
				}
			}
		}
		if (events)
		{
			foreach (NetCapt i in NetWatch.Active())
			{
				string nk = K(i.A);
				IEnumerable<AsapCapt> source = list.Where((AsapCapt x) => x.Zone == i.Zone && !x.Own && (x.Live || (!x.Exact && Math.Abs(x.Start - i.Start) <= 10800 && (K(x.A).Length == 0 || nk.Length == 0 || i.A.StartsWith("#") || K(x.A) == nk))));
				Func<AsapCapt, long> keySelector = (AsapCapt x) => Math.Abs(x.Start - i.Start);
				AsapCapt asapCapt4 = source.OrderBy(keySelector).FirstOrDefault();
				if (asapCapt4 == null)
				{
					asapCapt4 = new AsapCapt();
					list.Add(asapCapt4);
				}
				asapCapt4.Zone = i.Zone;
				asapCapt4.A = i.A;
				if (!string.IsNullOrEmpty(i.D))
				{
					asapCapt4.D = i.D;
				}
				asapCapt4.Start = i.Start;
				asapCapt4.EndLo = i.Start + num * 60;
				asapCapt4.EndHi = i.Start + (num + num2) * 60;
				asapCapt4.Ended = false;
				asapCapt4.Exact = false;
				asapCapt4.Own = true;
				asapCapt4.FromGame = true;
				asapCapt4.Live = false;
				asapCapt4.Neutral = i.DId == 0;
			}
			foreach (NetCapt e in NetWatch.EndedSince(now - 108000))
			{
				Func<AsapCapt, bool> predicate2 = (AsapCapt x) => x.Zone == e.Zone && x.Start <= e.End && (x.Live || Math.Abs(x.Start - e.Start) <= 10800) && (!x.Exact || Math.Abs(x.EndHi - e.End) <= 120);
				AsapCapt asapCapt5 = (from x in list.Where(predicate2)
					orderby Math.Abs(x.Start - e.Start)
					select x).FirstOrDefault();
				if (asapCapt5 == null)
				{
					AsapCapt asapCapt6 = new AsapCapt();
					asapCapt6.Zone = e.Zone;
					asapCapt6.A = e.A;
					asapCapt6.D = e.D;
					asapCapt6.Neutral = e.DId == 0;
					asapCapt5 = asapCapt6;
					list.Add(asapCapt5);
				}
				if (string.IsNullOrEmpty(asapCapt5.D) && !string.IsNullOrEmpty(e.D))
				{
					asapCapt5.D = e.D;
				}
				if (string.IsNullOrEmpty(asapCapt5.A) || asapCapt5.A.StartsWith("#"))
				{
					asapCapt5.A = e.A;
				}
				asapCapt5.Start = e.Start;
				asapCapt5.Ended = true;
				asapCapt5.Exact = true;
				asapCapt5.EndLo = (asapCapt5.EndHi = e.End);
				asapCapt5.Own = true;
				asapCapt5.FromGame = true;
				asapCapt5.Live = false;
			}
			if (full)
			{
				foreach (AsapCapt item in list)
				{
					if (item.Own || item.Ended)
					{
						continue;
					}
					NetCapt netCapt = NetWatch.CaptOn(item.Zone);
					if (netCapt == null || (!item.Live && Math.Abs(netCapt.Start - item.Start) > 600))
					{
						if (item.Start > now)
						{
							item.Gone = true;
							continue;
						}
						item.Ended = true;
						item.EndHi = Math.Max(item.Start, Math.Min(item.EndHi, now));
						item.EndLo = Math.Min(item.EndLo, item.EndHi);
					}
				}
			}
		}
		if (seenEnds != null)
		{
			foreach (long[] se in seenEnds)
			{
				foreach (AsapCapt item2 in list.Where((AsapCapt c) => c.Zone == se[0] && !c.Ended && c.Start <= se[1]))
				{
					item2.Ended = true;
					item2.EndLo = (item2.EndHi = se[1]);
				}
			}
		}
		foreach (AsapCapt item3 in list)
		{
			if (!string.IsNullOrEmpty(item3.D) || item3.Neutral || item3.Live)
			{
				continue;
			}
			if (events && NetWatch.OwnerOf(item3.Zone, out var _, out var name) && name != null && K(name) != K(item3.A))
			{
				item3.D = name;
			}
			else if (st != null)
			{
				WarFamily warFamily2 = st.OwnerOf(Zones.ById(item3.Zone));
				if (warFamily2 != null && warFamily2.Key != K(item3.A))
				{
					item3.D = warFamily2.Name;
				}
			}
		}
		return list;
	}

	private static long EarliestPress(long from, long captFrom, int afterMin)
	{
		Func<long, bool> func = (long t) => MskUnix(CaptAt(War.FromUnix(t), War.FromUnix(t), afterMin, 10.0)) >= captFrom;
		if (func(from))
		{
			return from;
		}
		long num = from;
		long num2 = Math.Max(from + 1, captFrom);
		while (num2 - num > 1)
		{
			long num3 = num + (num2 - num) / 2;
			if (func(num3))
			{
				num2 = num3;
			}
			else
			{
				num = num3;
			}
		}
		return num2;
	}

	private static string CaptDesc(AsapCapt c, long now)
	{
		if (c.Live)
		{
			return "отметка капта на картах игроков TTC, замечена в " + War.Hm(c.Start);
		}
		string text = (c.A ?? "?") + " → " + (c.Neutral ? "ничья зона" : (c.D ?? "?"));
		string text2 = ((c.Start > now) ? ("капт в " + War.Hm(c.Start)) : ("капт идёт с " + War.Hm(c.Start)));
		return text + ", " + text2 + (c.Own ? " · из игры" : (c.FromGame ? " · из игры у копий TTC" : ""));
	}

	public static AsapVerdict Evaluate(WarState st, Zone target, string own, int afterMin, long now, IList<long[]> seenEnds)
	{
		AsapVerdict asapVerdict = new AsapVerdict();
		bool netEvents = NetEvents;
		bool flag = netEvents && NetWatch.LastFullUtc != DateTime.MinValue;
		asapVerdict.Events = netEvents;
		asapVerdict.Full = flag;
		asapVerdict.Server = st != null;
		asapVerdict.Src = SourceText(st, netEvents, flag);
		int num = st?.DefCdMin ?? 15;
		int num2 = st?.AtkCdMin ?? 0;
		int num3 = st?.NeutralAtkCdMin ?? 5;
		int num4 = ((st != null && st.Defenses > 0) ? st.Defenses : 2);
		int num5 = ((st != null && st.MansionDefenses > 0) ? st.MansionDefenses : 3);
		int num6 = ((st != null && st.Attacks > 0) ? st.Attacks : 2);
		asapVerdict.Capts = Merge(st, now, netEvents, flag, seenEnds);
		List<AsapCapt> capts = asapVerdict.Capts;
		string text = null;
		bool flag2 = false;
		WarFamily warFamily = st?.OwnerOf(target);
		if (netEvents && NetWatch.OwnerOf(target.Id, out var id, out var name))
		{
			flag2 = true;
			text = ((id == 0) ? null : (name ?? ((warFamily != null) ? warFamily.Name : ("#" + id))));
		}
		else if (st != null && st.OwnerIdx.Count > 0)
		{
			flag2 = true;
			text = warFamily?.Name;
		}
		WarFamily warFamily2 = ((st != null && text != null) ? st.Family(text) : null);
		asapVerdict.Owner = text;
		asapVerdict.OwnerKnown = flag2;
		asapVerdict.Neutral = flag2 && text == null;
		string tk = K(text);
		string fk = K(own);
		if (!netEvents && st == null)
		{
			asapVerdict.Level = -1;
			asapVerdict.PressAt = now;
			asapVerdict.CaptMsk = CaptAt(War.FromUnix(now), War.FromUnix(now), afterMin, 10.0);
			asapVerdict.Headline = "Нет данных о каптах";
			asapVerdict.Detail = "Своя игра не читается" + NetWhy() + ", а сервер вверху окна не выбран — что мешает цели, не видно.";
			asapVerdict.Plan = "Бот сразу откроет планшет и поставит атаку (капт на " + asapVerdict.CaptMsk.ToString("HH:mm") + "); если цель занята, игра откажет.";
			return asapVerdict;
		}
		if (tk.Length > 0 && tk == fk)
		{
			return Never(asapVerdict, "Это ваша зона", "Квадрат " + target.Label + " — зона вашей семьи.", "Бить свою зону не нужно — выберите другую цель.", now);
		}
		AsapCapt asapCapt = ((fk.Length == 0) ? null : capts.FirstOrDefault((AsapCapt c) => !c.Ended && !c.Gone && c.Zone == target.Id && K(c.A) == fk));
		if (asapCapt != null)
		{
			return Never(asapVerdict, "Капт уже стоит", "Ваша семья уже поставила капт на " + target.Label + " в " + War.Hm(asapCapt.Start) + ".", "Второй раз эту зону не забить.", now);
		}
		foreach (AsapCapt item in capts)
		{
			if (!item.Ended && !item.Gone)
			{
				string text2 = K(item.D);
				string text3 = K(item.A);
				string text4 = null;
				if (tk.Length > 0 && text2 == tk)
				{
					text4 = text + " защищается";
				}
				else if (tk.Length > 0 && text3 == tk)
				{
					text4 = text + " атакует";
				}
				else if (fk.Length > 0 && text3 == fk)
				{
					text4 = "Ваша семья атакует";
				}
				else if (fk.Length > 0 && text2 == fk)
				{
					text4 = "Ваша семья защищается";
				}
				else if (item.Zone == target.Id)
				{
					text4 = "На цели уже стоит капт";
				}
				if (text4 != null)
				{
					asapVerdict.Waiting.Add(item);
					asapVerdict.Blocks.Add(new AsapBlock
					{
						Capt = item,
						Text = text4 + " на " + item.Square + " (" + CaptDesc(item, now) + ")"
					});
				}
			}
		}
		foreach (AsapCapt item2 in capts)
		{
			if (!item2.Ended || item2.Gone || item2.EndHi < now - 10800)
			{
				continue;
			}
			if (tk.Length > 0 && K(item2.D) == tk && num > 0)
			{
				asapVerdict.Blocks.Add(new AsapBlock
				{
					CaptFrom = item2.EndHi + num * 60,
					Text = text + ": КД защиты после капта на " + item2.Square + " — капт не раньше " + War.Hm(item2.EndHi + num * 60)
				});
			}
			if (fk.Length > 0 && K(item2.A) == fk)
			{
				int num7 = (item2.Neutral ? num3 : num2);
				if (num7 > 0)
				{
					asapVerdict.Blocks.Add(new AsapBlock
					{
						CaptFrom = item2.EndHi + num7 * 60,
						Text = "У вашей семьи КД атаки после капта на " + item2.Square + " — капт не раньше " + War.Hm(item2.EndHi + num7 * 60)
					});
				}
			}
		}
		if (warFamily2 != null && warFamily2.DefCd != null && warFamily2.DefCd[1] > now)
		{
			asapVerdict.Blocks.Add(new AsapBlock
			{
				CaptFrom = warFamily2.DefCd[1],
				Text = text + ": КД защиты до " + War.Window(warFamily2.DefCd)
			});
		}
		NetLimits limits = NetWatch.Limits;
		bool flag3 = netEvents && limits != null && (NetWatch.Now() - limits.At).TotalHours < 3.0 && (string.IsNullOrEmpty(limits.Family) || fk.Length == 0 || K(limits.Family) == fk);
		WarFamily warFamily3 = ((st != null && fk.Length > 0) ? st.Family(own) : null);
		if (flag3 && limits.AtkCdUntil > now)
		{
			asapVerdict.Blocks.Add(new AsapBlock
			{
				PressFrom = limits.AtkCdUntil + 1,
				Text = "У вашей семьи КД атаки до " + War.Hms(limits.AtkCdUntil) + " (из игры)"
			});
		}
		else if (!flag3 && warFamily3 != null && warFamily3.AtkCd != null && warFamily3.AtkCd[1] > now)
		{
			asapVerdict.Blocks.Add(new AsapBlock
			{
				PressFrom = warFamily3.AtkCd[1] + 1,
				Text = "У вашей семьи КД атаки до " + War.Window(warFamily3.AtkCd)
			});
		}
		long num8 = now;
		long num9 = 0L;
		foreach (AsapBlock block in asapVerdict.Blocks)
		{
			block.At = ((block.Capt != null) ? Math.Max(now, block.Capt.EndLo) : ((block.PressFrom > 0) ? Math.Max(now, block.PressFrom) : EarliestPress(now, block.CaptFrom, afterMin)));
			if (block.At > num8)
			{
				num8 = block.At;
			}
			if (block.CaptFrom > num9)
			{
				num9 = block.CaptFrom;
			}
		}
		if (num9 > 0)
		{
			num8 = EarliestPress(num8, num9, afterMin);
		}
		DateTime dateTime = Clock.Msk(War.FromUnix(num8));
		DateTime dateTime2 = CaptAt(War.FromUnix(num8), War.FromUnix(num8), afterMin, 10.0);
		if (dateTime2.TimeOfDay < DayFrom && dateTime2.Date == dateTime.Date)
		{
			long num10 = MskUnix(dateTime.Date.Add(DayFrom).AddMinutes(-60.0)) + 1;
			asapVerdict.Blocks.Add(new AsapBlock
			{
				PressFrom = num10,
				At = Math.Max(now, num10),
				Text = "Капты ставят с 15:00 МСК — капт на 15:00 игра примет с 14:00"
			});
			num8 = Math.Max(num8, num10);
			dateTime = Clock.Msk(War.FromUnix(num8));
			dateTime2 = CaptAt(War.FromUnix(num8), War.FromUnix(num8), afterMin, 10.0);
		}
		asapVerdict.PressAt = num8;
		asapVerdict.CaptMsk = dateTime2;
		asapVerdict.CaptFrom = num9;
		asapVerdict.Blocks.RemoveAll((AsapBlock b) => b.Capt == null && b.At <= now);
		asapVerdict.Main = (from b in asapVerdict.Blocks
			orderby b.At descending, b.Capt != null descending
			select b).FirstOrDefault();
		long t = MskUnix(dateTime2);
		long ds = DayStart(t);
		long de = ds + 86400;
		bool flag4 = ds == DayStart(now);
		string text5 = (flag4 ? "за сегодня" : ("на " + dateTime2.ToString("dd.MM")));
		if (!asapVerdict.Neutral && tk.Length > 0)
		{
			int num11 = capts.Count((AsapCapt c) => !c.Gone && K(c.D) == tk && c.Start >= ds && c.Start < de);
			int num12 = ((warFamily2 != null && (warFamily2.Mansion || (warFamily2.MansionSoon && !flag4))) ? num5 : num4);
			if (warFamily2 != null && flag4 && warFamily2.DefMax > 0)
			{
				num12 = warFamily2.DefMax;
				num11 = Math.Max(num11, warFamily2.DefUsed);
			}
			asapVerdict.DefUsed = num11;
			asapVerdict.DefMax = num12;
			if (num11 >= num12)
			{
				return Never(asapVerdict, "Сегодня не забить", text + ": защит уже " + num11 + " из " + num12 + " " + text5 + ".", "Защиты обнуляются в 00:00 МСК — бот не будет ждать впустую. Выберите другую цель.", now);
			}
		}
		if (fk.Length > 0 && !asapVerdict.Neutral)
		{
			int num13 = num6 - capts.Count((AsapCapt c) => !c.Gone && K(c.A) == fk && !c.Neutral && c.Start >= ds && c.Start < de);
			string text6 = "";
			if (flag4 && flag3)
			{
				num13 = limits.AtkLeft;
				text6 = " (из игры)";
			}
			else if (flag4 && warFamily3 != null && warFamily3.AtkMax > 0)
			{
				num13 = Math.Min(num13, warFamily3.AtkLeft);
			}
			asapVerdict.AtkLeft = num13;
			if (num13 <= 0)
			{
				return Never(asapVerdict, "Сегодня не забить", "У вашей семьи атак не осталось" + text6 + " " + text5 + ".", "Атаки обнуляются в 00:00 МСК — бот не будет ждать впустую.", now);
			}
		}
		if (dateTime2.Date > dateTime.Date || dateTime2.TimeOfDay > DayLast)
		{
			return Never(asapVerdict, "Сегодня не забить", ((asapVerdict.Main != null) ? (asapVerdict.Main.Text + ", и капт") : "Капт") + " вышел бы на " + dateTime2.ToString("HH:mm") + ", а капты ставят с 15:00 до 21:59 МСК.", "Сегодня уже не успеть — бот не будет ждать впустую.", now);
		}
		AsapCapt mc = asapVerdict.MainCapt;
		if (asapVerdict.Waiting.Count > 0)
		{
			asapVerdict.Level = 1;
			asapVerdict.Exact = false;
		}
		else if (num8 > now)
		{
			asapVerdict.Level = 1;
			asapVerdict.Exact = true;
		}
		else
		{
			asapVerdict.Level = 0;
		}
		asapVerdict.ScreenWatch = mc != null && (!netEvents || (!flag && !mc.Own));
		long num14 = num8 - 75;
		string text7 = ((num14 > now) ? ("В " + War.Hms(num14) + " бот") : "Бот сразу");
		if (asapVerdict.Level == 0)
		{
			asapVerdict.Headline = "Можно бить сейчас";
			asapVerdict.Detail = OwnerText(asapVerdict) + " Ничего не мешает.";
			asapVerdict.Plan = "Бот сразу откроет планшет и поставит атаку: капт на " + dateTime2.ToString("HH:mm") + ".";
		}
		else if (!asapVerdict.Exact)
		{
			asapVerdict.Headline = "Ждём конца капта";
			AsapBlock asapBlock = asapVerdict.Blocks.Where((AsapBlock b) => b.Capt == mc).FirstOrDefault();
			asapVerdict.Detail = ((asapBlock != null) ? asapBlock.Text : ("Капт на " + mc.Square)) + ". " + (mc.Live ? "Когда кончится — неизвестно." : ((mc.EndLo == mc.EndHi) ? ("Кончится в " + War.Hm(mc.EndLo) + ".") : ("Кончится не раньше " + War.Hm(mc.EndLo) + ", не позже " + War.Hm(mc.EndHi) + "."))) + ((asapVerdict.Waiting.Count > 1) ? (" Ждём ещё капт" + ((asapVerdict.Waiting.Count > 2) ? "ы" : "") + " на " + Squares(asapVerdict.Waiting.Where((AsapCapt c) => c != mc)) + ".") : "");
			asapVerdict.Plan = (asapVerdict.ScreenWatch ? (text7 + " откроет карту в планшете и будет смотреть на " + mc.Square + ": как только капт кончится, откроет форму и забьёт — капт на конец + " + afterMin + " мин.") : ("Планшет свободен. " + text7 + " откроет форму атаки, заполнит её и нажмёт сразу после конца — капт на конец + " + afterMin + " мин."));
			long num15 = LatestPress(now, afterMin);
			if (num15 > 0 && mc.EndHi > num15)
			{
				asapVerdict.Plan = asapVerdict.Plan + " Если капт затянется после " + War.Hm(num15) + ", сегодня уже не успеть.";
			}
		}
		else
		{
			bool flag5 = asapVerdict.Main != null && asapVerdict.Main.Text.StartsWith("Капты ставят");
			asapVerdict.Headline = (flag5 ? "Ждём окна каптов" : "Ждём конца КД");
			asapVerdict.Detail = ((asapVerdict.Main != null) ? asapVerdict.Main.Text : ("Цель освободится в " + War.Hms(num8))) + ".";
			asapVerdict.Plan = "Забив ко времени: " + ((num14 > now) ? ("в " + War.Hms(num14) + " бот откроет форму") : "бот сразу откроет форму") + " и нажмёт в " + War.Hms(num8) + ", капт на " + dateTime2.ToString("HH:mm") + ".";
		}
		return asapVerdict;
	}

	private static long LatestPress(long now, int afterMin)
	{
		DateTime date = Clock.Msk(War.FromUnix(now)).Date;
		long num = MskUnix(date.Add(DayLast)) - afterMin * 60;
		int num2 = 0;
		while (num2 < 16 && num > now)
		{
			DateTime dateTime = CaptAt(War.FromUnix(num), War.FromUnix(num), afterMin, 10.0);
			if (dateTime.Date == date && dateTime.TimeOfDay <= DayLast)
			{
				return num;
			}
			num2++;
			num -= 60;
		}
		return 0L;
	}

	private static AsapVerdict Never(AsapVerdict v, string head, string detail, string plan, long now)
	{
		v.Level = 2;
		v.Headline = head;
		v.Detail = detail;
		v.Plan = plan;
		v.PressAt = now;
		return v;
	}

	private static string OwnerText(AsapVerdict v)
	{
		if (v.Neutral)
		{
			return "Зона ничья — атаку не тратит.";
		}
		if (v.Owner == null)
		{
			return "Владелец цели неизвестен.";
		}
		return "Цель — зона " + v.Owner + ((v.DefMax > 0) ? (", защит " + v.DefUsed + " из " + v.DefMax) : "") + ".";
	}

	private static string NetWhy()
	{
		if (!NetWatch.Running)
		{
			if (!NetWatch.IsAdmin)
			{
				return " (нужны права администратора)";
			}
			if (NetWatch.Error == null)
			{
				return "";
			}
			return " (" + NetWatch.Error + ")";
		}
		if (!NetWatch.GameRunning)
		{
			return " (игра не запущена)";
		}
		return " (трафик игры не виден)";
	}

	private static string SourceText(WarState st, bool events, bool full)
	{
		if (events)
		{
			return "Данные: своя игра" + (full ? "" : " (всей карты каптов из игры нет — её присылают при первом открытии карты в планшете)") + ((st != null) ? " и карта TTC." : ".");
		}
		if (st == null)
		{
			return "Своя игра не читается" + NetWhy() + ", сервер не выбран.";
		}
		return "Данные: карта TTC. Своя игра не читается" + NetWhy() + " — у самого конца бот смотрит отметку капта на экране.";
	}
}
