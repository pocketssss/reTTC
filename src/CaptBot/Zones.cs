using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace CaptBot;

internal static class Zones
{
	public const int Cols = 58;

	public const int Rows = 84;

	public const double Cell = 140.0;

	public const double GX0 = -3570.0;

	public const double GY0 = 8070.0;

	public static readonly List<Zone> All;

	private static readonly Dictionary<int, Zone> byId;

	private static readonly Zone[,] cells;

	static Zones()
	{
		All = new List<Zone>();
		byId = new Dictionary<int, Zone>();
		cells = new Zone[60, 86];
		try
		{
			string input;
			using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("zones.json"))
			{
				using StreamReader streamReader = new StreamReader(stream, Encoding.UTF8);
				input = streamReader.ReadToEnd();
			}
			Dictionary<string, object> dictionary = new JavaScriptSerializer
			{
				MaxJsonLength = 8388608
			}.DeserializeObject(input) as Dictionary<string, object>;
			foreach (object item in (IEnumerable)dictionary["zones"])
			{
				if (!(item is object[] array) || array.Length < 4)
				{
					continue;
				}
				Zone zone = new Zone
				{
					Id = Convert.ToInt32(array[0]),
					Label = Convert.ToString(array[1]),
					Big = (Convert.ToInt32(array[2]) != 0),
					Sub = Convert.ToInt32(array[3])
				};
				if (!Runner.ParseSquare(zone.Label, out zone.Col, out zone.Row))
				{
					continue;
				}
				All.Add(zone);
				byId[zone.Id] = zone;
				for (int i = zone.C0; i <= zone.C1; i++)
				{
					for (int j = zone.R0; j <= zone.R1; j++)
					{
						if (i >= 1 && i <= 58 && j >= 1 && j <= 84)
						{
							cells[i, j] = zone;
						}
					}
				}
			}
		}
		catch
		{
		}
	}

	public static Zone ById(int id)
	{
		if (!byId.TryGetValue(id, out var value))
		{
			return null;
		}
		return value;
	}

	public static Zone At(int col, int row)
	{
		if (col < 1 || col > 58 || row < 1 || row > 84)
		{
			return null;
		}
		return cells[col, row];
	}

	public static Zone ByCode(string code)
	{
		if (!Runner.ParseSquare(code, out var col, out var row))
		{
			return null;
		}
		return At(col, row);
	}

	public static HashSet<Zone> Neighbors(Zone z)
	{
		HashSet<Zone> hashSet = new HashSet<Zone>();
		for (int i = z.C0; i <= z.C1; i++)
		{
			Add(hashSet, z, i, z.R0 - 1);
			Add(hashSet, z, i, z.R1 + 1);
		}
		for (int j = z.R0; j <= z.R1; j++)
		{
			Add(hashSet, z, z.C0 - 1, j);
			Add(hashSet, z, z.C1 + 1, j);
		}
		return hashSet;
	}

	private static void Add(HashSet<Zone> res, Zone self, int c, int r)
	{
		Zone zone = At(c, r);
		if (zone != null && zone != self)
		{
			res.Add(zone);
		}
	}

	public static string Key(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return "";
		}
		StringBuilder stringBuilder = new StringBuilder();
		string text = name.ToLowerInvariant();
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(c);
			}
		}
		return stringBuilder.ToString();
	}
}
