using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CaptBot;

internal class BodyScroller : ScrollViewer
{
	public readonly HashSet<DependencyObject> WheelOwners = new HashSet<DependencyObject>();

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		DependencyObject dependencyObject = e.OriginalSource as DependencyObject;
		while (dependencyObject != null && dependencyObject != this)
		{
			if (WheelOwners.Contains(dependencyObject))
			{
				return;
			}
			dependencyObject = Up(dependencyObject);
		}
		base.OnMouseWheel(e);
	}

	private static DependencyObject Up(DependencyObject d)
	{
		if (!(d is Visual))
		{
			return LogicalTreeHelper.GetParent(d);
		}
		return VisualTreeHelper.GetParent(d);
	}
}
