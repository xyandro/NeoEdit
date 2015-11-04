using System;
using System.Collections.Generic;

namespace NeoEdit.HexEdit.Models
{
	class Model
	{
		public string ModelName { get; set; }
		public List<ModelAction> Actions { get; } = new List<ModelAction>();
		public string GUID { get; } = Guid.NewGuid().ToString();
	}
}
