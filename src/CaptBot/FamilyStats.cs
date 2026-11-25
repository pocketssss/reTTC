using System.Collections.Generic;

namespace CaptBot;

internal class FamilyStats
{
	public class Rival
	{
		public string Name;

		public int N;

		public int W;

		public int Lo;

		public int Atk;

		public int Def;
	}

	public class Item
	{
		public long Start;

		public bool Attack;

		public string Vs;

		public string Square;

		public bool? Win;
	}

	public string Family;

	public int Zones;

	public long HistoryFrom;

	public List<string> Mansions = new List<string>();

	public Dictionary<string, int[]> Periods = new Dictionary<string, int[]>();

	public List<Rival> Rivals = new List<Rival>();

	public List<int[]> Hours = new List<int[]>();

	public List<Item> Recent = new List<Item>();

	public long[] Ingame;
}
