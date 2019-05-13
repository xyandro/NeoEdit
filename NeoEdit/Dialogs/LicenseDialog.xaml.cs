using System.Text;
using System.Windows;
using NeoEdit.Controls;

namespace NeoEdit.Dialogs
{
	partial class LicenseDialog
	{
		[DepProp]
		string LicenseText { get { return UIHelper<LicenseDialog>.GetPropValue<string>(this); } set { UIHelper<LicenseDialog>.SetPropValue(this, value); } }

		static LicenseDialog() { UIHelper<LicenseDialog>.Register(); }

		LicenseDialog()
		{
			InitializeComponent();

			var stream = typeof(LicenseDialog).Assembly.GetManifestResourceStream(typeof(LicenseDialog).Namespace + ".License.txt");
			var buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			LicenseText = Encoding.UTF8.GetString(buf);

			license.Focus();
			license.Select(0, 0);
		}

		public static void Run() => new LicenseDialog().Show();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
