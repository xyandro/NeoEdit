using System;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class WindowCustomGridDialog
	{
		[DepProp]
		public int? Columns { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? Rows { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxColumns { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int? MaxRows { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<int?>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ActiveOnly { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<bool>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }

		static WindowCustomGridDialog() => UIHelper<WindowCustomGridDialog>.Register();

		WindowCustomGridDialog(WindowLayout windowLayout)
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
			var dialog = new WindowCustomGridDialog(windowLayout) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
