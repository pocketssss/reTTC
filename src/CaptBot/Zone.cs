namespace CaptBot;

internal class Zone
{
	public int Id;

	public int Sub;

	public int Col;

	public int Row;

	public bool Big;

	public string Label;

	public bool Mansion => Sub == 1;

	public int C0 => Col;

	public int C1
	{
		get
		{
			if (!Big)
			{
				return Col;
			}
			return Col + 1;
		}
	}

	public int R0
	{
		get
		{
			if (!Big)
			{
				return Row;
			}
			return Row - 1;
		}
	}

	public int R1 => Row;

	public double X0 => -3570.0 + 140.0 * (double)(C0 - 1);

	public double X1 => -3570.0 + 140.0 * (double)C1;

	public double YTop => 8070.0 - 140.0 * (double)(R0 - 1);

	public double YBottom => 8070.0 - 140.0 * (double)R1;

	public override string ToString()
	{
		return Label;
	}
}
