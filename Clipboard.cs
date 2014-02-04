using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using NeoEdit.UI.Resources;

namespace NeoEdit
{
	class Clipboard : DependencyObject
	{
		public static Clipboard Current { get; private set; }
		static Clipboard()
		{
			UIHelper<Clipboard>.Register();
			Current = new Clipboard();
		}

		public enum ClipboardType
		{
			Copy,
			Cut,
		}

		[DepProp]
		public ObservableCollection<object> Objects { get { return uiHelper.GetPropValue<ObservableCollection<object>>(); } set { uiHelper.SetPropValue(value); } }
		public ClipboardType Type { get; private set; }

		readonly UIHelper<Clipboard> uiHelper;
		public Clipboard()
		{
			uiHelper = new UIHelper<Clipboard>(this);
			Objects = new ObservableCollection<object>();
		}

		public void Set(IEnumerable<object> objects, ClipboardType type)
		{
			Objects = new ObservableCollection<object>(objects);
			Type = type;
		}

		public List<T> Get<T>()
		{
			return new List<T>(Objects.Where(a => a is T).Cast<T>());
		}
	}
}
