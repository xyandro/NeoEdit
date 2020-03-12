namespace NeoEdit.Program
{
	public class CommandState
	{
		public TabsWindow TabsWindow;
		public NECommand Command;
		public object Parameters;
		public bool ShiftDown;
		public bool ControlDown;
		public bool AltDown;
		public object PreHandleData;
		public bool Result;
		public bool? MultiStatus;

		public CommandState(NECommand command) => Command = command;

	}
}
