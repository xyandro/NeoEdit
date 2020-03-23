using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

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
