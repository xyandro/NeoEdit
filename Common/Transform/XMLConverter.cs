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
			var xml = converter.rToXML(obj, name, null);
			xml.Add(new XElement("NameMapping",
				new XElement("FieldNames", converter.fieldNames.Where(pair => pair.Key != pair.Value).Select(pair => new XElement("FieldName", new XAttribute("Key", pair.Key), new XAttribute("Value", pair.Value)))),
				new XElement("TypeNames", converter.typeNames.Where(pair => pair.Key != pair.Value).Select(pair => new XElement("TypeName", new XAttribute("Key", pair.Key), new XAttribute("Value", pair.Value))))
			));
			return xml;
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

		Dictionary<object, XElement> toXMLReferences = new Dictionary<object, XElement>();
		XElement rToXML(object obj, string name, Type startType)
		{
			var type = startType;
			if (obj != null)
				type = obj.GetType();

			var xml = new XElement(name);
			if (type != startType)
				xml.Add(new XAttribute("Type", EscapeType(type)));
			if (ToRaw(type, obj, xml))
				return xml;

			if (obj != null)
			{
				if (toXMLReferences.ContainsKey(obj))
				{
					if (toXMLReferences[obj].Attribute("GUID") == null)
						toXMLReferences[obj].Add(new XAttribute("GUID", Guid.NewGuid().ToString()));
					var guid = toXMLReferences[obj].Attribute("GUID").Value;
					xml.Add(new XAttribute("Reference", guid));
					return xml;
				}
				toXMLReferences[obj] = xml;

				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
					xml.Add(rToXML(field.GetValue(obj), EscapeField(field), field.FieldType));
			}

			return xml;
		}

		Dictionary<string, object> fromXMLReferences = new Dictionary<string, object>();
		object rFromXML(XElement xml, Type type)
		{
			if (xml.Attribute("Type") != null)
			{
				var typeName = xml.Attribute("Type").Value;
				typeName = typeNames.ContainsKey(typeName) ? typeNames[typeName] : typeName;
				type = AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(typeName)).First(val => val != null);
			}

			object raw;
			if (FromRaw(type, xml, out raw))
				return raw;

			var reference = xml.Attribute("Reference") == null ? null : xml.Attribute("Reference").Value;
			if (!String.IsNullOrEmpty(reference))
				return fromXMLReferences[reference];

			if (xml.IsEmpty)
				return null;

			var obj = FormatterServices.GetUninitializedObject(type);

			var guid = xml.Attribute("GUID") == null ? null : xml.Attribute("GUID").Value;
			if (!String.IsNullOrEmpty(guid))
				fromXMLReferences[guid] = obj;

			var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
			foreach (var field in fields)
			{
				var name = fieldNames.ContainsKey(field.Name) ? fieldNames[field.Name] : field.Name;
				field.SetValue(obj, rFromXML(xml.Element(name), field.FieldType));
			}
			return obj;
		}

		string GetUniqueName(string name, HashSet<string> used)
		{
			var ctr = 0;
			while (true)
			{
				var useName = name + (++ctr == 1 ? "" : ctr.ToString());
				if ((String.IsNullOrEmpty(useName)) || (used.Contains(useName)))
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
				if ((field.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null) && (name.Contains("BackingField")))
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
				name = GetUniqueName(name, new HashSet<string>(typeNames.Keys));
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

			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(List<>)))
			{
				var list = obj as IList;
				if (list != null)
				{
					var listType = type.GetGenericArguments()[0];
					foreach (var item in list)
						xml.Add(rToXML(item, "ListItem", listType));
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
						xml.Add(new XElement("DictionaryItem", rToXML(item.Key, "Key", keyType), rToXML(item.Value, "Value", valueType)));
				}

				return true;
			}

			return false;
		}

		bool FromRaw(Type type, XElement xml, out object raw)
		{
			raw = null;
			if ((type.IsPrimitive) || (!type.IsClass) || (type == typeof(string)))
			{
				if (xml.IsEmpty)
				{
					if (type.IsValueType)
						raw = Activator.CreateInstance(type);
					return true;
				}
				raw = TypeDescriptor.GetConverter(type).ConvertFrom(xml.Value);
				return true;
			}

			if (type == typeof(Regex))
			{
				if (!xml.IsEmpty)
					raw = new Regex(xml.Element("Pattern").Value, (RegexOptions)Enum.Parse(typeof(RegexOptions), xml.Element("Options").Value));
				return true;
			}

			if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(List<>)))
			{
				if (!xml.IsEmpty)
				{
					raw = type.GetConstructor(Type.EmptyTypes).Invoke(null);
					var list = raw as IList;

					var listType = type.GetGenericArguments()[0];
					foreach (var element in xml.Elements("ListItem"))
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
					foreach (var element in xml.Elements("DictionaryItem"))
						dictionary.Add(rFromXML(element.Element("Key"), keyType), rFromXML(element.Element("Value"), valueType));
				}

				return true;
			}

			return false;
		}
	}
}
