using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Browser.Dialogs
{
	public partial class ViewImage : Window
	{
		[DepProp]
		public string FileName { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static ViewImage() { UIHelper<ViewImage>.Register(); }

		readonly UIHelper<ViewImage> uiHelper;
		public ViewImage()
		{
			uiHelper = new UIHelper<ViewImage>(this);
			InitializeComponent();
			Show();
		}

		void okClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
