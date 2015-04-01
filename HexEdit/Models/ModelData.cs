using System.Collections.Generic;
using System.Linq;
using NeoEdit.Common.Transform;

namespace NeoEdit.HexEdit.Models
{
	class ModelData
	{
		public List<Model> Models { get; private set; }
		public string Default { get; set; }
		public string FileName { get; set; }

		public ModelData()
		{
			Models = new List<Model>();
		}

		public Model GetModel(string GUID)
		{
			return Models.FirstOrDefault(model => model.GUID == GUID);
		}

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
