namespace CaptBot;

public class CalibEntry
{
	public int W { get; set; }

	public int H { get; set; }

	public string Model { get; set; }

	public double Sx { get; set; }

	public double Sy { get; set; }

	public double Ox { get; set; }

	public double Oy { get; set; }

	public bool Verified { get; set; }

	public bool Refined { get; set; }

	public bool FormGeo { get; set; }

	public double MapP { get; set; }

	public double MapLeft { get; set; }

	public double MapTop { get; set; }

	public double MapBottom { get; set; }

	public bool MapLabelTop { get; set; }

	public bool MapLabelBottom { get; set; }

	public bool MapNoClamp { get; set; }

	public bool PingOk { get; set; }

	public int PingX { get; set; }

	public int PingY { get; set; }

	public double PingScale { get; set; }

	public string When { get; set; }

	public string Key => W + "x" + H;

	public Layout ToLayout()
	{
		return new Layout(Sx, Sy, Ox, Oy, Model ?? "калибровка");
	}

	public void SetLayout(Layout l)
	{
		Sx = l.Sx;
		Sy = l.Sy;
		Ox = l.Ox;
		Oy = l.Oy;
		Model = l.Model;
	}

	public CalibEntry Copy()
	{
		return (CalibEntry)MemberwiseClone();
	}
}
