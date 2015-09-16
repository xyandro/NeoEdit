using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeoEdit.TextEdit
{
	public class ResultsViewConverter : MarkupExtension, IValueConverter
	{
		static ResultsViewConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			if (converter == null)
				converter = new ResultsViewConverter();
			return converter;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var table = value as Table;
			if (table == null)
				return null;

			var gridView = new GridView();
			for (var ctr = 0; ctr < table.Headers.Count; ++ctr)
				gridView.Columns.Add(new GridViewColumn { Header = table.Headers[ctr].Replace("_", "__"), DisplayMemberBinding = new Binding(String.Format("[{0}]", ctr)) });
			return gridView;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
