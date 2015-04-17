using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit
{
	partial class DisplayValue : TextBox
	{
		[DepProp]
		public HexEditor HexEditor { get { return UIHelper<DisplayValue>.GetPropValue(() => this.HexEditor); } set { UIHelper<DisplayValue>.SetPropValue(() => this.HexEditor, value); } }
		[DepProp]
		public Coder.CodePage CodePage { get { return UIHelper<DisplayValue>.GetPropValue(() => this.CodePage); } set { UIHelper<DisplayValue>.SetPropValue(() => this.CodePage, value); } }

		static DisplayValue() { UIHelper<DisplayValue>.Register(); }

		public DisplayValue()
		{
			InitializeComponent();
			LostFocus += (s, e) => this.InvalidateBinding(TextProperty);
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

						var data = Coder.TryStringToBytes(Text, CodePage);
						if (data == null)
							break;

						HexEditor.DisplayValuesReplace(data);
					}
					break;
			}
		}
	}
}
