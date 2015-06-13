using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	partial class AggregateDialog
	{
		internal class Result
		{
			public TextEditor.SortAggregationScope AggregateScope { get; set; }
			public TextEditor.AggregateType AggregateType { get; set; }
			public string ConcatText { get; set; }
			public bool RemoveSelection { get; set; }
			public bool TrimWhitespace { get; set; }
		}

		[DepProp]
		public TextEditor.SortAggregationScope AggregateScope { get { return UIHelper<AggregateDialog>.GetPropValue<TextEditor.SortAggregationScope>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public TextEditor.AggregateType AggregateType { get { return UIHelper<AggregateDialog>.GetPropValue<TextEditor.AggregateType>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public string ConcatText { get { return UIHelper<AggregateDialog>.GetPropValue<string>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool RemoveSelection { get { return UIHelper<AggregateDialog>.GetPropValue<bool>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool TrimWhitespace { get { return UIHelper<AggregateDialog>.GetPropValue<bool>(this); } set { UIHelper<AggregateDialog>.SetPropValue(this, value); } }

		static AggregateDialog() { UIHelper<AggregateDialog>.Register(); }

		AggregateDialog()
		{
			InitializeComponent();

			AggregateScope = TextEditor.SortAggregationScope.Selections;
			AggregateType = TextEditor.AggregateType.Count;
			ConcatText = ", ";
			RemoveSelection = TrimWhitespace = true;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { AggregateScope = AggregateScope, AggregateType = AggregateType, ConcatText = ConcatText, RemoveSelection = RemoveSelection, TrimWhitespace = TrimWhitespace };
			DialogResult = true;
		}

		public static Result Run(Window parent)
		{
			var dialog = new AggregateDialog { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
