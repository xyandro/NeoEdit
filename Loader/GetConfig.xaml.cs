using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;

namespace NeoEdit.Loader
{
	partial class GetConfig
	{
		readonly Config config;

		GetConfig(Config config)
		{
			Dispatcher.UnhandledException += Dispatcher_UnhandledException;
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			DataContext = this.config = config;
			InitializeComponent();
			password.Password = confirm.Password = config.Password;
		}

		void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}

		void BrowseClick(object sender, RoutedEventArgs e)
		{
			if ((sender == x32StartBrowse) || (sender == x64StartBrowse))
			{
				var dialog = new OpenFileDialog
				{
					DefaultExt = "exe",
					Filter = "Programs|*.exe|All files|*.*",
					InitialDirectory = sender == x32StartBrowse ? config.X32StartFull : config.X64StartFull,
				};
				if (dialog.ShowDialog() == true)
					config.SetStart(dialog.FileName);
			}
			else if ((sender == x32PathBrowse) || (sender == x64PathBrowse))
			{
				var dialog = new System.Windows.Forms.FolderBrowserDialog
				{
					SelectedPath = sender == x32PathBrowse ? config.X32Path ?? config.X64Path : config.X64Path ?? config.X32Path,
					ShowNewFolderButton = false,
				};
				var result = dialog.ShowDialog();
				if (result == System.Windows.Forms.DialogResult.OK)
					config.SetPath(dialog.SelectedPath);
			}
			else if (sender == outputBrowse)
			{
				var dialog = new SaveFileDialog
				{
					DefaultExt = "exe",
					Filter = "Programs|*.exe|All files|*.*",
					FileName = Path.GetFileName(config.X64Start ?? config.X32Start),
					InitialDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
				};
				if (dialog.ShowDialog() == true)
					config.Output = dialog.FileName;
			}
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			if (password.Password != confirm.Password)
				throw new Exception("Passwords must match");

			config.Password = password.Password;
			DialogResult = true;
		}

		public static bool Run(Config config) => new GetConfig(config).ShowDialog() == true;
	}
}
