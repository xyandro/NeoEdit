using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.UI.Controls
{
	partial class NEFileLabel
	{
		static readonly Brush FocusedWindowBorderBrush = new SolidColorBrush(Color.FromRgb(31, 113, 216));
		static readonly Brush ActiveWindowBorderBrush = new SolidColorBrush(Color.FromRgb(28, 101, 193));
		static readonly Brush InactiveWindowBorderBrush = Brushes.Transparent;
		static readonly Brush FocusedWindowBackgroundBrush = new SolidColorBrush(Color.FromRgb(23, 81, 156));
		static readonly Brush ActiveWindowBackgroundBrush = new SolidColorBrush(Color.FromRgb(14, 50, 96));
		static readonly Brush InactiveWindowBackgroundBrush = Brushes.Transparent;

		static NEFileLabel()
		{
			FocusedWindowBorderBrush.Freeze();
			ActiveWindowBorderBrush.Freeze();
			InactiveWindowBorderBrush.Freeze();
			FocusedWindowBackgroundBrush.Freeze();
			ActiveWindowBackgroundBrush.Freeze();
			InactiveWindowBackgroundBrush.Freeze();
		}

		public NEFileLabel(INEFile neFile)
		{
			NEFile = neFile;
			InitializeComponent();
			close.Style = FindResource(ToolBar.ButtonStyleKey) as Style;
		}

		public INEFile NEFile { get; }

		public void Refresh(RenderParameters renderParameters)
		{
			if (renderParameters.FocusedFile == NEFile)
			{
				border.BorderBrush = FocusedWindowBorderBrush;
				border.Background = FocusedWindowBackgroundBrush;
			}
			else if (renderParameters.ActiveFiles.Contains(NEFile))
			{
				border.BorderBrush = ActiveWindowBorderBrush;
				border.Background = ActiveWindowBackgroundBrush;
			}
			else
			{
				border.BorderBrush = InactiveWindowBorderBrush;
				border.Background = InactiveWindowBackgroundBrush;
			}

			text.Text = NEFile.NEFileLabel;
		}

		public event RoutedEventHandler CloseClicked;

		void OnCloseClick(object sender, RoutedEventArgs e) => CloseClicked?.Invoke(sender, e);
	}
}
