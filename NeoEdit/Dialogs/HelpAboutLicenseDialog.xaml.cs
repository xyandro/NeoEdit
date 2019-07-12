using System.Linq;
using System.Text;
using System.Windows;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
{
	partial class HelpAboutLicenseDialog
	{
		[DepProp]
		string LicenseText { get { return UIHelper<HelpAboutLicenseDialog>.GetPropValue<string>(this); } set { UIHelper<HelpAboutLicenseDialog>.SetPropValue(this, value); } }

		static HelpAboutLicenseDialog() { UIHelper<HelpAboutLicenseDialog>.Register(); }

		HelpAboutLicenseDialog()
		{
			InitializeComponent();

			var streamName = typeof(HelpAboutLicenseDialog).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".License.txt")).Single();
			var stream = typeof(HelpAboutLicenseDialog).Assembly.GetManifestResourceStream(streamName);
			var buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			LicenseText = Encoding.UTF8.GetString(buf);

			license.Focus();
			license.Select(0, 0);
		}

		public static void Run() => new HelpAboutLicenseDialog().Show();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
