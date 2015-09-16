using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class EditTablesDialog
	{
		public class Result
		{
			public List<EditTableTab.Result> Results { get; internal set; }
		}

		[DepProp]
		public ObservableCollectionEx<EditTableTab> Tabs { get { return UIHelper<EditTablesDialog>.GetPropValue<ObservableCollectionEx<EditTableTab>>(this); } set { UIHelper<EditTablesDialog>.SetPropValue(this, value); } }
		[DepProp]
		public EditTableTab CurrentTab { get { return UIHelper<EditTablesDialog>.GetPropValue<EditTableTab>(this); } set { UIHelper<EditTablesDialog>.SetPropValue(this, value); } }

		static EditTablesDialog() { UIHelper<EditTablesDialog>.Register(); }

		EditTablesDialog(List<string> tablesStr, List<Table> tables)
		{
			InitializeComponent();
			Tabs = new ObservableCollectionEx<EditTableTab>();
			if (tablesStr != null)
				Tabs.AddRange(tablesStr.Select((table, index) => new EditTableTab(this, String.Format("Table {0}", index + 1), table, null)));
			if (tables != null)
				Tabs.AddRange(tables.Select((table, index) => new EditTableTab(this, String.Format("Table {0}", index + 1), null, table)));
		}

		bool ControlDown { get { return (Keyboard.Modifiers & ModifierKeys.Control) != 0; } }
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Enter: OkClick(null, null); break;
				case Key.PageUp:
				case Key.PageDown:
					if (ControlDown)
					{
						var index = Tabs.IndexOf(CurrentTab) + (e.Key == Key.PageDown ? 1 : -1);
						if (index < 0)
							index = Tabs.Count - 1;
						if (index >= Tabs.Count)
							index = 0;
						CurrentTab = Tabs[index];
					}
					else
						e.Handled = false;
					break;
				case Key.D1:
				case Key.D2:
				case Key.D3:
				case Key.D4:
				case Key.D5:
				case Key.D6:
				case Key.D7:
				case Key.D8:
				case Key.D9:
					{
						var index = e.Key - Key.D1;
						if (index < Tabs.Count)
							CurrentTab = Tabs[index];
					}
					break;
				default: e.Handled = false; break;
			}

			if (!e.Handled)
				base.OnPreviewKeyDown(e);
		}

		void TabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			CurrentTab.SetFocus();
		}

		int? joinTable, joinColumn;
		internal bool GetJoin(out int joinTable, out int joinColumn)
		{
			joinTable = this.joinTable ?? -1;
			joinColumn = this.joinColumn ?? -1;
			var result = this.joinTable.HasValue;
			this.joinTable = this.joinColumn = null;
			return result;
		}

		internal void SetJoin(EditTableTab tab, int joinColumn)
		{
			this.joinTable = Tabs.IndexOf(tab);
			this.joinColumn = joinColumn;
		}

		internal EditTableTab GetTab(int index)
		{
			return Tabs[index];
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Results = Tabs.Select(tab => tab.GetResult()).ToList() };
			DialogResult = true;
		}

		public static Result Run(Window parent, List<string> tableStr, List<Table> tables)
		{
			var dialog = new EditTablesDialog(tableStr, tables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
