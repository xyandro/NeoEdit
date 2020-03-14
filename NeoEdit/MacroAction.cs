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
		public object Configuration;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool? MultiStatus;
		public Key Key;
		public string Text;

		MacroAction() { }

		public static MacroAction GetMacroAction(ExecuteState state)
		{
			if (!macroInclude[state.Command])
				return null;

			return new MacroAction
			{
				Command = state.Command,
				Configuration = state.Configuration,
				ShiftDown = state.ShiftDown,
				ControlDown = state.ControlDown,
				AltDown = state.AltDown,
				MultiStatus = state.MultiStatus,
				Key = state.Key,
				Text = state.Text,
			};
		}

		public ExecuteState GetExecuteState()
		{
			return new ExecuteState(Command)
			{
				Configuration = Configuration,
				ShiftDown = ShiftDown,
				ControlDown = ControlDown,
				AltDown = AltDown,
				MultiStatus = MultiStatus,
				Key = Key,
				Text = Text,
			};
		}
	}
}
