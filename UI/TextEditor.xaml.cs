using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NeoEdit.Records;

namespace NeoEdit.UI
{
	/// <summary>
	/// Interaction logic for TextEditor.xaml
	/// </summary>
	public partial class TextEditor : Window
	{
		[DepProp]
		public FontFamily fontFamily { get { return uiHelper.GetProp<FontFamily>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public double fontSize { get { return uiHelper.GetProp<double>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public int lines { get { return uiHelper.GetProp<int>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public int cols { get { return uiHelper.GetProp<int>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public int viewportLines { get { return uiHelper.GetProp<int>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public int viewportCols { get { return uiHelper.GetProp<int>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public int onLine { get { return uiHelper.GetProp<int>(); } set { uiHelper.SetProp(value); } }
		[DepProp]
		public int onCol { get { return uiHelper.GetProp<int>(); } set { uiHelper.SetProp(value); } }

		int fontHeight { get { return (int)Math.Ceiling(fontSize * fontFamily.LineSpacing); } }
		int fontWidth { get { return (int)fontSize; } }

		readonly UIHelper<TextEditor> uiHelper;
		TextFile textFile = null;
		public TextEditor() : this(@"C:\Docs\Cpp\NeoEdit\UI\Encodings\UTF8.txt") { }
		public TextEditor(string filename)
		{
			uiHelper = new UIHelper<TextEditor>(this);
			//InitializeComponent();
			//textFile = new TextFile(new DiskFile(filename));
			//lines = textFile.numLines;
			//cols = textFile.numCols;
			//fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Resources/#Anonymous Pro");
			//fontSize = 16;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			Redraw();
			base.OnRender(drawingContext);
		}

		void Redraw()
		{
			var start = onLine;
			var numLines = Math.Min(lines - start, onLine + viewportLines);
			for (var line = 0; line < numLines; line++)
			{
				var text = new TextBlock() { Text = textFile.GetLine(start + line) };
				Canvas.SetTop(text, fontHeight * line);
				canvas.Children.Add(text);
			}
		}

		private void canvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			viewportLines = (int)Math.Ceiling(canvas.ActualHeight / fontHeight);
			InvalidateVisual();
		}
	}
}
