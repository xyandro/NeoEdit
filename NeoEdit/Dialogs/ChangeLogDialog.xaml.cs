using System.Text;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.Dialogs
{
	partial class ChangeLogDialog
	{
		[DepProp]
		string ChangeLogText { get { return UIHelper<ChangeLogDialog>.GetPropValue<string>(this); } set { UIHelper<ChangeLogDialog>.SetPropValue(this, value); } }

		static ChangeLogDialog() { UIHelper<ChangeLogDialog>.Register(); }

		ChangeLogDialog()
		{
			InitializeComponent();

			var stream = typeof(ChangeLogDialog).Assembly.GetManifestResourceStream(typeof(ChangeLogDialog).Namespace + ".ChangeLog.txt");
			var buf = new byte[stream.Length];
			stream.Read(buf, 0, buf.Length);
			ChangeLogText = Encoding.UTF8.GetString(buf).Replace("\r\n", "\n").Trim('\n');

			changeLog.Focus();
			changeLog.Select(0, 0);
		}

		public static void Run() => new ChangeLogDialog().Show();

		void OKClick(object sender, RoutedEventArgs e) => Close();
	}
}
