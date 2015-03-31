using System.Collections.Generic;
using System.Windows;
using NeoEdit.GUI.Common;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class EditModelDialog
	{
		[DepProp]
		public Model Model { get { return UIHelper<EditModelDialog>.GetPropValue<Model>(this); } set { UIHelper<EditModelDialog>.SetPropValue(this, value); } }

		readonly IEnumerable<Model> models;

		static EditModelDialog() { UIHelper<EditModelDialog>.Register(); }

		EditModelDialog(IEnumerable<Model> _models, Model model)
		{
			InitializeComponent();

			models = _models;
			Model = model;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		void NewAction(object sender, RoutedEventArgs e)
		{
			var action = EditActionDialog.Run(models, new ModelAction());
			if (action != null)
				Model.Actions.Add(action);
		}

		void EditAction(object sender, RoutedEventArgs e)
		{
			var action = actions.SelectedItem as ModelAction;
			if (action == null)
				return;

			action = EditActionDialog.Run(models, action);
			if (action != null)
				Model.Actions[actions.SelectedIndex] = action;
		}

		void DeleteAction(object sender, RoutedEventArgs e)
		{
			if (actions.SelectedIndex == -1)
				return;
			Model.Actions.RemoveAt(actions.SelectedIndex);
		}

		public static Model Run(IEnumerable<Model> models, Model model = null)
		{
			model = model == null ? new Model() : model.Copy();
			return new EditModelDialog(models, model).ShowDialog() == true ? model : null;
		}
	}
}
