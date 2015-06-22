using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using NeoEdit.Common;

namespace NeoEdit.GUI.Controls
{
	public class MultiMenuItem<ItemType> : MenuItem where ItemType : FrameworkElement
	{
		[DepProp]
		public ObservableCollection<Tabs<ItemType>.ItemData> Objects { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<ObservableCollection<Tabs<ItemType>.ItemData>>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public string Property { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<string>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public object TrueValue { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public object FalseValue { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool? MultiChecked { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<bool?>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }

		static MultiMenuItem()
		{
			UIHelper<MultiMenuItem<ItemType>>.Register();
			UIHelper<MultiMenuItem<ItemType>>.AddObservableCallback(a => a.Objects, (obj, o, n) => obj.SetupMultiCheckedBinding());
			UIHelper<MultiMenuItem<ItemType>>.AddCallback(a => a.Property, (obj, o, n) => obj.SetupMultiCheckedBinding());
			UIHelper<MultiMenuItem<ItemType>>.AddCallback(a => a.TrueValue, (obj, o, n) => obj.SetupMultiCheckedBinding());
			UIHelper<MultiMenuItem<ItemType>>.AddCallback(a => a.FalseValue, (obj, o, n) => obj.SetupMultiCheckedBinding());
		}

		public MultiMenuItem()
		{
			Click += (s, e) => MultiChecked = !MultiChecked;
			SetupMultiCheckedBinding();
			SetupStyle();
		}

		class MultiToBoolConverter : IMultiValueConverter
		{
			public object TrueValue { get; set; }
			public object FalseValue { get; set; }

			List<bool> Active;
			List<object> Values;
			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			{
				if (values == null)
					return default(bool?);
				Active = values.GetNth(2).Cast<bool>().ToList();
				Values = values.Skip(1).GetNth(2).ToList();
				var match = Values.Where((stat, index) => Active[index]).Select(value => value.Equals(TrueValue)).Distinct().ToList();
				return match.Count == 1 ? (bool?)match.First() : default(bool?);
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
			{
				var newValue = value as bool? ?? true;
				var result = new List<object>();
				for (var ctr = 0; ctr < Active.Count; ++ctr)
				{
					result.Add(Active[ctr]);
					result.Add(!Active[ctr] ? Values[ctr] : newValue ? TrueValue : FalseValue);
				}
				return result.ToArray();
			}
		}

		void SetupMultiCheckedBinding()
		{
			if (Property == null)
				return;

			var multiConverterBinding = new MultiBinding { Converter = new MultiToBoolConverter { TrueValue = TrueValue, FalseValue = FalseValue }, Mode = BindingMode.TwoWay };
			if (Objects != null)
				foreach (var item in Objects)
				{
					multiConverterBinding.Bindings.Add(new Binding(UIHelper<Tabs<ItemType>.ItemData>.GetProperty(a => a.Active).Name) { Source = item, Mode = BindingMode.OneWay });
					multiConverterBinding.Bindings.Add(new Binding(UIHelper<Tabs<ItemType>.ItemData>.GetProperty(a => a.Item).Name + "." + Property) { Source = item, Mode = BindingMode.TwoWay });
				}

			SetBinding(UIHelper<MultiMenuItem<ItemType>>.GetProperty(a => a.MultiChecked), multiConverterBinding);
		}

		void SetupStyle()
		{
			var style = new Style();

			{
				var trigger = new DataTrigger { Binding = new Binding("MultiChecked") { Source = this }, Value = true };
				trigger.Setters.Add(new Setter { Property = IconProperty, Value = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.GUI;component/Resources/Checked.png")) } });
				style.Triggers.Add(trigger);
			}

			{
				var trigger = new DataTrigger { Binding = new Binding("MultiChecked") { Source = this }, Value = false };
				trigger.Setters.Add(new Setter { Property = IconProperty, Value = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.GUI;component/Resources/Unchecked.png")) } });
				style.Triggers.Add(trigger);
			}

			{
				var trigger = new DataTrigger { Binding = new Binding("MultiChecked") { Source = this }, Value = null };
				trigger.Setters.Add(new Setter { Property = IconProperty, Value = new Image { Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit.GUI;component/Resources/Indeterminate.png")) } });
				style.Triggers.Add(trigger);
			}

			Style = style;
		}
	}
}
