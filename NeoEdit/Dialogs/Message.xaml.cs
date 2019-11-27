using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Dialogs
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

		public new MessageOptions Show() => Show(true);

		public MessageOptions Show(bool throwOnCancel)
		{
			ShowDialog();
			if ((throwOnCancel) && (Answer.HasFlag(MessageOptions.Cancel)))
				throw new OperationCanceledException();
			return Answer;
		}

		public static void Show(string text, string title = null, Window owner = null)
		{
			new Message(owner)
			{
				Text = text,
				Title = title ?? "Info",
				Options = MessageOptions.Ok,
			}.Show();
		}

		Dictionary<Button, MessageOptions> buttonActions = new Dictionary<Button, MessageOptions>();
		MessageOptions Answer { get; set; }

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

			Action<string, MessageOptions> AddButton = (text, option) =>
			{
				if (DefaultAccept == MessageOptions.None)
					DefaultAccept = option;
				if (DefaultCancel == MessageOptions.None)
					DefaultCancel = option;

				var button = new Button
				{
					Content = text,
					IsDefault = option == DefaultAccept,
					IsCancel = option == DefaultCancel,
				};
				buttonActions[button] = option;
				buttons.Children.Add(button);
			};

			if (Options.HasFlag(MessageOptions.Yes))
				AddButton("_Yes", MessageOptions.Yes);
			if (Options.HasFlag(MessageOptions.No))
				AddButton("_No", MessageOptions.No);
			if (Options.HasFlag(MessageOptions.All))
			{
				if (Options.HasFlag(MessageOptions.Yes))
					AddButton("Y_es to all", MessageOptions.Yes | MessageOptions.All);
				if (Options.HasFlag(MessageOptions.No))
					AddButton("N_o to all", MessageOptions.No | MessageOptions.All);
			}
			if (Options.HasFlag(MessageOptions.Ok))
				AddButton("_Ok", MessageOptions.Ok);
			if (Options.HasFlag(MessageOptions.Cancel))
				AddButton("_Cancel", MessageOptions.Cancel);
		}

		void ButtonHandler(object sender, RoutedEventArgs e)
		{
			Answer = buttonActions[sender as Button];
			DialogResult = true;
		}
	}
}
