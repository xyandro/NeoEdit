using System;

namespace NeoEdit.Records.Processes
{
	public class ProcessRecord : Record
	{
		public ProcessRecord(string uri) : base(uri) { }

		public override Record Parent
		{
			get
			{
				if (this is ProcessRoot)
					return new Root();
				if (this is Process)
					return new ProcessRoot();
				throw new ArgumentException();
			}
		}

		public override Type GetRootType()
		{
			return typeof(ProcessRecord);
		}
	}
}
