using System.IO;
using System.Windows;
using NeoEdit.Common.Controls;
using NeoEdit.Common.Expressions;

namespace NeoEdit.Common.Dialogs
{
	partial class FilesNamesMakeAbsoluteRelativeDialog
	{
		public enum ResultType
		{
			None,
			File,
			Directory,
		}

		public class Result
		{
			public string Expression { get; set; }
			public ResultType Type { get; set; }
		}

		[DepProp]
		public string Expression { get { return UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.GetPropValue<string>(this); } set { UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ResultType Type { get { return UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.GetPropValue<ResultType>(this); } set { UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckType { get { return UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.GetPropValue<bool>(this); } set { UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static FilesNamesMakeAbsoluteRelativeDialog()
		{
			UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.Register();
			UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.SetIsFile());
		}

		FilesNamesMakeAbsoluteRelativeDialog(NEVariables variables, bool absolute, bool checkType)
		{
			Variables = variables;
			InitializeComponent();
			Title = $"Make {(absolute ? "Absolute" : "Relative")}";
			CheckType = checkType;
			Expression = "f";
		}

		void SetIsFile()
		{
			if (!CheckType)
				return;

			try
			{
				var value = new NEExpression(Expression).Evaluate<string>(Variables);
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

		public static Result Run(Window parent, NEVariables variables, bool absolute, bool checkType)
		{
			var dialog = new FilesNamesMakeAbsoluteRelativeDialog(variables, absolute, checkType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
