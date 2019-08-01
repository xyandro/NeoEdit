using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.Program.Controls
{
	public static class ScrollViewerBinding
	{
		public static readonly DependencyProperty VerticalOffsetProperty = DependencyProperty.RegisterAttached("VerticalOffset", typeof(double), typeof(ScrollViewerBinding), new PropertyMetadata(0.0, OnVerticalOffsetPropertyChanged));
		public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.RegisterAttached("HorizontalOffset", typeof(double), typeof(ScrollViewerBinding), new PropertyMetadata(0.0, OnHorizontalOffsetPropertyChanged));

		private static void OnVerticalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ScrollViewer sv)
				sv.ScrollToVerticalOffset((double)e.NewValue);
		}

		private static void OnHorizontalOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is ScrollViewer sv)
				sv.ScrollToHorizontalOffset((double)e.NewValue);
		}

	}
}
