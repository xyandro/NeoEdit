using System;
using System.Windows.Controls;

namespace NeoEdit.GUI.BrowserUI
{
	public class SizingGridView : GridView
	{
		protected override void PrepareItem(ListViewItem item)
		{
			foreach (var column in Columns)
			{
				if (Double.IsNaN(column.Width))
					column.Width = column.ActualWidth;
				column.Width = Double.NaN;
			}
			base.PrepareItem(item);
		}
	}
}
