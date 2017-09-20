using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class EditSortDialog
	{
		internal class Result
		{
			public TextEditor.SortScope SortScope { get; set; }
			public TextEditor.SortType SortType { get; set; }
			public bool CaseSensitive { get; set; }
			public bool Ascending { get; set; }
		}

		[DepProp]
		public TextEditor.SortScope SortScope { get { return UIHelper<EditSortDialog>.GetPropValue<TextEditor.SortScope>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor.SortType SortType { get { return UIHelper<EditSortDialog>.GetPropValue<TextEditor.SortType>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CaseSensitive { get { return UIHelper<EditSortDialog>.GetPropValue<bool>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ascending { get { return UIHelper<EditSortDialog>.GetPropValue<bool>(this); } set { UIHelper<EditSortDialog>.SetPropValue(this, value); } }

		static EditSortDialog()
		{
			UIHelper<EditSortDialog>.Register();
		}

		EditSortDialog()
		{
			InitializeComponent();

			SortScope = TextEditor.SortScope.Selections;
			SortType = TextEditor.SortType.Smart;
			ascending.IsChecked = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { SortScope = SortScope, SortType = SortType, CaseSensitive = CaseSensitive, Ascending = Ascending };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new EditSortDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
