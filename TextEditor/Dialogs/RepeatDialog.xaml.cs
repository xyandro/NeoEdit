using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.TextEditor.Dialogs
{
	internal partial class RepeatDialog : Window
	{
		public class Response
		{
			public int RepeatCount { get; private set; }
			public bool SelectAll { get; private set; }

			public Response(int repeatCount, bool selectAll)
			{
				RepeatCount = repeatCount;
				SelectAll = selectAll;
			}
		}

		[DepProp]
		public int RepeatCount { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SelectAll { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static RepeatDialog() { UIHelper<RepeatDialog>.Register(); }

		readonly UIHelper<RepeatDialog> uiHelper;
		RepeatDialog(bool selectAll)
		{
			uiHelper = new UIHelper<RepeatDialog>(this);
			InitializeComponent();

			RepeatCount = 1;
			SelectAll = selectAll;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		static public Response Run(bool selectAll)
		{
			var dialog = new RepeatDialog(selectAll);
			if (dialog.ShowDialog() != true)
				return null;

			return new Response(dialog.RepeatCount, dialog.SelectAll);
		}
	}
}
