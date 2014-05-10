using System.Reflection;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.About
{
	public partial class AboutWindow : Window
	{
		[DepProp]
		string Product { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string Version { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		string Copyright { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static AboutWindow() { UIHelper<AboutWindow>.Register(); }

		readonly UIHelper<AboutWindow> uiHelper;
		public AboutWindow()
		{
			uiHelper = new UIHelper<AboutWindow>(this);
			InitializeComponent();

			Product = ((AssemblyProductAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyProductAttribute))).Product;
			Version = ((AssemblyFileVersionAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			Copyright = ((AssemblyCopyrightAttribute)Assembly.GetEntryAssembly().GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
		}

		void OKClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}
	}
}
