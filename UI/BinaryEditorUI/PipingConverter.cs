using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;

namespace NeoEdit.UI.BinaryEditorUI
{
	class PipingConverter : List<IValueConverter>, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
