using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Models
{
	class Model : DependencyObject
	{
		[DepProp]
		public string ModelName { get { return UIHelper<Model>.GetPropValue<string>(this); } set { UIHelper<Model>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ModelAction> Actions { get { return UIHelper<Model>.GetPropValue<ObservableCollection<ModelAction>>(this); } set { UIHelper<Model>.SetPropValue(this, value); } }
		[DepProp]
		public string GUID { get { return UIHelper<Model>.GetPropValue<string>(this); } set { UIHelper<Model>.SetPropValue(this, value); } }

		static Model() { UIHelper<Model>.Register(); }

		public Model()
		{
			Actions = new ObservableCollection<ModelAction>();
			GUID = Guid.NewGuid().ToString();
		}

		internal XElement ToXML()
		{
			return new XElement("Model",
				new XAttribute("ModelName", ModelName),
				new XAttribute("GUID", GUID),
				new XElement("Actions", Actions.Select(action => action.ToXML()))
			);
		}

		static internal Model FromXML(XElement xml)
		{
			return new Model
			{
				ModelName = xml.Attribute("ModelName").Value,
				GUID = xml.Attribute("GUID").Value,
				Actions = new ObservableCollection<ModelAction>(xml.Elements("Actions").SelectMany(actionsXml => actionsXml.Elements()).Select(actionXml => ModelAction.FromXML(actionXml))),
			};
		}

		internal void FromXML(XElement xml, IEnumerable<Model> models)
		{
			var actionXmls = xml.Elements("Actions").SelectMany(actionsXml => actionsXml.Elements()).ToList();
			for (var ctr = 0; ctr < actionXmls.Count; ++ctr)
				Actions[ctr].FromXML(actionXmls[ctr], models);
		}

		internal Model Copy()
		{
			return new Model { ModelName = ModelName, Actions = new ObservableCollection<ModelAction>(Actions) };
		}
	}
}
