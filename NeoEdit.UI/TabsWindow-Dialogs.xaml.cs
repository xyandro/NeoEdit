﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NeoEdit.Common;
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
		public MessageOptions RunMessageDialog(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None) => Message.Run(this, title, text, options, defaultAccept, defaultCancel);
		public ContentAttributeDialogResult RunContentAttributeDialog(List<ParserNode> nodes) => ContentAttributeDialog.Run(this, nodes);
		public ContentAttributesDialogResult RunContentAttributesDialog(List<ParserNode> nodes) => ContentAttributesDialog.Run(this, nodes);
		public string RunCryptorKeyDialog(Cryptor.Type type, bool encrypt) => CryptorKeyDialog.Run(this, type, encrypt);
		public DatabaseConnectDialogResult RunDatabaseConnectDialog() => DatabaseConnectDialog.Run(this);
		public void RunDatabaseExamineDialog(DbConnection dbConnection) => DatabaseExamineDialog.Run(this, dbConnection);
		public DateTimeFormatDialogResult RunDateTimeFormatDialog(string example) => DateTimeFormatDialog.Run(this, example);
		public DateTimeToTimeZoneDialogResult RunDateTimeToTimeZoneDialog() => DateTimeToTimeZoneDialog.Run(this);
		public DiffFixWhitespaceDialogResult RunDiffFixWhitespaceDialog() => DiffFixWhitespaceDialog.Run(this);
		public DiffIgnoreCharactersDialogResult RunDiffIgnoreCharactersDialog(string ignoreCharacters) => DiffIgnoreCharactersDialog.Run(this, ignoreCharacters);
		public EditConvertDialogResult RunEditConvertDialog() => EditConvertDialog.Run(this);
		public EditDataCompressDialogResult RunEditDataCompressDialog(Coder.CodePage codePage, bool compress) => EditDataCompressDialog.Run(this, codePage, compress);
		public EditDataEncryptDialogResult RunEditDataEncryptDialog(Coder.CodePage codePage, bool encrypt) => EditDataEncryptDialog.Run(this, codePage, encrypt);
		public EditDataHashDialogResult RunEditDataHashDialog(Coder.CodePage codePage) => EditDataHashDialog.Run(this, codePage);
		public EditDataSignDialogResult RunEditDataSignDialog(Coder.CodePage codePage) => EditDataSignDialog.Run(this, codePage);
		public EditExpressionExpressionDialogResult RunEditExpressionExpressionDialog(NEVariables variables, int? numRows = null) => EditExpressionExpressionDialog.Run(this, variables, numRows);
		public EditFindFindDialogResult RunEditFindFindDialog(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables) => EditFindFindDialog.Run(this, text, selectionOnly, codePages, variables);
		public EditFindRegexReplaceDialogResult RunEditFindRegexReplaceDialog(string text, bool selectionOnly) => EditFindRegexReplaceDialog.Run(this, text, selectionOnly);
		public EditModifyRegionsDialogResult RunEditModifyRegionsDialog() => EditModifyRegionsDialog.Run(this);
		public EditRepeatDialogResult RunEditRepeatDialog(bool selectRepetitions, NEVariables variables) => EditRepeatDialog.Run(this, selectRepetitions, variables);
		public EditRotateDialogResult RunEditRotateDialog(NEVariables variables) => EditRotateDialog.Run(this, variables);
		public EditSortDialogResult RunEditSortDialog() => EditSortDialog.Run(this);
		public EncodingDialogResult RunEncodingDialog(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None) => EncodingDialog.Run(this, codePage, detected);
		public FileEncodingLineEndingsDialogResult RunFileEncodingLineEndingsDialog(string lineEndings) => FileEncodingLineEndingsDialog.Run(this, lineEndings);
		public FilesCreateFromExpressionsDialogResult RunFilesCreateFromExpressionsDialog(NEVariables variables, Coder.CodePage codePage) => FilesCreateFromExpressionsDialog.Run(this, variables, codePage);
		public FilesFindDialogResult RunFilesFindDialog(NEVariables variables) => FilesFindDialog.Run(this, variables);
		public FilesHashDialogResult RunFilesHashDialog() => FilesHashDialog.Run(this);
		public FilesInsertDialogResult RunFilesInsertDialog() => FilesInsertDialog.Run(this);
		public FilesNamesGetUniqueDialogResult RunFilesNamesGetUniqueDialog() => FilesNamesGetUniqueDialog.Run(this);
		public FilesNamesMakeAbsoluteRelativeDialogResult RunFilesNamesMakeAbsoluteRelativeDialog(NEVariables variables, bool absolute, bool checkType) => FilesNamesMakeAbsoluteRelativeDialog.Run(this, variables, absolute, checkType);
		public FilesOperationsCombineFilesDialogResult RunFilesOperationsCombineFilesDialog(NEVariables variables) => FilesOperationsCombineFilesDialog.Run(this, variables);
		public FilesOperationsCopyMoveDialogResult RunFilesOperationsCopyMoveDialog(NEVariables variables, bool move) => FilesOperationsCopyMoveDialog.Run(this, variables, move);
		public FilesOperationsEncodingDialogResult RunFilesOperationsEncodingDialog() => FilesOperationsEncodingDialog.Run(this);
		public FilesOperationsSplitFileDialogResult RunFilesOperationsSplitFileDialog(NEVariables variables) => FilesOperationsSplitFileDialog.Run(this, variables);
		public FilesSelectByVersionControlStatusDialogResult RunFilesSelectByVersionControlStatusDialog() => FilesSelectByVersionControlStatusDialog.Run(this);
		public FilesSetAttributesDialogResult RunFilesSetAttributesDialog(Dictionary<FileAttributes, bool?> attributes) => FilesSetAttributesDialog.Run(this, attributes);
		public FilesSetSizeDialogResult RunFilesSetSizeDialog(NEVariables variables) => FilesSetSizeDialog.Run(this, variables);
		public FilesSetTimeDialogResult RunFilesSetTimeDialog(NEVariables variables, string expression) => FilesSetTimeDialog.Run(this, variables, expression);
		public FilesSignDialogResult RunFilesSignDialog() => FilesSignDialog.Run(this);
		public GetExpressionDialogResult RunGetExpressionDialog(NEVariables variables, int? numRows = null) => GetExpressionDialog.Run(this, variables, numRows);
		public ImageAddOverlayColorDialogResult RunImageAddOverlayColorDialog(bool add, NEVariables variables) => ImageAddOverlayColorDialog.Run(this, add, variables);
		public ImageAdjustColorDialogResult RunImageAdjustColorDialog(NEVariables variables) => ImageAdjustColorDialog.Run(this, variables);
		public ImageCropDialogResult RunImageCropDialog(NEVariables variables) => ImageCropDialog.Run(this, variables);
		public ImageGIFAnimateDialogResult RunImageGIFAnimateDialog(NEVariables variables) => ImageGIFAnimateDialog.Run(this, variables);
		public ImageGIFSplitDialogResult RunImageGIFSplitDialog(NEVariables variables) => ImageGIFSplitDialog.Run(this, variables);
		public ImageGrabColorDialogResult RunImageGrabColorDialog(string color) => ImageGrabColorDialog.Run(this, color);
		public ImageGrabImageDialogResult RunImageGrabImageDialog(NEVariables variables) => ImageGrabImageDialog.Run(this, variables);
		public ImageRotateDialogResult RunImageRotateDialog(NEVariables variables) => ImageRotateDialog.Run(this, variables);
		public ImageSizeDialogResult RunImageSizeDialog(NEVariables variables) => ImageSizeDialog.Run(this, variables);
		public MacroPlayRepeatDialogResult RunMacroPlayRepeatDialog(Func<string> chooseMacro) => MacroPlayRepeatDialog.Run(this, chooseMacro);
		public NetworkAbsoluteURLDialogResult RunNetworkAbsoluteURLDialog(NEVariables variables) => NetworkAbsoluteURLDialog.Run(this, variables);
		public NetworkFetchFileDialogResult RunNetworkFetchFileDialog(NEVariables variables) => NetworkFetchFileDialog.Run(this, variables);
		public NetworkFetchStreamDialogResult RunNetworkFetchStreamDialog(NEVariables variables, string outputDirectory) => NetworkFetchStreamDialog.Run(this, variables, outputDirectory);
		public NetworkPingDialogResult RunNetworkPingDialog() => NetworkPingDialog.Run(this);
		public NetworkScanPortsDialogResult RunNetworkScanPortsDialog() => NetworkScanPortsDialog.Run(this);
		public NetworkWCFInterceptCallsDialogResult RunNetworkWCFInterceptCallsDialog() => NetworkWCFInterceptCallsDialog.Run(this);
		public NumericCombinationsPermutationsDialogResult RunNumericCombinationsPermutationsDialog() => NumericCombinationsPermutationsDialog.Run(this);
		public NumericConvertBaseDialogResult RunNumericConvertBaseDialog() => NumericConvertBaseDialog.Run(this);
		public NumericCycleDialogResult RunNumericCycleDialog(NEVariables variables) => NumericCycleDialog.Run(this, variables);
		public NumericFloorRoundCeilingDialogResult RunNumericFloorRoundCeilingDialog(string title, NEVariables variables) => NumericFloorRoundCeilingDialog.Run(this, title, variables);
		public NumericLimitDialogResult RunNumericLimitDialog(NEVariables variables) => NumericLimitDialog.Run(this, variables);
		public NumericMinMaxValuesDialogResult RunNumericMinMaxValuesDialog() => NumericMinMaxValuesDialog.Run(this);
		public NumericRandomNumberDialogResult RunNumericRandomNumberDialog(NEVariables variables) => NumericRandomNumberDialog.Run(this, variables);
		public NumericScaleDialogResult RunNumericScaleDialog(NEVariables variables) => NumericScaleDialog.Run(this, variables);
		public NumericSeriesDialogResult RunNumericSeriesDialog(bool linear, NEVariables variables) => NumericSeriesDialog.Run(this, linear, variables);
		public PositionGotoDialogResult RunPositionGotoDialog(GotoType gotoType, int startValue, NEVariables variables) => PositionGotoDialog.Run(this, gotoType, startValue, variables);
		public SelectByCountDialogResult RunSelectByCountDialog() => SelectByCountDialog.Run(this);
		public SelectLimitDialogResult RunSelectLimitDialog(NEVariables variables) => SelectLimitDialog.Run(this, variables);
		public SelectSplitDialogResult RunSelectSplitDialog(NEVariables variables) => SelectSplitDialog.Run(this, variables);
		public TableAddColumnDialogResult RunTableAddColumnDialog(NEVariables variables, int numRows) => TableAddColumnDialog.Run(this, variables, numRows);
		public TableConvertDialogResult RunTableConvertDialog(ParserType tableType) => TableConvertDialog.Run(this, tableType);
		public TableDatabaseGenerateDeletesDialogResult RunTableDatabaseGenerateDeletesDialog(Table table, string tableName) => TableDatabaseGenerateDeletesDialog.Run(this, table, tableName);
		public TableDatabaseGenerateInsertsDialogResult RunTableDatabaseGenerateInsertsDialog(Table table, string tableName) => TableDatabaseGenerateInsertsDialog.Run(this, table, tableName);
		public TableDatabaseGenerateUpdatesDialogResult RunTableDatabaseGenerateUpdatesDialog(Table table, string tableName) => TableDatabaseGenerateUpdatesDialog.Run(this, table, tableName);
		public TableEditTableDialogResult RunTableEditTableDialog(Table input) => TableEditTableDialog.Run(this, input);
		public TableJoinDialogResult RunTableJoinDialog(Table leftTable, Table rightTable) => TableJoinDialog.Run(this, leftTable, rightTable);
		public TableTextToTableDialogResult RunTableTextToTableDialog(string text) => TableTextToTableDialog.Run(this, text);
		public TextFirstDistinctDialogResult RunTextFirstDistinctDialog() => TextFirstDistinctDialog.Run(this);
		public TextRandomTextDialogResult RunTextRandomTextDialog(NEVariables variables) => TextRandomTextDialog.Run(this, variables);
		public TextReverseRegExDialogResult RunTextReverseRegExDialog() => TextReverseRegExDialog.Run(this);
		public TextSelectWholeBoundedWordDialogResult RunTextSelectWholeBoundedWordDialog(bool wholeWord) => TextSelectWholeBoundedWordDialog.Run(this, wholeWord);
		public TextTrimDialogResult RunTextTrimDialog() => TextTrimDialog.Run(this);
		public TextUnicodeDialogResult RunTextUnicodeDialog() => TextUnicodeDialog.Run(this);
		public TextWidthDialogResult RunTextWidthDialog(bool numeric, bool isSelect, NEVariables variables) => TextWidthDialog.Run(this, numeric, isSelect, variables);
		public WindowCustomGridDialogResult RunWindowCustomGridDialog(int? columns, int? rows, int? maxColumns, int? maxRows) => WindowCustomGridDialog.Run(this, columns, rows, maxColumns, maxRows);
		public void RunWindowFontSizeDialog() => WindowFontSizeDialog.Run(this);
		public NetworkWCFGetConfigResult RunNetworkWCFGetConfigDialog() => NetworkWCFGetConfigDialog.Run(this);
		public object RunProgressDialog(string text, Func<Func<bool>, Action<int>, object> action) => ProgressDialog.Run(this, text, action);
		public HashSet<Coder.CodePage> RunCodePagesDialog(HashSet<Coder.CodePage> startCodePages = null) => CodePagesDialog.Run(this, startCodePages);
		public IRunTasksDialog CreateIRunTasksDialog() => new RunTasksDialog(this);

		public List<TResult> RunMultiProgressDialogAsync<TSource, TResult>(string title, IEnumerable<TSource> items, Func<TSource, IProgress<ProgressReport>, CancellationToken, Task<TResult>> getTask, Func<TSource, string> getName = null) => MultiProgressDialog.RunAsync<TSource, TResult>(this, title, items, getTask, getName);
		public void RunMultiProgressDialogAsync<TSource>(string title, IEnumerable<TSource> items, Func<TSource, IProgress<ProgressReport>, CancellationToken, Task> getTask, Func<TSource, string> getName = null) => MultiProgressDialog.RunAsync<TSource>(this, title, items, getTask, getName);
		public List<TResult> RunMultiProgressDialog<TSource, TResult>(string title, IEnumerable<TSource> items, Func<TSource, IProgress<ProgressReport>, CancellationToken, TResult> getTask, Func<TSource, string> getName = null) => MultiProgressDialog.Run<TSource, TResult>(this, title, items, getTask, getName);
		public void RunMultiProgressDialog<TSource>(string title, IEnumerable<TSource> items, Action<TSource, IProgress<ProgressReport>, CancellationToken> getTask, Func<TSource, string> getName = null) => MultiProgressDialog.Run<TSource>(this, title, items, getTask, getName);
		public void RunHelpAboutDialog() => HelpAboutDialog.Run(this);
	}
}
