using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.UI
{
	class DepPropAttribute : Attribute { }

	public static class UIHelper
	{
		static Dictionary<string, DependencyProperty> dependencyProperty;
		public static void Register<T>()
		{
			var properties = typeof(T).GetProperties().Where(a => a.CustomAttributes.Any(b => b.AttributeType == typeof(DepPropAttribute))).ToList();
			dependencyProperty = properties.ToDictionary(a => a.Name, a => DependencyProperty.Register(a.Name, a.PropertyType, typeof(T)));
		}

		public static T GetProp<T>(this Control control, [CallerMemberName] string caller = "")
		{
			return (T)control.GetValue(dependencyProperty[caller]);
		}

		public static void SetProp<T>(this Control control, T value, [CallerMemberName] string caller = "")
		{
			control.SetValue(dependencyProperty[caller], value);
		}
	}
}
