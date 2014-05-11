using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace NeoEdit.TextEditor
{
	partial class TextCanvas : Canvas
	{
		Action<DrawingContext> render;
		public TextCanvas()
		{
			InitializeComponent();
		}

		public void Initialize(Action<DrawingContext> _render)
		{
			render = _render;
		}

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			render(dc);
		}
	}
}
