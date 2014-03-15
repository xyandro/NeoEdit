using System;
using NeoEdit.GUI.Common;
using NeoEdit.GUI.ItemGridControl;
using NeoEdit.Win32;

namespace NeoEdit.Handles
{
	public class HandleItem : ItemGridItem<HandleItem>
	{
		[DepProp]
		public int PID { get { return GetValue<int>(); } private set { SetValue(value); } }
		[DepProp]
		public IntPtr Handle { get { return GetValue<IntPtr>(); } private set { SetValue(value); } }
		[DepProp]
		public string Type { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Name { get { return GetValue<string>(); } private set { SetValue(value); } }
		[DepProp]
		public string Data { get { return GetValue<string>(); } private set { SetValue(value); } }

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
