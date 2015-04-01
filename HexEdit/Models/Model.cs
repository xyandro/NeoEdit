using System;
using System.Collections.Generic;

namespace NeoEdit.HexEdit.Models
{
	class Model
	{
		public string ModelName { get; set; }
		public List<ModelAction> Actions { get; private set; }
		public string GUID { get; private set; }

		public Model()
		{
			Actions = new List<ModelAction>();
			GUID = Guid.NewGuid().ToString();
		}
	}
}
