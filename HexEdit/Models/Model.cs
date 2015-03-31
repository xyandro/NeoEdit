using System.Collections.ObjectModel;
using System.Windows;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Models
{
	class Model : DependencyObject
	{
		[DepProp]
		public string ModelName { get { return UIHelper<Model>.GetPropValue<string>(this); } set { UIHelper<Model>.SetPropValue(this, value); } }
		[DepProp]
		public ObservableCollection<ModelAction> Actions { get { return UIHelper<Model>.GetPropValue<ObservableCollection<ModelAction>>(this); } set { UIHelper<Model>.SetPropValue(this, value); } }

		static Model() { UIHelper<Model>.Register(); }

		public Model()
		{
			Actions = new ObservableCollection<ModelAction>();
		}

		internal Model Copy()
		{
			return new Model { ModelName = ModelName, Actions = new ObservableCollection<ModelAction>(Actions) };
		}
	}
}
