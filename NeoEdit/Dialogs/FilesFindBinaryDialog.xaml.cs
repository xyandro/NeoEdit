using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.MenuFiles.Dialogs
{
	partial class FilesFindBinaryDialog
	{
		public class Result
		{
			public string Text { get; }
			public List<Coder.CodePage> CodePages { get; }
			public bool MatchCase { get; }

			internal Result(string text, List<Coder.CodePage> codePages, bool matchCase)
			{
				Text = text;
				CodePages = codePages;
				MatchCase = matchCase;
			}
		}

		[DepProp]
		public string FindText { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<string>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<CodePageCheckBox> EncodingCheckBoxes { get { return UIHelper<FilesFindBinaryDialog>.GetPropValue<ObservableCollection<CodePageCheckBox>>(this); } set { UIHelper<FilesFindBinaryDialog>.SetPropValue(this, value); } }

		static bool savedShowLE = true;
		static bool savedShowBE = false;
		static bool savedShowInt = true;
		static bool savedShowFloat = false;
		static bool savedShowStr = true;
		static bool savedMatchCase = false;
		readonly List<CodePageCheckBox> checkBoxes;
		readonly static HashSet<Coder.CodePage> defaultCodePages = new HashSet<Coder.CodePage>(Coder.GetNumericCodePages().Concat(new Coder.CodePage[] { Coder.DefaultCodePage, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE }));
		readonly static HashSet<Coder.CodePage> savedCodePages = new HashSet<Coder.CodePage>(defaultCodePages);

		static FilesFindBinaryDialog() { UIHelper<FilesFindBinaryDialog>.Register(); }

		FilesFindBinaryDialog()
		{
			InitializeComponent();

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

			FindText = findText.GetLastSuggestion();

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
			if (string.IsNullOrEmpty(FindText))
				return;

			var codePages = new List<Coder.CodePage>();
			foreach (var checkBox in checkBoxes)
			{
				if ((!checkBox.IsEnabled) || (checkBox.IsChecked != true))
					continue;
				var str = Coder.IsStr(checkBox.CodePage);
				if ((str) && (!ShowStr))
					continue;
				if ((!str) && (!checkBox.IsVisible))
					continue;
				codePages.Add(checkBox.CodePage);
			}

			if (codePages.Count == 0)
				return;

			result = new Result(FindText, codePages, MatchCase);

			findText.AddCurrentSuggestion();

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

		public static Result Run(Window parent)
		{
			var find = new FilesFindBinaryDialog { Owner = parent };
			if (!find.ShowDialog())
				return null;
			return find.result;
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
