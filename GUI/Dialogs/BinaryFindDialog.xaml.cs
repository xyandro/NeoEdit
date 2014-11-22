using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	partial class BinaryFindDialog
	{
		internal class CodePageItem : CheckBox
		{
			[DepProp]
			public Coder.CodePage CodePage { get { return UIHelper<CodePageItem>.GetPropValue<Coder.CodePage>(this); } set { UIHelper<CodePageItem>.SetPropValue(this, value); } }

			static CodePageItem() { UIHelper<CodePageItem>.Register(); }

			public CodePageItem(Coder.CodePage codePage)
			{
				CodePage = codePage;
				Content = Coder.GetDescription(CodePage);
			}
		}

		public class Result
		{
			public string Text { get; private set; }
			public Searcher Searcher { get; private set; }

			internal Result(string text, Searcher searcher)
			{
				Text = text;
				Searcher = searcher;
			}
		}

		[DepProp]
		public string FindText { get { return UIHelper<BinaryFindDialog>.GetPropValue<string>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool MatchCase { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }
		[DepProp]
		ObservableCollection<CodePageItem> CodePageItems { get { return UIHelper<BinaryFindDialog>.GetPropValue<ObservableCollection<CodePageItem>>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }

		static BinaryFindDialog() { UIHelper<BinaryFindDialog>.Register(); }

		BinaryFindDialog()
		{
			InitializeComponent();

			var defaultCodePages = new HashSet<Coder.CodePage> { Coder.CodePage.Default, Coder.CodePage.UTF8, Coder.CodePage.UTF16LE };
			CodePageItems = new ObservableCollection<CodePageItem>(Coder.GetCodePages().Select(codePage => new CodePageItem(codePage) { IsChecked = defaultCodePages.Contains(codePage) }));
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

			ShowLE = ShowInt = ShowStr = true;
			ShowBE = ShowFloat = MatchCase = false;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(FindText))
				return;

			var valueConverters = Helpers.GetValues<Coder.CodePage>().Select(a => new { Key = a, Value = typeof(BinaryFindDialog).GetField(a.ToString(), BindingFlags.Instance | BindingFlags.NonPublic) }).Where(a => a.Value != null).ToDictionary(a => a.Key, a => a.Value.GetValue(this) as CheckBox);
			var valueConvertersToUse = valueConverters.Where(a => (a.Value.IsVisible) && (a.Value.IsEnabled) && (a.Value.IsChecked == true)).Select(a => a.Key).ToList();
			var data = valueConvertersToUse.Select(converter => new Tuple<byte[], bool>(Coder.StringToBytes(FindText, converter), true)).ToList();

			if (ShowStr)
			{
				var stringConverters = CodePageItems.Where(item => (item.IsEnabled) && (item.IsChecked == true)).Select(item => item.CodePage).ToList();
				data.AddRange(stringConverters.Select(converter => new Tuple<byte[], bool>(Coder.StringToBytes(FindText, converter), (MatchCase) || (Coder.AlwaysCaseSensitive(converter)))));
			}

			data = data.GroupBy(tuple => String.Format("{0}-{1}", Coder.BytesToString(tuple.Item1, Coder.CodePage.Hex), tuple.Item2)).Select(item => item.First()).ToList();

			if (data.Count == 0)
				return;

			result = new Result(FindText, new Searcher(data));

			DialogResult = true;
		}

		public static Result Run()
		{
			var find = new BinaryFindDialog();
			if (find.ShowDialog() == false)
				return null;
			return find.result;
		}
	}
}
