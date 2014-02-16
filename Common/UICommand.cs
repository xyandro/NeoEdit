using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public class UICommand : ICommand
	{
		public Key Key { get; set; }
		public ModifierKeys Modifiers { get; set; }
		public Enum Enum { get; set; }
		public string Header { get; set; }
		public object Parameter { get; set; }

		public delegate void NECommandRunHandler(UICommand command, object parameter);
		NECommandRunHandler runHandler;
		public event NECommandRunHandler Run
		{
			add { runHandler += value; }
			remove { runHandler -= value; }
		}

		public delegate bool NECommandCanRunHandler(UICommand command, object parameter);
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
			return canRunHandler(this, Parameter);
		}

		public void Execute(object obj)
		{
			runHandler(this, Parameter);
		}
	}
}
