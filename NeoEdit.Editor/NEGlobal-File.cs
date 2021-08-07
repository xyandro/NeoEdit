using NeoEdit.Common;

namespace NeoEdit.Editor
{
	partial class NEGlobal
	{
		void PreExecute__File_Advanced_DontExitOnClose() => Settings.DontExitOnClose = state.MultiStatus != true;
	}
}
