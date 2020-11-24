using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Configuration;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;
using NeoEdit.UI.Dialogs;

namespace NeoEdit.UI
{
	partial class NEWindowUI
	{
		public MessageOptions RunDialog_ShowMessage(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None) => RunOnUIThread(() => Message.Run(this, title, text, options, defaultAccept, defaultCancel));

		public SaveFileDialogResult RunSaveFileDialog(string fileName, string defaultExt, string initialDirectory, string filter)
		{
			return RunOnUIThread(() =>
			{
				var dialog = new SaveFileDialog
				{
					FileName = fileName,
					DefaultExt = defaultExt,
					InitialDirectory = initialDirectory,
					Filter = filter,
				};
				if (dialog.ShowDialog() != true)
					throw new OperationCanceledException();
				return new SaveFileDialogResult { FileName = dialog.FileName };
			});
		}

		public void RunDialog_Execute_File_Select_Choose(IEnumerable<INEFile> neFiles, IEnumerable<INEFile> activeFiles, INEFile focused, Func<IEnumerable<INEFile>, bool> canClose, Action<IEnumerable<INEFile>, IEnumerable<INEFile>, INEFile> updateFiles) => RunOnUIThread(() => File_Select_Choose_Dialog.Run(this, neFiles, activeFiles, focused, canClose, updateFiles));
		public Configuration_FileMacro_Open_Open RunDialog_Configure_FileMacro_Open_Open(string defaultExt, string initialDirectory = null, string filter = null, int filterIndex = 0, bool multiselect = false)
		{
			return RunOnUIThread(() =>
			{
				var dialog = new OpenFileDialog
				{
					DefaultExt = defaultExt,
					InitialDirectory = initialDirectory,
					Filter = filter,
					FilterIndex = filterIndex,
					Multiselect = multiselect,
				};
				if (dialog.ShowDialog() != true)
					throw new OperationCanceledException();
				return new Configuration_FileMacro_Open_Open { FileNames = dialog.FileNames.ToList() };
			});
		}
		public Configuration_File_OpenEncoding_ReopenWithEncoding RunDialog_Configure_File_OpenEncoding_ReopenWithEncoding(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None) => RunOnUIThread(() => File_OpenEncoding_ReopenWithEncoding_Dialog.Run(this, codePage, detected));
		public Configuration_FileTable_Various_Various RunDialog_Configure_FileTable_Various_Various(NEVariables variables, int? numRows = null) => RunOnUIThread(() => FileTable_Various_Various_Dialog.Run(this, variables, numRows));
		public Configuration_File_LineEndings RunDialog_Configure_File_LineEndings(string lineEndings) => RunOnUIThread(() => File_LineEndings_Dialog.Run(this, lineEndings));
		public Configuration_File_Advanced_Encrypt RunDialog_Configure_File_Advanced_Encrypt(Cryptor.Type type, bool encrypt) => RunOnUIThread(() => File_Advanced_Encrypt_Dialog.Run(this, type, encrypt));
		public Configuration_Edit_Select_Limit RunDialog_Configure_Edit_Select_Limit(NEVariables variables) => RunOnUIThread(() => Edit_Select_Limit_Dialog.Run(this, variables));
		public Configuration_Edit_Repeat RunDialog_Configure_Edit_Repeat(bool selectRepetitions, NEVariables variables) => RunOnUIThread(() => Edit_Repeat_Dialog.Run(this, selectRepetitions, variables));
		public Configuration_Edit_Rotate RunDialog_Configure_Edit_Rotate(NEVariables variables) => RunOnUIThread(() => Edit_Rotate_Dialog.Run(this, variables));
		public Configuration_Edit_Expression_Expression RunDialog_Configure_Edit_Expression_Expression(NEVariables variables, int? numRows = null) => RunOnUIThread(() => Edit_Expression_Expression_Dialog.Run(this, variables, numRows));
		public Configuration_Edit_ModifyRegions RunDialog_Configure_Edit_ModifyRegions() => RunOnUIThread(() => Edit_ModifyRegions_Dialog.Run(this));
		public Configuration_Edit_Advanced_Convert RunDialog_Configure_Edit_Advanced_Convert() => RunOnUIThread(() => Edit_Advanced_Convert_Dialog.Run(this));
		public Configuration_Edit_Advanced_Hash RunDialog_Configure_Edit_Advanced_Hash(Coder.CodePage codePage) => RunOnUIThread(() => Edit_Advanced_Hash_Dialog.Run(this, codePage));
		public Configuration_Edit_Advanced_CompressDecompress RunDialog_Configure_Edit_Advanced_CompressDecompress(Coder.CodePage codePage, bool compress) => RunOnUIThread(() => Edit_Advanced_CompressDecompress_Dialog.Run(this, codePage, compress));
		public Configuration_Edit_Advanced_EncryptDecrypt RunDialog_Configure_Edit_Advanced_EncryptDecrypt(Coder.CodePage codePage, bool encrypt) => RunOnUIThread(() => Edit_Advanced_EncryptDecrypt_Dialog.Run(this, codePage, encrypt));
		public Configuration_Edit_Advanced_Sign RunDialog_Configure_Edit_Advanced_Sign(Coder.CodePage codePage) => RunOnUIThread(() => Edit_Advanced_Sign_Dialog.Run(this, codePage));
		public Configuration_Text_SelectTrim_WholeBoundedWordTrim RunDialog_Configure_Text_SelectTrim_WholeBoundedWordTrim(int index) => RunOnUIThread(() => Text_SelectTrim_WholeBoundedWordTrim_Dialog.Run(this, index));
		public Configuration_Text_Select_Split RunDialog_Configure_Text_Select_Split(NEVariables variables) => RunOnUIThread(() => Text_Select_Split_Dialog.Run(this, variables));
		public Configuration_Text_Select_Repeats_ByCount_IgnoreMatchCase RunDialog_Configure_Text_Select_Repeats_ByCount_IgnoreMatchCase() => RunOnUIThread(() => Text_Select_Repeats_ByCount_IgnoreMatchCase_Dialog.Run(this));
		public Configuration_Text_SelectWidth_ByWidth RunDialog_Configure_Text_SelectWidth_ByWidth(bool numeric, bool isSelect, NEVariables variables) => RunOnUIThread(() => Text_SelectWidth_ByWidth_Dialog.Run(this, numeric, isSelect, variables));
		public Configuration_Text_Find_Find RunDialog_Configure_Text_Find_Find(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables) => RunOnUIThread(() => Text_Find_Find_Dialog.Run(this, text, selectionOnly, codePages, variables));
		public Configuration_Text_Find_RegexReplace RunDialog_Configure_Text_Find_RegexReplace(string text, bool selectionOnly) => RunOnUIThread(() => Text_Find_RegexReplace_Dialog.Run(this, text, selectionOnly));
		public Configuration_Text_Sort RunDialog_Configure_Text_Sort() => RunOnUIThread(() => Text_Sort_Dialog.Run(this));
		public Configuration_Text_Random RunDialog_Configure_Text_Random(NEVariables variables) => RunOnUIThread(() => Text_Random_Dialog.Run(this, variables));
		public Configuration_Text_Advanced_Unicode RunDialog_Configure_Text_Advanced_Unicode() => RunOnUIThread(() => Text_Advanced_Unicode_Dialog.Run(this));
		public Configuration_Text_Advanced_FirstDistinct RunDialog_Configure_Text_Advanced_FirstDistinct() => RunOnUIThread(() => Text_Advanced_FirstDistinct_Dialog.Run(this));
		public Configuration_Text_Advanced_ReverseRegex RunDialog_Configure_Text_Advanced_ReverseRegex() => RunOnUIThread(() => Text_Advanced_ReverseRegex_Dialog.Run(this));
		public Configuration_Numeric_Select_Limit RunDialog_Configure_Numeric_Select_Limit(NEVariables variables) => RunOnUIThread(() => Numeric_Select_Limit_Dialog.Run(this, variables));
		public Configuration_Numeric_Various RunDialog_Configure_Numeric_Various(string title, NEVariables variables) => RunOnUIThread(() => Numeric_Various_Dialog.Run(this, title, variables));
		public Configuration_Numeric_Scale RunDialog_Configure_Numeric_Scale(NEVariables variables) => RunOnUIThread(() => Numeric_Scale_Dialog.Run(this, variables));
		public Configuration_Numeric_Cycle RunDialog_Configure_Numeric_Cycle(NEVariables variables) => RunOnUIThread(() => Numeric_Cycle_Dialog.Run(this, variables));
		public Configuration_Numeric_Series_LinearGeometric RunDialog_Configure_Numeric_Series_LinearGeometric(bool linear, NEVariables variables) => RunOnUIThread(() => Numeric_Series_LinearGeometric_Dialog.Run(this, linear, variables));
		public Configuration_Numeric_ConvertBase_ConvertBase RunDialog_Configure_Numeric_ConvertBase_ConvertBase() => RunOnUIThread(() => Numeric_ConvertBase_ConvertBase_Dialog.Run(this));
		public Configuration_Numeric_RandomNumber RunDialog_Configure_Numeric_RandomNumber(NEVariables variables) => RunOnUIThread(() => Numeric_RandomNumber_Dialog.Run(this, variables));
		public Configuration_Numeric_CombinationsPermutations RunDialog_Configure_Numeric_CombinationsPermutations() => RunOnUIThread(() => Numeric_CombinationsPermutations_Dialog.Run(this));
		public Configuration_Numeric_MinMaxValues RunDialog_Configure_Numeric_MinMaxValues() => RunOnUIThread(() => Numeric_MinMaxValues_Dialog.Run(this));
		public Configuration_Files_Select_ByContent RunDialog_Configure_Files_Select_ByContent(NEVariables variables) => RunOnUIThread(() => Files_Select_ByContent_Dialog.Run(this, variables));
		public Configuration_Files_Select_BySourceControlStatus RunDialog_Configure_Files_Select_BySourceControlStatus() => RunOnUIThread(() => Files_Select_BySourceControlStatus_Dialog.Run(this));
		public Configuration_Files_CopyMove RunDialog_Configure_Files_CopyMove(NEVariables variables, bool move) => RunOnUIThread(() => Files_CopyMove_Dialog.Run(this, variables, move));
		public Configuration_Files_Name_MakeAbsoluteRelative RunDialog_Configure_Files_Name_MakeAbsoluteRelative(NEVariables variables, bool absolute, bool checkType) => RunOnUIThread(() => Files_Name_MakeAbsoluteRelative_Dialog.Run(this, variables, absolute, checkType));
		public Configuration_Files_Get_Hash RunDialog_Configure_Files_Get_Hash() => RunOnUIThread(() => Files_Get_Hash_Dialog.Run(this));
		public Configuration_Files_Get_Content RunDialog_Configure_Files_Get_Content() => RunOnUIThread(() => Files_Get_Content_Dialog.Run(this));
		public Configuration_Files_Set_Size RunDialog_Configure_Files_Set_Size(NEVariables variables) => RunOnUIThread(() => Files_Set_Size_Dialog.Run(this, variables));
		public Configuration_Files_Set_Time_Various RunDialog_Configure_Files_Set_Time_Various(NEVariables variables, string expression) => RunOnUIThread(() => Files_Set_Time_Various_Dialog.Run(this, variables, expression));
		public Configuration_Files_Set_Attributes RunDialog_Configure_Files_Set_Attributes(Dictionary<FileAttributes, bool?> attributes) => RunOnUIThread(() => Files_Set_Attributes_Dialog.Run(this, attributes));
		public Configuration_Files_Set_Content RunDialog_Configure_Files_Set_Content(NEVariables variables, Coder.CodePage codePage) => RunOnUIThread(() => Files_Set_Content_Dialog.Run(this, variables, codePage));
		public Configuration_Files_Set_Encoding RunDialog_Configure_Files_Set_Encoding() => RunOnUIThread(() => Files_Set_Encoding_Dialog.Run(this));
		public Configuration_Files_CompressDecompress RunDialog_Configure_Files_CompressDecompress(bool compress) => RunOnUIThread(() => Files_CompressDecompress_Dialog.Run(this, compress));
		public Configuration_Files_EncryptDecrypt RunDialog_Configure_Files_EncryptDecrypt(bool encrypt) => RunOnUIThread(() => Files_EncryptDecrypt_Dialog.Run(this, encrypt));
		public Configuration_Files_Sign RunDialog_Configure_Files_Sign() => RunOnUIThread(() => Files_Sign_Dialog.Run(this));
		public Configuration_Files_Advanced_SplitFiles RunDialog_Configure_Files_Advanced_SplitFiles(NEVariables variables) => RunOnUIThread(() => Files_Advanced_SplitFiles_Dialog.Run(this, variables));
		public Configuration_Files_Advanced_CombineFiles RunDialog_Configure_Files_Advanced_CombineFiles(NEVariables variables) => RunOnUIThread(() => Files_Advanced_CombineFiles_Dialog.Run(this, variables));
		public Configuration_Content_Various_WithAttribute RunDialog_Configure_Content_Various_WithAttribute(List<ParserNode> nodes) => RunOnUIThread(() => Content_Various_WithAttribute_Dialog.Run(this, nodes));
		public Configuration_Content_Attributes RunDialog_Configure_Content_Attributes(List<ParserNode> nodes) => RunOnUIThread(() => Content_Attributes_Dialog.Run(this, nodes));
		public Configuration_DateTime_ToTimeZone RunDialog_Configure_DateTime_ToTimeZone() => RunOnUIThread(() => DateTime_ToTimeZone_Dialog.Run(this));
		public Configuration_DateTime_Format RunDialog_Configure_DateTime_Format(string example) => RunOnUIThread(() => DateTime_Format_Dialog.Run(this, example));
		public Configuration_Table_New_FromSelection RunDialog_Configure_Table_New_FromSelection(string text) => RunOnUIThread(() => Table_New_FromSelection_Dialog.Run(this, text));
		public Configuration_Table_Edit RunDialog_Configure_Table_Edit(Table input) => RunOnUIThread(() => Table_Edit_Dialog.Run(this, input));
		public Configuration_Table_Convert RunDialog_Configure_Table_Convert(ParserType tableType) => RunOnUIThread(() => Table_Convert_Dialog.Run(this, tableType));
		public Configuration_Table_Join RunDialog_Configure_Table_Join(Table leftTable, Table rightTable) => RunOnUIThread(() => Table_Join_Dialog.Run(this, leftTable, rightTable));
		public Configuration_Table_Database_GenerateInserts RunDialog_Configure_Table_Database_GenerateInserts(Table table, string tableName) => RunOnUIThread(() => Table_Database_GenerateInserts_Dialog.Run(this, table, tableName));
		public Configuration_Table_Database_GenerateUpdates RunDialog_Configure_Table_Database_GenerateUpdates(Table table, string tableName) => RunOnUIThread(() => Table_Database_GenerateUpdates_Dialog.Run(this, table, tableName));
		public Configuration_Table_Database_GenerateDeletes RunDialog_Configure_Table_Database_GenerateDeletes(Table table, string tableName) => RunOnUIThread(() => Table_Database_GenerateDeletes_Dialog.Run(this, table, tableName));
		public Configuration_Image_Resize RunDialog_Configure_Image_Resize(NEVariables variables) => RunOnUIThread(() => Image_Resize_Dialog.Run(this, variables));
		public Configuration_Image_Crop RunDialog_Configure_Image_Crop(NEVariables variables) => RunOnUIThread(() => Image_Crop_Dialog.Run(this, variables));
		public Configuration_Image_GrabColor RunDialog_Configure_Image_GrabColor(string color) => RunOnUIThread(() => Image_GrabColor_Dialog.Run(this, color));
		public Configuration_Image_GrabImage RunDialog_Configure_Image_GrabImage(NEVariables variables) => RunOnUIThread(() => Image_GrabImage_Dialog.Run(this, variables));
		public Configuration_Image_AddOverlayColor RunDialog_Configure_Image_AddOverlayColor(bool add, NEVariables variables) => RunOnUIThread(() => Image_AddOverlayColor_Dialog.Run(this, add, variables));
		public Configuration_Image_AdjustColor RunDialog_Configure_Image_AdjustColor(NEVariables variables) => RunOnUIThread(() => Image_AdjustColor_Dialog.Run(this, variables));
		public Configuration_Image_Rotate RunDialog_Configure_Image_Rotate(NEVariables variables) => RunOnUIThread(() => Image_Rotate_Dialog.Run(this, variables));
		public Configuration_Image_GIF_Animate RunDialog_Configure_Image_GIF_Animate(NEVariables variables) => RunOnUIThread(() => Image_GIF_Animate_Dialog.Run(this, variables));
		public Configuration_Image_GIF_Split RunDialog_Configure_Image_GIF_Split(NEVariables variables) => RunOnUIThread(() => Image_GIF_Split_Dialog.Run(this, variables));
		public Configuration_Image_SetTakenDate RunDialog_Configure_Image_SetTakenDate(NEVariables variables) => RunOnUIThread(() => Image_SetTakenDate_Dialog.Run(this, variables));
		public Configuration_Position_Goto_Various RunDialog_Configure_Position_Goto_Various(GotoType gotoType, int startValue, NEVariables variables) => RunOnUIThread(() => Position_Goto_Various_Dialog.Run(this, gotoType, startValue, variables));
		public Configuration_Diff_IgnoreCharacters RunDialog_Configure_Diff_IgnoreCharacters(string ignoreCharacters) => RunOnUIThread(() => Diff_IgnoreCharacters_Dialog.Run(this, ignoreCharacters));
		public Configuration_Diff_Fix_Whitespace RunDialog_Configure_Diff_Fix_Whitespace() => RunOnUIThread(() => Diff_Fix_Whitespace_Dialog.Run(this));
		public Configuration_Network_AbsoluteURL RunDialog_Configure_Network_AbsoluteURL(NEVariables variables) => RunOnUIThread(() => Network_AbsoluteURL_Dialog.Run(this, variables));
		public Configuration_Network_Fetch_File RunDialog_Configure_Network_Fetch_File(NEVariables variables) => RunOnUIThread(() => Network_Fetch_File_Dialog.Run(this, variables));
		public Configuration_Network_Fetch_StreamPlaylist RunDialog_Configure_Network_Fetch_StreamPlaylist(NEVariables variables, string outputDirectory) => RunOnUIThread(() => Network_Fetch_StreamPlaylist_Dialog.Run(this, variables, outputDirectory));
		public Configuration_Network_Ping RunDialog_Configure_Network_Ping() => RunOnUIThread(() => Network_Ping_Dialog.Run(this));
		public Configuration_Network_ScanPorts RunDialog_Configure_Network_ScanPorts() => RunOnUIThread(() => Network_ScanPorts_Dialog.Run(this));
		public Configuration_Network_WCF_GetConfig RunDialog_Configure_Network_WCF_GetConfig() => RunOnUIThread(() => Network_WCF_GetConfig_Dialog.Run(this));
		public Configuration_Network_WCF_InterceptCalls RunDialog_Configure_Network_WCF_InterceptCalls() => RunOnUIThread(() => Network_WCF_InterceptCalls_Dialog.Run(this));
		public void RunDialog_Execute_Network_WCF_InterceptCalls() => RunOnUIThread(() => Execute_Network_WCF_InterceptCalls_Dialog.Run(this));
		public Configuration_Database_Connect RunDialog_Configure_Database_Connect() => RunOnUIThread(() => Database_Connect_Dialog.Run(this));
		public Configuration_Database_Examine RunDialog_Configure_Database_Examine(DbConnection dbConnection) => RunOnUIThread(() => Database_Examine_Dialog.Run(this, dbConnection));
		public MacroPlayRepeatDialogResult RunDialog_PreExecute_Macro_Play_Repeat(Func<string> chooseMacro) => RunOnUIThread(() => Macro_Play_Repeat_Dialog.Run(this, chooseMacro));
		public Configuration_Window_CustomGrid RunDialog_Configure_Window_CustomGrid(WindowLayout windowLayout) => RunOnUIThread(() => Window_CustomGrid_Dialog.Run(this, windowLayout));
		public void RunDialog_PreExecute_Window_Font_Size() => RunOnUIThread(() => Window_Font_Size_Dialog.Run(this));
		public Configuration_Window_BinaryCodePages RunDialog_Configure_Window_BinaryCodePages(HashSet<Coder.CodePage> startCodePages = null) => RunOnUIThread(() => Window_BinaryCodePages_Dialog.Run(this, startCodePages));
		public void RunDialog_PreExecute_Help_About() => RunOnUIThread(() => Help_About_Dialog.Run(this));
	}
}
