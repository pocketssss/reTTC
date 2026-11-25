using System.Collections.Generic;
using System.Globalization;

namespace CaptBot;

internal class FieldCycle
{
	private readonly bool hours;

	private readonly int n;

	private readonly List<Gray> seen = new List<Gray>();

	private int from = -1;

	private bool jumped;

	public bool Closed => from >= 0;

	public List<Gray> Crops => seen.GetRange(from, n);

	public FieldCycle(bool hours)
	{
		this.hours = hours;
		n = Ocr.Count(hours);
	}

	public void Add(Gray cur)
	{
		if (Closed)
		{
			return;
		}
		int num = seen.FindLastIndex((Gray s) => Ocr.FieldDiff(s, cur) < 0.5);
		if (num >= 0 && seen.Count - num == n)
		{
			from = num;
			return;
		}
		if (num >= 0)
		{
			seen.Clear();
			jumped = true;
		}
		seen.Add(cur);
	}

	public int Solve(out string how, out string diag)
	{
		List<Gray> crops = Crops;
		int votes = 0;
		int rival = 0;
		double best;
		double second;
		int num = Ocr.FitCycle(crops, hours, out best, out second);
		int num2 = ((hours || from <= 0 || jumped) ? (-1) : 0);
		int num3 = -1;
		if (num < 0 || (num2 >= 0 && num2 != num))
		{
			num3 = Ocr.TextCycle(crops, hours, out votes, out rival);
		}
		int result = ((num >= 0) ? num : ((num3 >= 0) ? num3 : num2));
		how = ((num >= 0) ? "по образцам" : ((num3 >= 0) ? "распознаванием текста" : "по переходу через 0"));
		if (num >= 0 && num3 >= 0 && num != num3 && num2 == num3)
		{
			result = num3;
			how = "распознаванием текста";
		}
		diag = string.Format(CultureInfo.InvariantCulture, "образцы: {0} (оценка {1:0.00}, следующая {2:0.00}), текст: {3} ({4} против {5}), переход через 0: {6}", Show(num), best, second, Show(num3), votes, rival, (num2 < 0) ? "—" : "да");
		return result;
	}

	private string Show(int idx)
	{
		if (idx >= 0)
		{
			return Ocr.Value(hours, idx).ToString("00");
		}
		return "—";
	}
}
