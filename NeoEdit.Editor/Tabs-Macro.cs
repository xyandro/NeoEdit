using System;

namespace NeoEdit.Editor
{
	partial class Tabs
	{
		public Macro playingMacro, recordingMacro;
		Action playingMacroNextAction;
		public void PlayMacro(Macro macro, Action action = null)
		{
			playingMacro = macro;
			playingMacroNextAction = action;
		}

		public void EnsureNotRecording()
		{
			if (recordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}
	}
}
