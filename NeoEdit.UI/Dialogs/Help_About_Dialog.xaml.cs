using System.Reflection;
using System.Windows;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class Help_About_Dialog
	{
		[DepProp]
		string Product { get { return UIHelper<Help_About_Dialog>.GetPropValue<string>(this); } set { UIHelper<Help_About_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Version { get { return UIHelper<Help_About_Dialog>.GetPropValue<string>(this); } set { UIHelper<Help_About_Dialog>.SetPropValue(this, value); } }
		[DepProp]
		string Copyright { get { return UIHelper<Help_About_Dialog>.GetPropValue<string>(this); } set { UIHelper<Help_About_Dialog>.SetPropValue(this, value); } }

		static Help_About_Dialog() { UIHelper<Help_About_Dialog>.Register(); }

		Help_About_Dialog()
		{
			InitializeComponent();

			Product = ((AssemblyProductAttribute)typeof(Help_About_Dialog).Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product;
			Version = ((AssemblyFileVersionAttribute)typeof(Help_About_Dialog).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			Copyright = ((AssemblyCopyrightAttribute)typeof(Help_About_Dialog).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
		}

		public static void Run(Window owner) => new Help_About_Dialog { Owner = owner }.ShowDialog();

		void LicenseClick(object sender, RoutedEventArgs e) => HelpAboutLicenseDialog.Run();

		void ChangeLogClick(object sender, RoutedEventArgs e) => HelpAboutChangeLogDialog.Run();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
