using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.UI.Resources;

namespace NeoEdit.UI.Dialogs
{
	public partial class Message : Window
	{
		public enum Options
		{
			None = 0,
			Yes = 1,
			No = 2,
			YesNo = 3,
			YesToAll = 4,
			YesNoYesAll = 7,
			NoToAll = 8,
			YesNoNoAll = 11,
			YesNoYesAllNoAll = 15,
		}

		static Dictionary<Options, string> buttonContent = new Dictionary<Options, string>()
		{
			{ Options.Yes, "_Yes" },
			{ Options.No, "_No" },
			{ Options.YesToAll, "Yes to _all" },
			{ Options.NoToAll, "No to all" },
		};

		public static Options Show(string text, string title, Options options, Options defaultYes, Options defaultNo = Options.None)
		{
			var message = new Message(text, title, options, defaultYes, defaultNo);
			message.ShowDialog();
			return message.Answer;
		}

		Dictionary<Button, Options> buttonActions = new Dictionary<Button, Options>();
		public Options Answer { get; private set; }

		static bool IsPowerOfTwo(int x)
		{
			return (x & (x - 1)) == 0;
		}

		readonly UIHelper<Message> uiHelper;
		public Message(string text, string title, Options options, Options defaultYes, Options defaultNo = Options.None)
		{
			uiHelper = new UIHelper<Message>(this);
			InitializeComponent();

			Title = title;
			label.Content = text;
			Answer = defaultNo;

			foreach (var option in Helpers.GetValues<Options>())
			{
				if ((!IsPowerOfTwo((int)option)) || ((options & option) == 0))
					continue;

				var button = new Button
				{
					Content = buttonContent[option],
					IsDefault = option == defaultYes,
					IsCancel = option == defaultNo,
				};
				buttonActions[button] = option;
				buttons.Children.Add(button);
			}
		}

		void buttonHandler(object sender, RoutedEventArgs e)
		{
			Answer = buttonActions[sender as Button];
			DialogResult = true;
		}
	}
}
