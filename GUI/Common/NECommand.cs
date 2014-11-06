using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.GUI.Common
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class KeyGestureAttribute : Attribute
	{
		public Key Key { get; private set; }
		public ModifierKeys Modifiers { get; private set; }

		public KeyGestureAttribute(Key key, ModifierKeys modifiers = ModifierKeys.None)
		{
			Key = key;
			Modifiers = modifiers;
		}
	}

	public class NEMenuItem<CommandEnumT> : MenuItem
	{
		class NECommand : RoutedCommand
		{
			public readonly CommandEnumT Command;
			public readonly string InputGestureText;
			public readonly List<Tuple<KeyGesture, string>> KeyGestures = new List<Tuple<KeyGesture, string>>();

			public NECommand(CommandEnumT command)
			{
				Command = command;

				var memberInfo = typeof(CommandEnumT).GetMember(command.ToString()).First();
				var keyGestureAttrs = memberInfo.GetCustomAttributes(typeof(KeyGestureAttribute), false);
				foreach (KeyGestureAttribute key in keyGestureAttrs)
				{
					var gesture = new KeyGesture(key.Key, key.Modifiers);
					KeyGestures.Add(new Tuple<KeyGesture, string>(gesture, gesture.GetDisplayStringForCulture(CultureInfo.CurrentCulture)));
				}
				if (KeyGestures.Any())
					InputGestureText = KeyGestures.First().Item2;
			}

			public void RegisterCommand(UIElement window, Action<object, ExecutedRoutedEventArgs, CommandEnumT> handler)
			{
				window.CommandBindings.Add(new CommandBinding(this, (s, e) => handler(s, e, Command)));
				foreach (var attr in KeyGestures)
					window.InputBindings.Add(new KeyBinding(this, attr.Item1));
			}
		}

		public NEMenuItem()
		{
			// Allow right-click
			SetValue(typeof(MenuItem).GetField("InsideContextMenuProperty", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as DependencyProperty, true);
		}

		MouseButton last = MouseButton.Left;
		static public MouseButton LastClick { get; private set; }

		protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
		{
			last = MouseButton.Right;
			base.OnMouseRightButtonUp(e);
			last = MouseButton.Left;
		}

		protected override void OnClick()
		{
			LastClick = last;
			base.OnClick();
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
		static public void RegisterCommands(UIElement window, Action<object, ExecutedRoutedEventArgs, CommandEnumT> handler)
		{
			if (commands == null)
			{
				commands = new Dictionary<CommandEnumT, NECommand>();
				foreach (CommandEnumT a in Enum.GetValues(typeof(CommandEnumT)))
					commands[a] = new NECommand(a);
				var duplicates = commands.Values.SelectMany(command => command.KeyGestures.Select(keyGesture => keyGesture.Item2)).GroupBy(key => key).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
				if (duplicates.Any())
					throw new Exception(String.Format("Duplicate hotkeys: {0}", String.Join(", ", duplicates)));
			}
			foreach (var command in commands.Values)
				command.RegisterCommand(window, handler);
		}
	}
}
