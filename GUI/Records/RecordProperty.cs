using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.GUI.Records
{
	public class RecordProperty
	{
		public enum PropertyName
		{
			FullName,
			Path,
			Name,
			NameWoExtension,
			Extension,
			Size,
			WriteTime,
			CreateTime,
			AccessTime,
			Type,
			Data,
			MD5,
			Identify,
			CompressedSize,
			ID,
			CPU,
		};

		public PropertyName Name { get; private set; }
		public string DisplayName { get { return MenuHeader.Replace("_", ""); } }
		public string MenuHeader { get; private set; }
		public bool DefaultAscending { get; private set; }
		public Type Type { get; private set; }

		static List<RecordProperty> properties = new List<RecordProperty>
		{
			new RecordProperty { Name = PropertyName.FullName, MenuHeader = "_Full Name", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.Path, MenuHeader = "_Path", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.Name, MenuHeader = "_Name", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.NameWoExtension, MenuHeader = "Name w/o Ext", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.Extension, MenuHeader = "_Extension", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.Size, MenuHeader = "_Size", DefaultAscending = false, Type = typeof(Int64?) },
			new RecordProperty { Name = PropertyName.WriteTime, MenuHeader = "Last _Write", DefaultAscending = false, Type = typeof(DateTime?) },
			new RecordProperty { Name = PropertyName.CreateTime, MenuHeader = "_Created", DefaultAscending = false, Type = typeof(DateTime?) },
			new RecordProperty { Name = PropertyName.AccessTime, MenuHeader = "Last _Access", DefaultAscending = false, Type = typeof(DateTime?) },
			new RecordProperty { Name = PropertyName.Type, MenuHeader = "_Type", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.Data, MenuHeader = "_Data", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.MD5, MenuHeader = "_MD5", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.Identify, MenuHeader = "_Identify", DefaultAscending = true, Type = typeof(String) },
			new RecordProperty { Name = PropertyName.CompressedSize, MenuHeader = "Compressed Size", DefaultAscending = true, Type = typeof(Int64?) },
			new RecordProperty { Name = PropertyName.ID, MenuHeader = "ID", DefaultAscending = true, Type = typeof(Int32?) },
			new RecordProperty { Name = PropertyName.CPU, MenuHeader = "CPU", DefaultAscending = false, Type = typeof(double?) },
		};

		public static RecordProperty Get(PropertyName name)
		{
			return properties.Single(a => a.Name == name);
		}

		public static PropertyName PropertyFromMenuHeader(string str)
		{
			return properties.Single(a => a.MenuHeader == str).Name;
		}
	}
}
