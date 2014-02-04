using System.Windows.Controls;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.BinaryEditorUI
{
	public partial class Values : StackPanel
	{
		[DepProp]
		public byte[] Data { get { return uiHelper.GetPropValue<byte[]>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelStart { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public long SelEnd { get { return uiHelper.GetPropValue<long>(); } set { uiHelper.SetPropValue(value); } }

		static Values() { UIHelper<Values>.Register(); }

		readonly UIHelper<Values> uiHelper;
		public Values()
		{
			uiHelper = new UIHelper<Values>(this);
			InitializeComponent();
		}
	}
}
