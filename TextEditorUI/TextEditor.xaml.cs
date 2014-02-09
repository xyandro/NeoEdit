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

		protected override void OnTextInput(System.Windows.Input.TextCompositionEventArgs e)
		{
			base.OnTextInput(e);
			if (e.Handled)
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
	}
}
