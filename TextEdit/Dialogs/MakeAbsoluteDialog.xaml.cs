using System.Collections.Generic;
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
		public Dictionary<string, List<object>> ExpressionData { get; private set; }

		static MakeAbsoluteDialog()
		{
			UIHelper<MakeAbsoluteDialog>.Register();
			UIHelper<MakeAbsoluteDialog>.AddCallback(a => a.Expression, (obj, o, n) => obj.SetIsFile());
		}

		MakeAbsoluteDialog(Dictionary<string, List<object>> expressionData, bool getType)
		{
			ExpressionData = expressionData;
			InitializeComponent();
			CheckType = getType;
		}

		void SetIsFile()
		{
			if (!CheckType)
				return;

			try
			{
				var neExpression = new NEExpression(Expression);
				var value = neExpression.EvaluateRow(ExpressionData, 0);
				var path = value as string;
				if (path == null)
					return;
				if (File.Exists(path))
					Type = ResultType.File;
				else if (Directory.Exists(path))
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

		public static Result Run(Window parent, Dictionary<string, List<object>> expressionData, bool getType)
		{
			var dialog = new MakeAbsoluteDialog(expressionData, getType) { Owner = parent };
			return dialog.ShowDialog() ? dialog.result : null;
		}
	}
}
