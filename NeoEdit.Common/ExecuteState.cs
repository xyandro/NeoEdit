using System.Windows.Input;
using NeoEdit.Common.Configuration;

namespace NeoEdit.Common
{
	public class ExecuteState
	{
		public NECommand Command;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public bool? MultiStatus;
		public Key Key;
		public string Text;
		public IConfiguration Configuration;

		public ExecuteState(NECommand command)
		{
			Command = command;

			var modifiers = Keyboard.Modifiers;
			ShiftDown = modifiers.HasFlag(ModifierKeys.Shift);
			ControlDown = modifiers.HasFlag(ModifierKeys.Control);
			AltDown = modifiers.HasFlag(ModifierKeys.Alt);
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
