using System.Windows.Controls;
using System.Windows.Media;

namespace NeoEdit.TextEdit.Controls
{
	public class RenderCanvas : Canvas
	{
		public delegate void RenderDelegate(object s, DrawingContext dc);
		RenderDelegate renderDelegate = (s, dc) => { };
		public event RenderDelegate Render { add { renderDelegate += value; } remove { renderDelegate -= value; } }

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			renderDelegate(this, dc);
		}
	}
}
