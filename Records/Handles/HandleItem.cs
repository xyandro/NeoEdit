using System;
using System.Collections.Generic;
using NeoEdit.Common;
using NeoEdit.Interop;

namespace NeoEdit.Records.Handles
{
	public class HandleItem : HandleRecord
	{
		public HandleItem(HandleInfo handle)
			: base(String.Format("{0}/{1}", handle.PID, handle.Handle))
		{
			SetProperty(RecordProperty.PropertyName.Type, handle.Type);
			SetProperty(RecordProperty.PropertyName.Name, handle.Name);
		}

		public override IEnumerable<RecordAction.ActionName> Actions
		{
			get
			{
				var actions = new List<RecordAction.ActionName>(base.Actions);
				if (GetProperty<string>(RecordProperty.PropertyName.Type) == "Section")
					actions.Add(RecordAction.ActionName.Open);
				return actions;
			}
		}

		public override BinaryData Read()
		{
			return new SharedMemoryBinaryData(FullName);
		}
	}
}
