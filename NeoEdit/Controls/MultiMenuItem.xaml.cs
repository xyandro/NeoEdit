using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeoEdit.Program;

namespace NeoEdit.Program.Controls
{
	partial class MultiMenuItem
	{
		[DepProp]
		public ObservableCollection<TextEditor> Objects { get { return UIHelper<MultiMenuItem>.GetPropValue<ObservableCollection<TextEditor>>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public string Property { get { return UIHelper<MultiMenuItem>.GetPropValue<string>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public IValueConverter Converter { get { return UIHelper<MultiMenuItem>.GetPropValue<IValueConverter>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public object MultiValue { get { return UIHelper<MultiMenuItem>.GetPropValue<object>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); } }
		[DepProp]
		public bool? MultiChecked { get { return UIHelper<MultiMenuItem>.GetPropValue<bool?>(this); } set { UIHelper<MultiMenuItem>.SetPropValue(this, value); MultiStatus = value; } }

		static MultiMenuItem() { UIHelper<MultiMenuItem>.Register(); }

		public MultiMenuItem() => InitializeComponent();

		protected override Visual GetVisualChild(int index)
		{
			if ((Property == null) || (Objects == null))
				MultiChecked = null;
			else
			{
				var property = typeof(TextEditor).GetProperty(Property);
				var match = Objects.Where(obj => obj.Active).Select(obj => property.GetValue(obj)).Select(value => Converter?.Convert(value, MultiValue.GetType(), null, CultureInfo.DefaultThreadCurrentCulture) ?? value).Select(value => value.Equals(MultiValue)).Distinct().ToList();
				MultiChecked = match.Count == 1 ? match.First() : default(bool?);
			}

			return base.GetVisualChild(index);
		}
	}
}
