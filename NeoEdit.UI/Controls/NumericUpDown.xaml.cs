using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace NeoEdit.UI.Controls
{
	partial class NumericUpDown
	{
		[DepProp]
		public bool IsHex { get { return UIHelper<NumericUpDown>.GetPropValue<bool>(this); } set { UIHelper<NumericUpDown>.SetPropValue(this, value); } }
		[DepProp]
		public long Minimum { get { return UIHelper<NumericUpDown>.GetPropValue<long>(this); } set { UIHelper<NumericUpDown>.SetPropValue(this, value); } }
		[DepProp(BindsTwoWayByDefault = true)]
		public long? Value { get { return UIHelper<NumericUpDown>.GetPropValue<long?>(this); } set { UIHelper<NumericUpDown>.SetPropValue(this, value); } }
		[DepProp]
		public long Maximum { get { return UIHelper<NumericUpDown>.GetPropValue<long>(this); } set { UIHelper<NumericUpDown>.SetPropValue(this, value); } }

		public TextAlignment TextAlignment { set { this.value.TextAlignment = value; } }

		static NumericUpDown()
		{
			UIHelper<NumericUpDown>.Register();
			UIHelper<NumericUpDown>.AddCallback(a => a.Value, (obj, o, n) => obj.Validate());
			UIHelper<NumericUpDown>.AddCallback(a => a.Minimum, (obj, o, n) => obj.Validate());
			UIHelper<NumericUpDown>.AddCallback(a => a.Maximum, (obj, o, n) => obj.Validate());
		}

		public NumericUpDown()
		{
			InitializeComponent();
			IsHex = false;
			Minimum = long.MinValue;
			Maximum = long.MaxValue;
		}

		void Validate() => Value = Value.HasValue ? Math.Max(Minimum, Math.Min(Maximum, Value.Value)) : default(long?);

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			value.Focus();
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			if (e.Handled)
				return;
			var controlDown = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
			e.Handled = true;
			switch (e.Key)
			{
				case Key.Up: ++Value; break;
				case Key.Down: --Value; break;
				case Key.H: IsHex = !IsHex; break;
				default: e.Handled = false; break;
			}
			if (e.Handled)
				value.CaretIndex = value.Text.Length;
		}
	}

	class NumericUpDownConverter : MarkupExtension, IMultiValueConverter
	{
		public override object ProvideValue(IServiceProvider serviceProvider) => this;

		public object Convert(object[] _values, Type targetType, object parameter, CultureInfo culture)
		{
			var value = (long?)_values[0];
			var isHex = (bool)_values[1];
			if (value == null)
				return "";
			else if (isHex)
				return $"0x{value:x}";
			else
				return value.Value.ToString("n0");
		}

		public object[] ConvertBack(object _values, Type[] targetType, object parameter, CultureInfo culture)
		{
			if (!(_values is string))
				throw new ArgumentException();
			try
			{
				var values = _values as string;
				var isHex = values.StartsWith("0x");
				long? value;
				if (string.IsNullOrEmpty(values))
					value = null;
				else if (isHex)
					value = long.Parse(values.Substring(2), NumberStyles.AllowHexSpecifier);
				else
					value = long.Parse(values, NumberStyles.AllowThousands | NumberStyles.AllowLeadingSign);
				return new object[] { value, isHex };
			}
			catch { return null; }
		}
	}
}
