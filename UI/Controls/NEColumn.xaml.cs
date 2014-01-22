using NeoEdit.Records;
using System.Windows.Controls;

namespace NeoEdit.UI.Controls
{
	/// <summary>
	/// Interaction logic for NEList.xaml
	/// </summary>
	public partial class NEColumn : GridViewColumn
	{
		[DepProp]
		public Record.Property Property { get { return uiHelper.GetPropValue<Record.Property>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<NEColumn> uiHelper;
		public NEColumn()
		{
			uiHelper = new UIHelper<NEColumn>(this);
			InitializeComponent();
			binding.Source = this;
		}
	}
}
