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
	public interface INEWindowUI
	{
		static Func<INEWindow, INEWindowUI> CreateNEWindowUIStatic { get; set; }
		static Func<Cryptor.Type, string> GetDecryptKeyStatic { get; set; }
		static Action<Exception> ShowExceptionMessageStatic { get; set; }

		MessageOptions RunDialog_ShowMessage(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None);

		SaveFileDialogResult RunSaveFileDialog(string fileName, string defaultExt, string initialDirectory, string filter);

		void RunDialog_Execute_File_Select_Choose(IEnumerable<INEFile> neFiles, IEnumerable<INEFile> activeFiles, INEFile focused, Func<IEnumerable<INEFile>, bool> canClose, Action<IEnumerable<INEFile>, IEnumerable<INEFile>, INEFile> updateFiles);
		Configuration_FileMacro_Open_Open RunDialog_Configure_FileMacro_Open_Open(string defaultExt, string initialDirectory = null, string filter = null, int filterIndex = 0, bool multiselect = false);
		Configuration_File_OpenEncoding_ReopenWithEncoding RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(Coder.CodePage codePage, bool hasBOM);
		Configuration_FileTable_Various_Various RunDialog_Configure_FileTable_Various_Various(NEVariables variables, int? numRows = null);
		Configuration_File_LineEndings RunDialog_Configure_File_LineEndings(string lineEndings);
		Configuration_File_Advanced_Encrypt RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type type, bool encrypt);
		Configuration_Edit_Select_Limit RunDialog_Configure_Edit_Select_Limit(NEVariables variables);
		Configuration_Edit_Repeat RunDialog_Configure_Edit_Repeat(NEVariables variables);
		Configuration_Edit_Rotate RunDialog_Configure_Edit_Rotate(NEVariables variables);
		Configuration_Edit_Expression_Expression RunDialog_Configure_Edit_Expression_Expression(NEVariables variables, int? numRows = null);
		Configuration_Edit_ModifyRegions RunDialog_Configure_Edit_ModifyRegions();
		Configuration_Edit_Advanced_Convert RunDialog_Configure_Edit_Advanced_Convert();
		Configuration_Edit_Advanced_Hash RunDialog_Configure_Edit_Advanced_Hash(Coder.CodePage codePage);
		Configuration_Edit_Advanced_CompressDecompress RunDialog_Configure_Edit_Advanced_CompressDecompress(Coder.CodePage codePage, bool compress);
		Configuration_Edit_Advanced_EncryptDecrypt RunDialog_Configure_Edit_Advanced_EncryptDecrypt(Coder.CodePage codePage, bool encrypt);
		Configuration_Edit_Advanced_Sign RunDialog_Configure_Edit_Advanced_Sign(Coder.CodePage codePage);
		Configuration_Text_SelectTrim_WholeBoundedWordTrim RunDialog_Configure_Text_SelectTrim_WholeBoundedWordTrim(int index);
		Configuration_Text_Select_Split RunDialog_Configure_Text_Select_Split(NEVariables variables);
		Configuration_Text_Select_Repeats_ByCount_IgnoreMatchCase RunDialog_Configure_Text_Select_Repeats_ByCount_IgnoreMatchCase();
		Configuration_Text_SelectWidth_ByWidth RunDialog_Configure_Text_SelectWidth_ByWidth(bool numeric, bool isSelect, NEVariables variables);
		Configuration_Text_Find_Find RunDialog_Configure_Text_Find_Find(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables);
		Configuration_Text_Find_RegexReplace RunDialog_Configure_Text_Find_RegexReplace(string text, bool selectionOnly);
		Configuration_Text_Sort RunDialog_Configure_Text_Sort();
		Configuration_Text_Random RunDialog_Configure_Text_Random(NEVariables variables);
		Configuration_Text_Advanced_Unicode RunDialog_Configure_Text_Advanced_Unicode();
		Configuration_Text_Advanced_FirstDistinct RunDialog_Configure_Text_Advanced_FirstDistinct();
		Configuration_Text_Advanced_ReverseRegex RunDialog_Configure_Text_Advanced_ReverseRegex();
		Configuration_Numeric_Select_Limit RunDialog_Configure_Numeric_Select_Limit(NEVariables variables);
		Configuration_Numeric_Various RunDialog_Configure_Numeric_Various(string title, NEVariables variables);
		Configuration_Numeric_Scale RunDialog_Configure_Numeric_Scale(NEVariables variables);
		Configuration_Numeric_Cycle RunDialog_Configure_Numeric_Cycle(NEVariables variables);
		Configuration_Numeric_Series_LinearGeometric RunDialog_Configure_Numeric_Series_LinearGeometric(bool linear, NEVariables variables);
		Configuration_Numeric_ConvertBase_ConvertBase RunDialog_Configure_Numeric_ConvertBase_ConvertBase();
		Configuration_Numeric_RandomNumber RunDialog_Configure_Numeric_RandomNumber(NEVariables variables);
		Configuration_Numeric_CombinationsPermutations RunDialog_Configure_Numeric_CombinationsPermutations();
		Configuration_Numeric_MinMaxValues RunDialog_Configure_Numeric_MinMaxValues();
		Configuration_Files_Select_ByContent RunDialog_Configure_Files_Select_ByContent(NEVariables variables);
		Configuration_Files_Select_BySourceControlStatus RunDialog_Configure_Files_Select_BySourceControlStatus();
		Configuration_Files_CopyMove RunDialog_Configure_Files_CopyMove(NEVariables variables, bool move);
		Configuration_Files_Name_MakeAbsoluteRelative RunDialog_Configure_Files_Name_MakeAbsoluteRelative(NEVariables variables, bool absolute, bool checkType);
		Configuration_Files_Get_Hash RunDialog_Configure_Files_Get_Hash();
		Configuration_Files_Get_Content RunDialog_Configure_Files_Get_Content();
		Configuration_Files_Set_Size RunDialog_Configure_Files_Set_Size(NEVariables variables);
		Configuration_Files_Set_Time_Various RunDialog_Configure_Files_Set_Time_Various(NEVariables variables, string expression);
		Configuration_Files_Set_Attributes RunDialog_Configure_Files_Set_Attributes(Dictionary<FileAttributes, bool?> attributes);
		Configuration_Files_Set_Content RunDialog_Configure_Files_Set_Content(NEVariables variables, Coder.CodePage codePage);
		Configuration_Files_Set_Encoding RunDialog_Configure_Files_Set_Encoding();
		Configuration_Files_CompressDecompress RunDialog_Configure_Files_CompressDecompress(bool compress);
		Configuration_Files_EncryptDecrypt RunDialog_Configure_Files_EncryptDecrypt(bool encrypt);
		Configuration_Files_Sign RunDialog_Configure_Files_Sign();
		Configuration_Files_Advanced_SplitFiles RunDialog_Configure_Files_Advanced_SplitFiles(NEVariables variables);
		Configuration_Files_Advanced_CombineFiles RunDialog_Configure_Files_Advanced_CombineFiles(NEVariables variables);
		Configuration_Content_Various_WithAttribute RunDialog_Configure_Content_Various_WithAttribute(List<ParserNode> nodes);
		Configuration_Content_Attributes RunDialog_Configure_Content_Attributes(List<ParserNode> nodes);
		Configuration_DateTime_ToTimeZone RunDialog_Configure_DateTime_ToTimeZone();
		Configuration_DateTime_Format RunDialog_Configure_DateTime_Format(string example);
		Configuration_Table_New_FromSelection RunDialog_Configure_Table_New_FromSelection(string text);
		Configuration_Table_Edit RunDialog_Configure_Table_Edit(Table input);
		Configuration_Table_Convert RunDialog_Configure_Table_Convert(ParserType tableType);
		Configuration_Table_Join RunDialog_Configure_Table_Join(Table leftTable, Table rightTable);
		Configuration_Table_Database_GenerateInserts RunDialog_Configure_Table_Database_GenerateInserts(Table table, string tableName);
		Configuration_Table_Database_GenerateUpdates RunDialog_Configure_Table_Database_GenerateUpdates(Table table, string tableName);
		Configuration_Table_Database_GenerateDeletes RunDialog_Configure_Table_Database_GenerateDeletes(Table table, string tableName);
		Configuration_Image_Resize RunDialog_Configure_Image_Resize(NEVariables variables);
		Configuration_Image_Crop RunDialog_Configure_Image_Crop(NEVariables variables);
		Configuration_Image_GrabColor RunDialog_Configure_Image_GrabColor(string color);
		Configuration_Image_GrabImage RunDialog_Configure_Image_GrabImage(NEVariables variables);
		Configuration_Image_AddOverlayColor RunDialog_Configure_Image_AddOverlayColor(bool add, NEVariables variables);
		Configuration_Image_AdjustColor RunDialog_Configure_Image_AdjustColor(NEVariables variables);
		Configuration_Image_Rotate RunDialog_Configure_Image_Rotate(NEVariables variables);
		Configuration_Image_GIF_Animate RunDialog_Configure_Image_GIF_Animate(NEVariables variables);
		Configuration_Image_GIF_Split RunDialog_Configure_Image_GIF_Split(NEVariables variables);
		Configuration_Image_SetTakenDate RunDialog_Configure_Image_SetTakenDate(NEVariables variables);
		Configuration_Position_Goto_Various RunDialog_Configure_Position_Goto_Various(GotoType gotoType, int startValue, NEVariables variables);
		Configuration_Diff_IgnoreCharacters RunDialog_Configure_Diff_IgnoreCharacters(string ignoreCharacters);
		Configuration_Diff_Fix_Whitespace RunDialog_Configure_Diff_Fix_Whitespace();
		Configuration_Network_AbsoluteURL RunDialog_Configure_Network_AbsoluteURL(NEVariables variables);
		Configuration_Network_Fetch_File RunDialog_Configure_Network_Fetch_File(NEVariables variables);
		Configuration_Network_Fetch_StreamPlaylist RunDialog_Configure_Network_Fetch_StreamPlaylist(NEVariables variables, string outputDirectory);
		Configuration_Network_Ping RunDialog_Configure_Network_Ping();
		Configuration_Network_ScanPorts RunDialog_Configure_Network_ScanPorts();
		Configuration_Network_WCF_GetConfig RunDialog_Configure_Network_WCF_GetConfig();
		Configuration_Network_WCF_InterceptCalls RunDialog_Configure_Network_WCF_InterceptCalls();
		void RunDialog_Execute_Network_WCF_InterceptCalls();
		Configuration_Database_Connect RunDialog_Configure_Database_Connect();
		Configuration_Database_Examine RunDialog_Configure_Database_Examine(DbConnection dbConnection);
		MacroPlayRepeatDialogResult RunDialog_PreExecute_Macro_Play_Repeat(Func<string> chooseMacro);
		Configuration_Window_CustomGrid RunDialog_Configure_Window_CustomGrid(WindowLayout windowLayout);
		void RunDialog_PreExecute_Window_Font_Size();
		Configuration_Window_BinaryCodePages RunDialog_Configure_Window_BinaryCodePages(HashSet<Coder.CodePage> startCodePages = null);
		void RunDialog_PreExecute_Help_About();

		void Render(RenderParameters renderParameters);
		void ShowExceptionMessage(Exception ex);
		void SendActivateIfActive();
		void SetMacroProgress(double? percent);
		void SetTaskRunnerProgress(double? percent);

		void SetForeground();
		void CloseWindow();
	}
}
