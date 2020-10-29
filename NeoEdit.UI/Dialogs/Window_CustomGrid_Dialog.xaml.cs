using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Window_CustomGrid_Dialog
	{
		[DepProp]
		public int? Columns { get { return UIHelper<Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxColumns { get { return UIHelper<Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Window_CustomGrid_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxRows { get { return UIHelper<Window_CustomGrid_Dialog>.GetPropValue<int?>(this); } set { UIHelper<Window_CustomGrid_Dialog>.SetPropValue(this, value); } }

		static Window_CustomGrid_Dialog() => UIHelper<Window_CustomGrid_Dialog>.Register();

		readonly WindowLayout windowLayout;

		Window_CustomGrid_Dialog(WindowLayout windowLayout)
		{
			InitializeComponent();
			this.windowLayout = windowLayout;
			OnReset();
		}

		void OnReset(object sender = null, RoutedEventArgs e = null)
		{
			Columns = windowLayout.Columns;
			Rows = windowLayout.Rows;
			MaxColumns = windowLayout.MaxColumns;
			MaxRows = windowLayout.MaxRows;
		}

		Configuration_Window_CustomGrid result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((Columns < 1) || (Rows < 1) || (MaxColumns < 1) || (MaxRows < 1))
				return;
			result = new Configuration_Window_CustomGrid { WindowLayout = new WindowLayout(Columns, Rows, MaxColumns, MaxRows) };
			DialogResult = true;
		}

		public static Configuration_Window_CustomGrid Run(Window parent, WindowLayout windowLayout)
		{
			var dialog = new Window_CustomGrid_Dialog(windowLayout) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
