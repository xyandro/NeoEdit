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
		const string itemTag = "Item";
		const string nullType = "NULL";
		const string rootName = "Root";
		const string optionsTag = "Options";
		const string keyTag = "Key";
		const string valueTag = "Value";

		public static XElement ToXML(object obj)
		{
			var name = obj == null ? rootName : obj.GetType().Name.Replace("`", "");
			return rToXML(name, obj, null, true) as XElement;
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

			string useName;
			for (var ctr = 0; ; ++ctr)
			{
				useName = name + (ctr == 0 ? "" : ctr.ToString());
				if ((!string.IsNullOrWhiteSpace(useName)) && (useName != typeTag) && (!found.Contains(useName)))
					break;
			}
			found.Add(useName);
			return useName;
		}

		public static XObject rToXML(string name, object obj, Type expectedType, bool createElement)
		{
			if (obj == null)
				return new XElement(name, new XAttribute(typeTag, nullType));

			if (expectedType != null)
				expectedType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;
			var type = obj.GetType();
			var isPrimitive = (type.IsPrimitive) || (type.IsEnum) || (type == typeof(string)) || (obj is Type);
			if ((isPrimitive) && (type == expectedType) && (!createElement))
				return new XAttribute(name, obj);

			var xml = new XElement(name);
			if (type != expectedType)
				xml.Add(new XAttribute(typeTag, type.FullName));

			if (isPrimitive)
				xml.Add(obj);
			else if (type == typeof(Regex))
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
						xml.Add(rToXML(itemTag, item, itemType, true));
				}
			}
			else if ((type.IsGenericType) && ((type.GetGenericTypeDefinition() == typeof(List<>)) || (type.GetGenericTypeDefinition() == typeof(HashSet<>))))
			{
				if (obj is IEnumerable<char>)
					xml.Add(new XAttribute("Values", Coder.ConvertString(new string((obj as IEnumerable<char>).ToArray()), Coder.CodePage.UTF8, Coder.CodePage.Base64)));
				else
				{
					var items = obj as IEnumerable;
					var itemType = type.GetGenericArguments()[0];
					foreach (var item in items)
						xml.Add(rToXML(itemTag, item, itemType, true));
				}
			}
			else if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
			{
				var items = obj as IDictionary;
				var keyType = type.GetGenericArguments()[0];
				var valueType = type.GetGenericArguments()[1];
				foreach (DictionaryEntry item in items)
					xml.Add(
						new XElement(itemTag,
							rToXML(keyTag, item.Key, keyType, false),
							rToXML(valueTag, item.Value, valueType, false))
					);
			}
			else
			{
				var found = new HashSet<string>();
				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var value = field.GetValue(obj);
					if ((value == null) || (value is Delegate))
						continue;
					if ((field.FieldType.IsValueType) && (value.Equals(Activator.CreateInstance(field.FieldType))))
						continue;

					xml.Add(rToXML(EscapeField(field, found), value, field.FieldType, false));
				}
			}

			return xml;
		}

		public static T FromXML<T>(XElement xml) => (T)(rFromXML(xml, typeof(T)));

		public static T Load<T>(string fileName) => FromXML<T>(XElement.Load(fileName));

		public static OutputType Next<InputType, OutputType>(this InputType input, Func<InputType, OutputType> func) => input == null ? default(OutputType) : func(input);

		public static object rFromXML(XObject xObj, Type type)
		{
			if (xObj is XAttribute)
				return TypeDescriptor.GetConverter(type).ConvertFrom((xObj as XAttribute).Value);

			var xml = xObj as XElement;

			var typeValue = xml.Attribute(typeTag).Next(a => a.Value);
			if (typeValue == nullType)
				return null;

			type = typeValue.Next(a => AppDomain.CurrentDomain.GetAssemblies().Select(assembly => assembly.GetType(a)).First(val => val != null)) ?? type;
			if ((type.IsPrimitive) || (type.IsEnum) || (type == typeof(string)))
				return TypeDescriptor.GetConverter(type).ConvertFrom(xml.Value);
			else if (type == typeof(Regex))
				return new Regex(xml.Value, (RegexOptions)Enum.Parse(typeof(RegexOptions), xml.Attribute(optionsTag).Value));
			else if (type.IsArray)
			{
				if (type.GetElementType() == typeof(byte))
					return Coder.StringToBytes(xml.Value, Coder.CodePage.Hex);

				var elements = xml.Elements(itemTag).ToList();
				var obj = type.GetConstructor(new Type[] { typeof(int) }).Invoke(new object[] { elements.Count });
				var array = obj as Array;
				var itemType = type.GetElementType();
				for (var ctr = 0; ctr < elements.Count; ++ctr)
					array.SetValue(rFromXML(elements[ctr], itemType), ctr);
				return obj;
			}
			else if (type == typeof(Type))
				return Type.GetType(xml.Value);
			else if ((type.IsGenericType) && ((type.GetGenericTypeDefinition() == typeof(List<>)) || (type.GetGenericTypeDefinition() == typeof(HashSet<>))))
			{
				var obj = type.GetConstructor(Type.EmptyTypes).Invoke(null);
				var itemType = type.GetGenericArguments()[0];
				var addMethod = type.GetMethod("Add");
				if (itemType == typeof(char))
				{
					foreach (var ch in Coder.ConvertString(xml.Attribute("Values").Value, Coder.CodePage.Base64, Coder.CodePage.UTF8))
						addMethod.Invoke(obj, new object[] { ch });
				}
				else
				{
					foreach (var element in xml.Elements(itemTag))
						addMethod.Invoke(obj, new object[] { rFromXML(element, itemType) });
				}
				return obj;
			}
			else if ((type.IsGenericType) && (type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
			{
				var obj = type.GetConstructor(Type.EmptyTypes).Invoke(null);
				var items = obj as IDictionary;
				var keyType = type.GetGenericArguments()[0];
				var valueType = type.GetGenericArguments()[1];
				foreach (var element in xml.Elements(itemTag))
				{
					var key = element.Element(keyTag) ?? element.Attribute(keyTag) as XObject;
					var value = element.Element(valueTag) ?? element.Attribute(valueTag) as XObject;
					items.Add(rFromXML(key, keyType), rFromXML(value, valueType));
				}
				return obj;
			}
			else
			{
				var obj = FormatterServices.GetUninitializedObject(type);
				var found = new HashSet<string>();
				var fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				foreach (var field in fields)
				{
					var fieldName = EscapeField(field, found);
					var value = xml.Element(fieldName) ?? xml.Attribute(fieldName) as XObject;
					if (value != null)
						field.SetValue(obj, rFromXML(value, field.FieldType));
				}
				return obj;
			}
		}
	}
}
