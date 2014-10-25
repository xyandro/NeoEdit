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
			Header = "_" + property.Name;
		}
	}

	partial class DiskMenu
	{
		[DepProp]
		public new DiskTabs Parent { get { return UIHelper<DiskMenu>.GetPropValue<DiskTabs>(this); } set { UIHelper<DiskMenu>.SetPropValue(this, value); } }

		static DiskMenu() { UIHelper<DiskMenu>.Register(); }

		public DiskMenu()
		{
			InitializeComponent();
		}

		void ToggleColumn(object sender, RoutedEventArgs e)
		{
			Parent.ToggleColumn((e.OriginalSource as ColumnMenuItem).Property);
		}

		void SetSort(object sender, RoutedEventArgs e)
		{
			if (e.OriginalSource is ColumnMenuItem)
				Parent.SetSort((e.OriginalSource as ColumnMenuItem).Property);
		}
	}
}
