using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.GUI.Controls
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class KeyGestureAttribute : Attribute
	{
		internal Key Key { get; }
		internal ModifierKeys Modifiers { get; }
		internal int Order { get; }
		internal string GestureText { get; }

		public KeyGestureAttribute(Key key, ModifierKeys modifiers = ModifierKeys.None, int order = 1)
		{
			Key = key;
			Modifiers = modifiers;
			Order = order;
			var mods = new List<string>();
			if ((modifiers & ModifierKeys.Control) != 0)
				mods.Add("Ctrl");
			if ((modifiers & ModifierKeys.Windows) != 0)
				mods.Add("Win");
			if ((modifiers & ModifierKeys.Alt) != 0)
				mods.Add("Alt");
			if ((modifiers & ModifierKeys.Shift) != 0)
				mods.Add("Shift");
			switch (key)
			{
				case Key.D0:
				case Key.D1:
				case Key.D2:
				case Key.D3:
				case Key.D4:
				case Key.D5:
				case Key.D6:
				case Key.D7:
				case Key.D8:
				case Key.D9: mods.Add(key.ToString().Substring(1)); break;
				case Key.OemPlus: mods.Add("+"); break;
				case Key.OemMinus: mods.Add("-"); break;
				case Key.OemPeriod: mods.Add("."); break;
				case Key.OemOpenBrackets: mods.Add("["); break;
				case Key.OemCloseBrackets: mods.Add("]"); break;
				case Key.OemQuestion: mods.Add("/"); break;
				default: mods.Add(key.ToString()); break;
			}
			GestureText = string.Join("+", mods);
		}
	}

	public class NEMenuItem<CommandEnumT> : MenuItem
	{
		class NECommand : RoutedCommand
		{
			public readonly CommandEnumT Command;
			public readonly string InputGestureText;
			public readonly List<KeyGestureAttribute> KeyGestures = new List<KeyGestureAttribute>();
			public bool? MultiStatus;

			public NECommand(CommandEnumT command)
			{
				Command = command;

				var memberInfo = typeof(CommandEnumT).GetMember(command.ToString()).First();
				KeyGestures = memberInfo.GetCustomAttributes(typeof(KeyGestureAttribute), false).Cast<KeyGestureAttribute>().OrderBy(key => key.Order).ToList();
				if (KeyGestures.Duplicate(gesture => gesture.Order).Any())
					throw new Exception($"Duplicate order for command {command}");
				InputGestureText = string.Join(", ", KeyGestures.Select(keyGesture => keyGesture.GestureText));
			}

			public void RegisterCommand(UIElement window, Action<CommandEnumT, bool?> handler)
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
			commands = Enum.GetValues(typeof(CommandEnumT)).Cast<CommandEnumT>().ToDictionary(commandEnum => commandEnum, commandEnum => new NECommand(commandEnum));
			var duplicates = commands.Values.SelectMany(command => command.KeyGestures).GroupBy(keyGesture => keyGesture.GestureText).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			if (duplicates.Any())
				throw new Exception($"Duplicate hotkeys: {string.Join(", ", duplicates)}");
		}

		public CommandEnumT CommandEnum
		{
			get { return (Command as NECommand).Command; }
			set
			{
				neCommand = commands[value];
				Command = neCommand;
				InputGestureText = neCommand.InputGestureText;
			}
		}

		static Dictionary<CommandEnumT, NECommand> commands;
		static public void RegisterCommands(UIElement window, Action<CommandEnumT, bool?> handler)
		{
			foreach (var command in commands.Values)
				command.RegisterCommand(window, handler);
		}
	}
}
