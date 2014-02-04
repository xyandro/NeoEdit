using System.Windows.Controls;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class Value : TextBox
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public string Type { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static Value() { UIHelper<Value>.Register(); }

		readonly UIHelper<Value> uiHelper;
		public Value()
		{
			uiHelper = new UIHelper<Value>(this);
			InitializeComponent();
		}
	}
}
