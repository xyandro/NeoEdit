using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	internal partial class GotoLineDialog
	{
		[DepProp]
		public int Line { get { return UIHelper<GotoLineDialog>.GetPropValue<int>(this); } set { UIHelper<GotoLineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MinLine { get { return UIHelper<GotoLineDialog>.GetPropValue<int>(this); } set { UIHelper<GotoLineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public int MaxLine { get { return UIHelper<GotoLineDialog>.GetPropValue<int>(this); } set { UIHelper<GotoLineDialog>.SetPropValue(this, value); } }
		[DepProp]
		public bool Relative { get { return UIHelper<GotoLineDialog>.GetPropValue<bool>(this); } set { UIHelper<GotoLineDialog>.SetPropValue(this, value); } }

		static GotoLineDialog()
		{
			UIHelper<GotoLineDialog>.Register();
			UIHelper<GotoLineDialog>.AddCallback(a => a.Relative, (obj, o, n) => obj.SetRelative());
		}

		readonly int numLines, startLine;
		GotoLineDialog(int _numLines, int _startLine)
		{
			numLines = _numLines;
			startLine = _startLine + 1;

			InitializeComponent();

			SetRelative();
			Line = startLine;
		}

		void SetRelative()
		{
			var line = Line - MinLine;
			if (Relative)
			{
				MinLine = -startLine + 1;
				MaxLine = numLines - startLine;
			}
			else
			{
				MinLine = 1;
				MaxLine = numLines;
			}
			Line = line + MinLine;
		}

		int result;
		void OkClick(object sender, RoutedEventArgs e)
		{
			result = Line - 1;
			if (Relative)
				result += startLine;
			DialogResult = true;
		}

		public static int? Run(int numLines, int result)
		{
			var dialog = new GotoLineDialog(numLines, result);
			return dialog.ShowDialog() == true ? (int?)dialog.result : null;
		}
	}
}
