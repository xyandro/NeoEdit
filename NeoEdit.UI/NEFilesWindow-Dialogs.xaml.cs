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
	partial class NEFilesWindow
	{
		public MessageOptions RunMessageDialog(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None) => RunOnUIThread(() => Message.Run(this, title, text, options, defaultAccept, defaultCancel));
		public MacroPlayRepeatDialogResult RunMacroPlayRepeatDialog(Func<string> chooseMacro) => RunOnUIThread(() => MacroPlayRepeatDialog.Run(this, chooseMacro));
		public void RunWindowActiveFilesDialog(WindowActiveFilesDialogData data) => RunOnUIThread(() => WindowActiveFilesDialog.Run(this, data));
		public void RunWindowFontSizeDialog() => RunOnUIThread(() => WindowFontSizeDialog.Run(this));
		public void RunWCFInterceptDialog() => RunOnUIThread(() => WCFInterceptDialog.Run(this));
		public void RunHelpAboutDialog() => RunOnUIThread(() => HelpAboutDialog.Run(this));

		public Configuration_File_Open_Open Configure_File_Open_Open(string defaultExt, string initialDirectory = null, string filter = null, int filterIndex = 0, bool multiselect = false)
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
					return null;
				return new Configuration_File_Open_Open { FileNames = dialog.FileNames.ToList() };
			});
		}
		public Configuration_Window_CustomGrid Configure_Window_CustomGrid(WindowLayout windowLayout) => RunOnUIThread(() => Configure_Window_CustomGrid_Dialog.Run(this, windowLayout));
		public Configuration_File_SaveCopyRename_ByExpression Configure_File_SaveCopyRename_ByExpression(NEVariables variables, int? numRows = null) => RunOnUIThread(() => Configure_File_SaveCopyRename_ByExpression_Dialog.Run(this, variables, numRows));
		public Configuration_File_Encoding_Encoding Configure_File_Encoding_Encoding(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None) => RunOnUIThread(() => Configure_File_Encoding_Encoding_Dialog.Run(this, codePage, detected));
		public Configuration_File_Encoding_LineEndings Configure_File_Encoding_LineEndings(string lineEndings) => RunOnUIThread(() => Configure_File_Encoding_LineEndings_Dialog.Run(this, lineEndings));
		public Configuration_File_Encrypt Configure_File_Encrypt(Cryptor.Type type, bool encrypt) => RunOnUIThread(() => Configure_File_Encrypt_Dialog.Run(this, type, encrypt));
		public Configuration_Edit_Find_Find Configure_Edit_Find_Find(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables) => RunOnUIThread(() => Configure_Edit_Find_Find_Dialog.Run(this, text, selectionOnly, codePages, variables));
		public Configuration_Edit_Find_RegexReplace Configure_Edit_Find_RegexReplace(string text, bool selectionOnly) => RunOnUIThread(() => Configure_Edit_Find_RegexReplace_Dialog.Run(this, text, selectionOnly));
		public Configuration_Edit_Expression_Expression Configure_Edit_Expression_Expression(NEVariables variables, int? numRows = null) => RunOnUIThread(() => Configure_Edit_Expression_Expression_Dialog.Run(this, variables, numRows));
		public Configuration_Edit_Rotate Configure_Edit_Rotate(NEVariables variables) => RunOnUIThread(() => Configure_Edit_Rotate_Dialog.Run(this, variables));
		public Configuration_Edit_Repeat Configure_Edit_Repeat(bool selectRepetitions, NEVariables variables) => RunOnUIThread(() => Configure_Edit_Repeat_Dialog.Run(this, selectRepetitions, variables));
		public Configuration_Edit_Data_Hash Configure_Edit_Data_Hash(Coder.CodePage codePage) => RunOnUIThread(() => Configure_Edit_Data_Hash_Dialog.Run(this, codePage));
		public Configuration_Edit_Data_Compress Configure_Edit_Data_Compress(Coder.CodePage codePage, bool compress) => RunOnUIThread(() => Configure_Edit_Data_Compress_Dialog.Run(this, codePage, compress));
		public Configuration_Edit_Data_Encrypt Configure_Edit_Data_Encrypt(Coder.CodePage codePage, bool encrypt) => RunOnUIThread(() => Configure_Edit_Data_Encrypt_Dialog.Run(this, codePage, encrypt));
		public Configuration_Edit_Data_Sign Configure_Edit_Data_Sign(Coder.CodePage codePage) => RunOnUIThread(() => Configure_Edit_Data_Sign_Dialog.Run(this, codePage));
		public Configuration_Edit_Sort Configure_Edit_Sort() => RunOnUIThread(() => Configure_Edit_Sort_Dialog.Run(this));
		public Configuration_Edit_Convert Configure_Edit_Convert() => RunOnUIThread(() => Configure_Edit_Convert_Dialog.Run(this));
		public Configuration_Edit_ModifyRegions Configure_Edit_ModifyRegions() => RunOnUIThread(() => Configure_Edit_ModifyRegions_Dialog.Run(this));
		public Configuration_Diff_IgnoreCharacters Configure_Diff_IgnoreCharacters(string ignoreCharacters) => RunOnUIThread(() => Configure_Diff_IgnoreCharacters_Dialog.Run(this, ignoreCharacters));
		public Configuration_Diff_Fix_Whitespace Configure_Diff_Fix_Whitespace() => RunOnUIThread(() => Configure_Diff_Fix_Whitespace_Dialog.Run(this));
		public Configuration_Files_Name_MakeAbsolute Configure_Files_Name_MakeAbsolute(NEVariables variables, bool absolute, bool checkType) => RunOnUIThread(() => Configure_Files_Name_MakeAbsolute_Dialog.Run(this, variables, absolute, checkType));
		public Configuration_Files_Name_GetUnique Configure_Files_Name_GetUnique() => RunOnUIThread(() => Configure_Files_Name_GetUnique_Dialog.Run(this));
		public Configuration_Files_Set_Size Configure_Files_Set_Size(NEVariables variables) => RunOnUIThread(() => Configure_Files_Set_Size_Dialog.Run(this, variables));
		public Configuration_Files_Set_Time Configure_Files_Set_Time(NEVariables variables, string expression) => RunOnUIThread(() => Configure_Files_Set_Time_Dialog.Run(this, variables, expression));
		public Configuration_Files_Set_Attributes Configure_Files_Set_Attributes(Dictionary<FileAttributes, bool?> attributes) => RunOnUIThread(() => Configure_Files_Set_Attributes_Dialog.Run(this, attributes));
		public Configuration_Files_Find Configure_Files_Find(NEVariables variables) => RunOnUIThread(() => Configure_Files_Find_Dialog.Run(this, variables));
		public Configuration_Files_Insert Configure_Files_Insert() => RunOnUIThread(() => Configure_Files_Insert_Dialog.Run(this));
		public Configuration_Files_Create_FromExpressions Configure_Files_Create_FromExpressions(NEVariables variables, Coder.CodePage codePage) => RunOnUIThread(() => Configure_Files_Create_FromExpressions_Dialog.Run(this, variables, codePage));
		public Configuration_Files_Select_ByVersionControlStatus Configure_Files_Select_ByVersionControlStatus() => RunOnUIThread(() => Configure_Files_Select_ByVersionControlStatus_Dialog.Run(this));
		public Configuration_Files_Hash Configure_Files_Hash() => RunOnUIThread(() => Configure_Files_Hash_Dialog.Run(this));
		public Configuration_Files_Compress Configure_Files_Compress(bool compress) => RunOnUIThread(() => Configure_Files_Compress_Dialog.Run(this, compress));
		public Configuration_Files_Encrypt Configure_Files_Encrypt(bool encrypt) => RunOnUIThread(() => Configure_Files_Encrypt_Dialog.Run(this, encrypt));
		public Configuration_Files_Sign Configure_Files_Sign() => RunOnUIThread(() => Configure_Files_Sign_Dialog.Run(this));
		public Configuration_Files_Operations_CopyMove Configure_Files_Operations_CopyMove(NEVariables variables, bool move) => RunOnUIThread(() => Configure_Files_Operations_CopyMove_Dialog.Run(this, variables, move));
		public Configuration_Files_Operations_Encoding Configure_Files_Operations_Encoding() => RunOnUIThread(() => Configure_Files_Operations_Encoding_Dialog.Run(this));
		public Configuration_Files_Operations_SplitFile Configure_Files_Operations_SplitFile(NEVariables variables) => RunOnUIThread(() => Configure_Files_Operations_SplitFile_Dialog.Run(this, variables));
		public Configuration_Files_Operations_CombineFiles Configure_Files_Operations_CombineFiles(NEVariables variables) => RunOnUIThread(() => Configure_Files_Operations_CombineFiles_Dialog.Run(this, variables));
		public Configuration_Text_Select_Chars Configure_Text_Select_Chars(int index) => RunOnUIThread(() => Configure_Text_Select_Chars_Dialog.Run(this, index));
		public Configuration_Text_Select_ByWidth Configure_Text_Select_ByWidth(bool numeric, bool isSelect, NEVariables variables) => RunOnUIThread(() => Configure_Text_Select_ByWidth_Dialog.Run(this, numeric, isSelect, variables));
		public Configuration_Text_Unicode Configure_Text_Unicode() => RunOnUIThread(() => Configure_Text_Unicode_Dialog.Run(this));
		public Configuration_Text_RandomText Configure_Text_RandomText(NEVariables variables) => RunOnUIThread(() => Configure_Text_RandomText_Dialog.Run(this, variables));
		public Configuration_Text_ReverseRegEx Configure_Text_ReverseRegEx() => RunOnUIThread(() => Configure_Text_ReverseRegEx_Dialog.Run(this));
		public Configuration_Text_FirstDistinct Configure_Text_FirstDistinct() => RunOnUIThread(() => Configure_Text_FirstDistinct_Dialog.Run(this));
		public Configuration_Numeric_ConvertBase Configure_Numeric_ConvertBase() => RunOnUIThread(() => Configure_Numeric_ConvertBase_Dialog.Run(this));
		public Configuration_Numeric_Series_LinearGeometric Configure_Numeric_Series_LinearGeometric(bool linear, NEVariables variables) => RunOnUIThread(() => Configure_Numeric_Series_LinearGeometric_Dialog.Run(this, linear, variables));
		public Configuration_Numeric_Scale Configure_Numeric_Scale(NEVariables variables) => RunOnUIThread(() => Configure_Numeric_Scale_Dialog.Run(this, variables));
		public Configuration_Numeric_Floor Configure_Numeric_Floor(string title, NEVariables variables) => RunOnUIThread(() => Configure_Numeric_Floor_Dialog.Run(this, title, variables));
		public Configuration_Numeric_Limit Configure_Numeric_Limit(NEVariables variables) => RunOnUIThread(() => Configure_Numeric_Limit_Dialog.Run(this, variables));
		public Configuration_Numeric_Cycle Configure_Numeric_Cycle(NEVariables variables) => RunOnUIThread(() => Configure_Numeric_Cycle_Dialog.Run(this, variables));
		public Configuration_Numeric_RandomNumber Configure_Numeric_RandomNumber(NEVariables variables) => RunOnUIThread(() => Configure_Numeric_RandomNumber_Dialog.Run(this, variables));
		public Configuration_Numeric_CombinationsPermutations Configure_Numeric_CombinationsPermutations() => RunOnUIThread(() => Configure_Numeric_CombinationsPermutations_Dialog.Run(this));
		public Configuration_Numeric_MinMaxValues Configure_Numeric_MinMaxValues() => RunOnUIThread(() => Configure_Numeric_MinMaxValues_Dialog.Run(this));
		public Configuration_DateTime_Format Configure_DateTime_Format(string example) => RunOnUIThread(() => Configure_DateTime_Format_Dialog.Run(this, example));
		public Configuration_DateTime_ToTimeZone Configure_DateTime_ToTimeZone() => RunOnUIThread(() => Configure_DateTime_ToTimeZone_Dialog.Run(this));
		public Configuration_Image_GrabColor Configure_Image_GrabColor(string color) => RunOnUIThread(() => Configure_Image_GrabColor_Dialog.Run(this, color));
		public Configuration_Image_GrabImage Configure_Image_GrabImage(NEVariables variables) => RunOnUIThread(() => Configure_Image_GrabImage_Dialog.Run(this, variables));
		public Configuration_Image_AdjustColor Configure_Image_AdjustColor(NEVariables variables) => RunOnUIThread(() => Configure_Image_AdjustColor_Dialog.Run(this, variables));
		public Configuration_Image_AddOverlayColor Configure_Image_AddOverlayColor(bool add, NEVariables variables) => RunOnUIThread(() => Configure_Image_AddOverlayColor_Dialog.Run(this, add, variables));
		public Configuration_Image_Size Configure_Image_Size(NEVariables variables) => RunOnUIThread(() => Configure_Image_Size_Dialog.Run(this, variables));
		public Configuration_Image_Crop Configure_Image_Crop(NEVariables variables) => RunOnUIThread(() => Configure_Image_Crop_Dialog.Run(this, variables));
		public Configuration_Image_Rotate Configure_Image_Rotate(NEVariables variables) => RunOnUIThread(() => Configure_Image_Rotate_Dialog.Run(this, variables));
		public Configuration_Image_GIF_Animate Configure_Image_GIF_Animate(NEVariables variables) => RunOnUIThread(() => Configure_Image_GIF_Animate_Dialog.Run(this, variables));
		public Configuration_Image_GIF_Split Configure_Image_GIF_Split(NEVariables variables) => RunOnUIThread(() => Configure_Image_GIF_Split_Dialog.Run(this, variables));
		public Configuration_Image_SetTakenDate Configure_Image_SetTakenDate(NEVariables variables) => RunOnUIThread(() => Configure_Image_SetTakenDate_Dialog.Run(this, variables));
		public Configuration_Table_Convert Configure_Table_Convert(ParserType tableType) => RunOnUIThread(() => Configure_Table_Convert_Dialog.Run(this, tableType));
		public Configuration_Table_TextToTable Configure_Table_TextToTable(string text) => RunOnUIThread(() => Configure_Table_TextToTable_Dialog.Run(this, text));
		public Configuration_Table_EditTable Configure_Table_EditTable(Table input) => RunOnUIThread(() => Configure_Table_EditTable_Dialog.Run(this, input));
		public Configuration_Table_AddColumn Configure_Table_AddColumn(NEVariables variables, int numRows) => RunOnUIThread(() => Configure_Table_AddColumn_Dialog.Run(this, variables, numRows));
		public Configuration_Table_Join Configure_Table_Join(Table leftTable, Table rightTable) => RunOnUIThread(() => Configure_Table_Join_Dialog.Run(this, leftTable, rightTable));
		public Configuration_Table_Database_GenerateInserts Configure_Table_Database_GenerateInserts(Table table, string tableName) => RunOnUIThread(() => Configure_Table_Database_GenerateInserts_Dialog.Run(this, table, tableName));
		public Configuration_Table_Database_GenerateUpdates Configure_Table_Database_GenerateUpdates(Table table, string tableName) => RunOnUIThread(() => Configure_Table_Database_GenerateUpdates_Dialog.Run(this, table, tableName));
		public Configuration_Table_Database_GenerateDeletes Configure_Table_Database_GenerateDeletes(Table table, string tableName) => RunOnUIThread(() => Configure_Table_Database_GenerateDeletes_Dialog.Run(this, table, tableName));
		public Configuration_Position_Goto Configure_Position_Goto(GotoType gotoType, int startValue, NEVariables variables) => RunOnUIThread(() => Configure_Position_Goto_Dialog.Run(this, gotoType, startValue, variables));
		public Configuration_Content_Ancestor Configure_Content_Ancestor(List<ParserNode> nodes) => RunOnUIThread(() => Configure_Content_Ancestor_Dialog.Run(this, nodes));
		public Configuration_Content_Attributes Configure_Content_Attributes(List<ParserNode> nodes) => RunOnUIThread(() => Configure_Content_Attributes_Dialog.Run(this, nodes));
		public Configuration_Network_AbsoluteURL Configure_Network_AbsoluteURL(NEVariables variables) => RunOnUIThread(() => Configure_Network_AbsoluteURL_Dialog.Run(this, variables));
		public Configuration_Network_FetchFile Configure_Network_FetchFile(NEVariables variables) => RunOnUIThread(() => Configure_Network_FetchFile_Dialog.Run(this, variables));
		public Configuration_Network_FetchStream Configure_Network_FetchStream(NEVariables variables, string outputDirectory) => RunOnUIThread(() => Configure_Network_FetchStream_Dialog.Run(this, variables, outputDirectory));
		public Configuration_Network_Ping Configure_Network_Ping() => RunOnUIThread(() => Configure_Network_Ping_Dialog.Run(this));
		public Configuration_Network_ScanPorts Configure_Network_ScanPorts() => RunOnUIThread(() => Configure_Network_ScanPorts_Dialog.Run(this));
		public Configuration_Network_WCF_GetConfig Configure_Network_WCF_GetConfig() => RunOnUIThread(() => Configure_Network_WCF_GetConfig_Dialog.Run(this));
		public Configuration_Network_WCF_InterceptCalls Configure_Network_WCF_InterceptCalls() => RunOnUIThread(() => Configure_Network_WCF_InterceptCalls_Dialog.Run(this));
		public Configuration_Database_Connect Configure_Database_Connect() => RunOnUIThread(() => Configure_Database_Connect_Dialog.Run(this));
		public Configuration_Database_Examine Configure_Database_Examine(DbConnection dbConnection) => RunOnUIThread(() => Configure_Database_Examine_Dialog.Run(this, dbConnection));
		public Configuration_Select_Limit Configure_Select_Limit(NEVariables variables) => RunOnUIThread(() => Configure_Select_Limit_Dialog.Run(this, variables));
		public Configuration_Select_Repeats_ByCount Configure_Select_Repeats_ByCount() => RunOnUIThread(() => Configure_Select_Repeats_ByCount_Dialog.Run(this));
		public Configuration_Select_Split Configure_Select_Split(NEVariables variables) => RunOnUIThread(() => Configure_Select_Split_Dialog.Run(this, variables));
		public Configuration_Window_ViewBinaryCodePages Configure_Window_ViewBinaryCodePages(HashSet<Coder.CodePage> startCodePages = null) => RunOnUIThread(() => Configure_Window_ViewBinaryCodePages_Dialog.Run(this, startCodePages));

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
					return null;
				return new SaveFileDialogResult { FileName = dialog.FileName };
			});
		}
	}
}
