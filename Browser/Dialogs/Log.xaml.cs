﻿using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.Browser.Dialogs
{
	public partial class Log : Window
	{
		[DepProp]
		string Messages { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }

		static Log() { UIHelper<Log>.Register(); }

		readonly UIHelper<Log> uiHelper;
		public Log()
		{
			uiHelper = new UIHelper<Log>(this);
			InitializeComponent();
			uiHelper.AddCallback(self => self.Messages, (o, n) => messages.ScrollToEnd());
			Messages = "";
		}

		public void AddMessage(string message)
		{
			Messages += message + "\n";
		}

		void okClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
