using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeoEdit.Program;
using NeoEdit.Program.Misc;

namespace NeoEdit.Program.Controls
{
	public class MultiMenuItem : NEMenuItem
	{
		[DepProp]
		public ObservableCollection<TextEditor> Objects { get { return UIHelper<MultiMenuItem>.GetPropValue<ObservableCollection<TextEditor>>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Property { get { return UIHelper<MultiMenuItem>.GetPropValue<string>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public IValueConverter Converter { get { return UIHelper<MultiMenuItem>.GetPropValue<IValueConverter>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public object MultiValue { get { return UIHelper<MultiMenuItem>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }

		static MultiMenuItem()
		{
			UIHelper<MultiMenuItem>.Register();
			UIHelper<MultiMenuItem>.AddObservableCallback(x => x.Objects, (obj, s, e) => obj.InvalidateIconBinding());
		}

		readonly RunOnceTimer timer;

		public MultiMenuItem() => timer = new RunOnceTimer(SetupIconBinding);

		void InvalidateIconBinding() => timer.Start();

		void SetupIconBinding()
		{
			var multiBinding = new MultiBinding { Converter = new AggregateConverter(this) };
			foreach (var obj in Objects)
				multiBinding.Bindings.Add(new Binding(Property) { Source = obj, Converter = Converter });
			SetBinding(IconProperty, multiBinding);
		}

		class AggregateConverter : IMultiValueConverter
		{
			readonly MultiMenuItem multiMenuItem;

			public AggregateConverter(MultiMenuItem multiMenuItem) => this.multiMenuItem = multiMenuItem;

			public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
			{
				var match = values?.Select(value => value.Equals(multiMenuItem.MultiValue)).Distinct().ToList();
				multiMenuItem.MultiStatus = match?.Count == 1 ? match.First() : default(bool?);

				switch (multiMenuItem.MultiStatus)
				{
					case true: return new Image { Stretch = Stretch.None, Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit;component/Resources/Checked.png")) };
					case false: return new Image { Stretch = Stretch.None, Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit;component/Resources/Unchecked.png")) };
					case null: return new Image { Stretch = Stretch.None, Source = new BitmapImage(new Uri("pack://application:,,,/NeoEdit;component/Resources/Indeterminate.png")) };
					default: return null;
				}
			}

			public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
		}
	}
}
