using System.Collections.Generic;
using NeoEdit.Common.Transform;

namespace NeoEdit.Editor.PreExecution
{
	public class PreExecution_Files_SelectGet_BySourceControlStatus : IPreExecution
	{
		public Dictionary<string, Versioner.Status> Statuses { get; set; }
	}
}
