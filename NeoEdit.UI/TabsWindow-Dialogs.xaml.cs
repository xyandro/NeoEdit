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
	partial class TabsWindow
	{
		public MessageOptions RunMessageDialog(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None) => RunOnUIThread(() => Message.Run(this, title, text, options, defaultAccept, defaultCancel));
		public ContentAttributeDialogResult RunContentAttributeDialog(List<ParserNode> nodes) => RunOnUIThread(() => ContentAttributeDialog.Run(this, nodes));
		public ContentAttributesDialogResult RunContentAttributesDialog(List<ParserNode> nodes) => RunOnUIThread(() => ContentAttributesDialog.Run(this, nodes));
		public string RunCryptorKeyDialog(Cryptor.Type type, bool encrypt) => RunOnUIThread(() => CryptorKeyDialog.Run(this, type, encrypt));
		public DatabaseConnectDialogResult RunDatabaseConnectDialog() => RunOnUIThread(() => DatabaseConnectDialog.Run(this));
		public void RunDatabaseExamineDialog(DbConnection dbConnection) => RunOnUIThread(() => DatabaseExamineDialog.Run(this, dbConnection));
		public DateTimeFormatDialogResult RunDateTimeFormatDialog(string example) => RunOnUIThread(() => DateTimeFormatDialog.Run(this, example));
		public DateTimeToTimeZoneDialogResult RunDateTimeToTimeZoneDialog() => RunOnUIThread(() => DateTimeToTimeZoneDialog.Run(this));
		public DiffFixWhitespaceDialogResult RunDiffFixWhitespaceDialog() => RunOnUIThread(() => DiffFixWhitespaceDialog.Run(this));
		public DiffIgnoreCharactersDialogResult RunDiffIgnoreCharactersDialog(string ignoreCharacters) => RunOnUIThread(() => DiffIgnoreCharactersDialog.Run(this, ignoreCharacters));
		public EditConvertDialogResult RunEditConvertDialog() => RunOnUIThread(() => EditConvertDialog.Run(this));
		public EditDataCompressDialogResult RunEditDataCompressDialog(Coder.CodePage codePage, bool compress) => RunOnUIThread(() => EditDataCompressDialog.Run(this, codePage, compress));
		public EditDataEncryptDialogResult RunEditDataEncryptDialog(Coder.CodePage codePage, bool encrypt) => RunOnUIThread(() => EditDataEncryptDialog.Run(this, codePage, encrypt));
		public EditDataHashDialogResult RunEditDataHashDialog(Coder.CodePage codePage) => RunOnUIThread(() => EditDataHashDialog.Run(this, codePage));
		public EditDataSignDialogResult RunEditDataSignDialog(Coder.CodePage codePage) => RunOnUIThread(() => EditDataSignDialog.Run(this, codePage));
		public EditExpressionExpressionDialogResult RunEditExpressionExpressionDialog(NEVariables variables, int? numRows = null) => RunOnUIThread(() => EditExpressionExpressionDialog.Run(this, variables, numRows));
		public EditFindFindDialogResult RunEditFindFindDialog(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables) => RunOnUIThread(() => EditFindFindDialog.Run(this, text, selectionOnly, codePages, variables));
		public EditFindRegexReplaceDialogResult RunEditFindRegexReplaceDialog(string text, bool selectionOnly) => RunOnUIThread(() => EditFindRegexReplaceDialog.Run(this, text, selectionOnly));
		public EditModifyRegionsDialogResult RunEditModifyRegionsDialog() => RunOnUIThread(() => EditModifyRegionsDialog.Run(this));
		public EditRepeatDialogResult RunEditRepeatDialog(bool selectRepetitions, NEVariables variables) => RunOnUIThread(() => EditRepeatDialog.Run(this, selectRepetitions, variables));
		public EditRotateDialogResult RunEditRotateDialog(NEVariables variables) => RunOnUIThread(() => EditRotateDialog.Run(this, variables));
		public EditSortDialogResult RunEditSortDialog() => RunOnUIThread(() => EditSortDialog.Run(this));
		public EncodingDialogResult RunEncodingDialog(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None) => RunOnUIThread(() => EncodingDialog.Run(this, codePage, detected));
		public FileEncodingLineEndingsDialogResult RunFileEncodingLineEndingsDialog(string lineEndings) => RunOnUIThread(() => FileEncodingLineEndingsDialog.Run(this, lineEndings));
		public FilesCreateFromExpressionsDialogResult RunFilesCreateFromExpressionsDialog(NEVariables variables, Coder.CodePage codePage) => RunOnUIThread(() => FilesCreateFromExpressionsDialog.Run(this, variables, codePage));
		public FilesFindDialogResult RunFilesFindDialog(NEVariables variables) => RunOnUIThread(() => FilesFindDialog.Run(this, variables));
		public FilesHashDialogResult RunFilesHashDialog() => RunOnUIThread(() => FilesHashDialog.Run(this));
		public FilesInsertDialogResult RunFilesInsertDialog() => RunOnUIThread(() => FilesInsertDialog.Run(this));
		public FilesNamesGetUniqueDialogResult RunFilesNamesGetUniqueDialog() => RunOnUIThread(() => FilesNamesGetUniqueDialog.Run(this));
		public FilesNamesMakeAbsoluteRelativeDialogResult RunFilesNamesMakeAbsoluteRelativeDialog(NEVariables variables, bool absolute, bool checkType) => RunOnUIThread(() => FilesNamesMakeAbsoluteRelativeDialog.Run(this, variables, absolute, checkType));
		public FilesOperationsCombineFilesDialogResult RunFilesOperationsCombineFilesDialog(NEVariables variables) => RunOnUIThread(() => FilesOperationsCombineFilesDialog.Run(this, variables));
		public FilesOperationsCopyMoveDialogResult RunFilesOperationsCopyMoveDialog(NEVariables variables, bool move) => RunOnUIThread(() => FilesOperationsCopyMoveDialog.Run(this, variables, move));
		public FilesOperationsEncodingDialogResult RunFilesOperationsEncodingDialog() => RunOnUIThread(() => FilesOperationsEncodingDialog.Run(this));
		public FilesOperationsSplitFileDialogResult RunFilesOperationsSplitFileDialog(NEVariables variables) => RunOnUIThread(() => FilesOperationsSplitFileDialog.Run(this, variables));
		public FilesSelectByVersionControlStatusDialogResult RunFilesSelectByVersionControlStatusDialog() => RunOnUIThread(() => FilesSelectByVersionControlStatusDialog.Run(this));
		public FilesSetAttributesDialogResult RunFilesSetAttributesDialog(Dictionary<FileAttributes, bool?> attributes) => RunOnUIThread(() => FilesSetAttributesDialog.Run(this, attributes));
		public FilesSetSizeDialogResult RunFilesSetSizeDialog(NEVariables variables) => RunOnUIThread(() => FilesSetSizeDialog.Run(this, variables));
		public FilesSetTimeDialogResult RunFilesSetTimeDialog(NEVariables variables, string expression) => RunOnUIThread(() => FilesSetTimeDialog.Run(this, variables, expression));
		public FilesSignDialogResult RunFilesSignDialog() => RunOnUIThread(() => FilesSignDialog.Run(this));
		public GetExpressionDialogResult RunGetExpressionDialog(NEVariables variables, int? numRows = null) => RunOnUIThread(() => GetExpressionDialog.Run(this, variables, numRows));
		public ImageAddOverlayColorDialogResult RunImageAddOverlayColorDialog(bool add, NEVariables variables) => RunOnUIThread(() => ImageAddOverlayColorDialog.Run(this, add, variables));
		public ImageAdjustColorDialogResult RunImageAdjustColorDialog(NEVariables variables) => RunOnUIThread(() => ImageAdjustColorDialog.Run(this, variables));
		public ImageCropDialogResult RunImageCropDialog(NEVariables variables) => RunOnUIThread(() => ImageCropDialog.Run(this, variables));
		public ImageGIFAnimateDialogResult RunImageGIFAnimateDialog(NEVariables variables) => RunOnUIThread(() => ImageGIFAnimateDialog.Run(this, variables));
		public ImageGIFSplitDialogResult RunImageGIFSplitDialog(NEVariables variables) => RunOnUIThread(() => ImageGIFSplitDialog.Run(this, variables));
		public ImageGrabColorDialogResult RunImageGrabColorDialog(string color) => RunOnUIThread(() => ImageGrabColorDialog.Run(this, color));
		public ImageGrabImageDialogResult RunImageGrabImageDialog(NEVariables variables) => RunOnUIThread(() => ImageGrabImageDialog.Run(this, variables));
		public ImageRotateDialogResult RunImageRotateDialog(NEVariables variables) => RunOnUIThread(() => ImageRotateDialog.Run(this, variables));
		public ImageSizeDialogResult RunImageSizeDialog(NEVariables variables) => RunOnUIThread(() => ImageSizeDialog.Run(this, variables));
		public MacroPlayRepeatDialogResult RunMacroPlayRepeatDialog(Func<string> chooseMacro) => RunOnUIThread(() => MacroPlayRepeatDialog.Run(this, chooseMacro));
		public NetworkAbsoluteURLDialogResult RunNetworkAbsoluteURLDialog(NEVariables variables) => RunOnUIThread(() => NetworkAbsoluteURLDialog.Run(this, variables));
		public NetworkFetchFileDialogResult RunNetworkFetchFileDialog(NEVariables variables) => RunOnUIThread(() => NetworkFetchFileDialog.Run(this, variables));
		public NetworkFetchStreamDialogResult RunNetworkFetchStreamDialog(NEVariables variables, string outputDirectory) => RunOnUIThread(() => NetworkFetchStreamDialog.Run(this, variables, outputDirectory));
		public NetworkPingDialogResult RunNetworkPingDialog() => RunOnUIThread(() => NetworkPingDialog.Run(this));
		public NetworkScanPortsDialogResult RunNetworkScanPortsDialog() => RunOnUIThread(() => NetworkScanPortsDialog.Run(this));
		public NetworkWCFInterceptCallsDialogResult RunNetworkWCFInterceptCallsDialog() => RunOnUIThread(() => NetworkWCFInterceptCallsDialog.Run(this));
		public NumericCombinationsPermutationsDialogResult RunNumericCombinationsPermutationsDialog() => RunOnUIThread(() => NumericCombinationsPermutationsDialog.Run(this));
		public NumericConvertBaseDialogResult RunNumericConvertBaseDialog() => RunOnUIThread(() => NumericConvertBaseDialog.Run(this));
		public NumericCycleDialogResult RunNumericCycleDialog(NEVariables variables) => RunOnUIThread(() => NumericCycleDialog.Run(this, variables));
		public NumericFloorRoundCeilingDialogResult RunNumericFloorRoundCeilingDialog(string title, NEVariables variables) => RunOnUIThread(() => NumericFloorRoundCeilingDialog.Run(this, title, variables));
		public NumericLimitDialogResult RunNumericLimitDialog(NEVariables variables) => RunOnUIThread(() => NumericLimitDialog.Run(this, variables));
		public NumericMinMaxValuesDialogResult RunNumericMinMaxValuesDialog() => RunOnUIThread(() => NumericMinMaxValuesDialog.Run(this));
		public NumericRandomNumberDialogResult RunNumericRandomNumberDialog(NEVariables variables) => RunOnUIThread(() => NumericRandomNumberDialog.Run(this, variables));
		public NumericScaleDialogResult RunNumericScaleDialog(NEVariables variables) => RunOnUIThread(() => NumericScaleDialog.Run(this, variables));
		public NumericSeriesDialogResult RunNumericSeriesDialog(bool linear, NEVariables variables) => RunOnUIThread(() => NumericSeriesDialog.Run(this, linear, variables));
		public PositionGotoDialogResult RunPositionGotoDialog(GotoType gotoType, int startValue, NEVariables variables) => RunOnUIThread(() => PositionGotoDialog.Run(this, gotoType, startValue, variables));
		public SelectByCountDialogResult RunSelectByCountDialog() => RunOnUIThread(() => SelectByCountDialog.Run(this));
		public SelectLimitDialogResult RunSelectLimitDialog(NEVariables variables) => RunOnUIThread(() => SelectLimitDialog.Run(this, variables));
		public SelectSplitDialogResult RunSelectSplitDialog(NEVariables variables) => RunOnUIThread(() => SelectSplitDialog.Run(this, variables));
		public TableAddColumnDialogResult RunTableAddColumnDialog(NEVariables variables, int numRows) => RunOnUIThread(() => TableAddColumnDialog.Run(this, variables, numRows));
		public TableConvertDialogResult RunTableConvertDialog(ParserType tableType) => RunOnUIThread(() => TableConvertDialog.Run(this, tableType));
		public TableDatabaseGenerateDeletesDialogResult RunTableDatabaseGenerateDeletesDialog(Table table, string tableName) => RunOnUIThread(() => TableDatabaseGenerateDeletesDialog.Run(this, table, tableName));
		public TableDatabaseGenerateInsertsDialogResult RunTableDatabaseGenerateInsertsDialog(Table table, string tableName) => RunOnUIThread(() => TableDatabaseGenerateInsertsDialog.Run(this, table, tableName));
		public TableDatabaseGenerateUpdatesDialogResult RunTableDatabaseGenerateUpdatesDialog(Table table, string tableName) => RunOnUIThread(() => TableDatabaseGenerateUpdatesDialog.Run(this, table, tableName));
		public TableEditTableDialogResult RunTableEditTableDialog(Table input) => RunOnUIThread(() => TableEditTableDialog.Run(this, input));
		public TableJoinDialogResult RunTableJoinDialog(Table leftTable, Table rightTable) => RunOnUIThread(() => TableJoinDialog.Run(this, leftTable, rightTable));
		public TableTextToTableDialogResult RunTableTextToTableDialog(string text) => RunOnUIThread(() => TableTextToTableDialog.Run(this, text));
		public TextFirstDistinctDialogResult RunTextFirstDistinctDialog() => RunOnUIThread(() => TextFirstDistinctDialog.Run(this));
		public TextRandomTextDialogResult RunTextRandomTextDialog(NEVariables variables) => RunOnUIThread(() => TextRandomTextDialog.Run(this, variables));
		public TextReverseRegExDialogResult RunTextReverseRegExDialog() => RunOnUIThread(() => TextReverseRegExDialog.Run(this));
		public TextSelectWholeBoundedWordDialogResult RunTextSelectWholeBoundedWordDialog(bool wholeWord) => RunOnUIThread(() => TextSelectWholeBoundedWordDialog.Run(this, wholeWord));
		public TextTrimDialogResult RunTextTrimDialog() => RunOnUIThread(() => TextTrimDialog.Run(this));
		public TextUnicodeDialogResult RunTextUnicodeDialog() => RunOnUIThread(() => TextUnicodeDialog.Run(this));
		public TextWidthDialogResult RunTextWidthDialog(bool numeric, bool isSelect, NEVariables variables) => RunOnUIThread(() => TextWidthDialog.Run(this, numeric, isSelect, variables));
		public WindowLayout RunWindowCustomGridDialog(WindowLayout windowLayout) => RunOnUIThread(() => WindowCustomGridDialog.Run(this, windowLayout));
		public void RunWindowActiveTabsDialog(WindowActiveTabsDialogData data) => RunOnUIThread(() => WindowActiveTabsDialog.Run(this, data));
		public void RunWindowFontSizeDialog() => RunOnUIThread(() => WindowFontSizeDialog.Run(this));
		public NetworkWCFGetConfigDialogResult RunNetworkWCFGetConfigDialog() => RunOnUIThread(() => NetworkWCFGetConfigDialog.Run(this));
		public void RunWCFInterceptDialog() => RunOnUIThread(() => WCFInterceptDialog.Run(this));
		public HashSet<Coder.CodePage> RunCodePagesDialog(HashSet<Coder.CodePage> startCodePages = null) => RunOnUIThread(() => CodePagesDialog.Run(this, startCodePages));
		public void RunHelpAboutDialog() => RunOnUIThread(() => HelpAboutDialog.Run(this));

		public OpenFileDialogResult RunOpenFileDialog(string defaultExt, string initialDirectory = null, string filter = null, int filterIndex = 0, bool multiselect = false)
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
				return new OpenFileDialogResult { FileNames = dialog.FileNames.ToList() };
			});
		}

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
