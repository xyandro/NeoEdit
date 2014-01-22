using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace NeoEdit.UI
{
	class DepPropAttribute : Attribute { }

	public class UIHelper<HelperType> where HelperType : DependencyObject
	{
		static Dictionary<string, DependencyProperty> dependencyProperty;
		readonly HelperType control;
		static UIHelper()
		{
			var properties = typeof(HelperType).GetProperties().Where(a => a.CustomAttributes.Any(b => b.AttributeType == typeof(DepPropAttribute))).ToList();
			dependencyProperty = properties.ToDictionary(a => a.Name, a => DependencyProperty.Register(a.Name, a.PropertyType, typeof(HelperType)));
		}

		public UIHelper(HelperType _control)
		{
			control = _control;
		}

		public T GetProp<T>([CallerMemberName] string caller = "")
		{
			return (T)control.GetValue(dependencyProperty[caller]);
		}

		public void SetProp<T>(T value, [CallerMemberName] string caller = "")
		{
			control.SetValue(dependencyProperty[caller], value);
		}
	}
}
