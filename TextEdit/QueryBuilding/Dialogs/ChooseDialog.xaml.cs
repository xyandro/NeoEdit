using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.QueryBuilding.Dialogs
{
	partial class ChooseDialog
	{
		[DepProp]
		public string Item { get { return UIHelper<ChooseDialog>.GetPropValue<string>(this); } set { UIHelper<ChooseDialog>.SetPropValue(this, value); } }

		static ChooseDialog() { UIHelper<ChooseDialog>.Register(); }

		readonly HashSet<string> items;
		ChooseDialog(IEnumerable<string> items)
		{
			this.items = new HashSet<string>(items);
			InitializeComponent();
			itemsList.AddSuggestions(items.ToArray());
			itemsList.IsDropDownOpen = true;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(Item))
				throw new Exception("Nothing selected.");
			if (!items.Contains(Item))
				throw new Exception("Invalid selection.");
			DialogResult = true;
		}

		public static T Run<T>(Window parent, List<T> items, Func<T, string> getValue = null) where T : class
		{
			if (getValue == null)
				getValue = x => x?.ToString() ?? "";
			var dialog = new ChooseDialog(items.Select(getValue)) { Owner = parent };
			if (!dialog.ShowDialog())
				return null;

			return items.FirstOrDefault(x => getValue(x) == dialog.Item);
		}
	}
}
