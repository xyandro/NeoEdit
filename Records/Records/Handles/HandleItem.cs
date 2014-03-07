using System;
using NeoEdit.GUI.Common;
using NeoEdit.Win32Interop;

namespace NeoEdit.GUI.Records.Handles
{
	public class HandleItem : HandleRecord
	{
		public HandleItem(HandleInfo handle)
			: base(String.Format(@"Handles\{0}\{1}\{2}", handle.Type, handle.PID, handle.Handle))
		{
			SetProperty(RecordProperty.PropertyName.Handle, handle.Handle);
			SetProperty(RecordProperty.PropertyName.Type, handle.Type);
			SetProperty(RecordProperty.PropertyName.Name, handle.Name);
			SetProperty(RecordProperty.PropertyName.Data, handle.Data);
		}

		int GetPID()
		{
			return Convert.ToInt32(FullName.Split('\\')[2]);
		}

		IntPtr GetHandle()
		{
			return (IntPtr)Convert.ToInt64(FullName.Split('\\')[3]);
		}

		public override bool CanOpen()
		{
			return GetProperty<string>(RecordProperty.PropertyName.Type) == "Section";
		}

		public override BinaryData Read()
		{
			return new SharedMemoryBinaryData(GetPID(), GetHandle());
		}
	}
}
