using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NeoEdit.TextEdit
{
	class MarkupNode
	{
		[Flags]
		public enum MarkupNodeList
		{
			None = 0,
			Self = 1,
			Children = 2,
			Descendants = 4,
			Parents = 8,
			SelfAndChildren = Self | Children,
			SelfAndDescendants = Self | Descendants,
			SelfAndParents = Self | Parents,
		}

		public MarkupNode Parent { get; set; }

		public int Start { get; set; }
		public int End { get; set; }
		public int Length { get { return End - Start; } }

		public int Depth { get; set; }

		public bool SelfClosing { get; set; }

		public string Type { get; set; }

		readonly Dictionary<string, List<Tuple<string, int, int>>> attributes = new Dictionary<string, List<Tuple<string, int, int>>>(StringComparer.OrdinalIgnoreCase);

		public Range RangeFull { get { return new Range(Start, End); } }
		public Range RangeStart { get { return new Range(Start); } }
		public Range RangeEnd { get { return new Range(End); } }

		readonly List<MarkupNode> children = new List<MarkupNode>();

		internal MarkupNode()
		{
		}

		public string GetText(TextData data)
		{
			return data.GetString(Start, Length);
		}

		public IEnumerable<MarkupNode> List(MarkupNodeList list)
		{
			if (list.HasFlag(MarkupNodeList.Self))
				yield return this;
			if (list.HasFlag(MarkupNodeList.Parents))
			{
				for (var parent = this.Parent; parent != null; parent = parent.Parent)
					yield return parent;
			}
			if ((list.HasFlag(MarkupNodeList.Children)) || (list.HasFlag(MarkupNodeList.Descendants)))
			{
				foreach (var child in children)
				{
					yield return child;
					if (list.HasFlag(MarkupNodeList.Descendants))
						foreach (var childChild in child.List(MarkupNodeList.Descendants))
							yield return childChild;
				}
			}
		}

		public List<string> GetAttributes()
		{
			return attributes.Keys.ToList();
		}

		public string GetAttribute(string name)
		{
			if (!attributes.ContainsKey(name))
				return null;
			return attributes[name].Select(attr => attr.Item1).FirstOrDefault();
		}

		public List<string> GetAllAttributes(string name)
		{
			if (!attributes.ContainsKey(name))
				return new List<string>();
			return attributes[name].Select(attr => attr.Item1).ToList();
		}

		public void AddAttribute(string name, string value, int itemStart, int itemEnd)
		{
			if (!attributes.ContainsKey(name))
				attributes[name] = new List<Tuple<string, int, int>>();
			attributes[name].Add(Tuple.Create(value, itemStart, itemEnd));
		}

		public bool HasAttribute(string name, Regex regex)
		{
			if (!attributes.ContainsKey(name))
				return false;
			return attributes[name].Any(attr => regex.IsMatch(attr.Item1));
		}

		public IEnumerable<Range> GetAttributeRanges(string name, bool firstOnly)
		{
			if (!attributes.ContainsKey(name))
				yield break;
			foreach (var attr in attributes[name])
			{
				yield return new Range(attr.Item2, attr.Item3);
				if (firstOnly)
					break;
			}
		}

		public void AddChild(MarkupNode node)
		{
			node.Depth = Depth + 1;
			node.Parent = this;
			children.Add(node);
		}

		public override string ToString()
		{
			return String.Format("{0} {1}: {2}", Type.ToString(), GetAttribute("Tag"), RangeFull.ToString());
		}
	}
}
