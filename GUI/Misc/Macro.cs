﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using Microsoft.Win32;
using NeoEdit.Common;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Controls;

namespace NeoEdit.GUI.Misc
{
	public class Macro<CommandType>
	{
		abstract class MacroAction { }

		class MacroActionKey : MacroAction
		{
			public Key key { get; }
			public bool shiftDown { get; }
			public bool controlDown { get; }
			public bool altDown { get; }

			public MacroActionKey(Key key, bool shiftDown, bool controlDown, bool altDown)
			{
				this.key = key;
				this.shiftDown = shiftDown;
				this.controlDown = controlDown;
				this.altDown = altDown;
			}
		}

		class MacroActionText : MacroAction
		{
			public string text { get; }

			public MacroActionText(string text) { this.text = text; }
		}

		class MacroActionCommand : MacroAction
		{
			public CommandType command { get; }
			public bool shiftDown { get; }
			public object dialogResult { get; }

			public MacroActionCommand(CommandType command, bool shiftDown, object dialogResult)
			{
				this.command = command;
				this.shiftDown = shiftDown;
				this.dialogResult = dialogResult;
			}
		}

		bool stop = false;
		readonly List<MacroAction> macroActions = new List<MacroAction>();

		public void AddKey(Key key, bool shiftDown, bool controlDown, bool altDown) => macroActions.Add(new MacroActionKey(key, shiftDown, controlDown, altDown));

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

		public void AddCommand(CommandType command, bool shiftDown, object dialogResult) => macroActions.Add(new MacroActionCommand(command, shiftDown, dialogResult));

		public void Play<ItemType>(TabsWindow<ItemType, CommandType> tabs, Action<Macro<CommandType>> setMacroPlaying, Action finished = null) where ItemType : TabsControl<ItemType, CommandType>
		{
			setMacroPlaying(this);
			var timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle);
			var ctr = 0;
			timer.Tick += (s, e) => tabs.Dispatcher.Invoke(() =>
			{
				try
				{
					if (stop)
						throw new Exception("Macro processing aborted.");

					if (ctr >= macroActions.Count)
					{
						timer.Stop();
						setMacroPlaying(null);
						if (finished != null)
							finished();
						return;
					}

					var action = macroActions[ctr++];
					if (action is MacroActionKey)
					{
						var keyAction = action as MacroActionKey;
						tabs.HandleKey(keyAction.key, keyAction.shiftDown, keyAction.controlDown, keyAction.altDown);
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
					setMacroPlaying(null);
					throw;
				}
			});
			timer.Start();
		}

		public void Stop() => stop = true;

		public readonly static string MacroDirectory = Path.Combine(Helpers.NeoEditAppData, "Macro");

		public static string ChooseMacro()
		{
			var dialog = new OpenFileDialog
			{
				DefaultExt = "xml",
				Filter = "Macro files|*.xml|All files|*.*",
				InitialDirectory = MacroDirectory,
			};
			if (dialog.ShowDialog() != true)
				return null;
			return dialog.FileName;
		}

		public void Save(string fileName = null, bool macroDirRelative = false)
		{
			Directory.CreateDirectory(MacroDirectory);
			if (fileName == null)
			{
				var dialog = new SaveFileDialog
				{
					DefaultExt = "xml",
					Filter = "Macro files|*.xml|All files|*.*",
					FileName = "Macro.xml",
					InitialDirectory = MacroDirectory,
				};
				if (dialog.ShowDialog() != true)
					return;

				fileName = dialog.FileName;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			XMLConverter.ToXML(this).Save(fileName);
		}

		public static Macro<CommandType> Load(string fileName = null, bool macroDirRelative = false)
		{
			if (fileName == null)
			{
				fileName = Macro<CommandType>.ChooseMacro();
				if (fileName == null)
					return null;
			}
			else if (macroDirRelative)
				fileName = Path.Combine(MacroDirectory, fileName);

			return XMLConverter.FromXML<Macro<CommandType>>(XElement.Load(fileName));
		}
	}
}
