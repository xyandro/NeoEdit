using System;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.Win32;

namespace NeoEdit.Handles
{
	public class HandleItem : DependencyObject
	{
		[DepProp]
		public int PID { get { return UIHelper<HandleItem>.GetPropValue(() => this.PID); } private set { UIHelper<HandleItem>.SetPropValue(() => this.PID, value); } }
		[DepProp]
		public IntPtr Handle { get { return UIHelper<HandleItem>.GetPropValue(() => this.Handle); } private set { UIHelper<HandleItem>.SetPropValue(() => this.Handle, value); } }
		[DepProp]
		public string Type { get { return UIHelper<HandleItem>.GetPropValue(() => this.Type); } private set { UIHelper<HandleItem>.SetPropValue(() => this.Type, value); } }
		[DepProp]
		public string Name { get { return UIHelper<HandleItem>.GetPropValue(() => this.Name); } private set { UIHelper<HandleItem>.SetPropValue(() => this.Name, value); } }
		[DepProp]
		public string Data { get { return UIHelper<HandleItem>.GetPropValue(() => this.Data); } private set { UIHelper<HandleItem>.SetPropValue(() => this.Data, value); } }

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
