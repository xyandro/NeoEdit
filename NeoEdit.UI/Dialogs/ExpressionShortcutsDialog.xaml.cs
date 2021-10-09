using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace NeoEdit.UI.Dialogs
{
	partial class ExpressionShortcutsDialog
	{
		static Dictionary<Key, string> shortcuts = new Dictionary<Key, string>
		{
			[Key.X] = "x",
			[Key.Y] = "y",
			[Key.Z] = "z",
			[Key.C] = "c",
			[Key.K] = "k",
			[Key.D1] = "r1",
			[Key.D2] = "r2",
			[Key.D3] = "r3",
			[Key.D4] = "r4",
			[Key.D5] = "r5",
			[Key.D6] = "r6",
			[Key.D7] = "v7",
			[Key.D8] = "v8",
			[Key.D9] = "v9",
		};

		static public Dictionary<string, string> Shortcuts { get; } = shortcuts.ToDictionary(pair => $"Ctrl+Shift+{pair.Key.FromKey()}", pair => pair.Value);

		ExpressionShortcutsDialog() => InitializeComponent();

		void OkClick(object sender, RoutedEventArgs e) => Close();

		public static void Run()
		{
			var dialog = new ExpressionShortcutsDialog();
			dialog.Show();
			dialog.Focus();
		}

		public static (string, bool) HandleKey(KeyEventArgs e, string text, bool isExpression)
		{
			if ((Keyboard.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift)) && (shortcuts.ContainsKey(e.Key)))
			{
				text = shortcuts[e.Key];
				isExpression = true;
			}
			return (text, isExpression);
		}
	}
}
