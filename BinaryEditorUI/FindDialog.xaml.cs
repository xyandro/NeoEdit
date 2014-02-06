using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;

namespace NeoEdit.BinaryEditorUI
{
	public partial class FindDialog : Window
	{
		[DepProp]
		public string FindText { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowLE { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowBE { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowInt { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowFloat { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowStr { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool ShowHex { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			InitializeComponent();
			ShowLE = ShowInt = ShowStr = ShowHex = true;
			ShowBE = ShowFloat = false;
			MatchCase.IsChecked = UTF7.IsChecked = HexRev.IsChecked = false;
		}

		FindData result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(FindText))
				return;

			var converters = Helpers.GetValues<BinaryData.ConverterType>().ToDictionary(a => a, a => typeof(FindDialog).GetField(a.ToString(), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this) as CheckBox);
			var convertersToUse = converters.Where(a => (a.Value.IsVisible) && (a.Value.IsEnabled) && (a.Value.IsChecked == true)).Select(a => a.Key).ToList();
			var findData = convertersToUse.Select(a => new { converter = a, binaryData = BinaryData.FromString(a, FindText) }).GroupBy(a => a.binaryData.ToHexString()).Select(a => a.First()).ToDictionary(a => a.converter, a => a.binaryData);

			result = new FindData
			{
				Text = FindText,
				Data = findData.Select(a => a.Value).ToList(),
				IgnoreCase = findData.Select(a => (MatchCase.IsChecked != true) && (BinaryData.IsStr(a.Key))).ToList(),
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
