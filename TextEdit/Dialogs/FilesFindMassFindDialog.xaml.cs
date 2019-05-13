using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.TextEdit;
using NeoEdit.TextEdit.Expressions;
using NeoEdit.TextEdit.Transform;
using NeoEdit.TextEdit.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class FilesFindMassFindDialog
	{
		public class Result
		{
			public string Expression { get; set; }
			public List<Coder.CodePage> CodePages { get; set; }
			public bool MatchCase { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<string>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public NEVariables Variables { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<NEVariables>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<CodePageCheckBox> EncodingCheckBoxes { get { return UIHelper<FilesFindMassFindDialog>.GetPropValue<ObservableCollection<CodePageCheckBox>>(this); } set { UIHelper<FilesFindMassFindDialog>.SetPropValue(this, value); } }

		static bool savedShowLE = true;
		static bool savedShowBE = false;
		static bool savedShowInt = true;
		static bool savedShowFloat = false;
		static bool savedShowStr = true;
		static bool savedMatchCase = false;
		readonly List<CodePageCheckBox> checkBoxes;
		readonly static HashSet<Coder.CodePage> defaultCodePages = new HashSet<Coder.CodePage>(Coder.GetNumericCodePages().Concat(new Coder.CodePage[] { Coder.DefaultCodePage, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE }));
		readonly static HashSet<Coder.CodePage> savedCodePages = new HashSet<Coder.CodePage>(defaultCodePages);

		static FilesFindMassFindDialog() { UIHelper<FilesFindMassFindDialog>.Register(); }

		FilesFindMassFindDialog(NEVariables variables)
		{
			InitializeComponent();

			Variables = variables;
			EncodingCheckBoxes = new ObservableCollection<CodePageCheckBox>(Coder.GetStringCodePages().Select(codePage => new CodePageCheckBox { CodePage = codePage }));
			checkBoxes = this.FindLogicalChildren<CodePageCheckBox>().Concat(EncodingCheckBoxes).ToList();
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

			Expression = expression.GetLastSuggestion().CoalesceNullOrEmpty("k");

			ShowLE = savedShowLE;
			ShowBE = savedShowBE;
			ShowInt = savedShowInt;
			ShowFloat = savedShowFloat;
			ShowStr = savedShowStr;
			MatchCase = savedMatchCase;
			foreach (var checkBox in checkBoxes)
				checkBox.IsChecked = savedCodePages.Contains(checkBox.CodePage);
		}

		void Reset(object sender = null, RoutedEventArgs e = null)
		{
			ShowLE = ShowInt = ShowStr = true;
			ShowBE = ShowFloat = MatchCase = false;
			foreach (var checkBox in checkBoxes)
				checkBox.IsChecked = defaultCodePages.Contains(checkBox.CodePage);
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(Expression))
				return;

			var codePages = new List<Coder.CodePage>();
			foreach (var checkBox in checkBoxes)
			{
				if (checkBox.IsChecked != true)
					continue;
				var isStr = Coder.IsStr(checkBox.CodePage);
				if ((isStr) && (!ShowStr))
					continue;
				if ((!isStr) && (!checkBox.IsVisible))
					continue;
				codePages.Add(checkBox.CodePage);
			}

			if (codePages.Count == 0)
				return;

			result = new Result { Expression = Expression, CodePages = codePages, MatchCase = MatchCase };

			expression.AddCurrentSuggestion();

			savedShowLE = ShowLE;
			savedShowBE = ShowBE;
			savedShowInt = ShowInt;
			savedShowFloat = ShowFloat;
			savedShowStr = ShowStr;
			savedMatchCase = MatchCase;
			savedCodePages.Clear();
			foreach (var checkBox in checkBoxes)
				if (checkBox.IsChecked == true)
					savedCodePages.Add(checkBox.CodePage);

			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables)
		{
			var dialog = new FilesFindMassFindDialog(variables) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}

		void SelectAllNone(object sender, RoutedEventArgs e)
		{
			var button = e.Source as Button;
			var strings = (button == AllStrings) || (button == NoStrings);
			var all = button.Content as string == "All";
			if (strings)
			{
				foreach (var checkBox in EncodingCheckBoxes)
					checkBox.IsChecked = all;
			}
			else
			{
				var parent = (e.Source as FrameworkElement).FindParent<GroupBox>();
				var children = parent.FindLogicalChildren<CodePageCheckBox>().ToList();
				foreach (var child in children)
					if (child.IsVisible)
						child.IsChecked = all;
			}
		}

		void SelectAllNoneGlobal(object sender, RoutedEventArgs e)
		{
			var all = (e.Source as Button).Content as string == "_All";
			foreach (var checkBox in checkBoxes)
				checkBox.IsChecked = all;
		}
	}
}
