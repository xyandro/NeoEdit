using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Edit_Sort_Dialog
	{
		[DepProp]
		public SortScope SortScope { get { return UIHelper<Configure_Edit_Sort_Dialog>.GetPropValue<SortScope>(this); } set { UIHelper<Configure_Edit_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseRegion { get { return UIHelper<Configure_Edit_Sort_Dialog>.GetPropValue<int>(this); } set { UIHelper<Configure_Edit_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public SortType SortType { get { return UIHelper<Configure_Edit_Sort_Dialog>.GetPropValue<SortType>(this); } set { UIHelper<Configure_Edit_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CaseSensitive { get { return UIHelper<Configure_Edit_Sort_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Sort_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ascending { get { return UIHelper<Configure_Edit_Sort_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Edit_Sort_Dialog>.SetPropValue(this, value); } }

		static Configure_Edit_Sort_Dialog()
		{
			UIHelper<Configure_Edit_Sort_Dialog>.Register();
			UIHelper<Configure_Edit_Sort_Dialog>.AddCallback(a => a.UseRegion, (obj, o, n) => obj.SortScope = SortScope.Regions);
		}

		Configure_Edit_Sort_Dialog()
		{
			InitializeComponent();

			UseRegion = 0;
			SortScope = SortScope.Selections;
			SortType = SortType.Smart;
			ascending.IsChecked = true;
		}

		Configuration_Edit_Sort result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Edit_Sort { SortScope = SortScope, UseRegion = UseRegion, SortType = SortType, CaseSensitive = CaseSensitive, Ascending = Ascending };
			DialogResult = true;
		}

		public static Configuration_Edit_Sort Run(Window parent)
		{
			var dialog = new Configure_Edit_Sort_Dialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
