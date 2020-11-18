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

		public static MacroAction GetMacroAction()
		{
			if (!macroInclude[EditorExecuteState.CurrentState.Command])
				return null;

			return new MacroAction
			{
				Command = EditorExecuteState.CurrentState.Command,
				ShiftDown = EditorExecuteState.CurrentState.ShiftDown,
				ControlDown = EditorExecuteState.CurrentState.ControlDown,
				AltDown = EditorExecuteState.CurrentState.AltDown,
				MultiStatus = EditorExecuteState.CurrentState.MultiStatus,
				Key = EditorExecuteState.CurrentState.Key,
				Text = EditorExecuteState.CurrentState.Text,
				Configuration = EditorExecuteState.CurrentState.Configuration,
			};
		}

		public void ReplaceExecuteState(NEFiles neFiles)
		{
			EditorExecuteState.SetState(Command);
			EditorExecuteState.CurrentState.ShiftDown = ShiftDown;
			EditorExecuteState.CurrentState.ControlDown = ControlDown;
			EditorExecuteState.CurrentState.AltDown = AltDown;
			EditorExecuteState.CurrentState.MultiStatus = MultiStatus;
			EditorExecuteState.CurrentState.Key = Key;
			EditorExecuteState.CurrentState.Text = Text;
			EditorExecuteState.CurrentState.Configuration = Configuration;
			EditorExecuteState.CurrentState.NEFiles = neFiles;
		}
	}
}
