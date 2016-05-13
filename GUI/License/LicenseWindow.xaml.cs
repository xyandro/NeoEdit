using System.Linq;
using System.Text;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.About
{
	public partial class LicenseWindow
	{
		[DepProp]
		string LicenseText { get { return UIHelper<LicenseWindow>.GetPropValue<string>(this); } set { UIHelper<LicenseWindow>.SetPropValue(this, value); } }

		static LicenseWindow() { UIHelper<LicenseWindow>.Register(); }

		LicenseWindow()
		{
			InitializeComponent();

			var assembly = typeof(LicenseWindow).Assembly;
			var name = assembly.GetManifestResourceNames().Where(str => str.EndsWith("License.txt")).Single();
			var stream = assembly.GetManifestResourceStream(name);
			var buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			LicenseText = Encoding.UTF8.GetString(buf);

			license.Focus();
			license.Select(0, 0);
		}

		public static void Run() => new LicenseWindow().Show();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
