using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using NeoEdit.Common.Transform;

namespace NeoEdit.GUI.Dialogs
{
	public static class NEXML
	{
		public static string Name(Type type)
		{
			var name = type.Name;
			if (name != "Result")
				return name;
			var fullName = type.FullName;
			return fullName.Substring(fullName.LastIndexOf('.') + 1).Replace('+', '.');
		}

		public static NEXML<T> Create<T>(T _this) where T : class
		{
			return new NEXML<T>(_this);
		}
	}

	public class NEXML<Type> where Type : class
	{
		Type _this;
		public NEXML(Type _this)
		{
			this._this = _this;
		}

		public static string StaticName { get { return NEXML.Name(typeof(Type)); } }
		public string Name { get { return StaticName; } }

		static string GetName<T>(Expression<Func<Type, T>> expression)
		{
			return ((expression.Body as MemberExpression).Member as PropertyInfo).Name;
		}

		T GetValue<T>(Expression<Func<Type, T>> expression)
		{
			return expression.Compile()(_this);
		}

		public XAttribute Attribute<T>(Expression<Func<Type, T>> expression)
		{
			return new XAttribute(GetName(expression), GetValue(expression));
		}

		public XElement Element<T>(Expression<Func<Type, T>> expression)
		{
			return new XElement(GetName(expression), GetValue(expression));
		}

		XElement RegexToXML(string name, Regex regex)
		{
			return new XElement("Regex",
				new XAttribute("Options", regex.Options),
				regex
			);
		}

		public XElement Element(Expression<Func<Type, Regex>> expression)
		{
			return RegexToXML(GetName(expression), GetValue(expression));
		}

		public XElement Element<T>(Expression<Func<Type, List<T>>> expression)
		{
			return new XElement(GetName(expression), GetValue(expression).Select(item => new XElement("item", item)));
		}

		public XElement Element(Expression<Func<Type, List<byte[]>>> expression)
		{
			return new XElement(GetName(expression), GetValue(expression).Select(item => new XElement("item", StrCoder.BytesToString(item, StrCoder.CodePage.Hex))));
		}

		static T GetValue<T>(XAttribute attr)
		{
			if (attr == null)
				return default(T);
			return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(attr.Value);
		}

		static T GetValue<T>(XElement element)
		{
			if (typeof(T) == typeof(Regex))
			{
				var pattern = element.Value.ToString();
				var options = (RegexOptions)Enum.Parse(typeof(RegexOptions), element.Attribute("Options").Value);
				return (T)(object)new Regex(pattern, options);
			}
			if ((element == null) || (element.IsEmpty))
				return default(T);
			return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(element.Value);
		}

		public static T Attribute<T>(XElement xml, Expression<Func<Type, T>> expression)
		{
			return GetValue<T>(xml.Attribute(GetName(expression)));
		}

		public static T Element<T>(XElement xml, Expression<Func<Type, T>> expression)
		{
			return GetValue<T>(xml.Element(GetName(expression)));
		}

		public static List<T> Element<T>(XElement xml, Expression<Func<Type, List<T>>> expression)
		{
			var element = xml.Element(GetName(expression));
			if (element.IsEmpty)
				return null;
			return new List<T>(element.Elements().Select(item => GetValue<T>(item)));
		}

		public static List<byte[]> Element(XElement xml, Expression<Func<Type, List<byte[]>>> expression)
		{
			var element = xml.Element(GetName(expression));
			if (element.IsEmpty)
				return null;
			return new List<byte[]>(element.Elements().Select(item => StrCoder.StringToBytes(GetValue<string>(item), StrCoder.CodePage.Hex)));
		}
	}
}
