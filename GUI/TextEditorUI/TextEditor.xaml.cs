using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Data;
using NeoEdit.GUI.Records;

namespace NeoEdit.GUI.TextEditorUI
{
	public partial class TextEditor : Window
	{
		public static RoutedUICommand Command_File_New = new RoutedUICommand { Text = "_New" };
		public static RoutedUICommand Command_File_Open = new RoutedUICommand { Text = "_Open" };
		public static RoutedUICommand Command_File_Save = new RoutedUICommand { Text = "_Save" };
		public static RoutedUICommand Command_File_SaveAs = new RoutedUICommand { Text = "Save _As" };
		public static RoutedUICommand Command_File_Exit = new RoutedUICommand { Text = "_Exit" };
		public static RoutedUICommand Command_Edit_Undo = new RoutedUICommand { Text = "_Undo" };
		public static RoutedUICommand Command_Edit_Cut = new RoutedUICommand { Text = "C_ut" };
		public static RoutedUICommand Command_Edit_Copy = new RoutedUICommand { Text = "_Copy" };
		public static RoutedUICommand Command_Edit_Paste = new RoutedUICommand { Text = "_Paste" };
		public static RoutedUICommand Command_Edit_ShowClipboard = new RoutedUICommand { Text = "_Show Clipboard" };
		public static RoutedUICommand Command_Edit_Find = new RoutedUICommand { Text = "_Find" };
		public static RoutedUICommand Command_Edit_FindNext = new RoutedUICommand { Text = "Find _Next" };
		public static RoutedUICommand Command_Edit_FindPrev = new RoutedUICommand { Text = "Find _Prev" };
		public static RoutedUICommand Command_Edit_GotoLine = new RoutedUICommand { Text = "Goto _line" };
		public static RoutedUICommand Command_Edit_GotoIndex = new RoutedUICommand { Text = "Goto c_olumn" };
		public static RoutedUICommand Command_Edit_BOM = new RoutedUICommand { Text = "_BOM" };
		public static RoutedUICommand Command_Data_ToUpper = new RoutedUICommand { Text = "Upper case" };
		public static RoutedUICommand Command_Data_ToLower = new RoutedUICommand { Text = "Lower case" };
		public static RoutedUICommand Command_Data_ToHex = new RoutedUICommand { Text = "To hex" };
		public static RoutedUICommand Command_Data_FromHex = new RoutedUICommand { Text = "From hex" };
		public static RoutedUICommand Command_Data_ToChar = new RoutedUICommand { Text = "To char" };
		public static RoutedUICommand Command_Data_FromChar = new RoutedUICommand { Text = "From char" };
		public static RoutedUICommand Command_Data_Width = new RoutedUICommand { Text = "_Width" };
		public static RoutedUICommand Command_Data_Trim = new RoutedUICommand { Text = "_Trim" };
		public static RoutedUICommand Command_SelectMark_Toggle = new RoutedUICommand { Text = "Toggle marks/selection" };
		public static RoutedUICommand Command_Select_All = new RoutedUICommand { Text = "_All" };
		public static RoutedUICommand Command_Select_Unselect = new RoutedUICommand { Text = "_Unselect" };
		public static RoutedUICommand Command_Select_Single = new RoutedUICommand { Text = "_Single" };
		public static RoutedUICommand Command_Select_Lines = new RoutedUICommand { Text = "_Lines" };
		public static RoutedUICommand Command_Select_Marks = new RoutedUICommand { Text = "_Marks" };
		public static RoutedUICommand Command_Select_Find = new RoutedUICommand { Text = "_Find results" };
		public static RoutedUICommand Command_Select_Reverse = new RoutedUICommand { Text = "_Reverse selections" };
		public static RoutedUICommand Command_Select_Sort = new RoutedUICommand { Text = "S_ort selections" };
		public static RoutedUICommand Command_Select_Evaluate = new RoutedUICommand { Text = "_Evaluate selections" };
		public static RoutedUICommand Command_Mark_Selection = new RoutedUICommand { Text = "_Selection" };
		public static RoutedUICommand Command_Mark_Find = new RoutedUICommand { Text = "_Find results" };
		public static RoutedUICommand Command_Mark_Clear = new RoutedUICommand { Text = "_Clear marks" };
		public static RoutedUICommand Command_Mark_LimitToSelection = new RoutedUICommand { Text = "_Limit to selection" };

		[DepProp]
		public TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long ChangeCount { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Coder.Type CoderUsed { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditor() { UIHelper<TextEditor>.Register(); }

		readonly UIHelper<TextEditor> uiHelper;
		Record record;
		public TextEditor(Record _record = null, TextData data = null)
		{
			uiHelper = new UIHelper<TextEditor>(this);
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
			CoderUsed = Data.CoderUsed;
			TextData.ChangedDelegate changed = () => ++ChangeCount;
			Data.Changed += changed;
			Closed += (s, e) => Data.Changed -= changed;

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
			else if (command == Command_Edit_ShowClipboard) Clipboard.Show();
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
			new BinaryEditorUI.BinaryEditor(record, new MemoryBinaryData(data));
			this.Close();
		}
	}
}
