using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using NeoEdit.Common.Enums;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Models;
using NeoEdit.Common.Parsing;
using NeoEdit.Common.Transform;

namespace NeoEdit.Common
{
	public class TabsWindowCreator
	{
		public static Func<ITabs, ITabsWindow> CreateITabsWindow { get; set; }
	}

	public interface ITabsWindow
	{
		MessageOptions RunMessageDialog(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None);
		ContentAttributeDialogResult RunContentAttributeDialog(List<ParserNode> nodes);
		ContentAttributesDialogResult RunContentAttributesDialog(List<ParserNode> nodes);
		string RunCryptorKeyDialog(Cryptor.Type type, bool encrypt);
		DatabaseConnectDialogResult RunDatabaseConnectDialog();
		void RunDatabaseExamineDialog(DbConnection dbConnection);
		DateTimeFormatDialogResult RunDateTimeFormatDialog(string example);
		DateTimeToTimeZoneDialogResult RunDateTimeToTimeZoneDialog();
		DiffFixWhitespaceDialogResult RunDiffFixWhitespaceDialog();
		DiffIgnoreCharactersDialogResult RunDiffIgnoreCharactersDialog(string ignoreCharacters);
		EditConvertDialogResult RunEditConvertDialog();
		EditDataCompressDialogResult RunEditDataCompressDialog(Coder.CodePage codePage, bool compress);
		EditDataEncryptDialogResult RunEditDataEncryptDialog(Coder.CodePage codePage, bool encrypt);
		EditDataHashDialogResult RunEditDataHashDialog(Coder.CodePage codePage);
		EditDataSignDialogResult RunEditDataSignDialog(Coder.CodePage codePage);
		EditExpressionExpressionDialogResult RunEditExpressionExpressionDialog(NEVariables variables, int? numRows = null);
		EditFindFindDialogResult RunEditFindFindDialog(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables);
		EditFindRegexReplaceDialogResult RunEditFindRegexReplaceDialog(string text, bool selectionOnly);
		EditModifyRegionsDialogResult RunEditModifyRegionsDialog();
		EditRepeatDialogResult RunEditRepeatDialog(bool selectRepetitions, NEVariables variables);
		EditRotateDialogResult RunEditRotateDialog(NEVariables variables);
		EditSortDialogResult RunEditSortDialog();
		EncodingDialogResult RunEncodingDialog(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None);
		FileEncodingLineEndingsDialogResult RunFileEncodingLineEndingsDialog(string lineEndings);
		FilesCreateFromExpressionsDialogResult RunFilesCreateFromExpressionsDialog(NEVariables variables, Coder.CodePage codePage);
		FilesFindDialogResult RunFilesFindDialog(NEVariables variables);
		FilesHashDialogResult RunFilesHashDialog();
		FilesInsertDialogResult RunFilesInsertDialog();
		FilesNamesGetUniqueDialogResult RunFilesNamesGetUniqueDialog();
		FilesNamesMakeAbsoluteRelativeDialogResult RunFilesNamesMakeAbsoluteRelativeDialog(NEVariables variables, bool absolute, bool checkType);
		FilesOperationsCombineFilesDialogResult RunFilesOperationsCombineFilesDialog(NEVariables variables);
		FilesOperationsCopyMoveDialogResult RunFilesOperationsCopyMoveDialog(NEVariables variables, bool move);
		FilesOperationsEncodingDialogResult RunFilesOperationsEncodingDialog();
		FilesOperationsSplitFileDialogResult RunFilesOperationsSplitFileDialog(NEVariables variables);
		FilesSelectByVersionControlStatusDialogResult RunFilesSelectByVersionControlStatusDialog();
		FilesSetAttributesDialogResult RunFilesSetAttributesDialog(Dictionary<FileAttributes, bool?> attributes);
		FilesSetSizeDialogResult RunFilesSetSizeDialog(NEVariables variables);
		FilesSetTimeDialogResult RunFilesSetTimeDialog(NEVariables variables, string expression);
		FilesSignDialogResult RunFilesSignDialog();
		GetExpressionDialogResult RunGetExpressionDialog(NEVariables variables, int? numRows = null);
		ImageAddOverlayColorDialogResult RunImageAddOverlayColorDialog(bool add, NEVariables variables);
		ImageAdjustColorDialogResult RunImageAdjustColorDialog(NEVariables variables);
		ImageCropDialogResult RunImageCropDialog(NEVariables variables);
		ImageGIFAnimateDialogResult RunImageGIFAnimateDialog(NEVariables variables);
		ImageGIFSplitDialogResult RunImageGIFSplitDialog(NEVariables variables);
		ImageGrabColorDialogResult RunImageGrabColorDialog(string color);
		ImageGrabImageDialogResult RunImageGrabImageDialog(NEVariables variables);
		ImageRotateDialogResult RunImageRotateDialog(NEVariables variables);
		ImageSizeDialogResult RunImageSizeDialog(NEVariables variables);
		MacroPlayRepeatDialogResult RunMacroPlayRepeatDialog(Func<string> chooseMacro);
		NetworkAbsoluteURLDialogResult RunNetworkAbsoluteURLDialog(NEVariables variables);
		NetworkFetchFileDialogResult RunNetworkFetchFileDialog(NEVariables variables);
		NetworkFetchStreamDialogResult RunNetworkFetchStreamDialog(NEVariables variables, string outputDirectory);
		NetworkPingDialogResult RunNetworkPingDialog();
		NetworkScanPortsDialogResult RunNetworkScanPortsDialog();
		NetworkWCFInterceptCallsDialogResult RunNetworkWCFInterceptCallsDialog();
		NumericCombinationsPermutationsDialogResult RunNumericCombinationsPermutationsDialog();
		NumericConvertBaseDialogResult RunNumericConvertBaseDialog();
		NumericCycleDialogResult RunNumericCycleDialog(NEVariables variables);
		NumericFloorRoundCeilingDialogResult RunNumericFloorRoundCeilingDialog(string title, NEVariables variables);
		NumericLimitDialogResult RunNumericLimitDialog(NEVariables variables);
		NumericMinMaxValuesDialogResult RunNumericMinMaxValuesDialog();
		NumericRandomNumberDialogResult RunNumericRandomNumberDialog(NEVariables variables);
		NumericScaleDialogResult RunNumericScaleDialog(NEVariables variables);
		NumericSeriesDialogResult RunNumericSeriesDialog(bool linear, NEVariables variables);
		PositionGotoDialogResult RunPositionGotoDialog(GotoType gotoType, int startValue, NEVariables variables);
		SelectByCountDialogResult RunSelectByCountDialog();
		SelectLimitDialogResult RunSelectLimitDialog(NEVariables variables);
		SelectSplitDialogResult RunSelectSplitDialog(NEVariables variables);
		TableAddColumnDialogResult RunTableAddColumnDialog(NEVariables variables, int numRows);
		TableConvertDialogResult RunTableConvertDialog(ParserType tableType);
		TableDatabaseGenerateDeletesDialogResult RunTableDatabaseGenerateDeletesDialog(Table table, string tableName);
		TableDatabaseGenerateInsertsDialogResult RunTableDatabaseGenerateInsertsDialog(Table table, string tableName);
		TableDatabaseGenerateUpdatesDialogResult RunTableDatabaseGenerateUpdatesDialog(Table table, string tableName);
		TableEditTableDialogResult RunTableEditTableDialog(Table input);
		TableJoinDialogResult RunTableJoinDialog(Table leftTable, Table rightTable);
		TableTextToTableDialogResult RunTableTextToTableDialog(string text);
		TextFirstDistinctDialogResult RunTextFirstDistinctDialog();
		TextRandomTextDialogResult RunTextRandomTextDialog(NEVariables variables);
		TextReverseRegExDialogResult RunTextReverseRegExDialog();
		TextSelectWholeBoundedWordDialogResult RunTextSelectWholeBoundedWordDialog(bool wholeWord);
		TextTrimDialogResult RunTextTrimDialog();
		TextUnicodeDialogResult RunTextUnicodeDialog();
		TextWidthDialogResult RunTextWidthDialog(bool numeric, bool isSelect, NEVariables variables);
		WindowLayout RunWindowCustomGridDialog(WindowLayout windowLayout);
		void RunWindowFontSizeDialog();
		NetworkWCFGetConfigResult RunNetworkWCFGetConfigDialog();
		object RunProgressDialog(string text, Func<Func<bool>, Action<int>, object> action);
		HashSet<Coder.CodePage> RunCodePagesDialog(HashSet<Coder.CodePage> startCodePages = null);
		void RunHelpAboutDialog();

		void Render();
		void ShowExceptionMessage(Exception ex);
		void CloseWindow();
		void QueueActivateTabs();
		void SetForeground();
		void SetMacroProgress(double? percent);
		void SetTaskRunnerProgress(double? percent);
	}
}
