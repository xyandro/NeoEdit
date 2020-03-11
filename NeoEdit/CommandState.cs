namespace NeoEdit.Program
{
	public class CommandState
	{
		public NECommand Command { get; set; }

		public CommandState(NECommand command)
		{
			Command = command;
		}
	}
}
