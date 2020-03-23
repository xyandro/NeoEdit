using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows;
using NeoEdit.Common.Expressions;
using NeoEdit.Program;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class ImageSizeDialogResult
	{
		public string WidthExpression { get; set; }
		public string HeightExpression { get; set; }
		public InterpolationMode InterpolationMode { get; set; }
	}
}
