using System;
using System.Windows.Controls;

namespace NeoEdit.GUI.Controls
{
	public class BindableScrollViewer : ScrollViewer
	{
		[DepProp]
		public double HorizontalPosition { get { return UIHelper<BindableScrollViewer>.GetPropValue<double>(this); } set { UIHelper<BindableScrollViewer>.SetPropValue(this, value); } }
		[DepProp]
		public double VerticalPosition { get { return UIHelper<BindableScrollViewer>.GetPropValue<double>(this); } set { UIHelper<BindableScrollViewer>.SetPropValue(this, value); } }
		[DepProp]
		public double HorizontalMax { get { return UIHelper<BindableScrollViewer>.GetPropValue<double>(this); } set { UIHelper<BindableScrollViewer>.SetPropValue(this, value); } }
		[DepProp]
		public double VerticalMax { get { return UIHelper<BindableScrollViewer>.GetPropValue<double>(this); } set { UIHelper<BindableScrollViewer>.SetPropValue(this, value); } }

		static BindableScrollViewer()
		{
			UIHelper<BindableScrollViewer>.Register();
			UIHelper<BindableScrollViewer>.AddCallback(a => a.HorizontalPosition, (obj, o, n) => obj.ScrollToHorizontalOffset(obj.HorizontalPosition));
			UIHelper<BindableScrollViewer>.AddCallback(a => a.VerticalPosition, (obj, o, n) => obj.ScrollToHorizontalOffset(obj.VerticalPosition));
			UIHelper<BindableScrollViewer>.AddCoerce(a => a.HorizontalPosition, (obj, value) => Math.Max(0, Math.Min(value, obj.ScrollableWidth)));
			UIHelper<BindableScrollViewer>.AddCoerce(a => a.VerticalPosition, (obj, value) => Math.Max(0, Math.Min(value, obj.ScrollableHeight)));
		}

		protected override void OnScrollChanged(ScrollChangedEventArgs e)
		{
			HorizontalPosition = HorizontalOffset;
			VerticalPosition = VerticalOffset;
			HorizontalMax = ScrollableWidth;
			VerticalMax = ScrollableHeight;
		}
	}
}
