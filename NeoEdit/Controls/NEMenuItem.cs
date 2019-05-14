using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit;

namespace NeoEdit.Controls
{
	public class NEMenuItem : MenuItem
	{
		class NECommand : RoutedCommand
		{
			public readonly TextEditCommand Command;
			public readonly string InputGestureText;
			public readonly List<KeyGestureAttribute> KeyGestures = new List<KeyGestureAttribute>();
			public bool? MultiStatus;

			public NECommand(TextEditCommand command)
			{
				Command = command;

				var memberInfo = typeof(TextEditCommand).GetMember(command.ToString()).First();
				KeyGestures = memberInfo.GetCustomAttributes(typeof(KeyGestureAttribute), false).Cast<KeyGestureAttribute>().OrderBy(key => key.Order).ToList();
				if (KeyGestures.Duplicate(gesture => gesture.Order).Any())
					throw new Exception($"Duplicate order for command {command}");
				InputGestureText = string.Join(", ", KeyGestures.Select(keyGesture => keyGesture.GestureText));
			}

			public void RegisterCommand(UIElement window, Action<TextEditCommand, bool?> handler)
			{
				window.CommandBindings.Add(new CommandBinding(this, (s, e) => handler(Command, MultiStatus)));
				foreach (var keyGesture in KeyGestures)
					window.InputBindings.Add(new KeyBinding(this, new KeyGesture(keyGesture.Key, keyGesture.Modifiers)));
			}
		}

		public bool? MultiStatus { get { return neCommand?.MultiStatus; } protected set { if (neCommand != null) neCommand.MultiStatus = value; } }
		NECommand neCommand { get; set; }

		static NEMenuItem()
		{
			commands = Enum.GetValues(typeof(TextEditCommand)).Cast<TextEditCommand>().ToDictionary(commandEnum => commandEnum, commandEnum => new NECommand(commandEnum));
			var duplicates = commands.Values.SelectMany(command => command.KeyGestures).GroupBy(keyGesture => keyGesture.GestureText).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			if (duplicates.Any())
				throw new Exception($"Duplicate hotkeys: {string.Join(", ", duplicates)}");
		}

		public TextEditCommand CommandEnum
		{
			get { return (Command as NECommand).Command; }
			set
			{
				neCommand = commands[value];
				Command = neCommand;
				InputGestureText = neCommand.InputGestureText;
			}
		}

		static Dictionary<TextEditCommand, NECommand> commands;
		static public void RegisterCommands(UIElement window, Action<TextEditCommand, bool?> handler)
		{
			foreach (var command in commands.Values)
				command.RegisterCommand(window, handler);
		}
	}
}
