using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

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

		string GetExpressionValue<T1, T2>(Expression<Func<T1, T2>> expression)
		{
			return ((expression.Body as MemberExpression).Member as PropertyInfo).Name;
		}

		public void AddCallback<T>(Expression<Func<HelperType, T>> expression, Action<object, object> action)
		{
			callbacks[GetExpressionValue(expression)] = action;
		}

		public void AddObservableCallback<T>(Expression<Func<HelperType, ObservableCollection<T>>> expression, Action action)
		{
			NotifyCollectionChangedEventHandler func = (o, e) => action();

			AddCallback(expression, (o, n) =>
			{
				ObservableCollection<T> value;

				value = o as ObservableCollection<T>;
				if (value != null)
					value.CollectionChanged -= func;

				value = n as ObservableCollection<T>;
				if (value != null)
					value.CollectionChanged += func;

				action();
			});
		}

		public void AddCallback(DependencyProperty prop, DependencyObject obj, Action action)
		{
			var dpd = DependencyPropertyDescriptor.FromProperty(prop, obj.GetType());
			dpd.AddValueChanged(obj, (o, e) => action());
		}

		public DependencyProperty GetProp<T>(Expression<Func<HelperType, T>> expression)
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

		public void SetBinding<T, HelperType2>(Expression<Func<HelperType, T>> childExpression, HelperType2 parent, Expression<Func<HelperType2, T>> parentExpression)
		{
			BindingOperations.SetBinding(control, GetProp(childExpression), new Binding() { Source = parent, Path = new PropertyPath(GetExpressionValue(parentExpression)), Mode = BindingMode.TwoWay });
		}

		public void InvalidBinding(DependencyObject obj, DependencyProperty prop)
		{
			BindingOperations.GetBindingExpression(obj, prop).UpdateTarget();
		}
	}
}
