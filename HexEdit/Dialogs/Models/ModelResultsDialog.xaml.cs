using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	class ModelResultGrid : ItemGrid<ModelResult> { }

	partial class ModelResultsDialog
	{
		Action<long, long> changeSelection;
		[DepProp]
		public ObservableCollection<ModelResult> Results { get { return UIHelper<ModelResultsDialog>.GetPropValue<ObservableCollection<ModelResult>>(this); } set { UIHelper<ModelResultsDialog>.SetPropValue(this, value); } }

		static ModelResultsDialog() { UIHelper<ModelResultsDialog>.Register(); }

		ModelResultsDialog(List<ModelResult> _results, Action<long, long> _changeSelection)
		{
			InitializeComponent();

			changeSelection = _changeSelection;

			Results = new ObservableCollection<ModelResult>(_results);
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Num)) { SortAscending = true });
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Name)));
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Value)));
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Location)));
			results.Columns.Add(new ItemGridColumn(UIHelper<ModelResult>.GetProperty(a => a.Length)));
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		public static void Run(List<ModelResult> results, Action<long, long> changeSelection)
		{
			new ModelResultsDialog(results, changeSelection).Show();
		}

		public void SelectionChanged(object sender)
		{
			if (results.Selected.Count != 1)
				return;
			var item = results.Selected.Single();
			changeSelection(item.StartByte, item.EndByte);
		}
	}
}
