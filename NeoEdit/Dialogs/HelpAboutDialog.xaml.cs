using System.Reflection;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class HelpAboutDialog
	{
		[DepProp]
		string Product { get { return UIHelper<HelpAboutDialog>.GetPropValue<string>(this); } set { UIHelper<HelpAboutDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Version { get { return UIHelper<HelpAboutDialog>.GetPropValue<string>(this); } set { UIHelper<HelpAboutDialog>.SetPropValue(this, value); } }
		[DepProp]
		string Copyright { get { return UIHelper<HelpAboutDialog>.GetPropValue<string>(this); } set { UIHelper<HelpAboutDialog>.SetPropValue(this, value); } }

		static HelpAboutDialog() { UIHelper<HelpAboutDialog>.Register(); }

		HelpAboutDialog()
		{
			InitializeComponent();

			Product = ((AssemblyProductAttribute)typeof(HelpAboutDialog).Assembly.GetCustomAttribute(typeof(AssemblyProductAttribute))).Product;
			Version = ((AssemblyFileVersionAttribute)typeof(HelpAboutDialog).Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute))).Version;
			Copyright = ((AssemblyCopyrightAttribute)typeof(HelpAboutDialog).Assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute))).Copyright;
		}

		public static Window Run()
		{
			var dialog = new HelpAboutDialog();
			dialog.Show();
			return dialog;
		}

		void LicenseClick(object sender, RoutedEventArgs e) => HelpAboutLicenseDialog.Run();

		void ChangeLogClick(object sender, RoutedEventArgs e) => HelpAboutChangeLogDialog.Run();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
