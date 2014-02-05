using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class Find : Window
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

		static Find() { UIHelper<Find>.Register(); }

		readonly UIHelper<Find> uiHelper;
		Find()
		{
			uiHelper = new UIHelper<Find>(this);
			InitializeComponent();
			ShowLE = ShowInt = ShowStr = ShowHex = true;
			ShowBE = ShowFloat = false;
		}

		List<byte[]> result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			if (String.IsNullOrEmpty(FindText))
				return;

			var converters = Helpers.GetValues<Converter.ConverterType>().ToDictionary(a => a, a => typeof(Find).GetField(a.ToString(), BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this) as CheckBox);
			var convertersToUse = converters.Where(a => (a.Value.IsVisible) && (a.Value.IsEnabled) && (a.Value.IsChecked == true)).Select(a => a.Key).ToList();
			result = convertersToUse.Select(a => Converter.Convert(a, FindText)).GroupBy(a => BitConverter.ToString(a)).Select(a => a.First()).ToList();

			DialogResult = true;
		}

		public static IEnumerable<byte[]> RunFind()
		{
			var find = new Find();
			if (find.ShowDialog() == false)
				return null;
			return find.result;
		}
	}
}
