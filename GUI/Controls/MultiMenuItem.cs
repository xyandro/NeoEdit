using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NeoEdit.GUI.Controls
{
	public class MultiMenuItem<ItemType> : MenuItem where ItemType : TabsControl
	{
		[DepProp]
		public ObservableCollection<ItemType> Objects { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<ObservableCollection<ItemType>>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public string Property { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<string>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public object TrueValue { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public object FalseValue { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }
		[DepProp]
		public bool? MultiChecked { get { return UIHelper<MultiMenuItem<ItemType>>.GetPropValue<bool?>(this); } set { UIHelper<MultiMenuItem<ItemType>>.SetPropValue(this, value); } }

		static MultiMenuItem() { UIHelper<MultiMenuItem<ItemType>>.Register(); }

		public MultiMenuItem()
		{
			SetupStyle();
		}

		protected override Visual GetVisualChild(int index)
		{
			SetMultiChecked();
			return base.GetVisualChild(index);
		}

		protected override void OnClick()
		{
			var newValue = !MultiChecked ?? true;
			var property = typeof(ItemType).GetProperty(Property);
			foreach (var obj in Objects)
				if (obj.Active)
					property.SetValue(obj, newValue ? TrueValue : FalseValue);
			base.OnClick();
		}

		void SetMultiChecked()
		{
			if ((Property == null) || (Objects == null))
			{
				MultiChecked = null;
				return;
			}

			var property = typeof(ItemType).GetProperty(Property);
			var match = Objects.Where(obj => obj.Active).Select(obj => property.GetValue(obj).Equals(TrueValue)).Distinct().ToList();
			MultiChecked = match.Count == 1 ? (bool?)match.First() : default(bool?);
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
