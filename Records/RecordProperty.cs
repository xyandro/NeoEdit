using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records
{
	public class RecordProperty
	{
		public enum PropertyName
		{
			FullName,
			Name,
			Path,
			Extension,
			Size,
			WriteTime,
			CreateTime,
			AccessTime,
			Type,
			Data,
		};

		public PropertyName Name { get; private set; }
		public string DisplayName { get; private set; }
		public string MenuHeader { get; private set; }
		public bool DefaultAscending { get; private set; }
		public Type Type { get; private set; }

		static List<RecordProperty> properties = new List<RecordProperty>
		{
			new RecordProperty { Name = PropertyName.FullName,   DisplayName = "Full Name",   MenuHeader = "_Full Name",   DefaultAscending = true,  Type = typeof(String)    },
			new RecordProperty { Name = PropertyName.Name,       DisplayName = "Name",        MenuHeader = "_Name",        DefaultAscending = true,  Type = typeof(String)    },
			new RecordProperty { Name = PropertyName.Path,       DisplayName = "Path",        MenuHeader = "_Path",        DefaultAscending = true,  Type = typeof(String)    },
			new RecordProperty { Name = PropertyName.Extension,  DisplayName = "Extension",   MenuHeader = "_Extension",   DefaultAscending = true,  Type = typeof(String)    },
			new RecordProperty { Name = PropertyName.Size,       DisplayName = "Size",        MenuHeader = "_Size",        DefaultAscending = false, Type = typeof(Int64?)    },
			new RecordProperty { Name = PropertyName.WriteTime,  DisplayName = "Last Write",  MenuHeader = "Last _Write",  DefaultAscending = false, Type = typeof(DateTime?) },
			new RecordProperty { Name = PropertyName.CreateTime, DisplayName = "Created",     MenuHeader = "_Created",     DefaultAscending = false, Type = typeof(DateTime?) },
			new RecordProperty { Name = PropertyName.AccessTime, DisplayName = "Last Access", MenuHeader = "Last _Access", DefaultAscending = false, Type = typeof(DateTime?) },
			new RecordProperty { Name = PropertyName.Type,       DisplayName = "Type",        MenuHeader = "_Type",        DefaultAscending = true,  Type = typeof(String)    },
			new RecordProperty { Name = PropertyName.Data,       DisplayName = "Data",        MenuHeader = "_Data",        DefaultAscending = true,  Type = typeof(String)    },
		};

		public static RecordProperty Get(PropertyName type)
		{
			return properties.Single(a => a.Name == type);
		}

		public static PropertyName PropertyFromMenuHeader(string str)
		{
			return properties.Single(a => a.MenuHeader == str).Name;
		}
	}
}
