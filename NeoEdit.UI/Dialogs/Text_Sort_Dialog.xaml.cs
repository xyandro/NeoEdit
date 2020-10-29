using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Text_Sort_Dialog
	{
		[DepProp]
		public SortScope SortScope { get { return UIHelper<Text_Sort_Dialog>.GetPropValue<SortScope>(this); } set { UIHelper<Text_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseRegion { get { return UIHelper<Text_Sort_Dialog>.GetPropValue<int>(this); } set { UIHelper<Text_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public SortType SortType { get { return UIHelper<Text_Sort_Dialog>.GetPropValue<SortType>(this); } set { UIHelper<Text_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CaseSensitive { get { return UIHelper<Text_Sort_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ascending { get { return UIHelper<Text_Sort_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Text_Sort_Dialog>.SetPropValue(this, value); } }

		static Text_Sort_Dialog()
		{
			UIHelper<Text_Sort_Dialog>.Register();
			UIHelper<Text_Sort_Dialog>.AddCallback(a => a.UseRegion, (obj, o, n) => obj.SortScope = SortScope.Regions);
		}

		Text_Sort_Dialog()
		{
			InitializeComponent();

			UseRegion = 0;
			SortScope = SortScope.Selections;
			SortType = SortType.Smart;
			ascending.IsChecked = true;
		}

		Configuration_Text_Sort result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Text_Sort { SortScope = SortScope, UseRegion = UseRegion, SortType = SortType, CaseSensitive = CaseSensitive, Ascending = Ascending };
			DialogResult = true;
		}

		public static Configuration_Text_Sort Run(Window parent)
		{
			var dialog = new Text_Sort_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
