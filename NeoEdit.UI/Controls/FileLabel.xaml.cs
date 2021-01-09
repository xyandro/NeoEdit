using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NeoEdit.Common;

namespace NeoEdit.UI.Controls
{
	partial class NEFileLabel
	{
		static readonly Brush FocusedBorderBrushAll = new SolidColorBrush(Color.FromRgb(31, 113, 216));
		static readonly Brush ActiveBorderBrushAll = new SolidColorBrush(Color.FromRgb(28, 101, 193));
		static readonly Brush InactiveBorderBrushAll = Brushes.Transparent;
		static readonly Brush FocusedBackgroundBrushAll = new SolidColorBrush(Color.FromRgb(23, 81, 156));
		static readonly Brush ActiveBackgroundBrushAll = new SolidColorBrush(Color.FromRgb(14, 50, 96));
		static readonly Brush InactiveBackgroundBrushAll = Brushes.Transparent;
		static readonly Brush FocusedBorderBrushActiveFirst = new SolidColorBrush(Color.FromRgb(31, 216, 113));
		static readonly Brush ActiveBorderBrushActiveFirst = new SolidColorBrush(Color.FromRgb(28, 193, 101));
		static readonly Brush InactiveBorderBrushActiveFirst = Brushes.Transparent;
		static readonly Brush FocusedBackgroundBrushActiveFirst = new SolidColorBrush(Color.FromRgb(23, 156, 81));
		static readonly Brush ActiveBackgroundBrushActiveFirst = new SolidColorBrush(Color.FromRgb(14, 96, 50));
		static readonly Brush InactiveBackgroundBrushActiveFirst = Brushes.Transparent;

		static NEFileLabel()
		{
			FocusedBorderBrushAll.Freeze();
			ActiveBorderBrushAll.Freeze();
			InactiveBorderBrushAll.Freeze();
			FocusedBackgroundBrushAll.Freeze();
			ActiveBackgroundBrushAll.Freeze();
			InactiveBackgroundBrushAll.Freeze();
			FocusedBorderBrushActiveFirst.Freeze();
			ActiveBorderBrushActiveFirst.Freeze();
			InactiveBorderBrushActiveFirst.Freeze();
			FocusedBackgroundBrushActiveFirst.Freeze();
			ActiveBackgroundBrushActiveFirst.Freeze();
			InactiveBackgroundBrushActiveFirst.Freeze();
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
				border.BorderBrush = renderParameters.ActiveFirst ? FocusedBorderBrushActiveFirst : FocusedBorderBrushAll;
				border.Background = renderParameters.ActiveFirst ? FocusedBackgroundBrushActiveFirst : FocusedBackgroundBrushAll;
			}
			else if (renderParameters.ActiveFiles.Contains(NEFile))
			{
				border.BorderBrush = renderParameters.ActiveFirst ? ActiveBorderBrushActiveFirst : ActiveBorderBrushAll;
				border.Background = renderParameters.ActiveFirst ? ActiveBackgroundBrushActiveFirst : ActiveBackgroundBrushAll;
			}
			else
			{
				border.BorderBrush = renderParameters.ActiveFirst ? InactiveBorderBrushActiveFirst : InactiveBorderBrushAll;
				border.Background = renderParameters.ActiveFirst ? InactiveBackgroundBrushActiveFirst : InactiveBackgroundBrushAll;
			}

			text.Text = NEFile.NEFileLabel;
		}

		public event RoutedEventHandler CloseClicked;

		void OnCloseClick(object sender, RoutedEventArgs e) => CloseClicked?.Invoke(sender, e);
	}
}
