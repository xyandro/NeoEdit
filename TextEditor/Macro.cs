using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor
{
	internal class Macro
	{
		class MacroAction
		{
		}

		class MacroActionKey : MacroAction
		{
			public readonly Key key;
			public readonly bool shiftDown, controlDown;
			public MacroActionKey(Key key, bool shiftDown, bool controlDown)
			{
				this.key = key;
				this.shiftDown = shiftDown;
				this.controlDown = controlDown;
			}
		}

		class MacroActionText : MacroAction
		{
			public readonly string text;
			public MacroActionText(string text)
			{
				this.text = text;
			}
		}

		class MacroActionCommand : MacroAction
		{
			public readonly TextEditCommand command;
			public readonly bool shiftDown;
			public readonly IDialogResult dialogResult;
			public MacroActionCommand(TextEditCommand command, bool shiftDown, IDialogResult dialogResult)
			{
				this.command = command;
				this.shiftDown = shiftDown;
				this.dialogResult = dialogResult;
			}
		}

		readonly List<MacroAction> macroActions = new List<MacroAction>();

		public void AddKey(Key key, bool shiftDown, bool controlDown)
		{
			macroActions.Add(new MacroActionKey(key, shiftDown, controlDown));
		}

		public void AddText(string text)
		{
			macroActions.Add(new MacroActionText(text));
		}

		public void AddCommand(TextEditCommand command, bool shiftDown, IDialogResult dialogResult)
		{
			macroActions.Add(new MacroActionCommand(command, shiftDown, dialogResult));
		}

		public void Play(TextEditorTabs tabs)
		{
			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			var ctr = 0;
			timer.Tick += (s, e) =>
			{
				if (ctr >= macroActions.Count)
				{
					timer.Stop();
					return;
				}

				tabs.Dispatcher.Invoke(() =>
				{
					var action = macroActions[ctr++];
					if (action is MacroActionKey)
					{
						var keyAction = action as MacroActionKey;
						tabs.HandleKey(keyAction.key, keyAction.shiftDown, keyAction.controlDown);
					}
					else if (action is MacroActionText)
					{
						var textAction = action as MacroActionText;
						tabs.HandleText(textAction.text);
					}
					else if (action is MacroActionCommand)
					{
						var commandAction = action as MacroActionCommand;
						tabs.HandleCommand(commandAction.command, commandAction.shiftDown, commandAction.dialogResult);
					}
				});
			};
			timer.Start();
		}
	}
}
