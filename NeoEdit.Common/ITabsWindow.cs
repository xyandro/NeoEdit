using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	public static class ITabsWindowStatic
	{
		public delegate string RunCryptorKeyDialogDelegate(Cryptor.Type type, bool encrypt);
		public static RunCryptorKeyDialogDelegate RunCryptorKeyDialog { get; set; }
		public static Func<ITabs, ITabsWindow> CreateITabsWindow { get; set; }
		public static Action<Exception> ShowExceptionMessage { get; set; }
	}

	public interface ITabsWindow
	{
		MessageOptions RunMessageDialog(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None);
		MacroPlayRepeatDialogResult RunMacroPlayRepeatDialog(Func<string> chooseMacro);
		void RunWindowActiveTabsDialog(WindowActiveTabsDialogData data);
		void RunWindowFontSizeDialog();
		void RunWCFInterceptDialog();
		void RunHelpAboutDialog();
		SaveFileDialogResult RunSaveFileDialog(string fileName, string defaultExt, string initialDirectory, string filter);

		Configuration_File_Open_Open Configure_File_Open_Open(string defaultExt, string initialDirectory = null, string filter = null, int filterIndex = 0, bool multiselect = false);
		Configuration_Window_CustomGrid Configure_Window_CustomGrid(WindowLayout windowLayout);
		Configuration_File_SaveCopy_ByExpression Configure_File_SaveCopy_ByExpression(NEVariables variables, int? numRows = null);
		Configuration_File_Encoding_Encoding Configure_File_Encoding_Encoding(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None);
		Configuration_File_Encoding_LineEndings Configure_File_Encoding_LineEndings(string lineEndings);
		Configuration_File_Encrypt Configure_File_Encrypt(Cryptor.Type type, bool encrypt);
		Configuration_Edit_Find_Find Configure_Edit_Find_Find(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables);
		Configuration_Edit_Find_RegexReplace Configure_Edit_Find_RegexReplace(string text, bool selectionOnly);
		Configuration_Edit_Expression_Expression Configure_Edit_Expression_Expression(NEVariables variables, int? numRows = null);
		Configuration_Edit_Rotate Configure_Edit_Rotate(NEVariables variables);
		Configuration_Edit_Repeat Configure_Edit_Repeat(bool selectRepetitions, NEVariables variables);
		Configuration_Edit_Data_Hash Configure_Edit_Data_Hash(Coder.CodePage codePage);
		Configuration_Edit_Data_Compress Configure_Edit_Data_Compress(Coder.CodePage codePage, bool compress);
		Configuration_Edit_Data_Encrypt Configure_Edit_Data_Encrypt(Coder.CodePage codePage, bool encrypt);
		Configuration_Edit_Data_Sign Configure_Edit_Data_Sign(Coder.CodePage codePage);
		Configuration_Edit_Sort Configure_Edit_Sort();
		Configuration_Edit_Convert Configure_Edit_Convert();
		Configuration_Edit_ModifyRegions Configure_Edit_ModifyRegions();
		Configuration_Diff_IgnoreCharacters Configure_Diff_IgnoreCharacters(string ignoreCharacters);
		Configuration_Diff_Fix_Whitespace_Dialog Configure_Diff_Fix_Whitespace_Dialog();
		Configuration_Files_Name_MakeAbsolute Configure_Files_Name_MakeAbsolute(NEVariables variables, bool absolute, bool checkType);
		Configuration_Files_Name_GetUnique Configure_Files_Name_GetUnique();
		Configuration_Files_Set_Size Configure_Files_Set_Size(NEVariables variables);
		Configuration_Files_Set_Time Configure_Files_Set_Time(NEVariables variables, string expression);
		Configuration_Files_Set_Attributes Configure_Files_Set_Attributes(Dictionary<FileAttributes, bool?> attributes);
		Configuration_Files_Find Configure_Files_Find(NEVariables variables);
		Configuration_Files_Insert Configure_Files_Insert();
		Configuration_Files_Create_FromExpressions Configure_Files_Create_FromExpressions(NEVariables variables, Coder.CodePage codePage);
		Configuration_Files_Select_ByVersionControlStatus Configure_Files_Select_ByVersionControlStatus();
		Configuration_Files_Hash Configure_Files_Hash();
		Configuration_Files_Compress Configure_Files_Compress(bool compress);
		Configuration_Files_Sign Configure_Files_Sign();
		Configuration_Files_Operations_CopyMove Configure_Files_Operations_CopyMove(NEVariables variables, bool move);
		Configuration_Files_Operations_Encoding Configure_Files_Operations_Encoding();
		Configuration_Files_Operations_SplitFile Configure_Files_Operations_SplitFile(NEVariables variables);
		Configuration_Files_Operations_CombineFiles Configure_Files_Operations_CombineFiles(NEVariables variables);
		Configuration_Text_Select_Chars Configure_Text_Select_Chars(int index);
		Configuration_Text_Select_ByWidth Configure_Text_Select_ByWidth(bool numeric, bool isSelect, NEVariables variables);
		Configuration_Text_Unicode Configure_Text_Unicode();
		Configuration_Text_RandomText Configure_Text_RandomText(NEVariables variables);
		Configuration_Text_ReverseRegEx Configure_Text_ReverseRegEx();
		Configuration_Text_FirstDistinct Configure_Text_FirstDistinct();
		Configuration_Numeric_ConvertBase Configure_Numeric_ConvertBase();
		Configuration_Numeric_Series_LinearGeometric Configure_Numeric_Series_LinearGeometric(bool linear, NEVariables variables);
		Configuration_Numeric_Scale Configure_Numeric_Scale(NEVariables variables);
		Configuration_Numeric_Floor Configure_Numeric_Floor(string title, NEVariables variables);
		Configuration_Numeric_Limit Configure_Numeric_Limit(NEVariables variables);
		Configuration_Numeric_Cycle Configure_Numeric_Cycle(NEVariables variables);
		Configuration_Numeric_RandomNumber Configure_Numeric_RandomNumber(NEVariables variables);
		Configuration_Numeric_CombinationsPermutations Configure_Numeric_CombinationsPermutations();
		Configuration_Numeric_MinMaxValues Configure_Numeric_MinMaxValues();
		Configuration_DateTime_Format Configure_DateTime_Format(string example);
		Configuration_DateTime_ToTimeZone Configure_DateTime_ToTimeZone();
		Configuration_Image_GrabColor Configure_Image_GrabColor(string color);
		Configuration_Image_GrabImage Configure_Image_GrabImage(NEVariables variables);
		Configuration_Image_AdjustColor Configure_Image_AdjustColor(NEVariables variables);
		Configuration_Image_AddOverlayColor Configure_Image_AddOverlayColor(bool add, NEVariables variables);
		Configuration_Image_Size Configure_Image_Size(NEVariables variables);
		Configuration_Image_Crop Configure_Image_Crop(NEVariables variables);
		Configuration_Image_Rotate Configure_Image_Rotate(NEVariables variables);
		Configuration_Image_GIF_Animate Configure_Image_GIF_Animate(NEVariables variables);
		Configuration_Image_GIF_Split Configure_Image_GIF_Split(NEVariables variables);
		Configuration_Image_SetTakenDate Configure_Image_SetTakenDate(NEVariables variables);
		Configuration_Table_Convert Configure_Table_Convert(ParserType tableType);
		Configuration_Table_TextToTable Configure_Table_TextToTable(string text);
		Configuration_Table_EditTable Configure_Table_EditTable(Table input);
		Configuration_Table_AddColumn Configure_Table_AddColumn(NEVariables variables, int numRows);
		Configuration_Table_Join Configure_Table_Join(Table leftTable, Table rightTable);
		Configuration_Table_Database_GenerateInserts Configure_Table_Database_GenerateInserts(Table table, string tableName);
		Configuration_Table_Database_GenerateUpdates Configure_Table_Database_GenerateUpdates(Table table, string tableName);
		Configuration_Table_Database_GenerateDeletes Configure_Table_Database_GenerateDeletes(Table table, string tableName);
		Configuration_Position_Goto Configure_Position_Goto(GotoType gotoType, int startValue, NEVariables variables);
		Configuration_Content_Ancestor Configure_Content_Ancestor(List<ParserNode> nodes);
		Configuration_Content_Attributes Configure_Content_Attributes(List<ParserNode> nodes);
		Configuration_Network_AbsoluteURL Configure_Network_AbsoluteURL(NEVariables variables);
		Configuration_Network_FetchFile Configure_Network_FetchFile(NEVariables variables);
		Configuration_Network_FetchStream Configure_Network_FetchStream(NEVariables variables, string outputDirectory);
		Configuration_Network_Ping Configure_Network_Ping();
		Configuration_Network_ScanPorts Configure_Network_ScanPorts();
		Configuration_Network_WCF_GetConfig Configure_Network_WCF_GetConfig();
		Configuration_Network_WCF_InterceptCalls Configure_Network_WCF_InterceptCalls();
		Configuration_Database_Connect Configure_Database_Connect();
		Configuration_Database_Examine Configure_Database_Examine(DbConnection dbConnection);
		Configuration_Select_Limit Configure_Select_Limit(NEVariables variables);
		Configuration_Select_Repeats_ByCount Configure_Select_Repeats_ByCount();
		Configuration_Select_Split Configure_Select_Split(NEVariables variables);
		Configuration_Window_ViewBinaryCodePages Configure_Window_ViewBinaryCodePages(HashSet<Coder.CodePage> startCodePages = null);

		void Render(RenderParameters renderParameters);
		void ShowExceptionMessage(Exception ex);
		void CloseWindow();
		void QueueActivateTabs();
		void SetForeground();
		void SetMacroProgress(double? percent);
		void SetTaskRunnerProgress(double? percent);
	}
}
