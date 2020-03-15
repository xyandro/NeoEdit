using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program
{
	public class MacroAction
	{
		static readonly Dictionary<NECommand, bool> macroInclude = Helpers.GetValues<NECommand>().ToDictionary(command => command, command => typeof(NECommand).GetField(command.ToString()).GetCustomAttribute<NoMacroAttribute>() == null);

		public NECommand Command;
		public ModifierKeys Modifiers;
		public bool? MultiStatus;
		public Key Key;
		public string Text;
		public object Configuration;

		MacroAction() { }

		public static MacroAction GetMacroAction(ExecuteState state)
		{
			if (!macroInclude[state.Command])
				return null;

			return new MacroAction
			{
				Command = state.Command,
				Modifiers = state.Modifiers,
				MultiStatus = state.MultiStatus,
				Key = state.Key,
				Text = state.Text,
				Configuration = state.Configuration,
			};
		}

		public ExecuteState GetExecuteState()
		{
			return new ExecuteState(Command)
			{
				Modifiers = Modifiers,
				MultiStatus = MultiStatus,
				Key = Key,
				Text = Text,
				Configuration = Configuration,
			};
		}
	}
}
