using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common.Controls;

namespace NeoEdit.Common.Dialogs
{
	public partial class Message
	{
		[DepProp]
		public string Text { get { return UIHelper<Message>.GetPropValue<string>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }
		[DepProp]
		public MessageOptions Options { get { return UIHelper<Message>.GetPropValue<MessageOptions>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }
		[DepProp]
		public MessageOptions DefaultAccept { get { return UIHelper<Message>.GetPropValue<MessageOptions>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }
		[DepProp]
		public MessageOptions DefaultCancel { get { return UIHelper<Message>.GetPropValue<MessageOptions>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }

		static Dictionary<MessageOptions, string> buttonContent = new Dictionary<MessageOptions, string>()
		{
			[MessageOptions.Yes] = "_Yes",
			[MessageOptions.No] = "_No",
			[MessageOptions.YesToAll] = "Y_es to all",
			[MessageOptions.NoToAll] = "N_o to all",
			[MessageOptions.Ok] = "_Ok",
			[MessageOptions.Cancel] = "_Cancel",
		};

		public new MessageOptions Show()
		{
			ShowDialog();
			return Answer;
		}

		public static void Show(string text, string title = null, Window owner = null)
		{
			new Message(owner)
			{
				Text = text,
				Title = title ?? "Info",
				Options = MessageOptions.Ok,
				DefaultAccept = MessageOptions.Ok,
				DefaultCancel = MessageOptions.Ok,
			}.Show();
		}

		Dictionary<Button, MessageOptions> buttonActions = new Dictionary<Button, MessageOptions>();
		MessageOptions Answer { get; set; }

		static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

		static Message()
		{
			UIHelper<Message>.Register();
			UIHelper<Message>.AddCallback(a => a.Options, (obj, o, n) => obj.SetupButtons());
			UIHelper<Message>.AddCallback(a => a.DefaultAccept, (obj, o, n) => obj.SetupButtons());
			UIHelper<Message>.AddCallback(a => a.DefaultCancel, (obj, o, n) => obj.SetupButtons());
		}

		public Message(Window owner = null)
		{
			InitializeComponent();

			Owner = owner;
			Answer = DefaultCancel;

			Loaded += (s, e) => SetupButtons();
		}

		void SetupButtons()
		{
			buttons.Children.Clear();

			foreach (var option in Helpers.GetValues<MessageOptions>())
			{
				if ((!IsPowerOfTwo((int)option)) || ((Options & option) == 0))
					continue;

				if (DefaultAccept == MessageOptions.None)
					DefaultAccept = option;
				if (DefaultCancel == MessageOptions.None)
					DefaultCancel = option;

				var button = new Button
				{
					Content = buttonContent[option],
					IsDefault = option == DefaultAccept,
					IsCancel = option == DefaultCancel,
				};
				buttonActions[button] = option;
				buttons.Children.Add(button);
			}
		}

		void ButtonHandler(object sender, RoutedEventArgs e)
		{
			Answer = buttonActions[sender as Button];
			DialogResult = true;
		}
	}
}
