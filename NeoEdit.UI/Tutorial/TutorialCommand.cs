using System;
using System.Linq;
using System.Windows.Controls;
using NeoEdit.Common;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Tutorial
{
	public class TutorialCommand : TextBlock
	{
		const string NextClick = " ⇒ ";
		const string NextKey = ", ";

		static readonly NEMenu neMenu = new NEMenu();

		NECommand commandEnum;
		public NECommand CommandEnum
		{
			set
			{
				commandEnum = value;
				SetText();
			}
		}

		string accel;
		public string Accel
		{
			set
			{
				accel = value;
				SetText();
			}
		}

		void SetText()
		{
			var item = UIHelper.FindLogicalChildren<NEMenuItem>(neMenu).FirstOrDefault(mi => mi.CommandEnum == commandEnum);
			if (item == null)
				throw new Exception($"Command not found in menu: {commandEnum}");

			var menuItem = item as MenuItem;
			var click = "";
			var key = "";
			while (menuItem != null)
			{
				var header = menuItem.Header.ToString();

				if (click.Length != 0)
					click = $"{NextClick}{click}";
				click = header.Replace("_", "") + click;

				var accelIndex = header.IndexOf('_');
				if (accelIndex != -1)
				{
					if (key.Length != 0)
						key = $"{NextKey}{key}";
					key = $"{char.ToUpperInvariant(header[accelIndex + 1])}{key}";
				}

				menuItem = menuItem.Parent as MenuItem;
			}
			click = $"Menu{NextClick}{click}";
			key = $"Alt+{key}";

			if (!string.IsNullOrEmpty(item.InputGestureText))
				key = item.InputGestureText.Split(new string[] { ", " }, StringSplitOptions.None).OrderBy(s => !s.Contains(accel ?? "")).First();

			Text = $"{click} (or press {key})";
		}
	}
}
