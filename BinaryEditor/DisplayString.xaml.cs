using System.Windows.Controls;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	partial class DisplayString : TextBox
	{
		[DepProp]
		public BinaryEditor ParentWindow { get { return UIHelper<DisplayString>.GetPropValue<BinaryEditor>(this); } set { UIHelper<DisplayString>.SetPropValue(this, value); } }
		[DepProp]
		public StrCoder.CodePage Type { get { return UIHelper<DisplayString>.GetPropValue<StrCoder.CodePage>(this); } set { UIHelper<DisplayString>.SetPropValue(this, value); } }

		static DisplayString() { UIHelper<DisplayString>.Register(); }

		public DisplayString()
		{
			InitializeComponent();
			LostFocus += (s, e) => UIHelper<DisplayString>.InvalidateBinding(this, TextProperty);
		}
	}
}
