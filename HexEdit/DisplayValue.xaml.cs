using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit
{
	partial class DisplayValue : TextBox
	{
		[DepProp]
		public HexEditor HexEditor { get { return UIHelper<DisplayValue>.GetPropValue<HexEditor>(this); } set { UIHelper<DisplayValue>.SetPropValue(this, value); } }
		[DepProp]
		public Coder.Type Type { get { return UIHelper<DisplayValue>.GetPropValue<Coder.Type>(this); } set { UIHelper<DisplayValue>.SetPropValue(this, value); } }

		static DisplayValue() { UIHelper<DisplayValue>.Register(); }

		public DisplayValue()
		{
			InitializeComponent();
			LostFocus += (s, e) => UIHelper<DisplayValue>.InvalidateBinding(this, TextProperty);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.Handled)
				return;

			switch (e.Key)
			{
				case Key.Enter:
					{
						e.Handled = true;
						if (IsReadOnly)
							break;

						var data = Coder.TryStringToBytes(Text, Type);
						if (data == null)
							break;

						HexEditor.DisplayValuesReplace(data);
					}
					break;
			}
		}
	}
}
