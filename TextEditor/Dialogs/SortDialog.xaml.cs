using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor.Dialogs
{
	partial class SortDialog
	{
		internal class Result : IDialogResult
		{
			public TextEditor.SortScope SortScope { get; set; }
			public TextEditor.SortType SortType { get; set; }
			public bool Ascending { get; set; }
		}

		[DepProp]
		public TextEditor.SortScope SortScope { get { return UIHelper<SortDialog>.GetPropValue<TextEditor.SortScope>(this); } set { UIHelper<SortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor.SortType SortType { get { return UIHelper<SortDialog>.GetPropValue<TextEditor.SortType>(this); } set { UIHelper<SortDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Ascending { get { return UIHelper<SortDialog>.GetPropValue<bool>(this); } set { UIHelper<SortDialog>.SetPropValue(this, value); } }

		static SortDialog() { UIHelper<SortDialog>.Register(); }

		SortDialog()
		{
			InitializeComponent();

			SortScope = TextEditor.SortScope.Selections;
			SortType = TextEditor.SortType.String;
			ascending.IsChecked = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { SortScope = SortScope, SortType = SortType, Ascending = Ascending };
			DialogResult = true;
		}

		public static Result Run()
		{
			var dialog = new SortDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
