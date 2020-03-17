using System.Collections.Generic;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Controls
{
	partial class ViewValues
	{
		[DepProp]
		public IList<byte> Data { get { return UIHelper<ViewValues>.GetPropValue<IList<byte>>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public bool HasSel { get { return UIHelper<ViewValues>.GetPropValue<bool>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }
		[DepProp]
		public HashSet<Coder.CodePage> CodePages { get { return UIHelper<ViewValues>.GetPropValue<HashSet<Coder.CodePage>>(this); } set { UIHelper<ViewValues>.SetPropValue(this, value); } }

		static ViewValues() => UIHelper<ViewValues>.Register();

		public ViewValues() => InitializeComponent();
	}
}
