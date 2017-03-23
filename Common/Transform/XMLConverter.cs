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
		[AttributeUsage(AttributeTargets.Method)]
		public class ToXMLAttribute : Attribute { }
		[AttributeUsage(AttributeTargets.Method)]
		public class FromXMLAttribute : Attribute { }

		const string typeTag = "Type";
		const string guidTag = "GUID";
		const string itemTag = "Item";
		const string referenceType = "Reference";
		const string nullType = "NULL";
		const string rootName = "Root";
		const string optionsTag = "Options";
		const string keyTag = "Key";
		const string valueTag = "Value";

		public static XElement ToXML(object obj)
		{
			var name = obj == null ? rootName : obj.GetType().Name.Replace("`", "");
			return rToXML(name, obj, null, true, new Dictionary<object, XElement>()) as XElement;
		}

		public static void Save(object obj, string fileName) => ToXML(obj).Save(fileName);

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

			var reserved = new HashSet<string> { typeTag, guidTag, referenceType };

			string useName;
			for (var ctr = 0; ; ++ctr)
			{
				useName = name + (ctr == 0 ? "" : ctr.ToString());
				if ((!string.IsNullOrWhiteSpace(useName)) && (!reserved.Contains(useName)) && (!found.Contains(useName)))
					break;
			}
			found.Add(useName);
			return useName;
		}

		public static XObject rToXML(string name, object obj, Type expectedType, bool createElement, Dictionary<object, XElement> references)
		{
			if (obj == null)
				return new XElement(name, new XAttribute(typeTag, nullType));

			var type = obj.GetType();
			if ((type.IsPrimitive) || (type.IsEnum) || (type == typeof(string)) || (obj is Type))
			{
				if (createElement)
					return new XElement(name, obj);
				return new XAttribute(name, obj);
			}

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

			if (type == typeof(Regex))
			{
				var regex = obj as Regex;
				xml.Add(regex.ToString(), new XAttribute(optionsTag, regex.Options));
			}
			else if (type.IsArray)
			{
				var itemType = type.GetElementType();
				if (itemType == typeof(byte))
					xml.Add(Coder.BytesToString(obj as byte[], Coder.CodePage.Hex));
				else
				{
					var items = obj as Array;
					foreach (var item in items)
						xml.Add(rToXML(itemTag, item, itemType, true, references));
				}
			}
			else if ((type.IsGenericType) && ((type.GetGenericTypeDefinition() == typeof(List<>)) || (type.GetGenericTypeDefinition() == typeof(HashSet<>))))
			{
				var items = obj as IEnumerable;
				var itemType = type.GetGenericArguments()[0];
				foreach (var item in items)
					xml.Add(rToXML(itemTag, item, itemType, true, references));
			}
			else if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
			{
				var items = obj as IDictionary;
				var keyType = type.GetGenericArguments()[0];
				var valueType = type.GetGenericArguments()[1];
				foreach (DictionaryEntry item in items)
					xml.Add(
						new XElement(itemTag,
							rToXML(keyTag, item.Key, keyType, false, references),
							rToXML(valueTag, item.Value, valueType, false, references))
					);
			}
			else
			{
				var toXML = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).Where(method => method.GetCustomAttribute<ToXMLAttribute>() != null).FirstOrDefault();
				if (toXML != null)
					xml.Add(toXML.Invoke(obj, new object[0]));
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

						xml.Add(rToXML(fieldName, value, field.FieldType, false, references));
					}
				}
			}

			return xml;
		}

		public static T FromXML<T>(XElement xml) => (T)(rFromXML(xml, typeof(T), new Dictionary<string, object>()));

		public static T Load<T>(string fileName) => FromXML<T>(XElement.Load(fileName));

		public static OutputType Next<InputType, OutputType>(this InputType input, Func<InputType, OutputType> func) => input == null ? default(OutputType) : func(input);

		public static object rFromXML(XObject xObj, Type type, Dictionary<string, object> references)
		{
			if (xObj is XAttribute)
				return TypeDescriptor.GetConverter(type).ConvertFrom((xObj as XAttribute).Value);

			var xml = xObj as XElement;
			if ((type.IsPrimitive) || (type.IsEnum) || (type == typeof(string)))
				return TypeDescriptor.GetConverter(type).ConvertFrom(xml.Value);

			var typeValue = xml.Attribute(typeTag).Next(a => a.Value);
			if (typeValue == referenceType)
				return references[xml.Attribute(guidTag).Value];
			if (typeValue == nullType)
				return null;

			var guid = xml.Attribute(guidTag).Next(a => a.Value) ?? "";

			type = typeValue.Next(a => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(a)).First(val => val != null)) ?? type;
			if (type == typeof(Regex))
				return references[guid] = new Regex(xml.Value, (RegexOptions)Enum.Parse(typeof(RegexOptions), xml.Attribute(optionsTag).Value));
			else if (type.IsArray)
			{
				if (type.GetElementType() == typeof(byte))
					return references[guid] = Coder.StringToBytes(xml.Value, Coder.CodePage.Hex);

				var elements = xml.Elements(itemTag).ToList();
				var obj = references[guid] = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elements.Count });
				var array = obj as Array;
				var itemType = type.GetElementType();
				for (var ctr = 0; ctr < elements.Count; ++ctr)
					array.SetValue(rFromXML(elements[ctr], itemType, references), ctr);
				return obj;
			}
			else if (type == typeof(Type))
				return Type.GetType(xml.Value);
			else if ((type.IsGenericType) && ((type.GetGenericTypeDefinition() == typeof(List<>)) || (type.GetGenericTypeDefinition() == typeof(HashSet<>))))
			{
				var obj = references[guid] = type.GetConstructor(Type.EmptyTypes).Invoke(null);
				var itemType = type.GetGenericArguments()[0];
				var addMethod = type.GetMethod("Add");
				foreach (var element in xml.Elements(itemTag))
					addMethod.Invoke(obj, new object[] { rFromXML(element, itemType, references) });
				return obj;
			}
			else if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
			{
				var obj = references[guid] = type.GetConstructor(Type.EmptyTypes).Invoke(null);
				var items = obj as IDictionary;
				var keyType = type.GetGenericArguments()[0];
				var valueType = type.GetGenericArguments()[1];
				foreach (var element in xml.Elements(itemTag))
				{
					var key = element.Element(keyTag) as XObject ?? element.Attribute(keyTag) as XObject;
					var value = element.Element(valueTag) as XObject ?? element.Attribute(valueTag) as XObject;
					items.Add(rFromXML(key, keyType, references), rFromXML(value, valueType, references));
				}
				return obj;
			}
			else
			{
				var fromXML = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Where(method => method.GetCustomAttribute<FromXMLAttribute>() != null).FirstOrDefault();
				if (fromXML != null)
					return references[guid] = fromXML.Invoke(null, new object[] { xml });

				var obj = references[guid] = FormatterServices.GetUninitializedObject(type);
				var found = new HashSet<string>();
				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var fieldName = EscapeField(field, found);
					var value = xml.Element(fieldName) as XObject ?? xml.Attribute(fieldName) as XObject;
					if (value != null)
						field.SetValue(obj, rFromXML(value, field.FieldType, references));
				}
				return obj;
			}
		}
	}
}
