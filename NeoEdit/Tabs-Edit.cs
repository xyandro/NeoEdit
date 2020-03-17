namespace NeoEdit.Program
{
	partial class Tabs
	{
		void Execute_Edit_EscapeClearsSelections(bool? multiStatus) => Settings.EscapeClearsSelections = multiStatus != true;
	}
}
