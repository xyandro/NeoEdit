﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NeoEdit.Common;
using NeoEdit.Common.Parsing;

namespace NeoEdit.UI.Controls
{
	public class NEMenuItem : MenuItem
	{
		class NEMenuItemCommand : RoutedCommand
		{
			public readonly NECommand Command;
			public readonly string InputGestureText;
			public readonly List<KeyGestureAttribute> KeyGestures = new List<KeyGestureAttribute>();
			public bool? MultiStatus;

			public NEMenuItemCommand(NECommand command)
			{
				Command = command;

				var memberInfo = typeof(NECommand).GetMember(command.ToString()).First();
				KeyGestures = memberInfo.GetCustomAttributes(typeof(KeyGestureAttribute), false).Cast<KeyGestureAttribute>().OrderBy(key => key.Order).ToList();
				if (KeyGestures.Duplicate(gesture => gesture.Order).Any())
					throw new Exception($"Duplicate order for command {command}");
				InputGestureText = string.Join(", ", KeyGestures.Select(keyGesture => keyGesture.GestureText));
			}

			public void RegisterCommand(UIElement window, Action<NECommand, bool?> handler)
			{
				window.CommandBindings.Add(new CommandBinding(this, (s, e) => handler(Command, MultiStatus)));
				foreach (var keyGesture in KeyGestures)
					window.InputBindings.Add(new KeyBinding(this, new KeyGesture(keyGesture.Key, keyGesture.Modifiers)));
			}
		}

		public bool? MultiStatus
		{
			get => command?.MultiStatus;
			set
			{
				if (command != null)
				{
					command.MultiStatus = value;
					switch (command.MultiStatus)
					{
						case true: Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.UI;component/Resources/Checked.png")) }; break;
						case false: Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.UI;component/Resources/Unchecked.png")) }; break;
						case null: Icon = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.UI;component/Resources/Indeterminate.png")) }; break;
						default: throw new Exception("Invalid");
					}
				}
			}
		}
		NEMenuItemCommand command { get; set; }

		static NEMenuItem()
		{
			commands = Enum.GetValues(typeof(NECommand)).Cast<NECommand>().ToDictionary(commandEnum => commandEnum, commandEnum => new NEMenuItemCommand(commandEnum));
			var duplicates = commands.Values.SelectMany(command => command.KeyGestures).GroupBy(keyGesture => keyGesture.GestureText).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			if (duplicates.Any())
				throw new Exception($"Duplicate hotkeys: {string.Join(", ", duplicates)}");
		}

		public NECommand CommandEnum
		{
			get { return (Command as NEMenuItemCommand).Command; }
			set
			{
				command = commands[value];
				Command = command;
				InputGestureText = command.InputGestureText;
			}
		}

		static Dictionary<NECommand, NEMenuItemCommand> commands;
		static public void RegisterCommands(UIElement window, Action<NECommand, bool?> handler)
		{
			foreach (var command in commands.Values)
				command.RegisterCommand(window, handler);
		}
	}
}
