using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	internal partial class GotoLineDialog
	{
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinLine { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxLine { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Relative { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static GotoLineDialog()
		{
			UIHelper<GotoLineDialog>.Register();
			UIHelper<GotoLineDialog>.AddCallback(a => a.Relative, (obj, o, n) => obj.SetRelative());
		}

		readonly UIHelper<GotoLineDialog> uiHelper;
		readonly int numLines, startLine;
		GotoLineDialog(int _numLines, int _startLine)
		{
			numLines = _numLines;
			startLine = _startLine + 1;

			uiHelper = new UIHelper<GotoLineDialog>(this);
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
