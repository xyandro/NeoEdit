using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		void PreExecute_File_Advanced_DontExitOnClose() => Settings.DontExitOnClose = state.MultiStatus != true;
	}
}
