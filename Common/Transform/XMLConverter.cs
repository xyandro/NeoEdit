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
	public static class XMLConverter
	{
		const string typeTag = "Type";
		const string guidTag = "GUID";
		const string listItemTag = "Item";
		const string referenceType = "Reference";
		const string nullType = "NULL";

		public static XElement ToXML(object obj)
		{
			var name = obj == null ? "Root" : obj.GetType().Name;
			return rToXML(name, obj, null, new Dictionary<object, XElement>()) as XElement;
		}

		public static void Save(object obj, string fileName)
		{
			ToXML(obj).Save(fileName);
		}

		static string EscapeField(MemberInfo field, HashSet<string> found)
		{
			var name = field.Name;
			if ((field.GetCustomAttribute(typeof(CompilerGeneratedAttribute)) != null) && (name.Contains("BackingField")))
			{
				var start = name.IndexOf('<');
				var end = name.IndexOf('>');
				if ((start != -1) && (end != -1))
					name = name.Substring(start + 1, end - start - 1);
			}
			name = Regex.Replace(name, @"\W+", match => "_");
			while (name.StartsWith("_"))
				name = name.Substring(1);

			var reserved = new HashSet<string> { "Type", "GUID", "Reference" };

			string useName;
			for (var ctr = 0; ; ++ctr)
			{
				useName = name + (ctr == 0 ? "" : ctr.ToString());
				if ((!String.IsNullOrWhiteSpace(useName)) && (!reserved.Contains(useName)) && (!found.Contains(useName)))
					break;
			}
			found.Add(useName);
			return useName;
		}

		public static XObject rToXML(string name, object obj, Type expectedType, Dictionary<object, XElement> references)
		{
			if (obj == null)
				return new XElement(name, new XAttribute(typeTag, nullType));

			var type = obj.GetType();
			if ((type.IsPrimitive) || (type.IsEnum) || (type == typeof(string)))
				return new XAttribute(name, obj.ToString());

			if (references.ContainsKey(obj))
			{
				var reference = references[obj];
				if (reference.Attribute(guidTag) == null)
					reference.Add(new XAttribute(guidTag, Guid.NewGuid().ToString()));
				var guid = reference.Attribute(guidTag).Value;
				return new XElement(name, new XAttribute(typeTag, referenceType), new XAttribute(guidTag, guid));
			}

			var xml = references[obj] = new XElement(name);
			if (type != expectedType)
				xml.Add(new XAttribute(typeTag, type.FullName));

			if (obj is Regex)
			{
				var regex = obj as Regex;
				xml.Add(regex.ToString(), new XAttribute("Options", regex.Options));
			}
			else if (obj is IList)
			{
				var list = obj as IList;
				var listType = type.GetGenericArguments()[0];
				if (list != null)
					foreach (var item in list)
						xml.Add(rToXML(listItemTag, item, listType, references));
			}
			else
			{
				var found = new HashSet<string>();
				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var fieldName = EscapeField(field, found);

					var value = field.GetValue(obj);
					if (value == null)
						continue;

					xml.Add(rToXML(fieldName, value, field.FieldType, references));
				}
			}

			return xml;
		}

		public static T FromXML<T>(XElement xml)
		{
			return (T)(rFromXML(xml, typeof(T), new Dictionary<string, object>()));
		}

		public static T Load<T>(string fileName)
		{
			return FromXML<T>(XElement.Load(fileName));
		}

		public static OutputType Next<InputType, OutputType>(this InputType input, Func<InputType, OutputType> func)
		{
			return input == null ? default(OutputType) : func(input);
		}

		public static object rFromXML(XObject xObj, Type type, Dictionary<string, object> references)
		{
			if (xObj is XAttribute)
				return TypeDescriptor.GetConverter(type).ConvertFrom((xObj as XAttribute).Value);

			var xml = xObj as XElement;
			var typeValue = xml.Attribute(typeTag).Next(a => a.Value);
			if (typeValue == referenceType)
				return references[xml.Attribute(guidTag).Value];
			if (typeValue == nullType)
				return null;

			type = typeValue.Next(a => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(a)).First(val => val != null)) ?? type;
			var obj = FormatterServices.GetUninitializedObject(type);
			if (obj is IList)
				obj = Activator.CreateInstance(type);
			else if (obj is Regex)
				obj = new Regex(xml.Value, (RegexOptions)Enum.Parse(typeof(RegexOptions), xml.Attribute("Options").Value));

			var guid = xml.Attribute(guidTag).Next(a => a.Value);
			if (guid != null)
				references[guid] = obj;

			if (obj is Regex)
			{ }
			else if (obj is IList)
			{
				var list = obj as IList;

				var listType = type.GetGenericArguments()[0];
				foreach (var element in xml.Elements(listItemTag))
					list.Add(rFromXML(element, listType, references));
			}
			else
			{
				var found = new HashSet<string>();
				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var fieldName = EscapeField(field, found);
					var value = xml.Element(fieldName) as XObject ?? xml.Attribute(fieldName) as XObject;
					if (value != null)
						field.SetValue(obj, rFromXML(value, field.FieldType, references));
				}
			}

			return obj;
		}
	}
}
