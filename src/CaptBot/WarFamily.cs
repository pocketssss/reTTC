using System;

namespace CaptBot;

internal class WarFamily
{
	public int Index;

	public int Zones;

	public int AtkUsed;

	public int AtkMax;

	public int DefUsed;

	public int DefMax;

	public long Id;

	public string Name;

	public string Tag;

	public string Color;

	public string Key;

	public string Logo;

	public bool Mansion;

	public bool MansionSoon;

	public long[] AtkCd;

	public long[] DefCd;

	public long[] InCaptEnd;

	public string InCaptRole;

	public int AtkLeft => Math.Max(0, AtkMax - AtkUsed);

	public int DefLeft => Math.Max(0, DefMax - DefUsed);

	public string Title => Name + (string.IsNullOrEmpty(Tag) ? "" : (" [" + Tag + "]"));
}
