using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor.Dialogs
{
	public partial class FindDialog : Window
	{
		[DepProp]
		public string FindText { get { return UIHelper<FindDialog>.GetPropValue<string>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowLE { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowBE { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowInt { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowFloat { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowStr { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool ShowOther { get { return UIHelper<FindDialog>.GetPropValue<bool>(this); } set { UIHelper<FindDialog>.SetPropValue(this, value); } }

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		FindDialog()
		{
			InitializeComponent();

			ShowLE = ShowInt = ShowStr = ShowOther = true;
			ShowBE = ShowFloat = false;
			MatchCase.IsChecked = UTF7.IsChecked = Base64.IsChecked = false;
		}

		FindData result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(FindText))
				return;

			var converters = Helpers.GetValues<Coder.Type>().Select(a => new { Key = a, Value = typeof(FindDialog).GetField(a.ToString(), BindingFlags.Instance | BindingFlags.NonPublic) }).Where(a => a.Value != null).ToDictionary(a => a.Key, a => a.Value.GetValue(this) as CheckBox);
			var convertersToUse = converters.Where(a => (a.Value.IsVisible) && (a.Value.IsEnabled) && (a.Value.IsChecked == true)).Select(a => a.Key).ToList();
			if (convertersToUse.Count == 0)
				return;
			var findData = convertersToUse.Select(converter => new { converter = converter, bytes = Coder.StringToBytes(FindText, converter) }).GroupBy(obj => Coder.BytesToString(obj.bytes, Coder.Type.Hex)).Select(group => group.First()).ToDictionary(obj => obj.converter, obj => obj.bytes);

			result = new FindData
			{
				Text = FindText,
				Searcher = new Searcher(findData.Select(a => a.Value).ToList(), findData.Select(a => (MatchCase.IsChecked == true) && (a.Key.IsStr())).ToList()),
			};

			DialogResult = true;
		}

		public static FindData Run()
		{
			var find = new FindDialog();
			if (find.ShowDialog() == false)
				return null;
			return find.result;
		}
	}
}
