using System;

namespace NeoEdit.Editor
{
	partial class NEWindow
	{
		public Macro recordingMacro;
		public void PlayMacro(Macro macro, Action action = null) => state.NEGlobal.QueueActions(macro.Actions);

		public void EnsureNotRecording()
		{
			if (recordingMacro == null)
				return;

			throw new Exception("Cannot start recording; recording is already in progess.");
		}
	}
}
