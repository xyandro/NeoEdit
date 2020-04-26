using System;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class EditSortDialog
	{
		[DepProp]
		public SortScope SortScope { get { return UIHelper<EditSortDialog>.GetPropValue<SortScope>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int UseRegion { get { return UIHelper<EditSortDialog>.GetPropValue<int>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public SortType SortType { get { return UIHelper<EditSortDialog>.GetPropValue<SortType>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CaseSensitive { get { return UIHelper<EditSortDialog>.GetPropValue<bool>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ascending { get { return UIHelper<EditSortDialog>.GetPropValue<bool>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }

		static EditSortDialog()
		{
			UIHelper<EditSortDialog>.Register();
			UIHelper<EditSortDialog>.AddCallback(a => a.UseRegion, (obj, o, n) => obj.SortScope = SortScope.Regions);
		}

		EditSortDialog()
		{
			InitializeComponent();

			UseRegion = 0;
			SortScope = SortScope.Selections;
			SortType = SortType.Smart;
			ascending.IsChecked = true;
		}

		EditSortDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new EditSortDialogResult { SortScope = SortScope, UseRegion = UseRegion, SortType = SortType, CaseSensitive = CaseSensitive, Ascending = Ascending };
			DialogResult = true;
		}

		public static EditSortDialogResult Run(Window parent)
		{
			var dialog = new EditSortDialog { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
