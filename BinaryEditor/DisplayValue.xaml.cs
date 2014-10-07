using System.Windows.Controls;
using System.Windows.Input;
using NeoEdit.BinaryEditor.Data;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.BinaryEditor
{
	partial class DisplayValue : TextBox
	{
		[DepProp]
		public BinaryEditor ParentWindow { get { return uiHelper.GetPropValue<BinaryEditor>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Coder.Type Type { get { return uiHelper.GetPropValue<Coder.Type>(); } set { uiHelper.SetPropValue(value); } }

		static DisplayValue() { UIHelper<DisplayValue>.Register(); }

		readonly UIHelper<DisplayValue> uiHelper;
		public DisplayValue()
		{
			uiHelper = new UIHelper<DisplayValue>(this);
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

						ParentWindow.DisplayValuesReplace(data);
					}
					break;
			}
		}
	}
}
