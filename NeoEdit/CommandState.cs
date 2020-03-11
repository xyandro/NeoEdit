namespace NeoEdit.Program
{
	public class CommandState
	{
		public NECommand Command;
		public object Parameters;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public object PreHandleData;
		public bool Result;

		public CommandState(NECommand command) => Command = command;
	}
}
