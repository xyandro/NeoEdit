﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using Microsoft.Win32;
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
		public MessageOptions RunMessageDialog(string title, string text, MessageOptions options = MessageOptions.Ok, MessageOptions defaultAccept = MessageOptions.None, MessageOptions defaultCancel = MessageOptions.None) => Dispatcher.Invoke(() => Message.Run(this, title, text, options, defaultAccept, defaultCancel));
		public ContentAttributeDialogResult RunContentAttributeDialog(List<ParserNode> nodes) => Dispatcher.Invoke(() => ContentAttributeDialog.Run(this, nodes));
		public ContentAttributesDialogResult RunContentAttributesDialog(List<ParserNode> nodes) => Dispatcher.Invoke(() => ContentAttributesDialog.Run(this, nodes));
		public string RunCryptorKeyDialog(Cryptor.Type type, bool encrypt) => Dispatcher.Invoke(() => CryptorKeyDialog.Run(this, type, encrypt));
		public DatabaseConnectDialogResult RunDatabaseConnectDialog() => Dispatcher.Invoke(() => DatabaseConnectDialog.Run(this));
		public void RunDatabaseExamineDialog(DbConnection dbConnection) => Dispatcher.Invoke(() => DatabaseExamineDialog.Run(this, dbConnection));
		public DateTimeFormatDialogResult RunDateTimeFormatDialog(string example) => Dispatcher.Invoke(() => DateTimeFormatDialog.Run(this, example));
		public DateTimeToTimeZoneDialogResult RunDateTimeToTimeZoneDialog() => Dispatcher.Invoke(() => DateTimeToTimeZoneDialog.Run(this));
		public DiffFixWhitespaceDialogResult RunDiffFixWhitespaceDialog() => Dispatcher.Invoke(() => DiffFixWhitespaceDialog.Run(this));
		public DiffIgnoreCharactersDialogResult RunDiffIgnoreCharactersDialog(string ignoreCharacters) => Dispatcher.Invoke(() => DiffIgnoreCharactersDialog.Run(this, ignoreCharacters));
		public EditConvertDialogResult RunEditConvertDialog() => Dispatcher.Invoke(() => EditConvertDialog.Run(this));
		public EditDataCompressDialogResult RunEditDataCompressDialog(Coder.CodePage codePage, bool compress) => Dispatcher.Invoke(() => EditDataCompressDialog.Run(this, codePage, compress));
		public EditDataEncryptDialogResult RunEditDataEncryptDialog(Coder.CodePage codePage, bool encrypt) => Dispatcher.Invoke(() => EditDataEncryptDialog.Run(this, codePage, encrypt));
		public EditDataHashDialogResult RunEditDataHashDialog(Coder.CodePage codePage) => Dispatcher.Invoke(() => EditDataHashDialog.Run(this, codePage));
		public EditDataSignDialogResult RunEditDataSignDialog(Coder.CodePage codePage) => Dispatcher.Invoke(() => EditDataSignDialog.Run(this, codePage));
		public EditExpressionExpressionDialogResult RunEditExpressionExpressionDialog(NEVariables variables, int? numRows = null) => Dispatcher.Invoke(() => EditExpressionExpressionDialog.Run(this, variables, numRows));
		public EditFindFindDialogResult RunEditFindFindDialog(string text, bool selectionOnly, HashSet<Coder.CodePage> codePages, NEVariables variables) => Dispatcher.Invoke(() => EditFindFindDialog.Run(this, text, selectionOnly, codePages, variables));
		public EditFindRegexReplaceDialogResult RunEditFindRegexReplaceDialog(string text, bool selectionOnly) => Dispatcher.Invoke(() => EditFindRegexReplaceDialog.Run(this, text, selectionOnly));
		public EditModifyRegionsDialogResult RunEditModifyRegionsDialog() => Dispatcher.Invoke(() => EditModifyRegionsDialog.Run(this));
		public EditRepeatDialogResult RunEditRepeatDialog(bool selectRepetitions, NEVariables variables) => Dispatcher.Invoke(() => EditRepeatDialog.Run(this, selectRepetitions, variables));
		public EditRotateDialogResult RunEditRotateDialog(NEVariables variables) => Dispatcher.Invoke(() => EditRotateDialog.Run(this, variables));
		public EditSortDialogResult RunEditSortDialog() => Dispatcher.Invoke(() => EditSortDialog.Run(this));
		public EncodingDialogResult RunEncodingDialog(Coder.CodePage? codePage = null, Coder.CodePage detected = Coder.CodePage.None) => Dispatcher.Invoke(() => EncodingDialog.Run(this, codePage, detected));
		public FileEncodingLineEndingsDialogResult RunFileEncodingLineEndingsDialog(string lineEndings) => Dispatcher.Invoke(() => FileEncodingLineEndingsDialog.Run(this, lineEndings));
		public FilesCreateFromExpressionsDialogResult RunFilesCreateFromExpressionsDialog(NEVariables variables, Coder.CodePage codePage) => Dispatcher.Invoke(() => FilesCreateFromExpressionsDialog.Run(this, variables, codePage));
		public FilesFindDialogResult RunFilesFindDialog(NEVariables variables) => Dispatcher.Invoke(() => FilesFindDialog.Run(this, variables));
		public FilesHashDialogResult RunFilesHashDialog() => Dispatcher.Invoke(() => FilesHashDialog.Run(this));
		public FilesInsertDialogResult RunFilesInsertDialog() => Dispatcher.Invoke(() => FilesInsertDialog.Run(this));
		public FilesNamesGetUniqueDialogResult RunFilesNamesGetUniqueDialog() => Dispatcher.Invoke(() => FilesNamesGetUniqueDialog.Run(this));
		public FilesNamesMakeAbsoluteRelativeDialogResult RunFilesNamesMakeAbsoluteRelativeDialog(NEVariables variables, bool absolute, bool checkType) => Dispatcher.Invoke(() => FilesNamesMakeAbsoluteRelativeDialog.Run(this, variables, absolute, checkType));
		public FilesOperationsCombineFilesDialogResult RunFilesOperationsCombineFilesDialog(NEVariables variables) => Dispatcher.Invoke(() => FilesOperationsCombineFilesDialog.Run(this, variables));
		public FilesOperationsCopyMoveDialogResult RunFilesOperationsCopyMoveDialog(NEVariables variables, bool move) => Dispatcher.Invoke(() => FilesOperationsCopyMoveDialog.Run(this, variables, move));
		public FilesOperationsEncodingDialogResult RunFilesOperationsEncodingDialog() => Dispatcher.Invoke(() => FilesOperationsEncodingDialog.Run(this));
		public FilesOperationsSplitFileDialogResult RunFilesOperationsSplitFileDialog(NEVariables variables) => Dispatcher.Invoke(() => FilesOperationsSplitFileDialog.Run(this, variables));
		public FilesSelectByVersionControlStatusDialogResult RunFilesSelectByVersionControlStatusDialog() => Dispatcher.Invoke(() => FilesSelectByVersionControlStatusDialog.Run(this));
		public FilesSetAttributesDialogResult RunFilesSetAttributesDialog(Dictionary<FileAttributes, bool?> attributes) => Dispatcher.Invoke(() => FilesSetAttributesDialog.Run(this, attributes));
		public FilesSetSizeDialogResult RunFilesSetSizeDialog(NEVariables variables) => Dispatcher.Invoke(() => FilesSetSizeDialog.Run(this, variables));
		public FilesSetTimeDialogResult RunFilesSetTimeDialog(NEVariables variables, string expression) => Dispatcher.Invoke(() => FilesSetTimeDialog.Run(this, variables, expression));
		public FilesSignDialogResult RunFilesSignDialog() => Dispatcher.Invoke(() => FilesSignDialog.Run(this));
		public GetExpressionDialogResult RunGetExpressionDialog(NEVariables variables, int? numRows = null) => Dispatcher.Invoke(() => GetExpressionDialog.Run(this, variables, numRows));
		public ImageAddOverlayColorDialogResult RunImageAddOverlayColorDialog(bool add, NEVariables variables) => Dispatcher.Invoke(() => ImageAddOverlayColorDialog.Run(this, add, variables));
		public ImageAdjustColorDialogResult RunImageAdjustColorDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageAdjustColorDialog.Run(this, variables));
		public ImageCropDialogResult RunImageCropDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageCropDialog.Run(this, variables));
		public ImageGIFAnimateDialogResult RunImageGIFAnimateDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageGIFAnimateDialog.Run(this, variables));
		public ImageGIFSplitDialogResult RunImageGIFSplitDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageGIFSplitDialog.Run(this, variables));
		public ImageGrabColorDialogResult RunImageGrabColorDialog(string color) => Dispatcher.Invoke(() => ImageGrabColorDialog.Run(this, color));
		public ImageGrabImageDialogResult RunImageGrabImageDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageGrabImageDialog.Run(this, variables));
		public ImageRotateDialogResult RunImageRotateDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageRotateDialog.Run(this, variables));
		public ImageSizeDialogResult RunImageSizeDialog(NEVariables variables) => Dispatcher.Invoke(() => ImageSizeDialog.Run(this, variables));
		public MacroPlayRepeatDialogResult RunMacroPlayRepeatDialog(Func<string> chooseMacro) => Dispatcher.Invoke(() => MacroPlayRepeatDialog.Run(this, chooseMacro));
		public NetworkAbsoluteURLDialogResult RunNetworkAbsoluteURLDialog(NEVariables variables) => Dispatcher.Invoke(() => NetworkAbsoluteURLDialog.Run(this, variables));
		public NetworkFetchFileDialogResult RunNetworkFetchFileDialog(NEVariables variables) => Dispatcher.Invoke(() => NetworkFetchFileDialog.Run(this, variables));
		public NetworkFetchStreamDialogResult RunNetworkFetchStreamDialog(NEVariables variables, string outputDirectory) => Dispatcher.Invoke(() => NetworkFetchStreamDialog.Run(this, variables, outputDirectory));
		public NetworkPingDialogResult RunNetworkPingDialog() => Dispatcher.Invoke(() => NetworkPingDialog.Run(this));
		public NetworkScanPortsDialogResult RunNetworkScanPortsDialog() => Dispatcher.Invoke(() => NetworkScanPortsDialog.Run(this));
		public NetworkWCFInterceptCallsDialogResult RunNetworkWCFInterceptCallsDialog() => Dispatcher.Invoke(() => NetworkWCFInterceptCallsDialog.Run(this));
		public NumericCombinationsPermutationsDialogResult RunNumericCombinationsPermutationsDialog() => Dispatcher.Invoke(() => NumericCombinationsPermutationsDialog.Run(this));
		public NumericConvertBaseDialogResult RunNumericConvertBaseDialog() => Dispatcher.Invoke(() => NumericConvertBaseDialog.Run(this));
		public NumericCycleDialogResult RunNumericCycleDialog(NEVariables variables) => Dispatcher.Invoke(() => NumericCycleDialog.Run(this, variables));
		public NumericFloorRoundCeilingDialogResult RunNumericFloorRoundCeilingDialog(string title, NEVariables variables) => Dispatcher.Invoke(() => NumericFloorRoundCeilingDialog.Run(this, title, variables));
		public NumericLimitDialogResult RunNumericLimitDialog(NEVariables variables) => Dispatcher.Invoke(() => NumericLimitDialog.Run(this, variables));
		public NumericMinMaxValuesDialogResult RunNumericMinMaxValuesDialog() => Dispatcher.Invoke(() => NumericMinMaxValuesDialog.Run(this));
		public NumericRandomNumberDialogResult RunNumericRandomNumberDialog(NEVariables variables) => Dispatcher.Invoke(() => NumericRandomNumberDialog.Run(this, variables));
		public NumericScaleDialogResult RunNumericScaleDialog(NEVariables variables) => Dispatcher.Invoke(() => NumericScaleDialog.Run(this, variables));
		public NumericSeriesDialogResult RunNumericSeriesDialog(bool linear, NEVariables variables) => Dispatcher.Invoke(() => NumericSeriesDialog.Run(this, linear, variables));
		public PositionGotoDialogResult RunPositionGotoDialog(GotoType gotoType, int startValue, NEVariables variables) => Dispatcher.Invoke(() => PositionGotoDialog.Run(this, gotoType, startValue, variables));
		public SelectByCountDialogResult RunSelectByCountDialog() => Dispatcher.Invoke(() => SelectByCountDialog.Run(this));
		public SelectLimitDialogResult RunSelectLimitDialog(NEVariables variables) => Dispatcher.Invoke(() => SelectLimitDialog.Run(this, variables));
		public SelectSplitDialogResult RunSelectSplitDialog(NEVariables variables) => Dispatcher.Invoke(() => SelectSplitDialog.Run(this, variables));
		public TableAddColumnDialogResult RunTableAddColumnDialog(NEVariables variables, int numRows) => Dispatcher.Invoke(() => TableAddColumnDialog.Run(this, variables, numRows));
		public TableConvertDialogResult RunTableConvertDialog(ParserType tableType) => Dispatcher.Invoke(() => TableConvertDialog.Run(this, tableType));
		public TableDatabaseGenerateDeletesDialogResult RunTableDatabaseGenerateDeletesDialog(Table table, string tableName) => Dispatcher.Invoke(() => TableDatabaseGenerateDeletesDialog.Run(this, table, tableName));
		public TableDatabaseGenerateInsertsDialogResult RunTableDatabaseGenerateInsertsDialog(Table table, string tableName) => Dispatcher.Invoke(() => TableDatabaseGenerateInsertsDialog.Run(this, table, tableName));
		public TableDatabaseGenerateUpdatesDialogResult RunTableDatabaseGenerateUpdatesDialog(Table table, string tableName) => Dispatcher.Invoke(() => TableDatabaseGenerateUpdatesDialog.Run(this, table, tableName));
		public TableEditTableDialogResult RunTableEditTableDialog(Table input) => Dispatcher.Invoke(() => TableEditTableDialog.Run(this, input));
		public TableJoinDialogResult RunTableJoinDialog(Table leftTable, Table rightTable) => Dispatcher.Invoke(() => TableJoinDialog.Run(this, leftTable, rightTable));
		public TableTextToTableDialogResult RunTableTextToTableDialog(string text) => Dispatcher.Invoke(() => TableTextToTableDialog.Run(this, text));
		public TextFirstDistinctDialogResult RunTextFirstDistinctDialog() => Dispatcher.Invoke(() => TextFirstDistinctDialog.Run(this));
		public TextRandomTextDialogResult RunTextRandomTextDialog(NEVariables variables) => Dispatcher.Invoke(() => TextRandomTextDialog.Run(this, variables));
		public TextReverseRegExDialogResult RunTextReverseRegExDialog() => Dispatcher.Invoke(() => TextReverseRegExDialog.Run(this));
		public TextSelectWholeBoundedWordDialogResult RunTextSelectWholeBoundedWordDialog(bool wholeWord) => Dispatcher.Invoke(() => TextSelectWholeBoundedWordDialog.Run(this, wholeWord));
		public TextTrimDialogResult RunTextTrimDialog() => Dispatcher.Invoke(() => TextTrimDialog.Run(this));
		public TextUnicodeDialogResult RunTextUnicodeDialog() => Dispatcher.Invoke(() => TextUnicodeDialog.Run(this));
		public TextWidthDialogResult RunTextWidthDialog(bool numeric, bool isSelect, NEVariables variables) => Dispatcher.Invoke(() => TextWidthDialog.Run(this, numeric, isSelect, variables));
		public WindowLayout RunWindowCustomGridDialog(WindowLayout windowLayout) => Dispatcher.Invoke(() => WindowCustomGridDialog.Run(this, windowLayout));
		public void RunWindowActiveTabsDialog(WindowActiveTabsDialogData data) => Dispatcher.Invoke(() => WindowActiveTabsDialog.Run(this, data));
		public void RunWindowFontSizeDialog() => Dispatcher.Invoke(() => WindowFontSizeDialog.Run(this));
		public NetworkWCFGetConfigResult RunNetworkWCFGetConfigDialog() => Dispatcher.Invoke(() => NetworkWCFGetConfigDialog.Run(this));
		public void RunWCFInterceptDialog() => Dispatcher.Invoke(() => WCFInterceptDialog.Run(this));
		public T RunProgressDialog<T>(string text, Func<Func<bool>, Action<int>, T> action) => Dispatcher.Invoke(() => ProgressDialog.Run(this, text, action));
		public void RunProgressDialog(string text, Action<Func<bool>, Action<int>> action) => Dispatcher.Invoke(() => ProgressDialog.Run(this, text, action));
		public HashSet<Coder.CodePage> RunCodePagesDialog(HashSet<Coder.CodePage> startCodePages = null) => Dispatcher.Invoke(() => CodePagesDialog.Run(this, startCodePages));
		public void RunHelpAboutDialog() => Dispatcher.Invoke(() => HelpAboutDialog.Run(this));

		public OpenFileDialogResult RunOpenFileDialog(string defaultExt, string initialDirectory = null, string filter = null, int filterIndex = 0, bool multiselect = false)
		{
			return Dispatcher.Invoke(() =>
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
			return Dispatcher.Invoke(() =>
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
