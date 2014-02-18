using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.Common;

namespace NeoEdit.BrowserUI.Dialogs
{
	public partial class Log : Window
	{
		[DepProp]
		public ObservableCollection<string> Messages { get { return uiHelper.GetPropValue<ObservableCollection<string>>(); } set { uiHelper.SetPropValue(value); } }

		static Log() { UIHelper<Log>.Register(); }

		readonly UIHelper<Log> uiHelper;
		public Log()
		{
			uiHelper = new UIHelper<Log>(this);
			InitializeComponent();
			uiHelper.AddObservableCallback(self => self.Messages, () => messages.ScrollIntoView(Messages.LastOrDefault()));
			Messages = new ObservableCollection<string>();
			Show();
		}

		private void okClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
