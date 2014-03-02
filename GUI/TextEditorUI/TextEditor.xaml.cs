using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.Data;
using NeoEdit.GUI.Records;

namespace NeoEdit.GUI.TextEditorUI
{
	public partial class TextEditor : Window
	{
		public enum Commands
		{
			File_New,
			File_Open,
			File_Save,
			File_SaveAs,
			File_Exit,
			Edit_Undo,
			Edit_Cut,
			Edit_Copy,
			Edit_Paste,
			Edit_ShowClipboad,
			Edit_Find,
			Edit_FindNext,
			Edit_FindPrev,
			Edit_GotoLine,
			Edit_GotoIndex,
			Edit_BOM,
			Data_ToUpper,
			Data_ToLower,
			Data_ToHex,
			Data_FromHex,
			Data_ToChar,
			Data_FromChar,
			Data_Width,
			Data_Trim,
			SelectMark_Toggle,
			Select_All,
			Select_Unselect,
			Select_Single,
			Select_Lines,
			Select_Marks,
			Select_Find,
			Select_Reverse,
			Select_Sort,
			Select_Evaluate,
			Mark_Selection,
			Mark_Find,
			Mark_Clear,
			Mark_LimitToSelection,
		}

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
			uiHelper.InitializeCommands();

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

		void CommandRun(UICommand command, object parameter)
		{
			canvas.CommandRun(command, parameter);

			switch ((Commands)command.Enum)
			{
				case Commands.File_New:
					record = null;
					Data = new TextData();
					break;
				case Commands.File_Open: break;
				case Commands.File_Save:
					if (record != null)
						record.Write(new MemoryBinaryData(Data.GetBytes(Data.CoderUsed)));
					break;
				case Commands.File_SaveAs: break;
				case Commands.File_Exit: Close(); break;
				case Commands.Edit_ShowClipboad: Clipboard.Show(); break;
			}
		}

		bool CommandCanRun(UICommand command, object parameter)
		{
			return canvas.CommandCanRun(command, parameter);
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
