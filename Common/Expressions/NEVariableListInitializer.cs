using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class NEVariableListInitializer
	{
		public Action Action { get; }
		public List<NEVariableListInitializer> Dependencies { get; }
		public bool Initialized { get; private set; } = false;

		public NEVariableListInitializer(Action action, params NEVariableListInitializer[] dependencies)
		{
			Action = action;
			Dependencies = dependencies.ToList();
		}

		public void Initialize()
		{
			if (Initialized)
				return;
			lock (this)
			{
				if (Initialized)
					return;
				Dependencies.ForEach(dependency => dependency.Initialize());
				Action();
				Initialized = true;
			}
		}
	}
}
