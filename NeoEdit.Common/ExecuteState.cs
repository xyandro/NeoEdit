using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeoEdit.Common.Configuration;

namespace NeoEdit.Common
{
	public class ExecuteState
	{
		static readonly Dictionary<NECommand, bool> macroInclude = Helpers.GetValues<NECommand>().ToDictionary(command => command, command => typeof(NECommand).GetField(command.ToString()).GetCustomAttribute<NoMacroAttribute>() == null);

		public NECommand Command;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool? MultiStatus;
		public string Key;
		public string Text;
		public IConfiguration Configuration;

		public bool MacroInclude => macroInclude[Command];

		public ExecuteState(NECommand command, Modifiers modifiers)
		{
			Command = command;

			ShiftDown = modifiers.HasFlag(Modifiers.Shift);
			ControlDown = modifiers.HasFlag(Modifiers.Control);
			AltDown = modifiers.HasFlag(Modifiers.Alt);
		}

		public ExecuteState(ExecuteState state)
		{
			Command = state.Command;
			ShiftDown = state.ShiftDown;
			ControlDown = state.ControlDown;
			AltDown = state.AltDown;
			MultiStatus = state.MultiStatus;
			Key = state.Key;
			Text = state.Text;
			Configuration = state.Configuration;
		}

		public override string ToString() => Command.ToString();
	}
}
