using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeoEdit.GUI.Common
{
	[AttributeUsage(AttributeTargets.Field)]
	public class HeaderAttribute : Attribute
	{
		public string Header { get; private set; }
		public HeaderAttribute(string header)
		{
			Header = header;
		}
	}

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
			public CommandEnumT Command { get; private set; }
			public string Header { get; private set; }
			public string InputGestureText { get; private set; }

			readonly List<KeyGesture> keyGestures = new List<KeyGesture>();
			public NECommand(CommandEnumT command)
			{
				Command = command;

				var memberInfo = typeof(CommandEnumT).GetMember(command.ToString()).First();
				var headerAttr = memberInfo.GetCustomAttributes(typeof(HeaderAttribute), false).FirstOrDefault() as HeaderAttribute;
				if (headerAttr != null)
					Header = headerAttr.Header;
				if (String.IsNullOrEmpty(Header))
					Header = command.ToString();
				var keyGestureAttrs = memberInfo.GetCustomAttributes(typeof(KeyGestureAttribute), false);
				foreach (KeyGestureAttribute key in keyGestureAttrs)
					keyGestures.Add(new KeyGesture(key.Key, key.Modifiers));
				if (keyGestures.Any())
					InputGestureText = keyGestures.Last().GetDisplayStringForCulture(CultureInfo.CurrentCulture);
			}

			public void RegisterCommand(UIElement window, Action<object, ExecutedRoutedEventArgs, CommandEnumT> handler)
			{
				window.CommandBindings.Add(new CommandBinding(this, (s, e) => handler(s, e, Command)));
				foreach (var attr in keyGestures)
					window.InputBindings.Add(new KeyBinding(this, attr));
			}
		}

		public CommandEnumT CommandEnum
		{
			get { return (Command as NECommand).Command; }
			set
			{
				var neCommand = commands[value];
				Command = neCommand;
				Header = neCommand.Header;
				InputGestureText = neCommand.InputGestureText;
			}
		}

		static Dictionary<CommandEnumT, NECommand> commands;
		static void SetupCommands()
		{
			if (commands != null)
				return;
			commands = new Dictionary<CommandEnumT, NECommand>();
			foreach (CommandEnumT a in Enum.GetValues(typeof(CommandEnumT)))
				commands[a] = new NECommand(a);
		}

		static public void RegisterCommands(UIElement window, Action<object, ExecutedRoutedEventArgs, CommandEnumT> handler)
		{
			SetupCommands();
			foreach (var command in commands.Values)
				command.RegisterCommand(window, handler);
		}
	}
}
