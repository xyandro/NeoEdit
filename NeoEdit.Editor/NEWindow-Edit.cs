using System;
using NeoEdit.Common.Enums;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		public INEWindowData GetUndo(bool text)
		{
			var data = Data;
			while (true)
			{
				if (data.Undo == null)
					return data;

				if ((data.TextChanged) || (!text))
					return data.Undo;

				data = data.Undo;
			}
		}

		public INEWindowData GetRedo(bool text)
		{
			var data = Data;
			while (true)
			{
				if (data.Redo == null)
				{
					if (data.RedoText != null)
					{
						lock (state)
						{
							if (state.SavedAnswers[nameof(GetRedo)] != (MessageOptions.Yes | MessageOptions.All))
							{
								var answer = state.NEWindow.neWindowUI.RunDialog_ShowMessage("Confirm", "No more redo steps; move to last text update?", MessageOptions.Yes | MessageOptions.Cancel, MessageOptions.Yes, MessageOptions.Cancel);
								if (answer.HasFlag(MessageOptions.Cancel))
									throw new OperationCanceledException();
								state.SavedAnswers[nameof(GetRedo)] = MessageOptions.Yes | MessageOptions.All;
								return data.RedoText;
							}
						}
					}

					return data;
				}
				data = data.Redo;
				if ((data.TextChanged) || (!text))
					return data;
			}
		}

		void Configure__Edit_Select_Limit() => state.Configuration = neWindowUI.RunDialog_Configure_Edit_Select_Limit(Focused.GetVariables());

		void Execute__Edit_Undo_Text__Edit_Undo_Step(bool text) => SetData(GetUndo(text));

		void Execute__Edit_Redo_Text__Edit_Redo_Step(bool text) => SetData(GetRedo(text));
	}
}
