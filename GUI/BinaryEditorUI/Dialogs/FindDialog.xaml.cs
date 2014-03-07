using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.Common.Data;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.BinaryEditorUI.Dialogs
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
		public bool ShowOther { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static FindDialog() { UIHelper<FindDialog>.Register(); }

		readonly UIHelper<FindDialog> uiHelper;
		FindDialog()
		{
			uiHelper = new UIHelper<FindDialog>(this);
			InitializeComponent();
			ShowLE = ShowInt = ShowStr = ShowOther = true;
			ShowBE = ShowFloat = false;
			MatchCase.IsChecked = UTF7.IsChecked = HexRev.IsChecked = Base64.IsChecked = false;
		}

		FindData result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(FindText))
				return;

			var converters = Helpers.GetValues<Coder.Type>().Where(a => a != Coder.Type.None).ToDictionary(a => a, a => typeof(FindDialog).GetField(a.ToString(), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this) as CheckBox);
			var convertersToUse = converters.Where(a => (a.Value.IsVisible) && (a.Value.IsEnabled) && (a.Value.IsChecked == true)).Select(a => a.Key).ToList();
			if (convertersToUse.Count == 0)
				return;
			var findData = convertersToUse.Select(converter => new { converter = converter, bytes = Coder.StringToBytes(FindText, converter) }).GroupBy(obj => Coder.BytesToString(obj.bytes, Coder.Type.Hex)).Select(group => group.First()).ToDictionary(obj => obj.converter, obj => obj.bytes);

			result = new FindData
			{
				Text = FindText,
				Data = findData.Select(a => a.Value).ToList(),
				IgnoreCase = findData.Select(a => (MatchCase.IsChecked != true) && (a.Key.IsStr())).ToList(),
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
