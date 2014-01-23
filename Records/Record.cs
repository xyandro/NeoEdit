using System.Collections.Generic;
using System.IO;

namespace NeoEdit.Records
{
	public abstract class Record
	{
		protected Record(string uri, RecordList parent)
		{
			this[Property.PropertyType.FullName] = uri;
			this[Property.PropertyType.Name] = Path.GetFileName(FullName);
			this[Property.PropertyType.Path] = Path.GetDirectoryName(FullName);
			this[Property.PropertyType.Extension] = Path.GetExtension(FullName);

			Parent = parent == null ? this as RecordList : parent;
		}
		public RecordList Parent { get; private set; }
		public string FullName { get { return Prop<string>(Property.PropertyType.FullName); } }

		Dictionary<Property.PropertyType, object> properties = new Dictionary<Property.PropertyType, object>();

		public IEnumerable<Property.PropertyType> Properties { get { return properties.Keys; } }

		public T Prop<T>(Property.PropertyType property)
		{
			return (T)this[property];
		}

		public object this[Property.PropertyType property]
		{
			get
			{
				if (properties.ContainsKey(property))
					return properties[property];

				return null;
			}
			protected set { properties[property] = value; }
		}

		public virtual string Name
		{
			get { return Prop<string>(Property.PropertyType.Name); }
		}
	}
}
