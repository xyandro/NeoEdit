using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Dialogs
{
	internal class CodePageCheckBox : CheckBox
	{
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<CodePageCheckBox>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CodePageCheckBox>.SetPropValue(this, value); } }

		static CodePageCheckBox()
		{
			UIHelper<CodePageCheckBox>.Register();
			UIHelper<CodePageCheckBox>.AddCallback(a => a.CodePage, (obj, o, n) => obj.Content = Coder.IsStr(obj.CodePage) ? Coder.GetDescription(obj.CodePage) : obj.CodePage.ToString());
		}

		public CodePageCheckBox() { CodePage = Coder.CodePage.None; }
	}

	partial class FindBinaryDialog
	{
		public class Result
		{
			public string Text { get; }
			public Searcher Searcher { get; }

			internal Result(string text, Searcher searcher)
			{
				Text = text;
				Searcher = searcher;
			}
		}

		[DepProp]
		public string FindText { get { return UIHelper<FindBinaryDialog>.GetPropValue<string>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<FindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<FindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<FindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<FindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<FindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<FindBinaryDialog>.GetPropValue<bool>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<CodePageCheckBox> EncodingCheckBoxes { get { return UIHelper<FindBinaryDialog>.GetPropValue<ObservableCollection<CodePageCheckBox>>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<string> History { get { return UIHelper<FindBinaryDialog>.GetPropValue<ObservableCollection<string>>(this); } set { UIHelper<FindBinaryDialog>.SetPropValue(this, value); } }

		static bool savedShowLE = true;
		static bool savedShowBE = false;
		static bool savedShowInt = true;
		static bool savedShowFloat = false;
		static bool savedShowStr = true;
		static bool savedMatchCase = false;
		readonly static ObservableCollection<string> staticHistory = new ObservableCollection<string>();
		readonly List<CodePageCheckBox> checkBoxes;
		readonly static HashSet<Coder.CodePage> defaultCodePages = new HashSet<Coder.CodePage>(Coder.GetNumericCodePages().Concat(new Coder.CodePage[] { Coder.CodePage.Default, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE }));
		readonly static HashSet<Coder.CodePage> savedCodePages = new HashSet<Coder.CodePage>(defaultCodePages);

		static FindBinaryDialog() { UIHelper<FindBinaryDialog>.Register(); }

		FindBinaryDialog()
		{
			InitializeComponent();

			History = staticHistory;
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

			if ((String.IsNullOrEmpty(FindText)) && (History.Count != 0))
				FindText = History[0];

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
			if (String.IsNullOrEmpty(FindText))
				return;

			var data = new List<Tuple<byte[], bool>>();
			foreach (var checkBox in checkBoxes)
			{
				if ((!checkBox.IsEnabled) || (checkBox.IsChecked != true))
					continue;
				var str = Coder.IsStr(checkBox.CodePage);
				if ((str) && (!ShowStr))
					continue;
				if ((!str) && (!checkBox.IsVisible))
					continue;
				data.Add(Tuple.Create(Coder.StringToBytes(FindText, checkBox.CodePage), (!str) || (MatchCase) || (Coder.AlwaysCaseSensitive(checkBox.CodePage))));
			}

			data = data.Distinct(tuple => $"{Coder.BytesToString(tuple.Item1, Coder.CodePage.Hex)}-{tuple.Item2}").ToList();

			if (data.Count == 0)
				return;

			result = new Result(FindText, new Searcher(data));

			History.Remove(FindText);
			History.Insert(0, FindText);

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
			var find = new FindBinaryDialog { Owner = parent };
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
