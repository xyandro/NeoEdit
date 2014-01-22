using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using NeoEdit.UI;

namespace NeoEdit.UI.Controls
{
	/// <summary>
	/// Interaction logic for NEList.xaml
	/// </summary>
	public partial class NEColumn : GridViewColumn
	{
		public NEColumn()
		{
			InitializeComponent();
			multiBinding.Converter = new RecordToStringConverter();
			binding.Source = this;
		}
	}
}
