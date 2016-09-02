using System;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Handles
{
	public class HandleItem : DependencyObject
	{
		[DepProp]
		public int PID { get { return UIHelper<HandleItem>.GetPropValue<int>(this); } set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public IntPtr Handle { get { return UIHelper<HandleItem>.GetPropValue<IntPtr>(this); } set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Type { get { return UIHelper<HandleItem>.GetPropValue<string>(this); } set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Name { get { return UIHelper<HandleItem>.GetPropValue<string>(this); } set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<HandleItem>.GetPropValue<string>(this); } set { UIHelper<HandleItem>.SetPropValue(this, value); } }

		static HandleItem() { UIHelper<HandleItem>.Register(); }
	}
}
