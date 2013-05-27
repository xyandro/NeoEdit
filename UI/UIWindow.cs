using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

namespace NeoEdit.UI
{
	class DepPropAttribute : Attribute { }

	public class UIWindow : Window
	{
		static Dictionary<string, DependencyProperty> dependencyProperty;
		protected static void Register<T>()
		{
			var properties = typeof(T).GetProperties().Where(a => a.CustomAttributes.Any(b => b.AttributeType == typeof(DepPropAttribute))).ToList();
			dependencyProperty = properties.ToDictionary(a => a.Name, a => DependencyProperty.Register(a.Name, a.PropertyType, typeof(T)));
		}

		protected T GetProp<T>([CallerMemberName] string caller = "")
		{
			return (T)GetValue(dependencyProperty[caller]);
		}

		protected void SetProp<T>(T value, [CallerMemberName] string caller = "")
		{
			SetValue(dependencyProperty[caller], value);
		}
	}
}
