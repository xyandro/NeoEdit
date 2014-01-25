﻿using System;
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
			Type,
			Data,
		};

		public PropertyType Type { get; private set; }
		public string DisplayName { get; private set; }
		public string MenuHeader { get; private set; }
		public bool DefaultAscending { get; private set; }
		public Type SystemType { get; private set; }

		static List<Property> properties = new List<Property>
		{
			new Property { Type = PropertyType.FullName,   DisplayName = "Full Name",   MenuHeader = "_Full Name",   DefaultAscending = true,  SystemType = typeof(String)    },
			new Property { Type = PropertyType.Name,       DisplayName = "Name",        MenuHeader = "_Name",        DefaultAscending = true,  SystemType = typeof(String)    },
			new Property { Type = PropertyType.Path,       DisplayName = "Path",        MenuHeader = "_Path",        DefaultAscending = true,  SystemType = typeof(String)    },
			new Property { Type = PropertyType.Extension,  DisplayName = "Extension",   MenuHeader = "_Extension",   DefaultAscending = true,  SystemType = typeof(String)    },
			new Property { Type = PropertyType.Size,       DisplayName = "Size",        MenuHeader = "_Size",        DefaultAscending = false, SystemType = typeof(Int64?)    },
			new Property { Type = PropertyType.WriteTime,  DisplayName = "Last Write",  MenuHeader = "Last _Write",  DefaultAscending = false, SystemType = typeof(DateTime?) },
			new Property { Type = PropertyType.CreateTime, DisplayName = "Created",     MenuHeader = "_Created",     DefaultAscending = false, SystemType = typeof(DateTime?) },
			new Property { Type = PropertyType.AccessTime, DisplayName = "Last Access", MenuHeader = "Last _Access", DefaultAscending = false, SystemType = typeof(DateTime?) },
			new Property { Type = PropertyType.Type,       DisplayName = "Type",        MenuHeader = "_Type",        DefaultAscending = true,  SystemType = typeof(String)    },
			new Property { Type = PropertyType.Data,       DisplayName = "Data",        MenuHeader = "_Data",        DefaultAscending = true,  SystemType = typeof(String)    },
		};

		public static Property Get(PropertyType type)
		{
			return properties.Single(a => a.Type == type);
		}

		public static PropertyType PropertyFromMenuHeader(string str)
		{
			return properties.Single(a => a.MenuHeader == str).Type;
		}
	}
}
