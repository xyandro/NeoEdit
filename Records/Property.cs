using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Records
{
	public class Property
	{
		public enum PropertyType
		{
			FullName,
			Name,
			Path,
			Extension,
			Size,
			WriteTime,
			CreateTime,
			AccessTime,
		};

		public PropertyType Type { get; private set; }
		public string DisplayName { get; private set; }
		public bool DefaultAscending { get; private set; }

		static List<Property> properties = new List<Property>
		{
			new Property { Type = PropertyType.FullName,   DisplayName = "Full Name",   DefaultAscending = true  },
			new Property { Type = PropertyType.Name,       DisplayName = "Name",        DefaultAscending = true  },
			new Property { Type = PropertyType.Path,       DisplayName = "Path",        DefaultAscending = true  },
			new Property { Type = PropertyType.Extension,  DisplayName = "Extension",   DefaultAscending = true  },
			new Property { Type = PropertyType.Size,       DisplayName = "Size",        DefaultAscending = false },
			new Property { Type = PropertyType.WriteTime,  DisplayName = "Last Write",  DefaultAscending = false },
			new Property { Type = PropertyType.CreateTime, DisplayName = "Created",     DefaultAscending = false },
			new Property { Type = PropertyType.AccessTime, DisplayName = "Last Access", DefaultAscending = false },
		};

		public static Property Get(PropertyType type)
		{
			return properties.Single(a => a.Type == type);
		}

		public static PropertyType PropertyFromDisplayName(string str)
		{
			return properties.Single(a => a.DisplayName == str).Type;
		}
	}
}
