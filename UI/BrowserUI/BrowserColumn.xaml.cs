using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.Records;
using NeoEdit.UI.BrowserUI.Converters;

namespace NeoEdit.UI.BrowserUI
{
	/// <summary>
	/// Interaction logic for NEList.xaml
	/// </summary>
	public partial class BrowserColumn : GridViewColumn
	{
		[DepProp]
		public RecordProperty.PropertyName Property { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public RecordProperty.PropertyName SortProperty { get { return uiHelper.GetPropValue<RecordProperty.PropertyName>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<BrowserColumn> uiHelper;
		public BrowserColumn(BrowserListView parent)
		{
			uiHelper = new UIHelper<BrowserColumn>(this);
			InitializeComponent();

			header.DataContext = this;

			uiHelper.SetBinding(a => a.SortProperty, parent, a => a.SortProperty);
			uiHelper.SetBinding(a => a.SortAscending, parent, a => a.SortAscending);

			uiHelper.AddCallback(a => a.Property, (o, n) => DisplayMemberBinding = new Binding(Property.ToString()) { Converter = new PropertyFormatter() });
		}

		void HeaderClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (SortProperty != Property)
				SortProperty = Property;
			else
				SortAscending = !SortAscending;
		}
	}
}
