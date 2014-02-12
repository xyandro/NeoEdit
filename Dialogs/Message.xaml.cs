using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using NeoEdit.Common;

namespace NeoEdit.Dialogs
{
	public partial class Message : Window
	{
		[DepProp]
		public string Text { get { return uiHelper.GetPropValue<string>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public OptionsEnum Options { get { return uiHelper.GetPropValue<OptionsEnum>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public OptionsEnum DefaultYes { get { return uiHelper.GetPropValue<OptionsEnum>(); } set { uiHelper.SetPropValue(value); } }
		[DepProp]
		public OptionsEnum DefaultNo { get { return uiHelper.GetPropValue<OptionsEnum>(); } set { uiHelper.SetPropValue(value); } }

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

		Dictionary<Button, OptionsEnum> buttonActions = new Dictionary<Button, OptionsEnum>();
		public OptionsEnum Answer { get; private set; }

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

			Answer = DefaultNo;

			Loaded += (s, e) => SetupButtons();
			uiHelper.AddCallback(a => a.Options, (o, n) => SetupButtons());
			uiHelper.AddCallback(a => a.DefaultYes, (o, n) => SetupButtons());
			uiHelper.AddCallback(a => a.DefaultNo, (o, n) => SetupButtons());
		}

		void SetupButtons()
		{
			buttons.Children.Clear();
			foreach (var option in Helpers.GetValues<OptionsEnum>())
			{
				if ((!IsPowerOfTwo((int)option)) || ((Options & option) == 0))
					continue;

				var button = new Button
				{
					Content = buttonContent[option],
					IsDefault = option == DefaultYes,
					IsCancel = option == DefaultNo,
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
