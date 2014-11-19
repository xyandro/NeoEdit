using System.Xml.Linq;

namespace NeoEdit.GUI.Dialogs
{
	public abstract class DialogResult
	{
		public abstract XElement ToXML();
	}
}
