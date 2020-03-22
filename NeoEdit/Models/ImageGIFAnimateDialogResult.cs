using System;
using System.Windows;
using NeoEdit.Program.Controls;
using NeoEdit.Program.Expressions;

namespace NeoEdit.Program.Models
{
	public class ImageGIFAnimateDialogResult
	{
		public string InputFiles { get; set; }
		public string OutputFile { get; set; }
		public string Delay { get; set; }
		public string Repeat { get; set; }
	}
}
