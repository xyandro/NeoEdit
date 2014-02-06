using System;
using System.Windows.Input;

namespace NeoEdit.Common
{
	public class UICommand : ICommand
	{
		public Key Key { get; set; }
		public ModifierKeys Modifiers { get; set; }
		public object Parameter { get; set; }

		public delegate void NECommandExecuteHandler(object parameter);
		NECommandExecuteHandler executeHandler;
		public event NECommandExecuteHandler Executed
		{
			add { executeHandler += value; }
			remove { executeHandler -= value; }
		}

		public delegate bool NECommandCanRunHandler(object parameter);
		NECommandCanRunHandler canRunHandler;
		public event NECommandCanRunHandler CanRun
		{
			add { canRunHandler += value; }
			remove { canRunHandler -= value; }
		}

		public event EventHandler CanExecuteChanged { add { } remove { } }

		public bool CanExecute(object obj)
		{
			if (canRunHandler == null)
				return true;
			return canRunHandler(Parameter);
		}

		public void Execute(object obj)
		{
			executeHandler(Parameter);
		}
	}
}
