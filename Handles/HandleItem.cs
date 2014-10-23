using System;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.Win32;

namespace NeoEdit.Handles
{
	public class HandleItem : DependencyObject
	{
		[DepProp]
		public int PID { get { return UIHelper<HandleItem>.GetPropValue<int>(this); } private set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public IntPtr Handle { get { return UIHelper<HandleItem>.GetPropValue<IntPtr>(this); } private set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Type { get { return UIHelper<HandleItem>.GetPropValue<string>(this); } private set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Name { get { return UIHelper<HandleItem>.GetPropValue<string>(this); } private set { UIHelper<HandleItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Data { get { return UIHelper<HandleItem>.GetPropValue<string>(this); } private set { UIHelper<HandleItem>.SetPropValue(this, value); } }

		static HandleItem() { UIHelper<HandleItem>.Register(); }

		public HandleItem(HandleInfo info)
		{
			PID = info.PID;
			Handle = info.Handle;
			Type = info.Type;
			Name = info.Name;
			Data = info.Data;
		}
	}
}
