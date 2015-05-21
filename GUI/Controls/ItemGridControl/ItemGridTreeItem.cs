using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NeoEdit.GUI.Controls.ItemGridControl
{
	public abstract class ItemGridTreeItem : DependencyObject
	{
		[DepProp]
		public string FullName { get { return UIHelper<ItemGridTreeItem>.GetPropValue<string>(this); } private set { UIHelper<ItemGridTreeItem>.SetPropValue(this, value); } }

		public abstract ItemGridTreeItem GetParent();
		public virtual bool CanGetChildren() { return false; }
		public virtual IEnumerable<ItemGridTreeItem> GetChildren() { throw new NotImplementedException(); }

		static ItemGridTreeItem() { UIHelper<ItemGridTreeItem>.Register(); }

		protected ItemGridTreeItem(string FullName)
		{
			this.FullName = FullName;
		}

		protected virtual string GetPath(string fullName)
		{
			if (fullName == "")
				return "";

			var idx = fullName.LastIndexOf('\\');
			if (idx == -1)
				return "";

			return fullName.Substring(0, idx);
		}

		public ItemGridTreeItem GetChild(string fullName)
		{
			if (fullName == "")
				return this;

			var parts = new List<string>();
			while (fullName != "")
			{
				parts.Insert(0, fullName);
				fullName = GetPath(fullName);
			}

			var result = this;
			foreach (var part in parts)
			{
				if ((result == null) || (!result.CanGetChildren()))
					return null;
				result = result.GetChildren().FirstOrDefault(child => (child as ItemGridTreeItem).FullName.Equals(part, StringComparison.InvariantCultureIgnoreCase)) as ItemGridTreeItem;
			}

			return result;
		}
	}
}
