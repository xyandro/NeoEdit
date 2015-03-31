using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class EditModelDialog
	{
		[DepProp]
		public Model Model { get { return UIHelper<EditModelDialog>.GetPropValue<Model>(this); } set { UIHelper<EditModelDialog>.SetPropValue(this, value); } }

		readonly ModelData modelData;

		static EditModelDialog() { UIHelper<EditModelDialog>.Register(); }

		EditModelDialog(ModelData _modelData, Model model)
		{
			InitializeComponent();

			modelData = _modelData;
			Model = model;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		void NewAction(object sender, RoutedEventArgs e)
		{
			var action = EditActionDialog.Run(modelData, new ModelAction());
			if (action != null)
				Model.Actions.Add(action);
		}

		void EditAction(object sender, RoutedEventArgs e)
		{
			var action = actions.SelectedItem as ModelAction;
			if (action == null)
				return;

			action = EditActionDialog.Run(modelData, action);
			if (action != null)
				Model.Actions[actions.SelectedIndex] = action;
		}

		void DeleteAction(object sender, RoutedEventArgs e)
		{
			if (actions.SelectedIndex == -1)
				return;
			Model.Actions.RemoveAt(actions.SelectedIndex);
		}

		public static Model Run(ModelData modelData, Model model = null)
		{
			var dialog = new EditModelDialog(modelData, model == null ? new Model() : model.Copy());
			return dialog.ShowDialog() == true ? dialog.Model : null;
		}
	}
}
