using System.Collections.Generic;

namespace CaptBot;

internal static class ZoneTypes
{
	public const int AroundMin = 7;

	private static readonly Dictionary<int, ZoneType> T;

	private static void A(int sub, string name, int inf, bool around, bool business, int limit, bool defPlus, string needs)
	{
		T[sub] = new ZoneType
		{
			Name = name,
			Influence = inf,
			Around = around,
			Business = business,
			Limit = limit,
			DefPlus = defPlus,
			Needs = needs
		};
	}

	static ZoneTypes()
	{
		T = new Dictionary<int, ZoneType>();
		A(1, "Особняк", 0, around: true, business: false, 1, defPlus: true, null);
		A(4, "Казино", 21, around: true, business: false, 0, defPlus: false, null);
		A(5, "EMS", 15, around: true, business: false, 1, defPlus: false, null);
		A(6, "Government", 30, around: true, business: false, 0, defPlus: false, null);
		A(7, "Федеральная тюрьма", 26, around: true, business: false, 0, defPlus: false, null);
		A(8, "FIB", 31, around: true, business: false, 0, defPlus: false, null);
		A(9, "Sheriff Department", 28, around: true, business: false, 1, defPlus: false, null);
		A(10, "LSPD", 28, around: true, business: false, 1, defPlus: false, null);
		A(11, "Спутник", 0, around: true, business: false, 1, defPlus: false, null);
		A(15, "Точка скупки Green", 25, around: true, business: false, 0, defPlus: false, null);
		A(16, "X.X.V.I", 0, around: true, business: false, 1, defPlus: false, "своё «Помещение»");
		A(17, "Лаборатория", 24, around: true, business: false, 1, defPlus: false, null);
		A(18, "Нелегальная строительная компания", 14, around: true, business: false, 1, defPlus: false, null);
		A(24, "National Guard", 33, around: true, business: false, 1, defPlus: false, null);
		A(25, "Weazel News", 13, around: true, business: false, 0, defPlus: false, null);
		A(26, "Свалка транспорта", 12, around: true, business: false, 1, defPlus: false, null);
		A(100, "Магазин 24/7", 3, around: false, business: true, 0, defPlus: false, null);
		A(101, "Оружейный магазин", 6, around: false, business: true, 0, defPlus: false, null);
		A(102, "Барбершоп", 3, around: false, business: true, 0, defPlus: false, null);
		A(103, "Магазин одежды", 3, around: false, business: true, 0, defPlus: false, null);
		A(104, "LSC", 8, around: false, business: true, 0, defPlus: false, null);
		A(105, "АЗС", 5, around: false, business: true, 0, defPlus: false, null);
		A(106, "Татусалон", 4, around: false, business: true, 0, defPlus: false, null);
		A(107, "Автосалон", 8, around: false, business: true, 0, defPlus: false, null);
		A(108, "Автомойка", 4, around: false, business: true, 0, defPlus: false, null);
		A(109, "Корабль", 0, around: false, business: false, 0, defPlus: false, null);
		A(209, "Предприятие", 15, around: true, business: false, 2, defPlus: false, "свой бизнес");
		A(300, "Карьер", 21, around: true, business: false, 0, defPlus: false, null);
		A(301, "Точка сбора грибов", 16, around: true, business: false, 0, defPlus: false, null);
		A(302, "Точка ловли рыбы", 17, around: true, business: false, 0, defPlus: false, null);
		A(303, "Точка рубки леса", 18, around: true, business: false, 0, defPlus: false, null);
		A(304, "Ферма", 21, around: true, business: false, 0, defPlus: false, null);
		A(400, "Крафт из оружейных материалов", 0, around: false, business: false, 1, defPlus: false, null);
		A(401, "Крафт из технических материалов", 0, around: false, business: false, 1, defPlus: false, null);
		A(402, "Крафт из медицинских материалов", 0, around: false, business: false, 1, defPlus: false, null);
		A(702, "Аэропорт", 0, around: true, business: false, 1, defPlus: false, null);
		A(703, "Истребитель", 0, around: true, business: false, 1, defPlus: false, null);
		A(800, "Помещение свободного назначения", 3, around: true, business: false, 1, defPlus: true, null);
	}

	public static ZoneType Of(int sub)
	{
		if (!T.TryGetValue(sub, out var value))
		{
			return null;
		}
		return value;
	}

	public static string Describe(int sub)
	{
		ZoneType zoneType = Of(sub);
		if (zoneType == null)
		{
			return null;
		}
		string text = zoneType.Name + (zoneType.Business ? " (бизнес)" : "") + ((zoneType.Influence > 0) ? (", влияние " + zoneType.Influence) : "") + ".";
		text += (zoneType.Around ? (" Атаковать — только если вокруг " + 7 + "+ квадратов вашей семьи.") : " Атакуется как обычный квадрат.");
		if (zoneType.Limit > 0)
		{
			object obj = text;
			text = string.Concat(obj, " Держать можно не больше ", zoneType.Limit, ".");
		}
		if (zoneType.Needs != null)
		{
			text = text + " Нужно " + zoneType.Needs + ".";
		}
		if (zoneType.DefPlus)
		{
			text += " Даёт +1 защиту.";
		}
		return text;
	}
}
