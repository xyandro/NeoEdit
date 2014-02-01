using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace NeoEdit.UI
{
	class Clipboard : DependencyObject
	{
		public static Clipboard Current { get; private set; }
		static Clipboard()
		{
			Current = new Clipboard();
		}

		[DepProp]
		public ObservableCollection<object> Objects { get { return uiHelper.GetPropValue<ObservableCollection<object>>(); } set { uiHelper.SetPropValue(value); } }

		readonly UIHelper<Clipboard> uiHelper;
		public Clipboard()
		{
			uiHelper = new UIHelper<Clipboard>(this);
			Objects = new ObservableCollection<object>();
		}

		public void Set(IEnumerable<object> _objects)
		{
			Objects = new ObservableCollection<object>(_objects);
		}

		public List<T> Get<T>()
		{
			return new List<T>(Objects.Where(a => a is T).Cast<T>());
		}
	}
}
