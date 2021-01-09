namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		void Configure_Edit_Select_Limit() => state.Configuration = neWindowUI.RunDialog_Configure_Edit_Select_Limit(Focused.GetVariables());
	}
}
