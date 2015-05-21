using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.GUI.Controls
{
	partial class AutoCompleteTextBox
	{
		[DepProp]
		public ObservableCollection<string> AutoSuggestionList { get { return UIHelper<AutoCompleteTextBox>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<AutoCompleteTextBox>.SetPropValue(this, value); } }

		static AutoCompleteTextBox()
		{
			UIHelper<AutoCompleteTextBox>.Register();
			UIHelper<AutoCompleteTextBox>.AddCallback(a => a.AutoSuggestionList, (obj, o, n) => obj.PopulateItems());
		}

		TextBox textBox;
		public AutoCompleteTextBox()
		{
			InitializeComponent();
			AutoSuggestionList = new ObservableCollection<string>();
			Loaded += (s, e) => textBox = Template.FindName("PART_EditableTextBox", this) as TextBox;
		}

		protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
		{
			if ((e.Key == Key.Down) && (!IsDropDownOpen))
			{
				PopulateItems();
				if (HasItems)
				{
					var selStart = textBox.SelectionStart;
					var selLength = textBox.SelectionLength;
					IsDropDownOpen = true;
					textBox.SelectionStart = selStart;
					textBox.SelectionLength = selLength;
					e.Handled = true;
				}
			}

			if (!e.Handled)
				base.OnPreviewKeyDown(e);
		}

		void textBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!IsDropDownOpen)
				return;

			if ((textBox.SelectionStart != 0) || (textBox.Text.Length == 0))
				PopulateItems();
		}

		void PopulateItems()
		{
			if (AutoSuggestionList == null)
				return;

			Items.Clear();
			var find = textBox != null ? textBox.Text : null ?? Text ?? "";
			foreach (string str in AutoSuggestionList)
				if (str.StartsWith(find, StringComparison.InvariantCultureIgnoreCase))
					Items.Add(str);
		}
	}
}
