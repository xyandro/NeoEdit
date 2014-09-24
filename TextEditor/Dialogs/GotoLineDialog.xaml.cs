using System.Windows;
using System.Windows.Controls;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	public partial class GotoLineDialog : Window
	{
		[DepProp]
		public int Line { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MinLine { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public int MaxLine { get { return uiHelper.GetPropValue<int>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool Relative { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		static GotoLineDialog() { UIHelper<GotoLineDialog>.Register(); }

		readonly UIHelper<GotoLineDialog> uiHelper;
		readonly int numLines, startLine;
		GotoLineDialog(int _numLines, int _startLine)
		{
			numLines = _numLines;
			startLine = _startLine + 1;

			uiHelper = new UIHelper<GotoLineDialog>(this);
			InitializeComponent();

			uiHelper.AddCallback(a => a.Relative, (o, n) => SetRelative());

			okClick.Click += (s, e) =>
			{
				if (Validation.GetHasError(line))
					return;
				DialogResult = true;
			};

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

		public static int? Run(int numLines, int line)
		{
			var d = new GotoLineDialog(numLines, line);
			if (d.ShowDialog() != true)
				return null;
			line = d.Line - 1;
			if (d.Relative)
				line += d.startLine;
			return line;
		}
	}
}
