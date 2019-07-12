using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Program;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Parsing;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Dialogs
{
	partial class ViewValuesStringsDialog
	{
		[DepProp]
		ObservableCollection<CodePageCheckBox> EncodingCheckBoxes { get { return UIHelper<ViewValuesStringsDialog>.GetPropValue<ObservableCollection<CodePageCheckBox>>(this); } set { UIHelper<ViewValuesStringsDialog>.SetPropValue(this, value); } }

		readonly static HashSet<Coder.CodePage> defaultCodePages = new HashSet<Coder.CodePage>(Coder.GetNumericCodePages().Concat(new Coder.CodePage[] { Coder.DefaultCodePage, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE }));

		static ViewValuesStringsDialog() => UIHelper<ViewValuesStringsDialog>.Register();

		ViewValuesStringsDialog(List<Coder.CodePage> initialCodePages)
		{
			InitializeComponent();

			EncodingCheckBoxes = new ObservableCollection<CodePageCheckBox>(Coder.GetStringCodePages().Select(codePage => new CodePageCheckBox { CodePage = codePage }));
			codePages.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Space)
				{
					var selected = codePages.SelectedItems.Cast<CodePageCheckBox>().ToList();
					var status = !selected.All(item => item.IsChecked == true);
					selected.ForEach(item => item.IsChecked = status);
					e.Handled = true;
				}
			};

			foreach (var checkBox in EncodingCheckBoxes)
				checkBox.IsChecked = initialCodePages.Contains(checkBox.CodePage);
		}

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			foreach (var checkBox in EncodingCheckBoxes)
				checkBox.IsChecked = defaultCodePages.Contains(checkBox.CodePage);
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static List<Coder.CodePage> Run(Window parent, List<Coder.CodePage> initialCodePages)
		{
			var find = new ViewValuesStringsDialog(initialCodePages) { Owner = parent };
			return find.ShowDialog() ? find.EncodingCheckBoxes.Where(x => x.IsChecked == true).Select(x => x.CodePage).ToList() : null;
		}

		void SelectAllNone(object sender, RoutedEventArgs e)
		{
			var all = e.Source == allButton;
			EncodingCheckBoxes.ForEach(checkBox => checkBox.IsChecked = all);
		}
	}
}
