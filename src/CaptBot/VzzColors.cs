using System;

namespace CaptBot;

internal static class VzzColors
{
	public const string Random = "Случайный";

	public static readonly string[][] All = new string[103][]
	{
		new string[2] { "Красный", "#E53935" },
		new string[2] { "Алый", "#FF2400" },
		new string[2] { "Карминовый", "#960018" },
		new string[2] { "Бордовый", "#800020" },
		new string[2] { "Вишневый", "#911E42" },
		new string[2] { "Рубиновый", "#9B111E" },
		new string[2] { "Гранатовый", "#7B1E33" },
		new string[2] { "Винный", "#722F37" },
		new string[2] { "Малиновый", "#DC143C" },
		new string[2] { "Клюквенный", "#9F000F" },
		new string[2] { "Томатный", "#FF6347" },
		new string[2] { "Коралловый", "#FF7F50" },
		new string[2] { "Лососевый", "#FA8072" },
		new string[2] { "Розовый", "#FFC0CB" },
		new string[2] { "Ярко-розовый", "#FF69B4" },
		new string[2] { "Пыльно-розовый", "#D8A9A9" },
		new string[2] { "Фуксия", "#FF00FF" },
		new string[2] { "Пурпурный", "#800080" },
		new string[2] { "Маджента", "#CA1F7B" },
		new string[2] { "Сливовый", "#8E4585" },
		new string[2] { "Баклажановый", "#4B2A3B" },
		new string[2] { "Фиолетовый", "#8B00FF" },
		new string[2] { "Аметистовый", "#9966CC" },
		new string[2] { "Сиреневый", "#C8A2C8" },
		new string[2] { "Лиловый", "#B666D2" },
		new string[2] { "Лавандовый", "#B57EDC" },
		new string[2] { "Орхидея", "#DA70D6" },
		new string[2] { "Индиго", "#4B0082" },
		new string[2] { "Синий", "#1E4FD8" },
		new string[2] { "Темно-синий", "#0B1F5C" },
		new string[2] { "Ультрамарин", "#120A8F" },
		new string[2] { "Кобальтовый", "#0047AB" },
		new string[2] { "Сапфировый", "#0F52BA" },
		new string[2] { "Васильковый", "#6495ED" },
		new string[2] { "Королевский", "#4169E1" },
		new string[2] { "Джинсовый", "#1560BD" },
		new string[2] { "Голубой", "#42AAFF" },
		new string[2] { "Небесный", "#7FC7FF" },
		new string[2] { "Лазурный", "#007FFF" },
		new string[2] { "Бирюзовый", "#30D5C8" },
		new string[2] { "Аквамариновый", "#7FFFD4" },
		new string[2] { "Циановый", "#00B7EB" },
		new string[2] { "Петроль", "#005F6A" },
		new string[2] { "Изумрудный", "#50C878" },
		new string[2] { "Зеленый", "#2E9E44" },
		new string[2] { "Темно-зеленый", "#013220" },
		new string[2] { "Травяной", "#5DA130" },
		new string[2] { "Салатовый", "#99FF99" },
		new string[2] { "Лаймовый", "#BFFF00" },
		new string[2] { "Мятный", "#3EB489" },
		new string[2] { "Оливковый", "#808000" },
		new string[2] { "Хаки", "#C3B091" },
		new string[2] { "Болотный", "#6B6B2A" },
		new string[2] { "Нефритовый", "#00A86B" },
		new string[2] { "Малахитовый", "#0BDA51" },
		new string[2] { "Фисташковый", "#93C572" },
		new string[2] { "Хвойный", "#2F4F2F" },
		new string[2] { "Желтый", "#FFD400" },
		new string[2] { "Лимонный", "#FFF44F" },
		new string[2] { "Золотой", "#D4AF37" },
		new string[2] { "Горчичный", "#E1AD01" },
		new string[2] { "Янтарный", "#FFBF00" },
		new string[2] { "Шафрановый", "#F4C430" },
		new string[2] { "Медовый", "#EB9605" },
		new string[2] { "Песочный", "#C2B280" },
		new string[2] { "Бежевый", "#F5F5DC" },
		new string[2] { "Кремовый", "#FFFDD0" },
		new string[2] { "Ванильный", "#F3E5AB" },
		new string[2] { "Персиковый", "#FFE5B4" },
		new string[2] { "Абрикосовый", "#FBCEB1" },
		new string[2] { "Оранжевый", "#FF8C00" },
		new string[2] { "Морковный", "#ED9121" },
		new string[2] { "Мандариновый", "#F28500" },
		new string[2] { "Тыквенный", "#FF7518" },
		new string[2] { "Терракотовый", "#E2725B" },
		new string[2] { "Кирпичный", "#B22222" },
		new string[2] { "Ржавый", "#B7410E" },
		new string[2] { "Медный", "#B87333" },
		new string[2] { "Бронзовый", "#CD7F32" },
		new string[2] { "Коричневый", "#7B4A12" },
		new string[2] { "Шоколадный", "#5C3317" },
		new string[2] { "Кофейный", "#6F4E37" },
		new string[2] { "Каштановый", "#954535" },
		new string[2] { "Ореховый", "#7B5141" },
		new string[2] { "Карамельный", "#C68E17" },
		new string[2] { "Охристый", "#CC7722" },
		new string[2] { "Сепия", "#704214" },
		new string[2] { "Табачный", "#71543B" },
		new string[2] { "Белый", "#FFFFFF" },
		new string[2] { "Молочный", "#FDFFF5" },
		new string[2] { "Жемчужный", "#EAE0C8" },
		new string[2] { "Серебряный", "#C0C0C0" },
		new string[2] { "Платиновый", "#E5E4E2" },
		new string[2] { "Серый", "#808080" },
		new string[2] { "Светло-серый", "#C8C8C8" },
		new string[2] { "Темно-серый", "#4A4A4A" },
		new string[2] { "Пепельный", "#B2BEB5" },
		new string[2] { "Дымчатый", "#738276" },
		new string[2] { "Стальной", "#71797E" },
		new string[2] { "Графитовый", "#383838" },
		new string[2] { "Антрацитовый", "#293133" },
		new string[2] { "Угольный", "#1F1F1F" },
		new string[2] { "Черный", "#000000" }
	};

	private static readonly Random rnd = new Random();

	public static string Hex(string name)
	{
		string[][] all = All;
		foreach (string[] array in all)
		{
			if (string.Equals(array[0], name, StringComparison.OrdinalIgnoreCase))
			{
				return array[1];
			}
		}
		return null;
	}

	public static string Canon(string name)
	{
		if (string.Equals(name, "Случайный", StringComparison.OrdinalIgnoreCase))
		{
			return "Случайный";
		}
		string[][] all = All;
		foreach (string[] array in all)
		{
			if (string.Equals(array[0], name, StringComparison.OrdinalIgnoreCase))
			{
				return array[0];
			}
		}
		return null;
	}

	public static string Pick(string not)
	{
		lock (rnd)
		{
			string text;
			do
			{
				text = All[rnd.Next(All.Length)][0];
			}
			while (!(text != not));
			return text;
		}
	}
}
