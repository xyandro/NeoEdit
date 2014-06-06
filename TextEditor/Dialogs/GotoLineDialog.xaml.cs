using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

			Loaded += (s, e) => line.SelectAll();

			uiHelper.AddCallback(a => a.Line, (o, n) => Line = Math.Max(MinLine, Math.Min(Line, MaxLine)));
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
			Line -= MinLine;
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
			Line += MinLine;
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			e.Handled = true;
			switch (e.Key)
			{
				case Key.Up: ++Line; break;
				case Key.Down: --Line; break;
				default: e.Handled = false; break;
			}
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
