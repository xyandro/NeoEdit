using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using NeoEdit.Controls;
using NeoEdit.Converters;

namespace NeoEdit.Dialogs
{
	partial class JoinDisplay
	{
		[DepProp(BindsTwoWayByDefault = true)]
		public Table.JoinType JoinType { get { return UIHelper<JoinDisplay>.GetPropValue<Table.JoinType>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }
		[DepProp]
		public Table.JoinType JoinValue { get { return UIHelper<JoinDisplay>.GetPropValue<Table.JoinType>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }

		[DepProp]
		public string JoinText { get { return UIHelper<JoinDisplay>.GetPropValue<string>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }
		[DepProp]
		bool LeftSection { get { return UIHelper<JoinDisplay>.GetPropValue<bool>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }
		[DepProp]
		bool MiddleSection { get { return UIHelper<JoinDisplay>.GetPropValue<bool>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }
		[DepProp]
		bool RightSection { get { return UIHelper<JoinDisplay>.GetPropValue<bool>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }
		[DepProp]
		bool CrossSection { get { return UIHelper<JoinDisplay>.GetPropValue<bool>(this); } set { UIHelper<JoinDisplay>.SetPropValue(this, value); } }

		static JoinDisplay()
		{
			UIHelper<JoinDisplay>.Register();
			UIHelper<JoinDisplay>.AddCallback(a => a.JoinValue, (obj, o, n) => obj.SetupJoin());
		}

		public JoinDisplay()
		{
			InitializeComponent();
			SetupJoin();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);
			JoinType = JoinValue;
		}

		void SetupJoin()
		{
			LeftSection = MiddleSection = RightSection = CrossSection = false;
			switch (JoinValue)
			{
				case Table.JoinType.Inner: JoinText = "_Inner"; MiddleSection = true; break;
				case Table.JoinType.Left: JoinText = "_Left"; LeftSection = true; MiddleSection = true; break;
				case Table.JoinType.Full: JoinText = "_Full"; LeftSection = true; MiddleSection = true; RightSection = true; break;
				case Table.JoinType.Right: JoinText = "_Right"; MiddleSection = true; RightSection = true; break;
				case Table.JoinType.Cross: JoinText = "_Cross"; LeftSection = true; MiddleSection = true; RightSection = true; CrossSection = true; break;
				case Table.JoinType.LeftExc: JoinText = "L_eft Exclusive"; LeftSection = true; break;
				case Table.JoinType.FullExc: JoinText = "F_ull Exclusive"; LeftSection = true; RightSection = true; break;
				case Table.JoinType.RightExc: JoinText = "Ri_ght Exclusive"; RightSection = true; break;
			}

			radio.SetBinding(RadioButton.IsCheckedProperty, new Binding("JoinType") { Source = this, Converter = new RadioConverter(), ConverterParameter = JoinValue.ToString() });
		}
	}

	class VisibilityConverter : MarkupExtension, IValueConverter
	{
		static VisibilityConverter converter;
		public override object ProvideValue(IServiceProvider serviceProvider) => converter = converter ?? new VisibilityConverter();
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (value is bool) && ((bool)value) ? Visibility.Visible : Visibility.Hidden;
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) { throw new NotImplementedException(); }
	}
}
