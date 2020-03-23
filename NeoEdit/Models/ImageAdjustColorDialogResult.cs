using System;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class ImageAdjustColorDialogResult
	{
		public string Expression { get; set; }
		public bool Alpha { get; set; }
		public bool Red { get; set; }
		public bool Green { get; set; }
		public bool Blue { get; set; }
	}
}
