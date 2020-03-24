using System.Linq;
using System.Text;
using System.Windows;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class HelpAboutChangeLogDialog
	{
		[DepProp]
		string ChangeLogText { get { return UIHelper<HelpAboutChangeLogDialog>.GetPropValue<string>(this); } set { UIHelper<HelpAboutChangeLogDialog>.SetPropValue(this, value); } }

		static HelpAboutChangeLogDialog() { UIHelper<HelpAboutChangeLogDialog>.Register(); }

		HelpAboutChangeLogDialog()
		{
			InitializeComponent();

			var streamName = typeof(HelpAboutChangeLogDialog).Assembly.GetManifestResourceNames().Where(name => name.EndsWith(".ChangeLog.txt")).Single();
			var stream = typeof(HelpAboutChangeLogDialog).Assembly.GetManifestResourceStream(streamName);
			var buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			ChangeLogText = Encoding.UTF8.GetString(buf).Replace("\r\n", "\n").Trim('\n');

			changeLog.Focus();
			changeLog.Select(0, 0);
		}

		public static void Run() => new HelpAboutChangeLogDialog().Show();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
