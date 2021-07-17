using System;
using System.IO;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Files_Name_MakeAbsoluteRelative_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.GetPropValue<string>(this); } set { UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Configuration_Files_Name_MakeAbsoluteRelative.ResultType Type { get { return UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.GetPropValue<Configuration_Files_Name_MakeAbsoluteRelative.ResultType>(this); } set { UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckType { get { return UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Files_Name_MakeAbsoluteRelative_Dialog()
		{
			UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.Register();
			UIHelper<Files_Name_MakeAbsoluteRelative_Dialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.SetIsFile());
		}

		Files_Name_MakeAbsoluteRelative_Dialog(NEVariables variables, bool absolute, bool checkType)
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
				var value = new NEExpression(Expression).EvaluateOne<string>(Variables);
				if (value == null)
					return;
				if (File.Exists(value))
					Type = Configuration_Files_Name_MakeAbsoluteRelative.ResultType.File;
				else if (Directory.Exists(value))
					Type = Configuration_Files_Name_MakeAbsoluteRelative.ResultType.Directory;
			}
			catch { }
		}

		Configuration_Files_Name_MakeAbsoluteRelative result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Name_MakeAbsoluteRelative { Expression = Expression, Type = Type };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Files_Name_MakeAbsoluteRelative Run(Window parent, NEVariables variables, bool absolute, bool checkType)
		{
			var dialog = new Files_Name_MakeAbsoluteRelative_Dialog(variables, absolute, checkType) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
