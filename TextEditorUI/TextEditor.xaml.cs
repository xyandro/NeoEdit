using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.TextEditorUI
{
	public partial class TextEditor : Window
	{
		[DepProp]
		public TextData Data { get { return uiHelper.GetPropValue<TextData>(); } set { uiHelper.SetPropValue(value); } }

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

			Show();
		}

		void CommandRun(UICommand command, object parameter)
		{
			canvas.CommandRun(command, parameter);
		}

		bool CommandCanRun(UICommand command, object parameter)
		{
			return canvas.CommandCanRun(command, parameter);
		}
	}
}
