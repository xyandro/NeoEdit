using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.BrowserUI.Dialogs
{
	public partial class Log : Window
	{
		[DepProp]
		public string Messages { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static Log() { UIHelper<Log>.Register(); }

		readonly UIHelper<Log> uiHelper;
		public Log()
		{
			uiHelper = new UIHelper<Log>(this);
			InitializeComponent();
			uiHelper.AddCallback(self => self.Messages, (o, n) => messages.ScrollToEnd());
			Messages = "";
			Show();
		}

		public void AddMessage(string message)
		{
			Messages += message + "\n";
		}

		private void okClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
