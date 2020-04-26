using System;
using System.IO;
using System.Windows;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Expressions;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Configure_Files_Name_MakeAbsolute_Dialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.GetPropValue<string>(this); } set { UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public Configuration_Files_Name_MakeAbsolute.ResultType Type { get { return UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.GetPropValue<Configuration_Files_Name_MakeAbsolute.ResultType>(this); } set { UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckType { get { return UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.GetPropValue<bool>(this); } set { UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static Configure_Files_Name_MakeAbsolute_Dialog()
		{
			UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.Register();
			UIHelper<Configure_Files_Name_MakeAbsolute_Dialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.SetIsFile());
		}

		Configure_Files_Name_MakeAbsolute_Dialog(NEVariables variables, bool absolute, bool checkType)
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
					Type = Configuration_Files_Name_MakeAbsolute.ResultType.File;
				else if (Directory.Exists(value))
					Type = Configuration_Files_Name_MakeAbsolute.ResultType.Directory;
			}
			catch { }
		}

		Configuration_Files_Name_MakeAbsolute result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = new Configuration_Files_Name_MakeAbsolute { Expression = Expression, Type = Type };
			expression.AddCurrentSuggestion();
			DialogResult = true;
		}

		public static Configuration_Files_Name_MakeAbsolute Run(Window parent, NEVariables variables, bool absolute, bool checkType)
		{
			var dialog = new Configure_Files_Name_MakeAbsolute_Dialog(variables, absolute, checkType) { Owner = parent };
			if (!dialog.ShowDialog())
				throw new OperationCanceledException();
			return dialog.result;
		}
	}
}
