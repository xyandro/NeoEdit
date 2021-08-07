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
		static readonly Brush FocusedBorderBrushWorkMode = new SolidColorBrush(Color.FromRgb(31, 216, 113));
		static readonly Brush ActiveBorderBrushWorkMode = new SolidColorBrush(Color.FromRgb(28, 193, 101));
		static readonly Brush InactiveBorderBrushWorkMode = Brushes.Transparent;
		static readonly Brush FocusedBackgroundBrushWorkMode = new SolidColorBrush(Color.FromRgb(23, 156, 81));
		static readonly Brush ActiveBackgroundBrushWorkMode = new SolidColorBrush(Color.FromRgb(14, 96, 50));
		static readonly Brush InactiveBackgroundBrushWorkMode = Brushes.Transparent;

		static NEFileLabel()
		{
			FocusedBorderBrushAll.Freeze();
			ActiveBorderBrushAll.Freeze();
			InactiveBorderBrushAll.Freeze();
			FocusedBackgroundBrushAll.Freeze();
			ActiveBackgroundBrushAll.Freeze();
			InactiveBackgroundBrushAll.Freeze();
			FocusedBorderBrushWorkMode.Freeze();
			ActiveBorderBrushWorkMode.Freeze();
			InactiveBorderBrushWorkMode.Freeze();
			FocusedBackgroundBrushWorkMode.Freeze();
			ActiveBackgroundBrushWorkMode.Freeze();
			InactiveBackgroundBrushWorkMode.Freeze();
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
				border.BorderBrush = renderParameters.WorkMode ? FocusedBorderBrushWorkMode : FocusedBorderBrushAll;
				border.Background = renderParameters.WorkMode ? FocusedBackgroundBrushWorkMode : FocusedBackgroundBrushAll;
			}
			else if (renderParameters.ActiveFiles.Contains(NEFile))
			{
				border.BorderBrush = renderParameters.WorkMode ? ActiveBorderBrushWorkMode : ActiveBorderBrushAll;
				border.Background = renderParameters.WorkMode ? ActiveBackgroundBrushWorkMode : ActiveBackgroundBrushAll;
			}
			else
			{
				border.BorderBrush = renderParameters.WorkMode ? InactiveBorderBrushWorkMode : InactiveBorderBrushAll;
				border.Background = renderParameters.WorkMode ? InactiveBackgroundBrushWorkMode : InactiveBackgroundBrushAll;
			}

			text.Text = NEFile.NEFileLabel;
		}

		public event RoutedEventHandler CloseClicked;

		void OnCloseClick(object sender, RoutedEventArgs e) => CloseClicked?.Invoke(sender, e);
	}
}
