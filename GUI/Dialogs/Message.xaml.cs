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
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public OptionsEnum Options { get { return uiHelper.GetPropValue<OptionsEnum>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public OptionsEnum DefaultAccept { get { return uiHelper.GetPropValue<OptionsEnum>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public OptionsEnum DefaultCancel { get { return uiHelper.GetPropValue<OptionsEnum>(); } set { uiHelper.SetPropValue(value); } }

		public enum OptionsEnum
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
			Ok = 16,
		}

		static Dictionary<OptionsEnum, string> buttonContent = new Dictionary<OptionsEnum, string>()
		{
			{ OptionsEnum.Yes, "_Yes" },
			{ OptionsEnum.No, "_No" },
			{ OptionsEnum.YesToAll, "Yes to _all" },
			{ OptionsEnum.NoToAll, "No to all" },
			{ OptionsEnum.Ok, "Ok" },
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

		static Message() { UIHelper<Message>.Register(); }

		readonly UIHelper<Message> uiHelper;
		public Message()
		{
			uiHelper = new UIHelper<Message>(this);
			InitializeComponent();
			Transparency.MakeTransparent(this);

			Answer = DefaultCancel;

			Loaded += (s, e) => SetupButtons();
			uiHelper.AddCallback(a => a.Options, (o, n) => SetupButtons());
			uiHelper.AddCallback(a => a.DefaultAccept, (o, n) => SetupButtons());
			uiHelper.AddCallback(a => a.DefaultCancel, (o, n) => SetupButtons());
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

			buttons.Columns = buttons.Children.Count;
		}

		void ButtonHandler(object sender, RoutedEventArgs e)
		{
			Answer = buttonActions[sender as Button];
			DialogResult = true;
		}
	}
}
