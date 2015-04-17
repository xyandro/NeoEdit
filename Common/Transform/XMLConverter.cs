using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NeoEdit.Common.Transform
{
	public class XMLConverter
	{
		XMLConverter() { }

		public static XElement ToXML<T>(T obj, string name = null)
		{
			if (name == null)
				name = typeof(T).Name;
			var converter = new XMLConverter();
			var xml = converter.rToXML(obj, name, null, false) as XElement;
			xml.Add(new XElement("NameMapping",
				new XElement("FieldNames", converter.fieldNames.Select(pair => new XElement("FieldName", new XAttribute("Key", pair.Key), new XAttribute("Value", pair.Value)))),
				new XElement("TypeNames", converter.typeNames.Select(pair => new XElement("TypeName", new XAttribute("Key", pair.Key), new XAttribute("Value", pair.Value))))
			));
			return xml;
		}

		public static void Save<T>(T obj, string fileName, string name = null)
		{
			ToXML(obj, name).Save(fileName);
		}

		public static T FromXML<T>(XElement xml)
		{
			var nameMapping = xml.Element("NameMapping");
			var converter = new XMLConverter
			{
				fieldNames = nameMapping.Element("FieldNames").Elements().ToDictionary(element => element.Attribute("Key").Value, element => element.Attribute("Value").Value),
				typeNames = nameMapping.Element("TypeNames").Elements().ToDictionary(element => element.Attribute("Value").Value, element => element.Attribute("Key").Value),
			};
			return (T)converter.rFromXML(xml, typeof(T));
		}

		public static T Load<T>(string fileName)
		{
			return FromXML<T>(XElement.Load(fileName));
		}

		Dictionary<object, XElement> toXMLReferences = new Dictionary<object, XElement>();
		object rToXML(object obj, string name, Type expectedType, bool rawToAttr)
		{
			var type = expectedType;
			if (obj != null)
				type = obj.GetType();

			var xml = new XElement(name);
			if (type != expectedType)
				if ((expectedType == null) || (!expectedType.IsGenericType) || (expectedType.GetGenericTypeDefinition() != typeof(Nullable<>)) || (expectedType.GetGenericArguments()[0] != type))
					xml.Add(new XAttribute("Type", EscapeType(type)));

			if (ToRaw(type, obj, xml))
			{
				if ((rawToAttr) && (!xml.IsEmpty) && (!xml.HasElements) && (!xml.HasAttributes))
					return new XAttribute(xml.Name, xml.Value);

				return xml;
			}

			if (obj == null)
				xml.Add(new XAttribute("Type", "null"));
			else
			{
				if (toXMLReferences.ContainsKey(obj))
				{
					if (toXMLReferences[obj].Attribute("GUID") == null)
						toXMLReferences[obj].Add(new XAttribute("GUID", Guid.NewGuid().ToString()));
					xml.RemoveAttributes();
					xml.Add(new XAttribute("Reference", toXMLReferences[obj].Attribute("GUID").Value));
					return xml;
				}
				toXMLReferences[obj] = xml;

				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var value = field.GetValue(obj);
					if (value != null)
						xml.Add(rToXML(value, EscapeField(field), field.FieldType, true));
				}
			}

			return xml;
		}

		Dictionary<string, object> fromXMLReferences = new Dictionary<string, object>();
		object rFromXML(XElement xml, Type type)
		{
			var reference = xml.Attribute("Reference") == null ? null : xml.Attribute("Reference").Value;
			if (!String.IsNullOrEmpty(reference))
				return fromXMLReferences[reference];

			if (xml.Attribute("Type") != null)
			{
				var typeName = xml.Attribute("Type").Value;
				if (typeName == "null")
					return null;
				type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeNames[typeName])).First(val => val != null);
			}

			object raw;
			if (FromRaw(type, xml, out raw))
				return raw;

			var obj = FormatterServices.GetUninitializedObject(type);

			var guid = xml.Attribute("GUID") == null ? null : xml.Attribute("GUID").Value;
			if (!String.IsNullOrEmpty(guid))
				fromXMLReferences[guid] = obj;

			var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			foreach (var field in fields)
			{
				if (!fieldNames.ContainsKey(field.Name))
					continue;
				var name = fieldNames[field.Name];
				if (xml.Element(name) != null)
					field.SetValue(obj, rFromXML(xml.Element(name), field.FieldType));
				else if ((xml.Attribute(name) != null) && (FromRaw(field.FieldType, xml.Attribute(name).Value, out raw)))
					field.SetValue(obj, raw);
			}
			return obj;
		}

		string GetUniqueName(string name, HashSet<string> used)
		{
			var reserved = new HashSet<string> { "Type", "GUID", "Reference" };
			var ctr = 0;
			while (true)
			{
				var useName = name + (++ctr == 1 ? "" : ctr.ToString());
				if ((String.IsNullOrEmpty(useName)) || (reserved.Contains(useName)) || (used.Contains(useName)))
					continue;
				return useName;
			}
		}

		Dictionary<string, string> fieldNames = new Dictionary<string, string>();
		string EscapeField(MemberInfo field)
		{
			if (!fieldNames.ContainsKey(field.Name))
			{
				var name = field.Name;
				if ((field.GetCustomAttributes(typeof(CompilerGeneratedAttribute), true) != null) && (name.Contains("BackingField")))
				{
					var start = name.IndexOf('<');
					var end = name.IndexOf('>');
					if ((start != -1) && (end != -1))
						name = name.Substring(start + 1, end - start - 1);
				}
				name = Regex.Replace(name, @"\W", match => "_");
				while (name.StartsWith("_"))
					name = name.Substring(1);
				name = GetUniqueName(name, new HashSet<string>(fieldNames.Values));
				fieldNames[field.Name] = name;
			}

			return fieldNames[field.Name];
		}

		Dictionary<string, string> typeNames = new Dictionary<string, string>();
		string EscapeType(Type type)
		{
			if (!typeNames.ContainsKey(type.FullName))
			{
				var name = type.Name;
				if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(List<>)))
					name = "List-" + EscapeType(type.GetGenericArguments()[0]);
				else if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
					name = "Dictionary-" + EscapeType(type.GetGenericArguments()[0]) + "-" + EscapeType(type.GetGenericArguments()[1]);
				name = GetUniqueName(name, new HashSet<string>(typeNames.Values));
				typeNames[type.FullName] = name;
			}

			return typeNames[type.FullName];
		}

		bool ToRaw(Type type, object obj, XElement xml)
		{
			if ((type.IsPrimitive) || (!type.IsClass) || (type == typeof(string)))
			{
				xml.Add(obj);
				return true;
			}

			if (type == typeof(Regex))
			{
				var regex = obj as Regex;
				if (regex != null)
					xml.Add(new XElement("Pattern", regex.ToString()), new XElement("Options", regex.Options));
				return true;
			}

			if (type.IsArray)
			{
				var arrayType = type.GetElementType();
				if (arrayType == typeof(byte))
				{
					xml.Add(Coder.BytesToString(obj as byte[], Coder.CodePage.Hex));
					return true;
				}

				var array = obj as Array;
				if (array != null)
				{
					foreach (var item in array)
						xml.Add(rToXML(item, "Item", arrayType, false));
				}

				return true;
			}

			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(List<>)))
			{
				var list = obj as IList;
				if (list != null)
				{
					var listType = type.GetGenericArguments()[0];
					foreach (var item in list)
						xml.Add(rToXML(item, "Item", listType, false));
				}

				return true;
			}

			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
			{
				var dictionary = obj as IDictionary;
				if (dictionary != null)
				{
					var keyType = type.GetGenericArguments()[0];
					var valueType = type.GetGenericArguments()[1];
					foreach (DictionaryEntry item in dictionary)
						xml.Add(new XElement("Item", rToXML(item.Key, "Key", keyType, false), rToXML(item.Value, "Value", valueType, false)));
				}

				return true;
			}

			return false;
		}

		bool FromRaw(Type type, string value, out object raw)
		{
			raw = null;
			if ((type.IsPrimitive) || (!type.IsClass) || (type == typeof(string)))
			{
				if (value == null)
				{
					if (type.IsValueType)
						raw = Activator.CreateInstance(type);
					return true;
				}
				raw = TypeDescriptor.GetConverter(type).ConvertFrom(value);
				return true;
			}

			if ((type.IsArray) && (type.GetElementType() == typeof(byte)))
			{
				raw = Coder.StringToBytes(value, Coder.CodePage.Hex);
				return true;
			}

			return false;
		}

		bool FromRaw(Type type, XElement xml, out object raw)
		{
			if (FromRaw(type, xml.IsEmpty ? null : xml.Value, out raw))
				return true;

			if (type == typeof(Regex))
			{
				if (!xml.IsEmpty)
					raw = new Regex(xml.Element("Pattern").Value, (RegexOptions)Enum.Parse(typeof(RegexOptions), xml.Element("Options").Value));
				return true;
			}

			if (type.IsArray)
			{
				if (!xml.IsEmpty)
				{
					var elements = xml.Elements("Item").ToList();
					raw = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elements.Count });
					var array = raw as Array;

					var arrayType = type.GetElementType();
					for (var ctr = 0; ctr < elements.Count; ++ctr)
						array.SetValue(rFromXML(elements[ctr], arrayType), ctr);
				}

				return true;
			}

			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(List<>)))
			{
				if (!xml.IsEmpty)
				{
					raw = type.GetConstructor(Type.EmptyTypes).Invoke(null);
					var list = raw as IList;

					var listType = type.GetGenericArguments()[0];
					foreach (var element in xml.Elements("Item"))
						list.Add(rFromXML(element, listType));
				}

				return true;
			}

			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
			{
				if (!xml.IsEmpty)
				{
					raw = type.GetConstructor(Type.EmptyTypes).Invoke(null);
					var dictionary = raw as IDictionary;

					var keyType = type.GetGenericArguments()[0];
					var valueType = type.GetGenericArguments()[1];
					foreach (var element in xml.Elements("Item"))
						dictionary.Add(rFromXML(element.Element("Key"), keyType), rFromXML(element.Element("Value"), valueType));
				}

				return true;
			}

			return false;
		}
	}
}
