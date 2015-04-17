using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace NeoEdit.TextEdit
{
	internal class Macro
	{
		abstract class MacroAction { }

		class MacroActionKey : MacroAction
		{
			public Key key { get; private set; }
			public bool shiftDown { get; private set; }
			public bool controlDown { get; private set; }

			public MacroActionKey(Key key, bool shiftDown, bool controlDown)
			{
				this.key = key;
				this.shiftDown = shiftDown;
				this.controlDown = controlDown;
			}
		}

		class MacroActionText : MacroAction
		{
			public string text { get; private set; }

			public MacroActionText(string text)
			{
				this.text = text;
			}
		}

		class MacroActionCommand : MacroAction
		{
			public TextEditCommand command { get; private set; }
			public bool shiftDown { get; private set; }
			public object dialogResult { get; private set; }

			public MacroActionCommand(TextEditCommand command, bool shiftDown, object dialogResult)
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
			var last = macroActions.LastOrDefault() as MacroActionText;
			if (last != null)
			{
				text = last.text + text;
				macroActions.Remove(last);
			}

			macroActions.Add(new MacroActionText(text));
		}

		public void AddCommand(TextEditCommand command, bool shiftDown, object dialogResult)
		{
			macroActions.Add(new MacroActionCommand(command, shiftDown, dialogResult));
		}

		public void Play(TextEditTabs tabs, Action<bool> setPlayingStatus, Action finished=null)
		{
			setPlayingStatus(true);
			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			var ctr = 0;
			timer.Tick += (s, e) => tabs.Dispatcher.Invoke(new Action(() =>
			{
				if (ctr >= macroActions.Count)
				{
					timer.Stop();
					setPlayingStatus(false);
					if (finished != null)
						finished();
					return;
				}

				try
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
				}
				catch
				{
					timer.Stop();
					setPlayingStatus(false);
					throw;
				}
			}));
			timer.Start();
		}
	}
}
