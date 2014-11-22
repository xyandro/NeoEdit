using System.Windows.Controls;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit
{
	partial class DisplayString : TextBox
	{
		[DepProp]
		public HexEditor HexEditor { get { return UIHelper<DisplayString>.GetPropValue<HexEditor>(this); } set { UIHelper<DisplayString>.SetPropValue(this, value); } }
		[DepProp]
		public StrCoder.CodePage CodePage { get { return UIHelper<DisplayString>.GetPropValue<StrCoder.CodePage>(this); } set { UIHelper<DisplayString>.SetPropValue(this, value); } }

		static DisplayString() { UIHelper<DisplayString>.Register(); }

		public DisplayString()
		{
			InitializeComponent();
			LostFocus += (s, e) => UIHelper<DisplayString>.InvalidateBinding(this, TextProperty);
		}
	}
}
