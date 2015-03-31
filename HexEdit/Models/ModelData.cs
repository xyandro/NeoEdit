using System.Collections.ObjectModel;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Models
{
	class ModelData : DependencyObject
	{
		[DepProp]
		public ObservableCollection<Model> Models { get { return UIHelper<ModelData>.GetPropValue<ObservableCollection<Model>>(this); } set { UIHelper<ModelData>.SetPropValue(this, value); } }
		[DepProp]
		public Model Default { get { return UIHelper<ModelData>.GetPropValue<Model>(this); } set { UIHelper<ModelData>.SetPropValue(this, value); } }

		static ModelData() { UIHelper<ModelData>.Register(); }

		public ModelData()
		{
			Models = new ObservableCollection<Model>();
		}

		internal ModelData Copy()
		{
			return new ModelData { Models = new ObservableCollection<Model>(Models), Default = Default };
		}
	}
}
