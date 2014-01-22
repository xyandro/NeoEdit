using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;

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
			dependencyProperty = properties.ToDictionary(a => a.Name, a => DependencyProperty.Register(a.Name, a.PropertyType, typeof(HelperType), new PropertyMetadata(ValueChangedCallback)));
		}

		static T GetTarget<T>(WeakReference<T> myRef) where T : class
		{
			if (myRef == null)
				return null;
			T val;
			if (!myRef.TryGetTarget(out val))
				return null;
			return val;
		}

		static void ValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			UIHelper<HelperType> uiHelper;
			lock (UIHelpers)
				uiHelper = GetTarget(UIHelpers.Where(a => GetTarget(a.Key) == d).Select(a => a.Value).SingleOrDefault());
			if (uiHelper == null)
				return;
			var property = e.Property.Name;
			if (!uiHelper.callbacks.ContainsKey(property))
				return;
			uiHelper.callbacks[property](e.OldValue, e.NewValue);
		}

		static Dictionary<WeakReference<HelperType>, WeakReference<UIHelper<HelperType>>> UIHelpers = new Dictionary<WeakReference<HelperType>, WeakReference<UIHelper<HelperType>>>();
		Dictionary<string, Action<object, object>> callbacks = new Dictionary<string, Action<object, object>>();
		public UIHelper(HelperType _control)
		{
			lock (UIHelpers)
				UIHelpers[new WeakReference<HelperType>(_control)] = new WeakReference<UIHelper<HelperType>>(this);
			control = _control;
		}

		string GetExpressionValue<T>(Expression<Func<HelperType, T>> expression)
		{
			return ((expression.Body as MemberExpression).Member as PropertyInfo).Name;
		}

		public void AddCallback<T>(Expression<Func<HelperType, T>> expression, Action<object, object> action)
		{
			callbacks[GetExpressionValue(expression)] = action;
		}

		public void AddCallback(DependencyProperty prop, DependencyObject obj, Action action)
		{
			var dpd = DependencyPropertyDescriptor.FromProperty(prop, obj.GetType());
			dpd.AddValueChanged(obj, (o, e) => action());
		}

		public DependencyProperty GetProp(Expression<Func<HelperType, object>> expression)
		{
			return dependencyProperty[GetExpressionValue(expression)];
		}

		public T GetPropValue<T>([CallerMemberName] string caller = "")
		{
			return (T)control.GetValue(dependencyProperty[caller]);
		}

		public void SetPropValue<T>(T value, [CallerMemberName] string caller = "")
		{
			control.SetValue(dependencyProperty[caller], value);
		}
	}
}
