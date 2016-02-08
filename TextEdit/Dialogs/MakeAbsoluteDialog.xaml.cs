using System.IO;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.GUI.Controls;

namespace NeoEdit.TextEdit.Dialogs
{
	internal partial class MakeAbsoluteDialog
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
		public string Expression { get { return UIHelper<MakeAbsoluteDialog>.GetPropValue<string>(this); } set { UIHelper<MakeAbsoluteDialog>.SetPropValue(this, value); } }
		[DepProp]
		public ResultType Type { get { return UIHelper<MakeAbsoluteDialog>.GetPropValue<ResultType>(this); } set { UIHelper<MakeAbsoluteDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool CheckType { get { return UIHelper<MakeAbsoluteDialog>.GetPropValue<bool>(this); } set { UIHelper<MakeAbsoluteDialog>.SetPropValue(this, value); } }
		public NEVariables Variables { get; }

		static MakeAbsoluteDialog()
		{
			UIHelper<MakeAbsoluteDialog>.Register();
			UIHelper<MakeAbsoluteDialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.SetIsFile());
		}

		MakeAbsoluteDialog(NEVariables variables, bool getType)
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
			DialogResult = true;
		}

		public static Result Run(Window parent, NEVariables variables, bool getType)
		{
			var dialog = new MakeAbsoluteDialog(variables, getType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
