using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using NeoEdit.Common;

namespace NeoEdit.Editor
{
	public class MacroAction
	{
		static readonly Dictionary<NECommand, bool> macroInclude = Helpers.GetValues<NECommand>().ToDictionary(command => command, command => typeof(NECommand).GetField(command.ToString()).GetCustomAttribute<NoMacroAttribute>() == null);

		public NECommand Command;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
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
				ShiftDown = state.ShiftDown,
				ControlDown = state.ControlDown,
				AltDown = state.AltDown,
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
				ShiftDown = ShiftDown,
				ControlDown = ControlDown,
				AltDown = AltDown,
				MultiStatus = MultiStatus,
				Key = Key,
				Text = Text,
				Configuration = Configuration,
			};
		}
	}
}
