using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit
{
	partial class EncodingsDialog
	{
		class CodePageItem : CheckBox
		{
			readonly public Coder.CodePage CodePage;

			public CodePageItem(Coder.CodePage codePage)
			{
				CodePage = codePage;
				Content = Coder.GetDescription(CodePage);
			}
		}

		[DepProp]
		ObservableCollection<CodePageItem> CodePageItems { get { return UIHelper<EncodingsDialog>.GetPropValue<ObservableCollection<CodePageItem>>(this); } set { UIHelper<EncodingsDialog>.SetPropValue(this, value); } }

		static EncodingsDialog() { UIHelper<EncodingsDialog>.Register(); }

		HashSet<Coder.CodePage> result;

		EncodingsDialog(HashSet<Coder.CodePage> CodePages)
		{
			InitializeComponent();
			CodePageItems = new ObservableCollection<CodePageItem>(Coder.GetStringCodePages().Select(codePage => new CodePageItem(codePage) { IsChecked = CodePages.Contains(codePage) }));
			codePages.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Space)
				{
					var selected = codePages.SelectedItems.Cast<CodePageItem>().ToList();
					var status = !selected.All(item => item.IsChecked == true);
					selected.ForEach(item => item.IsChecked = status);
					e.Handled = true;
				}
			};
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new HashSet<Coder.CodePage>(CodePageItems.Where(item => item.IsChecked == true).Select(item => item.CodePage));
			DialogResult = true;
		}

		public static HashSet<Coder.CodePage> Run(Window parent, HashSet<Coder.CodePage> codePages)
		{
			var dialog = new EncodingsDialog(codePages) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		private void SelectAll(object sender, RoutedEventArgs e)
		{
			CodePageItems.ToList().ForEach(item => item.IsChecked = true);
		}

		private void SelectNone(object sender, RoutedEventArgs e)
		{
			CodePageItems.ToList().ForEach(item => item.IsChecked = false);
		}
	}
}
