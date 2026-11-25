using System;
using System.Collections.Generic;

namespace CaptBot;

internal class PatrolScan
{
	public DateTime At;

	public int R0;

	public int R1;

	public int C0;

	public int C1;

	public List<int[]> Marks = new List<int[]>();

	public int[] Limits;

	public bool MayEnd;

	public double P;
}
