using System.IO;

namespace NeoEdit.Records
{
	public abstract class Record
	{
		protected Record(string uri) { FullName = uri; }
		public abstract RecordList Parent { get; }
		public virtual string FullName { get; private set; }

		string name;
		public virtual string Name
		{
			get { return (name == null) ? Path.GetFileName(FullName) : name; }
			protected set { name = value; }
		}
	}
}
