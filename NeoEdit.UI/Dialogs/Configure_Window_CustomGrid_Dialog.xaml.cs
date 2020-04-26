using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Window_CustomGrid_Dialog
	{
		[DepProp]
		public int? Columns { get { return UIHelper<Configure_Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<Configure_Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxColumns { get { return UIHelper<Configure_Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxRows { get { return UIHelper<Configure_Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Configure_Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ActiveOnly { get { return UIHelper<Configure_Window_CustomGrid_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Window_CustomGrid_Dialog>.SetPropValue(this, value); } }

		static Configure_Window_CustomGrid_Dialog() => UIHelper<Configure_Window_CustomGrid_Dialog>.Register();

		Configure_Window_CustomGrid_Dialog(WindowLayout windowLayout)
		{
			InitializeComponent();
			Columns = windowLayout.Columns;
			Rows = windowLayout.Rows;
			MaxColumns = windowLayout.MaxColumns;
			MaxRows = windowLayout.MaxRows;
			ActiveOnly = windowLayout.ActiveOnly;
		}

		void OnReset(object sender, RoutedEventArgs e)
		{
			ActiveOnly = false;
		}

		Configuration_Window_CustomGrid result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((Columns < 1) || (Rows < 1) || (MaxColumns < 1) || (MaxRows < 1))
				return;
			result = new Configuration_Window_CustomGrid { WindowLayout = new WindowLayout(Columns, Rows, MaxColumns, MaxRows, ActiveOnly) };
			DialogResult = true;
		}

		public static Configuration_Window_CustomGrid Run(Window parent, WindowLayout windowLayout)
		{
			var dialog = new Configure_Window_CustomGrid_Dialog(windowLayout) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
