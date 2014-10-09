using System;
using System.Windows;

namespace NeoEdit.GUI.Common
{
	public static class EventLogger<T>
	{
		static bool enabled = false, hideCanExecute;
		public static void LogEvents(string prefix = "Event", bool _hideCanExecute = true)
		{
			if (enabled)
				return;
			hideCanExecute = _hideCanExecute;

			foreach (var routedEvent in EventManager.GetRoutedEvents())
				EventManager.RegisterClassHandler(typeof(T), routedEvent, new RoutedEventHandler((s, e) => handler(prefix, e)), true);
		}

		internal static void handler(string prefix, RoutedEventArgs e)
		{
			if ((!hideCanExecute) || ((e.RoutedEvent.ToString() != "CommandManager.PreviewCanExecute") && (e.RoutedEvent.ToString() != "CommandManager.CanExecute")))
				Console.WriteLine(String.Format("{0}: {1} => {2}", prefix, e.OriginalSource, e.RoutedEvent));
		}
	}
}
