using System.Drawing;
using System.Globalization;

namespace CaptBot;

internal class MapGrid
{
	public double Ax;

	public double Ay;

	public double P;

	public int Labels;

	public int Outliers;

	public double Rms;

	public PointF Cell(double c, double r)
	{
		return new PointF((float)(Ax + P * c), (float)(Ay + P * r));
	}

	public double Col(double x)
	{
		return (x - Ax) / P;
	}

	public double Row(double y)
	{
		return (y - Ay) / P;
	}

	public MapGrid Shift(double dx, double dy, double k)
	{
		MapGrid mapGrid = new MapGrid();
		mapGrid.Ax = (Ax + dx) * k;
		mapGrid.Ay = (Ay + dy) * k;
		mapGrid.P = P * k;
		mapGrid.Labels = Labels;
		mapGrid.Outliers = Outliers;
		mapGrid.Rms = Rms * k;
		return mapGrid;
	}

	public override string ToString()
	{
		return string.Format(CultureInfo.InvariantCulture, "шаг {0:0.0} px, подписей {1}{2}", P, Labels, (Outliers > 0) ? (", отброшено " + Outliers) : "");
	}
}
