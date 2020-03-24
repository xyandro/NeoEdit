using System.Windows.Controls;
using System.Windows.Media;

namespace NeoEdit.UI.Controls
{
	public class RenderCanvas : Canvas
	{
		public delegate void RenderDelegate(object s, DrawingContext dc);
		public event RenderDelegate Render;

		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			Render?.Invoke(this, dc);
		}
	}
}
