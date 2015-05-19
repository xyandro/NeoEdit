﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace NeoEdit.TextEdit
{
	class MarkupNode
	{
		[Flags]
		public enum MarkupNodeType
		{
			None = 0,
			Element = 1,
			Text = 2,
			Comment = 4,
			All = Element | Text | Comment
		}

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

		public int StartOuterPosition { get; set; }
		public int EndOuterPosition { get; set; }
		public int OuterLength { get { return EndOuterPosition - StartOuterPosition; } }

		public int StartInnerPosition { get; set; }
		public int EndInnerPosition { get; set; }
		public int InnerLength { get { return EndInnerPosition - StartInnerPosition; } }

		public int Depth { get; set; }

		public MarkupNodeType NodeType { get; set; }

		readonly Dictionary<string, List<string>> attributes = new Dictionary<string, List<string>>();

		public Range RangeOuter
		{
			get
			{
				switch (NodeType)
				{
					case MarkupNodeType.Text:
					case MarkupNodeType.Comment:
						return RangeOuterFull;
					default:
						return RangeOuterStart;
				}
			}
		}
		public Range RangeOuterFull { get { return new Range(StartOuterPosition, EndOuterPosition); } }
		public Range RangeOuterStart { get { return new Range(StartOuterPosition); } }
		public Range RangeOuterEnd { get { return new Range(EndOuterPosition); } }
		public Range RangeInnerFull { get { return new Range(StartInnerPosition, EndInnerPosition); } }

		readonly List<MarkupNode> children = new List<MarkupNode>();

		internal MarkupNode()
		{
		}

		public string GetOuterText(TextData data)
		{
			return data.GetString(StartOuterPosition, OuterLength);
		}

		public string GetInnerText(TextData data)
		{
			return data.GetString(StartInnerPosition, InnerLength);
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
			return attributes[name].FirstOrDefault();
		}

		public List<string> GetAllAttributes(string name)
		{
			if (!attributes.ContainsKey(name))
				return new List<string>();
			return attributes[name];
		}

		public void AddAttribute(string name, string value, int itemStart, int itemEnd)
		{
			if (!attributes.ContainsKey(name))
				attributes[name] = new List<string>();
			attributes[name].Add(value);
		}

		public bool HasAttribute(string name, string value)
		{
			if (!attributes.ContainsKey(name))
				return false;
			return attributes[name].Any(attr => attr == value);
		}

		public void AddChild(MarkupNode node)
		{
			node.Depth = Depth + 1;
			node.Parent = this;
			children.Add(node);
		}

		public override string ToString()
		{
			return String.Format("{0} {1}: Outer: {2}, Inner: {3}", NodeType.ToString(), GetAttribute("Tag"), RangeOuterFull.ToString(), RangeInnerFull.ToString());
		}
	}
}
