using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.GUI.Controls
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class KeyGestureAttribute : Attribute
	{
		internal Key Key { get; private set; }
		internal ModifierKeys Modifiers { get; private set; }
		internal bool Primary { get; private set; }
		internal int CommandSet { get; private set; }
		internal string GestureText { get; private set; }

		public KeyGestureAttribute(Key key, ModifierKeys modifiers = ModifierKeys.None, bool primary = true, int commandSet = 0)
		{
			Key = key;
			Modifiers = modifiers;
			Primary = primary;
			CommandSet = commandSet;
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
				default: mods.Add(key.ToString()); break;
			}
			GestureText = String.Join("+", mods);
		}
	}

	public class NEMenuItem<CommandEnumT> : MenuItem
	{
		class NECommand : RoutedCommand
		{
			public readonly CommandEnumT Command;
			public readonly string InputGestureText;
			public readonly List<KeyGestureAttribute> KeyGestures = new List<KeyGestureAttribute>();

			public NECommand(CommandEnumT command)
			{
				Command = command;

				var memberInfo = typeof(CommandEnumT).GetMember(command.ToString()).First();
				KeyGestures = memberInfo.GetCustomAttributes(typeof(KeyGestureAttribute), false).Cast<KeyGestureAttribute>().OrderBy(key => !key.Primary).ToList();
				if (KeyGestures.Any())
					InputGestureText = KeyGestures.First().GestureText;
			}

			public void RegisterCommand(UIElement window, Action<object, ExecutedRoutedEventArgs, CommandEnumT> handler)
			{
				window.CommandBindings.Add(new CommandBinding(this, (s, e) => handler(s, e, Command)));
			}

			public void RegisterInputBindings(UIElement window, int commandSet)
			{
				foreach (var keyGesture in KeyGestures)
					if ((keyGesture.CommandSet == 0) || ((keyGesture.CommandSet & commandSet) != 0))
						window.InputBindings.Add(new KeyBinding(this, new KeyGesture(keyGesture.Key, keyGesture.Modifiers)));
			}
		}

		static NEMenuItem()
		{
			commands = Enum.GetValues(typeof(CommandEnumT)).Cast<CommandEnumT>().ToDictionary(commandEnum => commandEnum, commandEnum => new NECommand(commandEnum));
			var duplicates = commands.Values.SelectMany(command => command.KeyGestures).GroupBy(keyGesture => keyGesture.GestureText + keyGesture.CommandSet.ToString()).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			if (duplicates.Any())
				throw new Exception(String.Format("Duplicate hotkeys: {0}", String.Join(", ", duplicates)));
		}

		public CommandEnumT CommandEnum
		{
			get { return (Command as NECommand).Command; }
			set
			{
				var neCommand = commands[value];
				Command = neCommand;
				InputGestureText = neCommand.InputGestureText;
			}
		}

		static Dictionary<CommandEnumT, NECommand> commands;
		static public void RegisterCommands(UIElement window, Action<object, ExecutedRoutedEventArgs, CommandEnumT> handler, int commandSet = 1)
		{
			foreach (var command in commands.Values)
				command.RegisterCommand(window, handler);
			RegisterInputBindings(window, commandSet);
		}

		static public void RegisterInputBindings(UIElement window, int commandSet = 1)
		{
			window.InputBindings.Clear();
			foreach (var command in commands.Values)
				command.RegisterInputBindings(window, commandSet);
		}
	}
}
