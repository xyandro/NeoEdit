using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public class UICommand : ICommand
	{
		public Key Key { get; set; }
		public ModifierKeys Modifiers { get; set; }
		public string Name { get; set; }
		public string Header { get; set; }
		public object Parameter { get; set; }

		public delegate void NECommandExecuteHandler(string name, object parameter);
		NECommandExecuteHandler executeHandler;
		public event NECommandExecuteHandler Executed
		{
			add { executeHandler += value; }
			remove { executeHandler -= value; }
		}

		public delegate bool NECommandCanRunHandler(string name, object parameter);
		NECommandCanRunHandler canRunHandler;
		public event NECommandCanRunHandler CanRun
		{
			add { canRunHandler += value; }
			remove { canRunHandler -= value; }
		}

		public event EventHandler CanExecuteChanged { add { } remove { } }

		public string InputGestureText
		{
			get
			{
				if (Key == Key.None)
					return "";
				var modifiers = "";
				if ((Modifiers & ModifierKeys.Control) != ModifierKeys.None)
					modifiers += "Ctrl+";
				if ((Modifiers & ModifierKeys.Alt) != ModifierKeys.None)
					modifiers += "Alt+";
				if ((Modifiers & ModifierKeys.Shift) != ModifierKeys.None)
					modifiers += "Shift+";
				return modifiers + Key.ToString();
			}
		}

		public bool CanExecute(object obj)
		{
			if (canRunHandler == null)
				return true;
			return canRunHandler(Name, Parameter);
		}

		public void Execute(object obj)
		{
			executeHandler(Name, Parameter);
		}
	}
}
