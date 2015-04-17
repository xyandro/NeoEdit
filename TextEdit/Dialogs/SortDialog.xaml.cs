using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class SortDialog
	{
		internal class Result
		{
			public TextEditor.SortScope SortScope { get; set; }
			public TextEditor.SortType SortType { get; set; }
			public bool CaseSensitive { get; set; }
			public bool Ascending { get; set; }
		}

		[DepProp]
		public TextEditor.SortScope SortScope { get { return UIHelper<SortDialog>.GetPropValue(() => this.SortScope); } set { UIHelper<SortDialog>.SetPropValue(() => this.SortScope, value); } }
		[DepProp]
		public TextEditor.SortType SortType { get { return UIHelper<SortDialog>.GetPropValue(() => this.SortType); } set { UIHelper<SortDialog>.SetPropValue(() => this.SortType, value); } }
		[DepProp]
		public bool CaseSensitive { get { return UIHelper<SortDialog>.GetPropValue(() => this.CaseSensitive); } set { UIHelper<SortDialog>.SetPropValue(() => this.CaseSensitive, value); } }
		[DepProp]
		public bool Ascending { get { return UIHelper<SortDialog>.GetPropValue(() => this.Ascending); } set { UIHelper<SortDialog>.SetPropValue(() => this.Ascending, value); } }

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
			result = new Result { SortScope = SortScope, SortType = SortType, CaseSensitive = CaseSensitive, Ascending = Ascending };
			DialogResult = true;
		}

		public static Result Run()
		{
			var dialog = new SortDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
