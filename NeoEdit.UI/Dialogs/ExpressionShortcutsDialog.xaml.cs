using System.Collections.Generic;
using System.Linq;
using System.Windows;
using NeoEdit.UI.Controls;

namespace NeoEdit.UI.Dialogs
{
	partial class ExpressionShortcutsDialog
	{
		static public Dictionary<string, string> Shortcuts { get; } = AutoCompleteTextBox.Shortcuts.ToDictionary(pair => $"Ctrl+Shift+{pair.Key.FromKey()}", pair => pair.Value);

		ExpressionShortcutsDialog() => InitializeComponent();

		void OkClick(object sender, RoutedEventArgs e) => Close();

		public static void Run()
		{
			var dialog = new ExpressionShortcutsDialog();
			dialog.Show();
			dialog.Focus();
		}
	}
}
