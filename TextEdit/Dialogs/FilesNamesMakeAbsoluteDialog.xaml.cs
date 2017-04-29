using System.IO;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class FilesNamesMakeAbsoluteDialog
	{
		internal enum ResultType
		{
			None,
			File,
			Directory,
		}

		internal class Result
		{
			public string Expression { get; set; }
			public ResultType Type { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<FilesNamesMakeAbsoluteDialog>.GetPropValue<string>(this); } set { UIHelper<FilesNamesMakeAbsoluteDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ResultType Type { get { return UIHelper<FilesNamesMakeAbsoluteDialog>.GetPropValue<ResultType>(this); } set { UIHelper<FilesNamesMakeAbsoluteDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckType { get { return UIHelper<FilesNamesMakeAbsoluteDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesNamesMakeAbsoluteDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static FilesNamesMakeAbsoluteDialog()
		{
			UIHelper<FilesNamesMakeAbsoluteDialog>.Register();
			UIHelper<FilesNamesMakeAbsoluteDialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.SetIsFile());
		}

		FilesNamesMakeAbsoluteDialog(NEVariables variables, bool getType)
		{
			Variables = variables;
			InitializeComponent();
			CheckType = getType;
			Expression = "f";
		}

		void SetIsFile()
		{
			if (!CheckType)
				return;

			try
			{
				var value = new NEExpression(Expression).EvaluateRow<string>(Variables);
				if (value == null)
					return;
				if (File.Exists(value))
					Type = ResultType.File;
				else if (Directory.Exists(value))
					Type = ResultType.Directory;
			}
			catch { }
		}

		Result result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Result { Expression = Expression, Type = Type };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables, bool getType)
		{
			var dialog = new FilesNamesMakeAbsoluteDialog(variables, getType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
