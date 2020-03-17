using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using NeoEdit.Program.Transform;

namespace NeoEdit.Program.Converters
{
	public class ViewValuesVisibilityConverter : MarkupExtension, IValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		bool HasCodePage(HashSet<Coder.CodePage> codePages, string find)
		{
			if ((find.Length == 8) && (find.Substring(1, 5) == "Int08"))
			{
				var isLE = find.EndsWith("LE");
				find = find.Substring(0, 4) + "8";
				var list = codePages.Intersect(Coder.GetNumericCodePages()).Select(x => x.ToString()).ToList();
				var hasLE = list.Any(x => x.EndsWith("LE"));
				var hasBE = list.Any(x => x.EndsWith("BE"));
				if ((isLE) && (!hasLE) && (hasBE))
					return false;
				if ((!isLE) && (!hasBE))
					return false;
			}
			if (find.StartsWith("SInt"))
				find = find.Substring(1);
			return codePages.Any(x => x.ToString() == find);
		}

		Visibility GetResult(HashSet<Coder.CodePage> codePages, string find)
		{
			if (find.Split('|').Any(str => HasCodePage(codePages, str)))
				return Visibility.Visible;
			return Visibility.Collapsed;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((!(value is HashSet<Coder.CodePage> codePages)) || (!(parameter is string find)))
				return Visibility.Collapsed;

			switch (find)
			{
				case "LE": return GetResult(codePages, "UInt08LE|SInt08LE|UInt16LE|SInt16LE|UInt32LE|SInt32LE|UInt64LE|SInt64LE");
				case "Int08LE": return GetResult(codePages, "UInt08LE|SInt08LE");
				case "Int16LE": return GetResult(codePages, "UInt16LE|SInt16LE");
				case "Int32LE": return GetResult(codePages, "UInt32LE|SInt32LE");
				case "Int64LE": return GetResult(codePages, "UInt64LE|SInt64LE");
				case "UIntLE": return GetResult(codePages, "UInt08LE|UInt16LE|UInt32LE|UInt64LE");
				case "SIntLE": return GetResult(codePages, "SInt08LE|SInt16LE|SInt32LE|SInt64LE");
				case "BE": return GetResult(codePages, "UInt08BE|SInt08BE|UInt16BE|SInt16BE|UInt32BE|SInt32BE|UInt64BE|SInt64BE");
				case "Int08BE": return GetResult(codePages, "UInt08BE|SInt08BE");
				case "Int16BE": return GetResult(codePages, "UInt16BE|SInt16BE");
				case "Int32BE": return GetResult(codePages, "UInt32BE|SInt32BE");
				case "Int64BE": return GetResult(codePages, "UInt64BE|SInt64BE");
				case "UIntBE": return GetResult(codePages, "UInt08BE|UInt16BE|UInt32BE|UInt64BE");
				case "SIntBE": return GetResult(codePages, "SInt08BE|SInt16BE|SInt32BE|SInt64BE");
				case "Float": return GetResult(codePages, "Single|Double");
				default: return GetResult(codePages, find);
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
