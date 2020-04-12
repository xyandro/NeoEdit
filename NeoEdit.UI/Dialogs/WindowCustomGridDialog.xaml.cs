using System;
using System.Windows;
using NeoEdit.Common;
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
		public bool ActiveFirst { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<bool>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ActiveOnly { get { return UIHelper<WindowCustomGridDialog>.GetPropValue<bool>(this); } set { UIHelper<WindowCustomGridDialog>.SetPropValue(this, value); } }

		static WindowCustomGridDialog()
		{
			UIHelper<WindowCustomGridDialog>.Register();
			UIHelper<WindowCustomGridDialog>.AddCallback(x => x.ActiveOnly, (obj, o, n) => { if (obj.ActiveOnly) obj.ActiveFirst = true; });
			UIHelper<WindowCustomGridDialog>.AddCallback(x => x.ActiveFirst, (obj, o, n) => { if (!obj.ActiveFirst) obj.ActiveOnly = false; });
		}

		WindowCustomGridDialog(WindowLayout windowLayout)
		{
			InitializeComponent();
			Columns = windowLayout.Columns;
			Rows = windowLayout.Rows;
			MaxColumns = windowLayout.MaxColumns;
			MaxRows = windowLayout.MaxRows;
			ActiveFirst = windowLayout.ActiveFirst;
			ActiveOnly = windowLayout.ActiveOnly;
		}

		WindowLayout result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if ((Columns < 1) || (Rows < 1) || (MaxColumns < 1) || (MaxRows < 1))
				return;
			result = new WindowLayout(Columns, Rows, MaxColumns, MaxRows, ActiveFirst, ActiveOnly);
			DialogResult = true;
		}

		public static WindowLayout Run(Window parent, WindowLayout windowLayout)
		{
			var dialog = new WindowCustomGridDialog(windowLayout) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
