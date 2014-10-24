using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk
{
	class DiskMenuItem : NEMenuItem<DiskCommand> { }

	class ColumnMenuItem : MenuItem
	{
		public readonly DependencyProperty Property;
		public ColumnMenuItem(DependencyProperty property)
		{
			Property = property;
			Header = property.Name;
		}
	}

	partial class DiskMenu
	{
		[DepProp]
		public new DiskTabs Parent { get { return UIHelper<DiskMenu>.GetPropValue<DiskTabs>(this); } set { UIHelper<DiskMenu>.SetPropValue(this, value); } }
		[DepProp]
		List<ColumnMenuItem> Columns { get { return UIHelper<DiskMenu>.GetPropValue<List<ColumnMenuItem>>(this); } set { UIHelper<DiskMenu>.SetPropValue(this, value); } }

		static DiskMenu() { UIHelper<DiskMenu>.Register(); }

		public DiskMenu()
		{
			InitializeComponent();
			Columns = UIHelper<DiskItem>.GetProperties().Select(prop => new ColumnMenuItem(prop)).ToList();
		}

		private void ToggleColumn(object sender, RoutedEventArgs e)
		{
			Parent.ToggleColumn((e.OriginalSource as ColumnMenuItem).Property);
		}
	}
}
