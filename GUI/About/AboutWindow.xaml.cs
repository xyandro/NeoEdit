using System.Linq;
using System.Reflection;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.About
{
	public partial class AboutWindow
	{
		[DepProp]
		string Product { get { return UIHelper<AboutWindow>.GetPropValue(() => this.Product); } set { UIHelper<AboutWindow>.SetPropValue(() => this.Product, value); } }
		[DepProp]
		string Version { get { return UIHelper<AboutWindow>.GetPropValue(() => this.Version); } set { UIHelper<AboutWindow>.SetPropValue(() => this.Version, value); } }
		[DepProp]
		string Copyright { get { return UIHelper<AboutWindow>.GetPropValue(() => this.Copyright); } set { UIHelper<AboutWindow>.SetPropValue(() => this.Copyright, value); } }

		static AboutWindow() { UIHelper<AboutWindow>.Register(); }

		AboutWindow()
		{
			InitializeComponent();

			Product = ((AssemblyProductAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), true).First()).Product;
			Version = ((AssemblyFileVersionAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true).First()).Version;
			Copyright = ((AssemblyCopyrightAttribute)Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), true).First()).Copyright;
		}

		public static void Run()
		{
			new AboutWindow().Show();
		}

		void OKClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
