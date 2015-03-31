using System.Collections.Generic;
using System.Windows;
using NeoEdit.Common.Transform;
using NeoEdit.GUI.Common;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class EditActionDialog
	{
		[DepProp]
		public ModelAction Action { get { return UIHelper<EditActionDialog>.GetPropValue<ModelAction>(this); } set { UIHelper<EditActionDialog>.SetPropValue(this, value); } }

		[DepProp]
		public List<Coder.CodePage> BasicTypes { get { return UIHelper<EditActionDialog>.GetPropValue<List<Coder.CodePage>>(this); } set { UIHelper<EditActionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public List<ModelAction.ActionStringType> StringTypes { get { return UIHelper<EditActionDialog>.GetPropValue<List<ModelAction.ActionStringType>>(this); } set { UIHelper<EditActionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Dictionary<Coder.CodePage, string> EncodingTypes { get { return UIHelper<EditActionDialog>.GetPropValue<Dictionary<Coder.CodePage, string>>(this); } set { UIHelper<EditActionDialog>.SetPropValue(this, value); } }
		[DepProp]
		public Dictionary<Model, string> Models { get { return UIHelper<EditActionDialog>.GetPropValue<Dictionary<Model, string>>(this); } set { UIHelper<EditActionDialog>.SetPropValue(this, value); } }

		static EditActionDialog() { UIHelper<EditActionDialog>.Register(); }

		EditActionDialog(ModelData modelData, ModelAction action)
		{
			StringTypes = new List<ModelAction.ActionStringType>();
			StringTypes.Add(ModelAction.ActionStringType.StringWithLength);
			StringTypes.Add(ModelAction.ActionStringType.StringNullTerminated);
			StringTypes.Add(ModelAction.ActionStringType.StringFixedWidth);

			BasicTypes = new List<Coder.CodePage>();
			EncodingTypes = new Dictionary<Coder.CodePage, string>();
			foreach (var codePage in Coder.GetCodePages(false))
			{
				if (Coder.IsStr(codePage))
					EncodingTypes[codePage] = Coder.GetDescription(codePage);
				else
					BasicTypes.Add(codePage);
			}

			Models = new Dictionary<HexEdit.Models.Model, string>();
			foreach (var model in modelData.Models)
				Models[model] = model.ModelName;

			InitializeComponent();

			Action = action;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public static ModelAction Run(ModelData modelData, ModelAction action = null)
		{
			action = action == null ? new ModelAction() : action.Copy();
			return new EditActionDialog(modelData, action).ShowDialog() == true ? action : null;
		}
	}
}
