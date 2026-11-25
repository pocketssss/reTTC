using System;
using System.Windows;
using System.Windows.Controls;

namespace CaptBot;

internal class ElasticBox : Decorator
{
	public double Shown = 240.0;

	private double measuredH = double.NaN;

	private double arrangedH = double.NaN;

	private bool arranging;

	protected override Size MeasureOverride(Size c)
	{
		UIElement child = Child;
		if (child == null)
		{
			return new Size(0.0, 0.0);
		}
		bool flag = double.IsInfinity(c.Height);
		child.Measure(new Size(height: measuredH = ((!flag) ? c.Height : ((!double.IsNaN(arrangedH)) ? arrangedH : Shown)), width: c.Width));
		return new Size(child.DesiredSize.Width, flag ? Math.Min(child.DesiredSize.Height, Shown) : child.DesiredSize.Height);
	}

	protected override Size ArrangeOverride(Size f)
	{
		UIElement child = Child;
		if (child == null)
		{
			return f;
		}
		if (double.IsNaN(measuredH) || Math.Abs(f.Height - measuredH) > 0.5)
		{
			arranging = true;
			try
			{
				child.Measure(f);
				measuredH = f.Height;
			}
			finally
			{
				arranging = false;
			}
		}
		arrangedH = f.Height;
		child.Arrange(new Rect(f));
		return f;
	}

	protected override void OnChildDesiredSizeChanged(UIElement child)
	{
		if (!arranging)
		{
			base.OnChildDesiredSizeChanged(child);
		}
	}
}
