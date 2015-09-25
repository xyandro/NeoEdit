using System.Windows.Controls;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.GUI.Controls
{
	public class TabsControl : UserControl
	{
		[DepProp]
		public int ItemOrder { get { return UIHelper<TabsControl>.GetPropValue<int>(this); } set { UIHelper<TabsControl>.SetPropValue(this, value); } }
		[DepProp]
		public bool Active { get { return UIHelper<TabsControl>.GetPropValue<bool>(this); } set { UIHelper<TabsControl>.SetPropValue(this, value); } }
		[DepProp]
		public string TabLabel { get { return UIHelper<TabsControl>.GetPropValue<string>(this); } set { UIHelper<TabsControl>.SetPropValue(this, value); } }

		static TabsControl() { UIHelper<TabsControl>.Register(); }

		public virtual bool Empty()
		{
			return false;
		}

		public bool CanClose()
		{
			Message.OptionsEnum answer = Message.OptionsEnum.None;
			return CanClose(ref answer);
		}

		public virtual bool CanClose(ref Message.OptionsEnum answer)
		{
			return true;
		}

		public virtual void Closed()
		{
		}
	}
}
