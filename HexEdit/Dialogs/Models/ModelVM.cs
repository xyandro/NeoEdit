using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using NeoEdit.HexEdit.Models;

namespace NeoEdit.HexEdit.Dialogs.Models
{
	class ModelVM : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = "")
		{
			var handler = PropertyChanged;
			if (handler != null)
				handler(this, new PropertyChangedEventArgs(name));
		}

		string modelName;
		public string ModelName { get { return modelName; } set { modelName = value; OnPropertyChanged(); } }
		ObservableCollection<ModelActionVM> actions;
		public ObservableCollection<ModelActionVM> Actions { get { return actions; } set { actions = value; OnPropertyChanged(); } }

		readonly ModelDataVM modelDataVM;
		public readonly Model model;

		public ModelVM(ModelDataVM modelDataVM, Model _model)
		{
			this.modelDataVM = modelDataVM;
			model = _model;
			ModelName = model.ModelName;
			Actions = new ObservableCollection<ModelActionVM>(model.Actions.Select(action => new ModelActionVM(modelDataVM, action)));
		}

		public void Save()
		{
			model.ModelName = ModelName;
			model.Actions.Clear();
			model.Actions.AddRange(Actions.Select(action => action.action));
		}

		public void NewAction()
		{
			var action = new ModelActionVM(modelDataVM, new ModelAction());
			if (action.EditDialog())
				Actions.Add(action);
		}

		public void EditAction(ModelActionVM modelActionVM)
		{
			if (modelActionVM == null)
				return;
			modelActionVM.EditDialog();
		}

		public void DeleteAction(IEnumerable<ModelActionVM> modelActionVMs)
		{
			if (!modelActionVMs.Any())
				return;
			foreach (var action in modelActionVMs)
				Actions.Remove(action);
		}

		public void MoveActionUp(IEnumerable<ModelActionVM> modelActionVMs)
		{
			var moveUp = Actions.Select(action => modelActionVMs.Contains(action)).ToList();
			var ctr = 0;
			while ((ctr != Actions.Count - 1) && (moveUp[ctr]))
				++ctr;
			for (; ctr < Actions.Count; ++ctr)
				if (moveUp[ctr])
					Actions.Move(ctr, ctr - 1);
		}

		public void MoveActionDown(IEnumerable<ModelActionVM> modelActionVMs)
		{
			var moveDown = Actions.Select(action => modelActionVMs.Contains(action)).ToList();
			var ctr = Actions.Count - 1;
			while ((ctr != 0) && (moveDown[ctr]))
				--ctr;
			for (; ctr >= 0; --ctr)
				if (moveDown[ctr])
					Actions.Move(ctr, ctr + 1);
		}

		public bool EditDialog() => ModelView.Run(this);
	}
}
