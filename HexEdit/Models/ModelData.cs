using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Transform;

namespace NeoEdit.HexEdit.Models
{
	class ModelData
	{
		public List<Model> Models { get; } = new List<Model>();
		public string Default { get; set; }
		public string FileName { get; set; }

		public Model GetModel(string GUID) => Models.FirstOrDefault(model => model.GUID == GUID);

		public void Save(string fileName)
		{
			FileName = fileName;
			XMLConverter.Save(this, FileName);
		}

		static public ModelData Load(string fileName)
		{
			var modelData = XMLConverter.Load<ModelData>(fileName);
			modelData.FileName = fileName;
			return modelData;
		}
	}
}
