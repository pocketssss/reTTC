using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptBot;

internal class AsapVerdict
{
	public int Level;

	public string Headline = "";

	public string Detail = "";

	public string Plan = "";

	public string Src = "";

	public string Owner;

	public bool OwnerKnown;

	public bool Neutral;

	public int DefUsed = -1;

	public int DefMax = -1;

	public int AtkLeft = -1;

	public List<AsapCapt> Capts = new List<AsapCapt>();

	public List<AsapCapt> Waiting = new List<AsapCapt>();

	public List<AsapBlock> Blocks = new List<AsapBlock>();

	public AsapBlock Main;

	public long PressAt;

	public bool Exact;

	public long CaptFrom;

	public DateTime CaptMsk;

	public bool ScreenWatch;

	public bool Events;

	public bool Full;

	public bool Server;

	public AsapCapt MainCapt
	{
		get
		{
			if (Main == null || Main.Capt == null)
			{
				return Waiting.OrderByDescending((AsapCapt c) => c.EndLo).FirstOrDefault();
			}
			return Main.Capt;
		}
	}
}
