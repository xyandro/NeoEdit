using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Models
{
	class ModelData : DependencyObject
	{
		[DepProp]
		public ObservableCollection<Model> Models { get { return UIHelper<ModelData>.GetPropValue<ObservableCollection<Model>>(this); } set { UIHelper<ModelData>.SetPropValue(this, value); } }
		[DepProp]
		public Model Default { get { return UIHelper<ModelData>.GetPropValue<Model>(this); } set { UIHelper<ModelData>.SetPropValue(this, value); } }
		[DepProp]
		public bool Modified { get { return UIHelper<ModelData>.GetPropValue<bool>(this); } set { UIHelper<ModelData>.SetPropValue(this, value); } }
		[DepProp]
		public string FileName { get { return UIHelper<ModelData>.GetPropValue<string>(this); } set { UIHelper<ModelData>.SetPropValue(this, value); } }

		static ModelData() { UIHelper<ModelData>.Register(); }

		public ModelData()
		{
			Models = new ObservableCollection<Model>();
		}

		internal XElement ToXML()
		{
			return new XElement("ModelData",
				new XElement("Models", Models.Select(model => model.ToXML())),
				Default == null ? null : new XAttribute("Default", Default.GUID)
			);
		}

		static internal ModelData FromXML(XElement xml)
		{
			var modelXmls = xml.Elements("Models").SelectMany(modelsXml => modelsXml.Elements()).ToList();
			var models = new ObservableCollection<Model>(modelXmls.Select(modelXml => Model.FromXML(modelXml)));
			for (var ctr = 0; ctr < models.Count; ++ctr)
				models[ctr].FromXML(modelXmls[ctr], models);
			var defAttr = xml.Attribute("Default");
			var defaultGUID = defAttr == null ? null : defAttr.Value;
			return new ModelData { Models = models, Default = models.FirstOrDefault(model => model.GUID == defaultGUID) };
		}

		internal ModelData Copy()
		{
			return new ModelData { Models = new ObservableCollection<Model>(Models), Default = Default, Modified = Modified, FileName = FileName };
		}
	}
}
