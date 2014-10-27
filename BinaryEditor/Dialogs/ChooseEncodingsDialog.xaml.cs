using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor.Dialogs
{
	internal partial class ChooseEncodingsDialog
	{
		class CodePageItem : DependencyObject
		{
			[DepProp]
			public bool IsChecked { get { return UIHelper<CodePageItem>.GetPropValue<bool>(this); } set { UIHelper<CodePageItem>.SetPropValue(this, value); } }
			[DepProp]
			public StrCoder.CodePage CodePage { get { return UIHelper<CodePageItem>.GetPropValue<StrCoder.CodePage>(this); } set { UIHelper<CodePageItem>.SetPropValue(this, value); } }
			[DepProp]
			public string Name { get { return UIHelper<CodePageItem>.GetPropValue<string>(this); } set { UIHelper<CodePageItem>.SetPropValue(this, value); } }

			static CodePageItem() { UIHelper<CodePageItem>.Register(); }

			public CodePageItem(StrCoder.CodePage codePage)
			{
				CodePage = codePage;
				Name = StrCoder.GetDescription(CodePage);
			}
		}

		[DepProp]
		ObservableCollection<CodePageItem> CodePageItems { get { return UIHelper<ChooseEncodingsDialog>.GetPropValue<ObservableCollection<CodePageItem>>(this); } set { UIHelper<ChooseEncodingsDialog>.SetPropValue(this, value); } }

		static ChooseEncodingsDialog() { UIHelper<ChooseEncodingsDialog>.Register(); }

		HashSet<StrCoder.CodePage> result;

		ChooseEncodingsDialog(HashSet<StrCoder.CodePage> CodePages)
		{
			InitializeComponent();
			CodePageItems = new ObservableCollection<CodePageItem>(StrCoder.GetCodePages().Select(codePage => new CodePageItem(codePage) { IsChecked = CodePages.Contains(codePage) }));
			codePages.PreviewKeyDown += (s, e) =>
			{
				if (e.Key == Key.Space)
				{
					var selected = codePages.SelectedItems.Cast<CodePageItem>().ToList();
					var status = !selected.All(item => item.IsChecked);
					selected.ForEach(item => item.IsChecked = status);
					e.Handled = true;
				}
			};
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new HashSet<StrCoder.CodePage>(CodePageItems.Where(item => item.IsChecked).Select(item => item.CodePage));
			DialogResult = true;
		}

		public static HashSet<StrCoder.CodePage> Run(HashSet<StrCoder.CodePage> codePages)
		{
			var dialog = new ChooseEncodingsDialog(codePages);
			return dialog.ShowDialog() == true ? dialog.result : null;
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
