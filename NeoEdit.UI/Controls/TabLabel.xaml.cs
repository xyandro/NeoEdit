using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.UI.Controls
{
	partial class TabLabel
	{
		static readonly Brush FocusedWindowBorderBrush = new SolidColorBrush(Color.FromRgb(31, 113, 216));
		static readonly Brush ActiveWindowBorderBrush = new SolidColorBrush(Color.FromRgb(28, 101, 193));
		static readonly Brush InactiveWindowBorderBrush = Brushes.Transparent;
		static readonly Brush FocusedWindowBackgroundBrush = new SolidColorBrush(Color.FromRgb(23, 81, 156));
		static readonly Brush ActiveWindowBackgroundBrush = new SolidColorBrush(Color.FromRgb(14, 50, 96));
		static readonly Brush InactiveWindowBackgroundBrush = Brushes.Transparent;

		static TabLabel()
		{
			FocusedWindowBorderBrush.Freeze();
			ActiveWindowBorderBrush.Freeze();
			InactiveWindowBorderBrush.Freeze();
			FocusedWindowBackgroundBrush.Freeze();
			ActiveWindowBackgroundBrush.Freeze();
			InactiveWindowBackgroundBrush.Freeze();
		}

		public TabLabel(ITab tab)
		{
			Tab = tab;
			InitializeComponent();
			close.Style = FindResource(ToolBar.ButtonStyleKey) as Style;
		}

		public ITab Tab { get; }

		public void Refresh(IReadOnlyList<ITab> activeTabs, ITab focusedTab)
		{
			if (focusedTab == Tab)
			{
				border.BorderBrush = FocusedWindowBorderBrush;
				border.Background = FocusedWindowBackgroundBrush;
			}
			else if (activeTabs.Contains(Tab))
			{
				border.BorderBrush = ActiveWindowBorderBrush;
				border.Background = ActiveWindowBackgroundBrush;
			}
			else
			{
				border.BorderBrush = InactiveWindowBorderBrush;
				border.Background = InactiveWindowBackgroundBrush;
			}

			text.Text = Tab.TabLabel;
		}

		public event RoutedEventHandler CloseClicked;

		void OnCloseClick(object sender, RoutedEventArgs e) => CloseClicked?.Invoke(sender, e);
	}
}
