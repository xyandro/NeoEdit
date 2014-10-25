using System.Text.RegularExpressions;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Disk.Dialogs
{
	public partial class ExpressionDialog
	{
		[DepProp]
		public string Expression { get { return UIHelper<ExpressionDialog>.GetPropValue<string>(this); } private set { UIHelper<ExpressionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool IsRegEx { get { return UIHelper<ExpressionDialog>.GetPropValue<bool>(this); } private set { UIHelper<ExpressionDialog>.SetPropValue(this, value); } }

		Regex result;

		static ExpressionDialog() { UIHelper<ExpressionDialog>.Register(); }

		ExpressionDialog()
		{
			InitializeComponent();
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			var regex = Expression;
			if (!IsRegEx)
				regex = "^(" + Regex.Escape(regex).Replace(@"\*", @".*").Replace(@"\?", ".?").Replace(";", "|") + ")$";
			result = new Regex(regex, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.Multiline | RegexOptions.IgnoreCase);

			DialogResult = true;
		}

		public static Regex Run()
		{
			var dialog = new ExpressionDialog();
			return dialog.ShowDialog() == true ? dialog.result : null;
		}
	}
}
