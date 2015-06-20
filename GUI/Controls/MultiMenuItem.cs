using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NeoEdit.GUI.Controls
{
	public class MultiMenuItem : MenuItem
	{
		[DepProp]
		public IEnumerable<object> Objects { get { return UIHelper<MultiMenuItem>.GetPropValue<IEnumerable<object>>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Property { get { return UIHelper<MultiMenuItem>.GetPropValue<string>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public object TrueValue { get { return UIHelper<MultiMenuItem>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public object FalseValue { get { return UIHelper<MultiMenuItem>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }

		static MultiMenuItem()
		{
			UIHelper<MultiMenuItem>.Register();
			UIHelper<MultiMenuItem>.AddCallback(a => a.Objects, (obj, o, n) => obj.Setup());
			UIHelper<MultiMenuItem>.AddCallback(a => a.Property, (obj, o, n) => obj.Setup());
			UIHelper<MultiMenuItem>.AddCallback(a => a.TrueValue, (obj, o, n) => obj.Setup());
			UIHelper<MultiMenuItem>.AddCallback(a => a.FalseValue, (obj, o, n) => obj.Setup());
		}

		public MultiMenuItem()
		{
			Click += (s, e) =>
			{
				var checkBox = Icon as CheckBox;
				if (checkBox != null)
					checkBox.IsChecked = !checkBox.IsChecked;
			};
			Setup();
		}

		class MultiConverter : IMultiValueConverter
		{
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			{
				if ((values == null) || (values.Count() == 0))
					return default(bool?);
				var match = values.Distinct().ToList();
				return match.Count == 1 ? (bool?)match.First() : default(bool?);
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			{
				return Enumerable.Repeat(value, targetTypes.Length).ToArray();
			}
		}

		class SingleConverter : IValueConverter
		{
			public object TrueValue { get; set; }
			public object FalseValue { get; set; }

			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return value.Equals(TrueValue);
			}

			public object ConvertBack(object value, Type targetTypes, object parameter, CultureInfo culture)
			{
				return ((bool?)value == true) ? TrueValue : FalseValue;
			}
		}

		void Setup()
		{
			Icon = null;
			if (Property == null)
				return;

			var multiBinding = new MultiBinding { Converter = new MultiConverter(), Mode = BindingMode.TwoWay };
			var converter = new SingleConverter { TrueValue = TrueValue, FalseValue = FalseValue };
			if (Objects != null)
				foreach (var item in Objects)
					multiBinding.Bindings.Add(new Binding(Property) { Source = item, Converter = converter, Mode = BindingMode.TwoWay });

			var cb = new CheckBox { BorderThickness = new Thickness(5, 10, 15, 20) };
			cb.SetBinding(CheckBox.IsCheckedProperty, multiBinding);
			Icon = cb;
		}
	}
}
