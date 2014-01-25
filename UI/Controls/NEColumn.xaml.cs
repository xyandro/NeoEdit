using System.Windows.Controls;
using NeoEdit.Records;

namespace NeoEdit.UI.Controls
{
	/// <summary>
	/// Interaction logic for NEList.xaml
	/// </summary>
	public partial class NEColumn : GridViewColumn
	{
		[DepProp]
		public Property.PropertyType Property { get { return uiHelper.GetPropValue<Property.PropertyType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public Property.PropertyType SortProperty { get { return uiHelper.GetPropValue<Property.PropertyType>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public bool SortAscending { get { return uiHelper.GetPropValue<bool>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<NEColumn> uiHelper;
		public NEColumn(NEListView parent)
		{
			uiHelper = new UIHelper<NEColumn>(this);
			InitializeComponent();

			uiHelper.SetBinding(a => a.SortProperty, parent, a => a.SortProperty);
			uiHelper.SetBinding(a => a.SortAscending, parent, a => a.SortAscending);
			header.DataContext = this;
			propertyBinding.Source = this;
		}

		void headerClicked(object sender, System.Windows.RoutedEventArgs e)
		{
			if (SortProperty != Property)
				SortProperty = Property;
			else
				SortAscending = !SortAscending;
		}
	}
}
