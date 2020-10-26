using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;

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
		public IConfiguration Configuration;

		MacroAction() { }

		public static MacroAction GetMacroAction(EditorExecuteState state)
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

		public EditorExecuteState GetExecuteState(NEFiles neFiles)
		{
			return new EditorExecuteState(Command)
			{
				ShiftDown = ShiftDown,
				ControlDown = ControlDown,
				AltDown = AltDown,
				MultiStatus = MultiStatus,
				Key = Key,
				Text = Text,
				Configuration = Configuration,
				NEFiles = neFiles,
			};
		}
	}
}
