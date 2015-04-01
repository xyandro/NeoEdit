using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class ModelActionView
	{
		[DepProp]
		public ModelActionVM ModelActionVM { get { return UIHelper<ModelActionView>.GetPropValue<ModelActionVM>(this); } set { UIHelper<ModelActionView>.SetPropValue(this, value); } }
		[DepProp]
		public Dictionary<string, string> Models { get { return UIHelper<ModelActionView>.GetPropValue<Dictionary<string, string>>(this); } set { UIHelper<ModelActionView>.SetPropValue(this, value); } }
		[DepProp]
		public List<Coder.CodePage> BasicTypes { get { return UIHelper<ModelActionView>.GetPropValue<List<Coder.CodePage>>(this); } set { UIHelper<ModelActionView>.SetPropValue(this, value); } }
		[DepProp]
		public Dictionary<Coder.CodePage, string> EncodingTypes { get { return UIHelper<ModelActionView>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<ModelActionView>.SetPropValue(this, value); } }

		static ModelActionView() { UIHelper<ModelActionView>.Register(); }

		ModelActionView(ModelDataVM modelDataVM, ModelActionVM actionVM)
		{
			Models = new Dictionary<string, string>();
			foreach (var model in modelDataVM.Models)
				Models[model.model.GUID] = model.ModelName;

			BasicTypes = new List<Coder.CodePage>();
			EncodingTypes = new Dictionary<Coder.CodePage, string>();
			foreach (var codePage in Coder.GetAllCodePages())
			{
				if (Coder.IsStr(codePage))
					EncodingTypes[codePage] = Coder.GetDescription(codePage);
				else
					BasicTypes.Add(codePage);
			}

			InitializeComponent();
			ModelActionVM = actionVM;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			ModelActionVM.Save();
			DialogResult = true;
		}

		static public bool Run(ModelDataVM modelDataVM, ModelActionVM action)
		{
			return new ModelActionView(modelDataVM, action).ShowDialog() == true;
		}
	}
}
