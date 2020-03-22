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
using System.Windows.Media;
using NeoEdit.Program;
using NeoEdit.Program.Dialogs;
using NeoEdit.Program.Parsing;

namespace NeoEdit.Program.Controls
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
			// Ensure base class UIHelper constructors called first
			var baseTypes = typeof(ControlType).Recurse(type => type.BaseType).Reverse().ToList();
			var uiHelperTypes = baseTypes.Where(type => typeof(DependencyObject).IsAssignableFrom(type)).Select(type => typeof(UIHelper<>).MakeGenericType(type)).ToList();
			uiHelperTypes.ForEach(type => RuntimeHelpers.RunClassConstructor(type.TypeHandle));

			dependencyProperty = new Dictionary<string, DependencyProperty>();
			foreach (var prop in typeof(ControlType).GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
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

		public static DependencyProperty GetProperty<T>(Expression<Func<ControlType, T>> expression) => dependencyProperty[((expression.Body as MemberExpression).Member as PropertyInfo).Name];

		public static IEnumerable<DependencyProperty> GetProperties() => dependencyProperty.Values;

		static List<Tuple<Func<ControlType, DependencyObject>, DependencyProperty, Action<ControlType>>> localCallbacks = new List<Tuple<Func<ControlType, DependencyObject>, DependencyProperty, Action<ControlType>>>();
		public static void AddCallback(Func<ControlType, DependencyObject> obj, DependencyProperty prop, Action<ControlType> callback) => localCallbacks.Add(Tuple.Create(obj, prop, callback));

		public static List<PropertyChangeNotifier> GetLocalCallbacks(ControlType control) => localCallbacks.Select(tuple => new PropertyChangeNotifier(tuple.Item1(control), tuple.Item2, () => tuple.Item3(control))).ToList();

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

		public static T GetPropValue<T>(ControlType control, [CallerMemberName] string caller = "") => (T)control.GetValue(dependencyProperty[caller]);
		public static void SetPropValue<T>(ControlType control, T value, [CallerMemberName] string caller = "") => control.SetValue(dependencyProperty[caller], value);

		public static List<ControlType> GetAllWindows() => Application.Current.Windows.OfType<ControlType>().Cast<ControlType>().ToList();
	}

	public static class UIHelper
	{
		public static void InvalidateBinding(this DependencyObject obj, DependencyProperty prop) => BindingOperations.GetBindingExpressionBase(obj, prop).UpdateTarget();

		public static void SetValidation(this FrameworkElement obj, DependencyProperty prop, bool valid = true)
		{
			var bindingExpression = obj.GetBindingExpression(prop);
			if (valid)
				Validation.ClearInvalid(bindingExpression);
			else
				Validation.MarkInvalid(bindingExpression, new ValidationError(new ExceptionValidationRule(), bindingExpression));
		}

		public static T FindParent<T>(this FrameworkElement obj) where T : DependencyObject
		{
			while (obj != null)
			{
				if (obj is T)
					return obj as T;
				obj = VisualTreeHelper.GetParent(obj) as FrameworkElement;
			}
			return null;
		}

		public static IEnumerable<T> FindLogicalChildren<T>(this DependencyObject obj) where T : DependencyObject
		{
			if (obj == null)
				yield break;
			foreach (var child in LogicalTreeHelper.GetChildren(obj))
			{
				if (child == null)
					continue;
				if (child is T)
					yield return child as T;
				if (child is DependencyObject)
					foreach (var subChild in FindLogicalChildren<T>(child as DependencyObject))
						yield return subChild;
			}
		}

		static void AuditMenu(string path, ItemsControl menu, List<string> errors)
		{
			var children = menu.Items.OfType<MenuItem>().Cast<MenuItem>().ToList();
			if (!children.Any())
				return;

			children.ForEach(child => AuditMenu($"{path} -> {child.Header.ToString()}", child, errors));

			var headers = children.Select(child => child.Header.ToString()).ToList();

			var dupHeaders = headers.GroupBy(header => header).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
			errors.AddRange(dupHeaders.Select(header => $"{path} -> {header}: Header used multiple times"));
			headers = headers.Distinct().ToList();

			var multipleAccels = headers.Where(header => header.Contains("_")).Where(header => header.Length - header.Replace("_", "").Length > 1).ToList();
			errors.AddRange(multipleAccels.Select(header => $"{path} -> {header}: Multiple accelerators"));

			var accels = headers.Where(header => header.Contains("_")).ToDictionary(header => header, header => char.ToUpper(header[header.IndexOf("_") + 1]));
			var accelsUse = accels.Values.GroupBy(key => key).ToDictionary(group => group.Key, group => group.Count());

			var headerAvail = headers.ToDictionary(header => header, header => string.Join("", header.Select(c => char.ToUpper(c)).Distinct().Where(c => (c != '_') && (char.IsLetterOrDigit(c))).Where(c => !accelsUse.ContainsKey(c)).ToList()));
			var allAvail = string.Join("", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".Where(c => !accelsUse.ContainsKey(c)).ToList());

			var reusedAccels = accels.Where(pair => accelsUse[pair.Value] > 1).Select(pair => pair.Key).ToList();
			errors.AddRange(reusedAccels.Select(header => $"{path} -> {header}: Accelerator used multiple times ({headerAvail[header]} / {allAvail} available)"));

			var noAccel = headers.Where(header => !header.Contains("_")).ToList();
			if (noAccel.Count != headers.Count)
				errors.AddRange(noAccel.Where(header => headerAvail[header].Length != 0).Select(header => $"{path} -> {header}: No accelerator ({headerAvail[header]} / {allAvail} available)"));
		}

		public static void AuditMenu(Menu menu)
		{
			var errors = new List<string>();
			AuditMenu("Menu", menu, errors);
			if (errors.Any())
				Message.Run(null, "Error", $"Menu errors:\r\n{string.Join("\r\n", errors)}");
		}
	}
}
