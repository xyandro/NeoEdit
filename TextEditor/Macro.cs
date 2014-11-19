using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Linq;
using NeoEdit.GUI.Dialogs;

namespace NeoEdit.TextEditor
{
	internal class Macro
	{
		abstract class MacroAction
		{
			internal abstract XElement ToXML();
		}

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

			internal override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Attribute(a => a.key),
					neXml.Attribute(a => a.shiftDown),
					neXml.Attribute(a => a.controlDown)
				);
			}

			internal static MacroActionKey FromXML(XElement xml)
			{
				return new MacroActionKey(NEXML<MacroActionKey>.Attribute(xml, a => a.key), NEXML<MacroActionKey>.Attribute(xml, a => a.shiftDown), NEXML<MacroActionKey>.Attribute(xml, a => a.controlDown));
			}
		}

		class MacroActionText : MacroAction
		{
			public string text { get; private set; }

			public MacroActionText(string text)
			{
				this.text = text;
			}

			internal override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Attribute(a => a.text)
				);
			}

			internal static MacroActionText FromXML(XElement xml)
			{
				return new MacroActionText(NEXML<MacroActionText>.Attribute(xml, a => a.text));
			}
		}

		class MacroActionCommand : MacroAction
		{
			public TextEditCommand command { get; private set; }
			public bool shiftDown { get; private set; }
			public DialogResult dialogResult { get; private set; }

			public MacroActionCommand(TextEditCommand command, bool shiftDown, DialogResult dialogResult)
			{
				this.command = command;
				this.shiftDown = shiftDown;
				this.dialogResult = dialogResult;
			}

			internal override XElement ToXML()
			{
				var neXml = NEXML.Create(this);
				return new XElement(neXml.Name,
					neXml.Attribute(a => a.command),
					neXml.Attribute(a => a.shiftDown),
					dialogResult == null ? null : new XElement("dialogResult", dialogResult.ToXML())
				);
			}

			internal static MacroActionCommand FromXML(XElement xml)
			{
				DialogResult dialogResult = null;
				if (xml.Element("dialogResult") != null)
				{
					var result = xml.Element("dialogResult").Elements().First();
					var name = result.Name.ToString();
					var resultType = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes()).Where(type => type.IsSubclassOf(typeof(DialogResult))).FirstOrDefault(type => NEXML.Name(type) == name);
					if (resultType != null)
					{
						var method = resultType.GetMethod("FromXML");
						dialogResult = method.Invoke(null, new object[] { result }) as DialogResult;
					}
				}
				return new MacroActionCommand(NEXML<MacroActionCommand>.Attribute(xml, a => a.command), NEXML<MacroActionCommand>.Attribute(xml, a => a.shiftDown), dialogResult);
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

		public void AddCommand(TextEditCommand command, bool shiftDown, DialogResult dialogResult)
		{
			macroActions.Add(new MacroActionCommand(command, shiftDown, dialogResult));
		}

		public XElement ToXML()
		{
			return new XElement(NEXML<Macro>.StaticName, macroActions.Select(action => action.ToXML()));
		}

		public static Macro FromXML(XElement xml)
		{
			if (xml.Name != "Macro")
				throw new ArgumentException();
			var macro = new Macro();
			foreach (var element in xml.Elements())
			{
				if (element.Name == NEXML<MacroActionKey>.StaticName)
					macro.macroActions.Add(MacroActionKey.FromXML(element));
				else if (element.Name == NEXML<MacroActionText>.StaticName)
					macro.macroActions.Add(MacroActionText.FromXML(element));
				else if (element.Name == NEXML<MacroActionCommand>.StaticName)
					macro.macroActions.Add(MacroActionCommand.FromXML(element));
			}
			return macro;
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
