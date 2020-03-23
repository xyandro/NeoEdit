using System;
using System.IO;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Models;

namespace NeoEdit.Program.Dialogs
{
	partial class FilesNamesMakeAbsoluteRelativeDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.GetPropValue<string>(this); } set { UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.SetPropValue(this, value); } }
		[DepProp]
		public FilesNamesMakeAbsoluteRelativeDialogResult.ResultType Type { get { return UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.GetPropValue<FilesNamesMakeAbsoluteRelativeDialogResult.ResultType>(this); } set { UIHelper<FilesNamesMakeAbsoluteRelativeDialog>.SetPropValue(this, value); } }
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
					Type = FilesNamesMakeAbsoluteRelativeDialogResult.ResultType.File;
				else if (Directory.Exists(value))
					Type = FilesNamesMakeAbsoluteRelativeDialogResult.ResultType.Directory;
			}
			catch { }
		}

		FilesNamesMakeAbsoluteRelativeDialogResult result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new FilesNamesMakeAbsoluteRelativeDialogResult { Expression = Expression, Type = Type };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static FilesNamesMakeAbsoluteRelativeDialogResult Run(Window parent, NEVariables variables, bool absolute, bool checkType)
		{
			var dialog = new FilesNamesMakeAbsoluteRelativeDialog(variables, absolute, checkType) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
