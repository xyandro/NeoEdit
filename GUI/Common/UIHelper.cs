using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NeoEdit.GUI.Common
{
	public class DepPropAttribute : Attribute
	{
		public object Default { get; set; }
		public bool BindsTwoWayByDefault { get; set; }
	}

	public static class UIHelper<ControlType> where ControlType : DependencyObject
	{
		class PropertyHolder
		{
			public DependencyProperty property;
		}

		static Dictionary<string, DependencyProperty> dependencyProperty;
		static UIHelper()
		{
			dependencyProperty = new Dictionary<string, DependencyProperty>();
			foreach (var prop in typeof(ControlType).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				var attr = prop.GetCustomAttribute<DepPropAttribute>();
				if (attr == null)
					continue;
				var def = attr.Default;
				if ((def == null) && (prop.PropertyType.IsValueType))
					def = Activator.CreateInstance(prop.PropertyType);
				var bindsTwoWayByDefault = attr.BindsTwoWayByDefault;
				var propertyHolder = new PropertyHolder();
				propertyHolder.property = dependencyProperty[prop.Name] = DependencyProperty.Register(prop.Name, prop.PropertyType, typeof(ControlType), new FrameworkPropertyMetadata { DefaultValue = def, BindsTwoWayByDefault = bindsTwoWayByDefault, PropertyChangedCallback = (d, e) => propertyChangedCallback(d as ControlType, e), CoerceValueCallback = (d, val) => coerceValueCallback(d as ControlType, propertyHolder.property, val) });
			}
		}
		public static void Register() { } // Only calls static constructor

		static Dictionary<DependencyProperty, Action<ControlType, object, object>> propertyChangedCallbacks = new Dictionary<DependencyProperty, Action<ControlType, object, object>>();
		static void propertyChangedCallback(ControlType d, DependencyPropertyChangedEventArgs e)
		{
			var prop = e.Property;
			if (!propertyChangedCallbacks.ContainsKey(prop))
				return;
			propertyChangedCallbacks[prop](d, e.OldValue, e.NewValue);
		}

		static Dictionary<DependencyProperty, Func<ControlType, object, object>> coerceValueCallbacks = new Dictionary<DependencyProperty, Func<ControlType, object, object>>();
		static object coerceValueCallback(ControlType d, DependencyProperty prop, object value)
		{
			if (!coerceValueCallbacks.ContainsKey(prop))
				return value;
			return coerceValueCallbacks[prop](d, value);
		}

		public static DependencyProperty GetProperty<T>(Expression<Func<ControlType, T>> expression)
		{
			return dependencyProperty[((expression.Body as MemberExpression).Member as PropertyInfo).Name];
		}

		public static IEnumerable<DependencyProperty> GetProperties()
		{
			return dependencyProperty.Values;
		}

		static List<Tuple<Func<ControlType, DependencyObject>, DependencyProperty, Action<ControlType>>> localCallbacks = new List<Tuple<Func<ControlType, DependencyObject>, DependencyProperty, Action<ControlType>>>();
		public static void AddCallback(Func<ControlType, DependencyObject> obj, DependencyProperty prop, Action<ControlType> callback)
		{
			localCallbacks.Add(Tuple.Create(obj, prop, callback));
		}

		public static List<PropertyChangeNotifier> GetLocalCallbacks(ControlType control)
		{
			return localCallbacks.Select(tuple => new PropertyChangeNotifier(tuple.Item1(control), tuple.Item2, () => tuple.Item3(control))).ToList();
		}

		public static void AddCallback<T>(Expression<Func<ControlType, T>> expression, Action<ControlType, T, T> callback)
		{
			var prop = GetProperty(expression);
			Action<ControlType, object, object> _callback = (obj, o, n) => callback(obj, (T)o, (T)n);
			if (!propertyChangedCallbacks.ContainsKey(prop))
				propertyChangedCallbacks[prop] = _callback;
			else
				propertyChangedCallbacks[prop] += _callback;
		}

		public static void AddCoerce<T>(Expression<Func<ControlType, T>> expression, Func<ControlType, T, T> callback)
		{
			var prop = GetProperty(expression);
			Func<ControlType, object, object> _callback = (obj, v) => callback(obj, (T)v);
			if (!coerceValueCallbacks.ContainsKey(prop))
				coerceValueCallbacks[prop] = _callback;
			else
				coerceValueCallbacks[prop] += _callback;
		}

		static ConditionalWeakTable<Action<ControlType, object, NotifyCollectionChangedEventArgs>, ConditionalWeakTable<ControlType, NotifyCollectionChangedEventHandler>> observableCallbacks = new ConditionalWeakTable<Action<ControlType, object, NotifyCollectionChangedEventArgs>, ConditionalWeakTable<ControlType, NotifyCollectionChangedEventHandler>>();
		public static void AddObservableCallback(Expression<Func<ControlType, IEnumerable>> expression, Action<ControlType, object, NotifyCollectionChangedEventArgs> action)
		{
			AddCallback(expression, (obj, o, n) =>
			{
				var subTable = observableCallbacks.GetOrCreateValue(action);
				NotifyCollectionChangedEventHandler handler;

				var value = o as INotifyCollectionChanged;
				if ((value != null) && (subTable.TryGetValue(obj, out handler)))
					value.CollectionChanged -= handler;
				subTable.Remove(obj);

				value = n as INotifyCollectionChanged;
				if (value != null)
				{
					handler = (s, e) => action(obj, s, e);
					value.CollectionChanged += handler;
					subTable.Add(obj, handler);
				}

				action(obj, null, null);
			});
		}

		public static void InvalidateBinding(DependencyObject obj, DependencyProperty prop)
		{
			BindingOperations.GetBindingExpressionBase(obj, prop).UpdateTarget();
		}

		public static void SetValidation(FrameworkElement obj, DependencyProperty prop, bool valid = true)
		{
			var bindingExpression = obj.GetBindingExpression(prop);
			if (valid)
				Validation.ClearInvalid(bindingExpression);
			else
				Validation.MarkInvalid(bindingExpression, new ValidationError(new ExceptionValidationRule(), bindingExpression));
		}

		public static T GetPropValue<T>(ControlType control, [CallerMemberName] string caller = "")
		{
			return (T)control.GetValue(dependencyProperty[caller]);
		}

		public static void SetPropValue<T>(ControlType control, T value, [CallerMemberName] string caller = "")
		{
			control.SetValue(dependencyProperty[caller], value);
		}

		public static ControlType GetNewest()
		{
			return Application.Current.Windows.OfType<ControlType>().Cast<ControlType>().LastOrDefault();
		}
	}
}
