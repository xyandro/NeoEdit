using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.ItemGridControl
{
	public interface IItemGridItem
	{
		object GetValue(DependencyProperty prop);
		T GetValue<T>(string propName);
		void SetValue<T>(T value, string propName);
		DependencyProperty GetDepProp(string propName);
	}

	public class ItemGridItem<ItemType> : DependencyObject, IItemGridItem
	{
		static Dictionary<string, DependencyProperty> dependencyProperty;
		static ItemGridItem()
		{
			var properties = typeof(ItemType).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(itr1 => itr1.CustomAttributes.Any(itr2 => itr2.AttributeType == typeof(DepPropAttribute))).ToList();
			dependencyProperty = properties.ToDictionary(itr => itr.Name, itr => DependencyProperty.Register(itr.Name, itr.PropertyType, typeof(ItemType)));
		}

		public DependencyProperty GetDepProp(string name)
		{
			return StaticGetDepProp(name);
		}

		public IEnumerable<DependencyProperty> GetDepProps()
		{
			return StaticGetDepProps();
		}

		public static DependencyProperty StaticGetDepProp(string name)
		{
			return dependencyProperty[name];
		}

		public static IEnumerable<DependencyProperty> StaticGetDepProps()
		{
			return dependencyProperty.Values;
		}

		public T GetValue<T>([CallerMemberName] string caller = "")
		{
			return (T)GetValue(dependencyProperty[caller]);
		}
		public void SetValue<T>(T value, [CallerMemberName] string caller = "")
		{
			SetValue(dependencyProperty[caller], value);
		}
	}
}
