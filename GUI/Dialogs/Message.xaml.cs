using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.GUI.Common;

namespace NeoEdit.GUI.Dialogs
{
	public partial class Message : Window
	{
		[DepProp]
		public string Text { get { return UIHelper<Message>.GetPropValue<string>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }
		[DepProp]
		public OptionsEnum Options { get { return UIHelper<Message>.GetPropValue<OptionsEnum>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }
		[DepProp]
		public OptionsEnum DefaultAccept { get { return UIHelper<Message>.GetPropValue<OptionsEnum>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }
		[DepProp]
		public OptionsEnum DefaultCancel { get { return UIHelper<Message>.GetPropValue<OptionsEnum>(this); } set { UIHelper<Message>.SetPropValue(this, value); } }

		[Flags]
		public enum OptionsEnum
		{
			None = 0,
			Yes = 1,
			No = 2,
			YesNo = Yes | No,
			YesToAll = 4,
			YesNoYesAll = Yes | No | YesToAll,
			NoToAll = 8,
			YesNoNoAll = Yes | No | NoToAll,
			YesNoYesAllNoAll = Yes | No | YesToAll | NoToAll,
			Ok = 16,
			Cancel = 32,
			OkCancel = Ok | Cancel,
			YesNoCancel = Yes | No | Cancel,
			YesNoYesAllNoAllCancel = Yes | No | YesToAll | NoToAll | Cancel,
		}

		static Dictionary<OptionsEnum, string> buttonContent = new Dictionary<OptionsEnum, string>()
		{
			{ OptionsEnum.Yes, "_Yes" },
			{ OptionsEnum.No, "_No" },
			{ OptionsEnum.YesToAll, "Yes to _all" },
			{ OptionsEnum.NoToAll, "No to all" },
			{ OptionsEnum.Ok, "Ok" },
			{ OptionsEnum.Cancel, "_Cancel" },
		};

		public new OptionsEnum Show()
		{
			ShowDialog();
			return Answer;
		}

		public static void Show(string text, string title = null)
		{
			new Message
			{
				Text = text,
				Title = title ?? "Info",
				Options = OptionsEnum.Ok,
				DefaultAccept = OptionsEnum.Ok,
				DefaultCancel = OptionsEnum.Ok,
			}.Show();
		}

		Dictionary<Button, OptionsEnum> buttonActions = new Dictionary<Button, OptionsEnum>();
		OptionsEnum Answer { get; set; }

		static bool IsPowerOfTwo(int x)
		{
			return (x & (x - 1)) == 0;
		}

		static Message()
		{
			UIHelper<Message>.Register();
			UIHelper<Message>.AddCallback(a => a.Options, (obj, o, n) => obj.SetupButtons());
			UIHelper<Message>.AddCallback(a => a.DefaultAccept, (obj, o, n) => obj.SetupButtons());
			UIHelper<Message>.AddCallback(a => a.DefaultCancel, (obj, o, n) => obj.SetupButtons());
		}

		public Message()
		{
			InitializeComponent();

			Answer = DefaultCancel;

			Loaded += (s, e) => SetupButtons();
		}

		void SetupButtons()
		{
			buttons.Children.Clear();

			foreach (var option in Helpers.GetValues<OptionsEnum>())
			{
				if ((!IsPowerOfTwo((int)option)) || ((Options & option) == 0))
					continue;

				if (DefaultAccept == OptionsEnum.None)
					DefaultAccept = option;
				if (DefaultCancel == OptionsEnum.None)
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
