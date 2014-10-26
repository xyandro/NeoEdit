using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	partial class BinaryFindDialog
	{
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
		public bool ShowOther { get { return UIHelper<BinaryFindDialog>.GetPropValue<bool>(this); } set { UIHelper<BinaryFindDialog>.SetPropValue(this, value); } }

		static BinaryFindDialog() { UIHelper<BinaryFindDialog>.Register(); }

		BinaryFindDialog()
		{
			InitializeComponent();

			ShowLE = ShowInt = ShowStr = ShowOther = true;
			ShowBE = ShowFloat = false;
			Default.IsChecked = MatchCase.IsChecked = Base64.IsChecked = false;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(FindText))
				return;

			var converters = Helpers.GetValues<Coder.Type>().Select(a => new { Key = a, Value = typeof(BinaryFindDialog).GetField(a.ToString(), BindingFlags.Instance | BindingFlags.NonPublic) }).Where(a => a.Value != null).ToDictionary(a => a.Key, a => a.Value.GetValue(this) as CheckBox);
			var convertersToUse = converters.Where(a => (a.Value.IsVisible) && (a.Value.IsEnabled) && (a.Value.IsChecked == true)).Select(a => a.Key).ToList();
			if (convertersToUse.Count == 0)
				return;
			var findData = convertersToUse.Select(converter => new { converter = converter, bytes = Coder.StringToBytes(FindText, converter) }).GroupBy(obj => StrCoder.BytesToString(obj.bytes, StrCoder.CodePage.Hex)).Select(group => group.First()).ToDictionary(obj => obj.converter, obj => obj.bytes);

			result = new Result(FindText, new Searcher(findData.Select(a => a.Value).ToList()));
			//result = new Result(FindText, new Searcher(findData.Select(a => a.Value).ToList(), findData.Select(a => (MatchCase.IsChecked == true) && (a.Key.IsStr())).ToList()));

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
