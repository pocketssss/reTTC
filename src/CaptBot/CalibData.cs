using System.Collections.Generic;

namespace CaptBot;

public class CalibData
{
	public bool HasBase { get; set; }

	public double BaseX { get; set; }

	public double BaseY { get; set; }

	public double BaseW { get; set; }

	public double BaseH { get; set; }

	public List<CalibEntry> Items { get; set; }

	public CalibData()
	{
		Items = new List<CalibEntry>();
	}
}
