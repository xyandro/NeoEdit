using NeoEdit.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using System.Timers;

namespace NeoEdit.Handles
{
	public class HandleItem : DependencyObject
	{
		public enum Property
		{
			PID,
			Handle,
			Type,
			Name,
			Data,
		}

		static Dictionary<Property, Type> propertyType = new Dictionary<Property, Type>
		{
			{ Property.PID, typeof(int?) },
			{ Property.Handle, typeof(IntPtr?) },
			{ Property.Type, typeof(string) },
			{ Property.Name, typeof(string) },
			{ Property.Data, typeof(string) },
		};
		static Dictionary<Property, DependencyProperty> dependencyProperty;
		static HandleItem()
		{
			dependencyProperty = propertyType.ToDictionary(a => a.Key, a => DependencyProperty.Register(a.Key.ToString(), a.Value, typeof(HandleItem)));
		}

		public static DependencyProperty GetDepProp(Property property)
		{
			return dependencyProperty[property];
		}

		public HandleItem(HandleInfo info)
		{
			SetProperty(Property.PID, info.PID);
			SetProperty(Property.Handle, info.Handle);
			SetProperty(Property.Type, info.Type);
			SetProperty(Property.Name, info.Name);
			SetProperty(Property.Data, info.Data);
		}

		void SetProperty(Property property, object value)
		{
			if ((value is string) && (((string)value).Length == 0))
				value = null;
			SetValue(dependencyProperty[property], value);
		}

		public T GetProperty<T>(Property property)
		{
			return (T)GetValue(dependencyProperty[property]);
		}
	}
}
