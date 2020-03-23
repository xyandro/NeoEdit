using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using NeoEdit.Common.Expressions;
using NeoEdit.Common.Transform;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Models
{
	public class ImageCropDialogResult
	{
		public string XExpression { get; set; }
		public string YExpression { get; set; }
		public string WidthExpression { get; set; }
		public string HeightExpression { get; set; }
		public string FillColor { get; set; }
	}
}
