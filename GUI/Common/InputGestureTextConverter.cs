using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace NeoEdit.GUI.Common
{
	public class InputGestureTextConverter : MarkupExtension, IValueConverter
	{
		static InputGestureTextConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new InputGestureTextConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var item = value as System.Windows.Controls.MenuItem;
			if ((item == null) || (item.Command == null))
				return null;

			for (var ctr = item as FrameworkElement; ctr != null; ctr = ctr.Parent as FrameworkElement)
			{
				var binding = ctr.InputBindings.Cast<KeyBinding>().Where(keyBinding => keyBinding != null).FirstOrDefault(keyBinding => keyBinding.Command == item.Command);
				if (binding == null)
					continue;

				var gesture = binding.Key.ToString();
				if ((binding.Key >= Key.D0) && (binding.Key <= Key.D9))
					gesture = (binding.Key - Key.D0).ToString();
				if ((binding.Modifiers & ModifierKeys.Shift) != 0)
					gesture = "Shift+" + gesture;
				if ((binding.Modifiers & ModifierKeys.Alt) != 0)
					gesture = "Alt+" + gesture;
				if ((binding.Modifiers & ModifierKeys.Control) != 0)
					gesture = "Ctrl+" + gesture;
				return gesture;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
