using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.Common.Expressions
{
	public class NEVariableInitializer
	{
		public Action Action { get; }
		public List<NEVariableInitializer> Dependencies { get; }

		public NEVariableInitializer(Action action, params NEVariableInitializer[] dependencies)
		{
			Action = action;
			Dependencies = dependencies.ToList();
		}

		public IEnumerable<Action> AllActions()
		{
			foreach (var dependency in Dependencies)
				foreach (var action in dependency.AllActions())
					yield return action;
			yield return Action;
		}
	}
}
