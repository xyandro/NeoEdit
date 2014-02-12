using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
	public partial class TextEditor : Window
	{
		public const string Edit_Undo = "Edit_Undo";
		public const string Edit_Redo = "Edit_Redo";
		public const string Edit_Cut = "Edit_Cut";
		public const string Edit_Copy = "Edit_Copy";
		public const string Edit_Paste = "Edit_Paste";
		public const string Edit_Find = "Edit_Find";
		public const string Edit_FindNext = "Edit_FindNext";
		public const string Edit_FindPrev = "Edit_FindPrev";
		public const string Edit_GotoLine = "Edit_GotoLine";
		public const string Edit_GotoIndex = "Edit_GotoIndex";
		public const string Edit_BOM = "Edit_BOM";
		public const string Data_ToUpper = "Data_ToUpper";
		public const string Data_ToLower = "Data_ToLower";
		public const string Data_ToHex = "Data_ToHex";
		public const string Data_FromHex = "Data_FromHex";
		public const string Data_ToChar = "Data_ToChar";
		public const string Data_FromChar = "Data_FromChar";
		public const string Data_Width = "Data_Width";
		public const string Data_Trim = "Data_Trim";
		public const string Select_All = "Select_All";
		public const string Select_Unselect = "Select_Unselect";
		public const string Select_Single = "Select_Single";
		public const string Select_Lines = "Select_Lines";
		public const string Select_Marks = "Select_Marks";
		public const string Select_Find = "Select_Find";
		public const string Select_Reverse = "Select_Reverse";
		public const string Select_Sort = "Select_Sort";
		public const string Mark_Selection = "Mark_Selection";
		public const string Mark_Find = "Mark_Find";
		public const string Mark_Clear = "Mark_Clear";
		public const string Mark_LimitToSelection = "Mark_LimitToSelection";

		[DepProp]
		public TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Highlighting.HighlightingType HighlightType { get { return uiHelper.GetPropValue<Highlighting.HighlightingType>(); } set { uiHelper.SetPropValue(value); } }

		static TextEditor() { UIHelper<TextEditor>.Register(); }

		readonly UIHelper<TextEditor> uiHelper;
		public TextEditor() : this(GetData()) { }

		static TextData GetData()
		{
			var bytes = new BinaryData(System.IO.File.ReadAllBytes(@"C:\Docs\Cpp\NeoEdit\bin\Debug\Clipboard.cs"));
			return new TextData(bytes);
		}

		public TextEditor(TextData data)
		{
			uiHelper = new UIHelper<TextEditor>(this);
			InitializeComponent();
			uiHelper.InitializeCommands();

			Data = data;

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
			try
			{
				var header = (e.OriginalSource as MenuItem).Header as string;
				var encoding = Helpers.ParseEnum<BinaryData.EncodingName>(header);
				var data = Data.GetBinaryData(encoding);
				new BinaryEditorUI.BinaryEditor(data);
				this.Close();
			}
			catch (Exception ex) { MessageBox.Show(ex.Message, "Error"); }
		}
	}
}
