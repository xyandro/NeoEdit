using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewValues
	{
		static ViewValues() => UIHelper<ViewValues>.Register();

		public ViewValues() => InitializeComponent();

		public void SetData(IList<byte> data, bool hasSel, HashSet<Coder.CodePage> codePages)
		{
			var controls = new List<FrameworkElement> { LE, Int08LEHeader, Int16LEHeader, Int32LEHeader, Int64LEHeader, Int08LESizing, Int16LESizing, Int32LESizing, Int64LESizing, UIntLE, SIntLE, UInt08LE, UInt16LE, UInt32LE, UInt64LE, SInt08LE, SInt16LE, SInt32LE, SInt64LE, BE, Int08BEHeader, Int16BEHeader, Int32BEHeader, Int64BEHeader, Int08BESizing, Int16BESizing, Int32BESizing, Int64BESizing, UIntBE, SIntBE, UInt08BE, UInt16BE, UInt32BE, UInt64BE, SInt08BE, SInt16BE, SInt32BE, SInt64BE, Float, SingleHeader, DoubleHeader, SingleSizing, DoubleSizing, Single, Double };
			controls.ForEach(control => control.Visibility = GetVisibility(codePages, control.Tag as string));
			controls.OfType<ViewValue>().ForEach(viewValue => viewValue.SetData(data, hasSel));
		}

		bool HasCodePage(HashSet<Coder.CodePage> codePages, string find)
		{
			if ((find.Length == 8) && (find.Substring(1, 5) == "Int08"))
			{
				var isLE = find.EndsWith("LE");
				find = find.Substring(0, 4) + "8";
				var list = codePages.Intersect(Coder.GetNumericCodePages()).Select(x => x.ToString()).ToList();
				var hasLE = list.Any(x => x.EndsWith("LE"));
				var hasBE = list.Any(x => x.EndsWith("BE"));
				if ((isLE) && (!hasLE) && (hasBE))
					return false;
				if ((!isLE) && (!hasBE))
					return false;
			}
			if (find.StartsWith("SInt"))
				find = find.Substring(1);
			return codePages.Any(x => x.ToString() == find);
		}

		Visibility GetVisibility(HashSet<Coder.CodePage> codePages, string find)
		{
			if (find.Split('|').Any(str => HasCodePage(codePages, str)))
				return Visibility.Visible;
			return Visibility.Collapsed;
		}
	}
}
