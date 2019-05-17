using System.Windows;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class EditSortDialog
	{
		public class Result
		{
			public SortScope SortScope { get; set; }
			public int UseRegion { get; set; }
			public SortType SortType { get; set; }
			public bool CaseSensitive { get; set; }
			public bool Ascending { get; set; }
		}

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

			UseRegion = 1;
			SortScope = SortScope.Selections;
			SortType = SortType.Smart;
			ascending.IsChecked = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { SortScope = SortScope, UseRegion = UseRegion, SortType = SortType, CaseSensitive = CaseSensitive, Ascending = Ascending };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new EditSortDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
