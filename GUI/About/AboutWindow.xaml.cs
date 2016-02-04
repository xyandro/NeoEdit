using System.Reflection;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.About
{
	public partial class AboutWindow
	{
		[DepProp]
		string Product { get { return UIHelper<AboutWindow>.GetPropValue<string>(this); } set { UIHelper<AboutWindow>.SetPropValue(this, value); } }
		[DepProp]
		string Version { get { return UIHelper<AboutWindow>.GetPropValue<string>(this); } set { UIHelper<AboutWindow>.SetPropValue(this, value); } }
		[DepProp]
		string Copyright { get { return UIHelper<AboutWindow>.GetPropValue<string>(this); } set { UIHelper<AboutWindow>.SetPropValue(this, value); } }

		static AboutWindow() { UIHelper<AboutWindow>.Register(); }

		AboutWindow()
		{
			InitializeComponent();

			Product = ((AssemblyProductAttribute)typeof(AboutWindow).Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product;
			Version = ((AssemblyFileVersionAttribute)typeof(AboutWindow).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			Copyright = ((AssemblyCopyrightAttribute)typeof(AboutWindow).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
		}

		public static void Run() => new AboutWindow().Show();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
