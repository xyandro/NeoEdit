using System;
using System.Linq;
using System.Windows.Input;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void Execute_Internal_Activate()
		{
			LastActive = DateTime.Now;
			NEFiles.ForEach(neFile => neFile.CheckForRefresh());
		}

		void Execute_Internal_MouseActivate()
		{
			var neFile = (state.Configuration as Configuration_Internal_MouseActivate).NEFile as NEFile;
			SetActiveFiles(NEFiles.Where(file => (file == neFile) || ((state.ShiftDown) && (ActiveFiles.Contains(file)))));
			Focused = neFile;
		}

		void Execute_Internal_CloseFile()
		{
			var neFile = (state.Configuration as Configuration_Internal_CloseFile).NEFile as NEFile;
			neFile.VerifyCanClose();
			neFile.Close();
		}

		void Configure_Internal_Key()
		{
			switch (state.Key)
			{
				case Key.Back:
				case Key.Delete:
				case Key.Left:
				case Key.Right:
				case Key.Up:
				case Key.Down:
					state.Configuration = new Configuration_Internal_Key { HasSelections = ActiveFiles.Any(neFile => neFile.Selections.Any(range => range.HasSelection)) };
					break;
			}
		}

		bool PreExecute_Internal_Key()
		{
			if ((!state.ControlDown) || (state.AltDown))
				return false;

			switch (state.Key)
			{
				case Key.PageUp: MovePrevNext(-1, state.ShiftDown); return true;
				case Key.PageDown: MovePrevNext(1, state.ShiftDown); return true;
				case Key.Tab: MovePrevNext(1, state.ShiftDown, true); return true;
				default: return false;
			}
		}
	}
}
