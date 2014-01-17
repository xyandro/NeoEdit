using System.IO;

namespace NeoEdit.Records
{
	public abstract class Record
	{
		protected Record(string uri, RecordList parent) { FullName = uri; Parent = parent == null ? this as RecordList : parent; }
		public RecordList Parent { get; private set; }
		public string FullName { get; private set; }

		string name;
		public virtual string Name
		{
			get { return (name == null) ? Path.GetFileName(FullName) : name; }
			protected set { name = value; }
		}
	}
}
