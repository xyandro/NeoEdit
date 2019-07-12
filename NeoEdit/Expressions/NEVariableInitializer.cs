using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Program.Expressions
{
	public class NEVariableInitializer
	{
		public Action Action { get; }
		public List<NEVariableInitializer> Dependencies { get; }
		public bool Initialized { get; private set; } = false;

		public NEVariableInitializer(Action action, params NEVariableInitializer[] dependencies)
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
