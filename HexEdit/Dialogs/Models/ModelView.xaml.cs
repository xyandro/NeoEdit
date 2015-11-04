using System.Linq;
using System.Windows;
using NeoEdit.GUI.Controls;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	partial class ModelView
	{
		[DepProp]
		public ModelVM ModelVM { get { return UIHelper<ModelView>.GetPropValue<ModelVM>(this); } set { UIHelper<ModelView>.SetPropValue(this, value); } }

		static ModelView() { UIHelper<ModelView>.Register(); }

		ModelView(ModelVM modelVM)
		{
			InitializeComponent();
			ModelVM = modelVM;
		}

		void OkClick(object sender, RoutedEventArgs e)
		{
			ModelVM.Save();
			DialogResult = true;
		}

		void NewAction(object sender, RoutedEventArgs e) => ModelVM.NewAction();
		void EditAction(object sender, RoutedEventArgs e) => ModelVM.EditAction(actions.SelectedItem as ModelActionVM);
		void DeleteAction(object sender, RoutedEventArgs e) => ModelVM.DeleteAction(actions.SelectedItems.Cast<ModelActionVM>().ToList());
		void MoveActionUp(object sender, RoutedEventArgs e) => ModelVM.MoveActionUp(actions.SelectedItems.Cast<ModelActionVM>().ToList());
		void MoveActionDown(object sender, RoutedEventArgs e) => ModelVM.MoveActionDown(actions.SelectedItems.Cast<ModelActionVM>().ToList());

		static public bool Run(ModelVM modelVM) => new ModelView(modelVM).ShowDialog() == true;
	}
}
