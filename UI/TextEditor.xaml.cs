using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NeoEdit.Records;
using NeoEdit.Records.Disk;

namespace NeoEdit.UI
{
	/// <summary>
	/// Interaction logic for TextEditor.xaml
	/// </summary>
	public partial class TextEditor : UIWindow
	{
		static TextEditor()
		{
			Register<TextEditor>();
		}

		[DepProp]
		public FontFamily fontFamily { get { return GetProp<FontFamily>(); } set { SetProp(value); } }
		[DepProp]
		public double fontSize { get { return GetProp<double>(); } set { SetProp(value); } }
		[DepProp]
		public int lines { get { return GetProp<int>(); } set { SetProp(value); } }
		[DepProp]
		public int cols { get { return GetProp<int>(); } set { SetProp(value); } }
		[DepProp]
		public int viewportLines { get { return GetProp<int>(); } set { SetProp(value); } }
		[DepProp]
		public int viewportCols { get { return GetProp<int>(); } set { SetProp(value); } }
		[DepProp]
		public int onLine { get { return GetProp<int>(); } set { SetProp(value); } }
		[DepProp]
		public int onCol { get { return GetProp<int>(); } set { SetProp(value); } }

		int fontHeight { get { return (int)Math.Ceiling(fontSize * fontFamily.LineSpacing); } }
		int fontWidth { get { return (int)fontSize; } }

		TextFile textFile;
		public TextEditor() : this(@"C:\Docs\Cpp\NeoEdit\UI\Encodings\UTF8.txt") { }
		public TextEditor(string filename)
		{
			InitializeComponent();
			textFile = new TextFile(new DiskFile(filename));
			lines = textFile.numLines;
			cols = textFile.numCols;
			fontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./UI/Resources/#Anonymous Pro");
			fontSize = 16;
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
