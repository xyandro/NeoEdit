using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Data;
using NeoEdit.Common.Transform;
using NeoEdit.GUI;
using NeoEdit.GUI.BinaryEditor;
using NeoEdit.GUI.Common;
using NeoEdit.Records;

namespace NeoEdit.TextEditor
{
	public partial class TextEditorWindow : Window
	{
		public static RoutedCommand Command_File_New = new RoutedCommand();
		public static RoutedCommand Command_File_Open = new RoutedCommand();
		public static RoutedCommand Command_File_Save = new RoutedCommand();
		public static RoutedCommand Command_File_SaveAs = new RoutedCommand();
		public static RoutedCommand Command_File_Exit = new RoutedCommand();
		public static RoutedCommand Command_Edit_Undo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Redo = new RoutedCommand();
		public static RoutedCommand Command_Edit_Cut = new RoutedCommand();
		public static RoutedCommand Command_Edit_Copy = new RoutedCommand();
		public static RoutedCommand Command_Edit_Paste = new RoutedCommand();
		public static RoutedCommand Command_Edit_ShowClipboard = new RoutedCommand();
		public static RoutedCommand Command_Edit_Find = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindNext = new RoutedCommand();
		public static RoutedCommand Command_Edit_FindPrev = new RoutedCommand();
		public static RoutedCommand Command_Edit_GotoLine = new RoutedCommand();
		public static RoutedCommand Command_Edit_GotoIndex = new RoutedCommand();
		public static RoutedCommand Command_Edit_BOM = new RoutedCommand();
		public static RoutedCommand Command_Data_ToUpper = new RoutedCommand();
		public static RoutedCommand Command_Data_ToLower = new RoutedCommand();
		public static RoutedCommand Command_Data_ToHex = new RoutedCommand();
		public static RoutedCommand Command_Data_FromHex = new RoutedCommand();
		public static RoutedCommand Command_Data_ToChar = new RoutedCommand();
		public static RoutedCommand Command_Data_FromChar = new RoutedCommand();
		public static RoutedCommand Command_Data_Width = new RoutedCommand();
		public static RoutedCommand Command_Data_Trim = new RoutedCommand();
		public static RoutedCommand Command_Data_SetKeys = new RoutedCommand();
		public static RoutedCommand Command_Data_SetValues = new RoutedCommand();
		public static RoutedCommand Command_Data_KeysToValues = new RoutedCommand();
		public static RoutedCommand Command_Data_Reverse = new RoutedCommand();
		public static RoutedCommand Command_Data_Sort = new RoutedCommand();
		public static RoutedCommand Command_Data_Evaluate = new RoutedCommand();
		public static RoutedCommand Command_Data_Duplicates = new RoutedCommand();
		public static RoutedCommand Command_Data_Randomize = new RoutedCommand();
		public static RoutedCommand Command_Data_Series = new RoutedCommand();
		public static RoutedCommand Command_SelectMark_Toggle = new RoutedCommand();
		public static RoutedCommand Command_Select_All = new RoutedCommand();
		public static RoutedCommand Command_Select_Unselect = new RoutedCommand();
		public static RoutedCommand Command_Select_Single = new RoutedCommand();
		public static RoutedCommand Command_Select_Lines = new RoutedCommand();
		public static RoutedCommand Command_Select_Marks = new RoutedCommand();
		public static RoutedCommand Command_Select_Find = new RoutedCommand();
		public static RoutedCommand Command_Mark_Selection = new RoutedCommand();
		public static RoutedCommand Command_Mark_Find = new RoutedCommand();
		public static RoutedCommand Command_Mark_Clear = new RoutedCommand();
		public static RoutedCommand Command_Mark_LimitToSelection = new RoutedCommand();

		[DepProp]
		TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditorWindow() { UIHelper<TextEditorWindow>.Register(); }

		readonly UIHelper<TextEditorWindow> uiHelper;
		Record record;
		public TextEditorWindow(Record _record = null, TextData data = null, int? line = null, int? column = null)
		{
			uiHelper = new UIHelper<TextEditorWindow>(this);
			InitializeComponent();

			record = _record;
			if (data == null)
			{
				if (record == null)
					data = new TextData();
				else
					data = new TextData(record.Read().GetAllBytes());
			}
			Data = data;
			canvas.GotoPos(line.HasValue ? line.Value : 1, column.HasValue ? column.Value : 1);
			CoderUsed = Data.CoderUsed;

			KeyDown += (s, e) => uiHelper.RaiseEvent(canvas, e);
			MouseWheel += (s, e) => uiHelper.RaiseEvent(yScroll, e);
			yScroll.MouseWheel += (s, e) => (s as ScrollBar).Value -= e.Delta;

			Show();
		}

		protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
				return;

			if (e.OriginalSource is MenuItem)
				return;

			uiHelper.RaiseEvent(canvas, e);
		}

		void Command_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			RunCommand(e.Command);
		}

		void RunCommand(ICommand command)
		{
			canvas.RunCommand(command);

			if (command == Command_File_New)
			{
				record = null;
				Data = new TextData();
			}
			else if (command == Command_File_Open)
			{
				var dialog = new OpenFileDialog();
				if (dialog.ShowDialog() == true)
				{
					record = new Root().GetRecord(dialog.FileName);
					Data = new TextData(record.Read().GetAllBytes());
				}
			}
			else if (command == Command_File_Save)
			{
				if (record == null)
					RunCommand(Command_File_SaveAs);
				else
					record.Write(new MemoryBinaryData(Data.GetBytes(Data.CoderUsed)));
			}
			else if (command == Command_File_SaveAs)
			{
				var dialog = new SaveFileDialog();
				if (dialog.ShowDialog() == true)
				{
					if (Directory.Exists(dialog.FileName))
						throw new Exception("A directory by that name already exists.");
					if (!Directory.Exists(Path.GetDirectoryName(dialog.FileName)))
						throw new Exception("Directory doesn't exist.");
					var dir = new Root().GetRecord(Path.GetDirectoryName(dialog.FileName));
					record = dir.CreateFile(Path.GetFileName(dialog.FileName));
					RunCommand(Command_File_Save);
				}
			}
			else if (command == Command_File_Exit) Close();
			else if (command == Command_Edit_ShowClipboard) ClipboardWindow.Show();
		}

		void HighlightingClicked(object sender, RoutedEventArgs e)
		{
			var header = (sender as MenuItem).Header.ToString();
			HighlightType = Helpers.ParseEnum<Highlighting.HighlightingType>(header);
		}

		void EncodeClick(object sender, RoutedEventArgs e)
		{
			var header = (e.OriginalSource as MenuItem).Header as string;
			Coder.Type encoding;
			if (header == "Current")
				encoding = Data.CoderUsed;
			else
				encoding = Helpers.ParseEnum<Coder.Type>(header);
			var data = Data.GetBytes(encoding);
			new BinaryEditorWindow(record, new MemoryBinaryData(data));
			this.Close();
		}
	}
}
