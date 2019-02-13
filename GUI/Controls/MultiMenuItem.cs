using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeoEdit.GUI.Controls
{
	public class MultiMenuItem<ItemType, CommandType> : NEMenuItem<CommandType> where ItemType : TabsControl<ItemType, CommandType>
	{
		[DepProp]
		public ObservableCollection<ItemType> Objects { get { return UIHelper<MultiMenuItem<ItemType, CommandType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<MultiMenuItem<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public string Property { get { return UIHelper<MultiMenuItem<ItemType, CommandType>>.GetPropValue<string>(this); } set { UIHelper<MultiMenuItem<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public IValueConverter Converter { get { return UIHelper<MultiMenuItem<ItemType, CommandType>>.GetPropValue<IValueConverter>(this); } set { UIHelper<MultiMenuItem<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public object MultiValue { get { return UIHelper<MultiMenuItem<ItemType, CommandType>>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem<ItemType, CommandType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool? MultiChecked { get { return UIHelper<MultiMenuItem<ItemType, CommandType>>.GetPropValue<bool?>(this); } set { UIHelper<MultiMenuItem<ItemType, CommandType>>.SetPropValue(this, value); MultiStatus = value; } }

		static MultiMenuItem() { UIHelper<MultiMenuItem<ItemType, CommandType>>.Register(); }

		public MultiMenuItem() { SetupStyle(); }

		protected override Visual GetVisualChild(int index)
		{
			if ((Property == null) || (Objects == null))
				MultiChecked = null;
			else
			{
				var property = typeof(ItemType).GetProperty(Property);
				var match = Objects.Where(obj => obj.Active).Select(obj => property.GetValue(obj)).Select(value => Converter?.Convert(value, MultiValue.GetType(), null, CultureInfo.DefaultThreadCurrentCulture) ?? value).Select(value => value.Equals(MultiValue)).Distinct().ToList();
				MultiChecked = match.Count == 1 ? match.First() : default(bool?);
			}

			return base.GetVisualChild(index);
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
