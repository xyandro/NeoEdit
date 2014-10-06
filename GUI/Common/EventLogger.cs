﻿using System;
using System.Windows;

namespace NeoEdit.GUI.Common
{
	public static class EventLogger<T>
	{
		static bool enabled = false, hideCanExecute;
		public static void LogEvents(bool _hideCanExecute = true)
		{
			if (enabled)
				return;
			hideCanExecute = _hideCanExecute;

			foreach (var routedEvent in EventManager.GetRoutedEvents())
				EventManager.RegisterClassHandler(typeof(T), routedEvent, new RoutedEventHandler(handler));
		}

		internal static void handler(object sender, RoutedEventArgs e)
		{
			if ((!hideCanExecute) || ((e.RoutedEvent.ToString() != "CommandManager.PreviewCanExecute") && (e.RoutedEvent.ToString() != "CommandManager.CanExecute")))
				Console.WriteLine(e.OriginalSource + "=>" + e.RoutedEvent);
		}
	}
}
