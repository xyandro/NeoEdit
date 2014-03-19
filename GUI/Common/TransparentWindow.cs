using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace NeoEdit.GUI.Common
{
	public class TransparentWindow : Window
	{
		public static Style GetStyle()
		{
			var resourceDict = new ResourceDictionary { Source = new Uri("/GUI;component/Common/TransparentWindow.xaml", UriKind.RelativeOrAbsolute) };
			return resourceDict["TransparentWindowStyle"] as Style;
		}

		public static BitmapFrame GetAppIcon(int size)
		{
			var decoder = BitmapDecoder.Create(new Uri("pack://application:,,,/NeoEdit.ico"), BitmapCreateOptions.DelayCreation, BitmapCacheOption.OnDemand);
			var result = decoder.Frames.SingleOrDefault(f => f.Width == size);
			if (result == null)
				result = decoder.Frames.OrderBy(f => f.Width).First();
			return result;
		}

		public TransparentWindow()
		{
			Style = GetStyle();
			Icon = GetAppIcon(16);

			this.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, this.OnCloseWindow));
			this.CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, this.OnMaximizeWindow, this.OnCanResizeWindow));
			this.CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, this.OnMinimizeWindow, this.OnCanMinimizeWindow));
			this.CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, this.OnRestoreWindow, this.OnCanResizeWindow));
		}

		void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = this.ResizeMode == ResizeMode.CanResize || this.ResizeMode == ResizeMode.CanResizeWithGrip;
		}

		void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = this.ResizeMode != ResizeMode.NoResize;
		}

		void OnCloseWindow(object target, ExecutedRoutedEventArgs e)
		{
			SystemCommands.CloseWindow(this);
		}

		void OnMaximizeWindow(object target, ExecutedRoutedEventArgs e)
		{
			SystemCommands.MaximizeWindow(this);
		}

		void OnMinimizeWindow(object target, ExecutedRoutedEventArgs e)
		{
			SystemCommands.MinimizeWindow(this);
		}

		void OnRestoreWindow(object target, ExecutedRoutedEventArgs e)
		{
			SystemCommands.RestoreWindow(this);
		}
	}
}
