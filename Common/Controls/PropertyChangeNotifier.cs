using System;
using System.Windows;
using System.Windows.Data;

namespace NeoEdit.Common.Controls
{
	public sealed class PropertyChangeNotifier : DependencyObject
	{
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(object), typeof(PropertyChangeNotifier), new PropertyMetadata(null, OnPropertyChanged));

		static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => (d as PropertyChangeNotifier).callback();

		Action callback;

		public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property, Action callback)
		{
			this.callback = callback;
			var binding = new Binding
			{
				Path = new PropertyPath(property),
				Mode = BindingMode.OneWay,
				Source = propertySource
			};
			BindingOperations.SetBinding(this, ValueProperty, binding);
		}
	}
}
