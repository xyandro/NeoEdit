using System;
using System.Linq;
using System.Windows.Controls;
using NeoEdit.Program.Controls;

namespace NeoEdit.Program.Tutorial
{
	public class TutorialCommand : TextBlock
	{
		const string NextClick = " ⇒ ";
		const string NextKey = ", ";

		readonly static NEMenu neMenu = new NEMenu();

		public NECommand CommandEnum
		{
			set
			{
				var item = UIHelper.FindLogicalChildren<NEMenuItem>(neMenu).FirstOrDefault(mi => mi.CommandEnum == value);
				if (item == null)
					throw new Exception($"Command not found in menu: {value}");

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
					key = item.InputGestureText;

				Text = $"Click {click} (or press {key})";
			}
		}
	}
}
