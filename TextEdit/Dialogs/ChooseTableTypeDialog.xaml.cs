using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Common;
using NeoEdit.GUI.Controls;
using NeoEdit.TextEdit.Content;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class ChooseTableTypeDialog
	{
		internal class Result
		{
			public Parser.ParserType TableType { get; set; }
		}

		[DepProp]
		public Parser.ParserType TableType { get { return UIHelper<ChooseTableTypeDialog>.GetPropValue<Parser.ParserType>(this); } set { UIHelper<ChooseTableTypeDialog>.SetPropValue(this, value); } }

		public List<Parser.ParserType> TableTypes { get; } = Helpers.GetValues<Parser.ParserType>().Where(type => type != Parser.ParserType.None).ToList();

		static ChooseTableTypeDialog() { UIHelper<ChooseTableTypeDialog>.Register(); }

		ChooseTableTypeDialog(Parser.ParserType tableType)
		{
			InitializeComponent();
			TableType = tableType;
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { TableType = TableType };
			DialogResult = true;
		}

		static public Result Run(Window parent, Parser.ParserType tableType)
		{
			var dialog = new ChooseTableTypeDialog(tableType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
