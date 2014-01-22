using System.IO;
using System.Collections.Generic;

namespace NeoEdit.Records
{
	public abstract class Record
	{
		public enum Property
		{
			FullName,
			Name,
			Size,
			WriteTime,
		};
		public static Dictionary<Property, string> DisplayName = new Dictionary<Property, string>
		{
			{ Property.FullName, "Full Name" },
			{ Property.Name, "Name" },
			{ Property.Size, "Size" },
			{ Property.WriteTime, "Last Write" },
		};
		protected Record(string uri, RecordList parent) { this[Property.FullName] = uri; Parent = parent == null ? this as RecordList : parent; }
		public RecordList Parent { get; private set; }
		public string FullName { get { return Prop<string>(Property.FullName); } }

		Dictionary<Property, object> properties = new Dictionary<Property, object>();

		public T Prop<T>(Property property)
		{
			return (T)this[property];
		}

		public object this[Property property]
		{
			get
			{
				if (properties.ContainsKey(property))
					return properties[property];

				switch (property)
				{
					case Property.Name: return Path.GetFileName(FullName);
					default: return null;
				}
			}
			protected set { properties[property] = value; }
		}

		public virtual string Name
		{
			get { return Prop<string>(Property.Name); }
		}
	}
}
