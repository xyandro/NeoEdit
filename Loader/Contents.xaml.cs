using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Loader
{
	partial class Contents
	{
		public static DependencyProperty ResourceItemsProperty = DependencyProperty.Register(nameof(ResourceItems), typeof(ObservableCollection<Resource>), typeof(Contents));
		public static DependencyProperty StartProperty = DependencyProperty.Register(nameof(Start), typeof(string), typeof(Contents));
		public static DependencyProperty Extract32Property = DependencyProperty.Register(nameof(Extract32), typeof(bool), typeof(Contents));
		public static DependencyProperty Extract64Property = DependencyProperty.Register(nameof(Extract64), typeof(bool), typeof(Contents));

		public ObservableCollection<Resource> ResourceItems { get { return (ObservableCollection<Resource>)GetValue(ResourceItemsProperty); } set { SetValue(ResourceItemsProperty, value); } }
		public string Start { get { return (string)GetValue(StartProperty); } set { SetValue(StartProperty, value); } }
		public bool Extract32 { get { return (bool)GetValue(Extract32Property); } set { SetValue(Extract32Property, value); } }
		public bool Extract64 { get { return (bool)GetValue(Extract64Property); } set { SetValue(Extract64Property, value); } }
		public ExtractActions ExtractAction { get; private set; }

		Contents()
		{
			Dispatcher.UnhandledException += Dispatcher_UnhandledException;
			EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotFocusEvent, new RoutedEventHandler((s, e2) => (s as TextBox).SelectAll()));
			DataContext = this;
			InitializeComponent();
			ResourceItems = new ObservableCollection<Resource>(ResourceReader.AllResources);
			if (Environment.Is64BitProcess)
				Extract64 = true;
			else
				Extract32 = true;
			Start = ResourceReader.Config.X64Start ?? ResourceReader.Config.X32Start;
		}

		private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}

		void ExtractRunClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			ExtractAction = sender == extractButton ? ExtractActions.Extract : ExtractActions.GUI;
		}

		public static Tuple<ExtractActions, BitDepths> Run()
		{
			var dialog = new Contents();
			if (dialog.ShowDialog() != true)
				return null;
			return Tuple.Create(dialog.ExtractAction, dialog.Extract64 ? BitDepths.x64 : BitDepths.x32);
		}
	}
}
