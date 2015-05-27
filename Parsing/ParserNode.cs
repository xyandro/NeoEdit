﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace NeoEdit.Parsing
{
	public class ParserNode
	{
		public ParserNode(bool isAttr = false)
		{
			IsAttr = isAttr;
		}

		ParserNode parent;
		public ParserNode Parent
		{
			get { return parent; }
			internal set
			{
				if (parent != null)
				{
					parent.children.Remove(this);
					parent = null;
					Depth = 0;
				}

				if (value != null)
				{
					if (value.IsAttr)
						throw new Exception("Attributes cannot have children");

					parent = value;
					Depth = parent.Depth + 1;
					parent.children.Add(this);
				}
			}
		}

		public readonly bool IsAttr;

		int? start, end;
		public int Start { get { return start.Value; } internal set { start = value; } }
		public int End { get { return end.Value; } internal set { end = value; } }
		public bool HasLocation { get { return (start.HasValue) && (end.HasValue); } }

		internal ParserRuleContext LocationContext
		{
			set
			{
				int lStart, lEnd;
				value.GetBounds(out lStart, out lEnd);
				start = lStart;
				end = lEnd;
			}
		}

		string type;
		public string Type
		{
			get { return IsAttr ? type : GetAttrText("Type"); }
			internal set
			{
				if (IsAttr)
					type = value;
				else
					SetAttr("Type", value);
			}
		}
		public string Text { get; internal set; }
		public int Depth { get; internal set; }

		readonly List<ParserNode> children = new List<ParserNode>();

		[Flags]
		public enum ParserNodeListType
		{
			None = 0,
			Self = 1,
			Parents = 2,
			Attributes = 4,
			Children = 8,
			Descendants = Children | 16,
			SelfAndChildren = Self | Children,
			SelfAttributesAndChildren = Self | Attributes | Children,
			SelfAndDescendants = Self | Descendants,
			SelfAttributesAndDescendants = Self | Attributes | Descendants,
			SelfAndParents = Self | Parents,
		}

		public IEnumerable<ParserNode> List(ParserNodeListType list)
		{
			if (list.HasFlag(ParserNodeListType.Self))
			{
				yield return this;
				list &= ~ParserNodeListType.Self;
			}

			if ((list.HasFlag(ParserNodeListType.Parents)) && (Parent != null))
			{
				for (var parent = Parent; parent != null; parent = parent.Parent)
					yield return parent;
				list &= ~ParserNodeListType.Parents;
			}

			if (list == ParserNodeListType.None)
				yield break;

			foreach (var child in children)
			{
				if ((child.IsAttr) && (!list.HasFlag(ParserNodeListType.Attributes)))
					continue;
				if ((!child.IsAttr) && (!list.HasFlag(ParserNodeListType.Children)))
					continue;

				yield return child;

				if (list.HasFlag(ParserNodeListType.Descendants))
				{
					foreach (var childChild in child.List(list))
						yield return childChild;
				}
			}
		}

		public IEnumerable<string> GetAttrTypes()
		{
			return List(ParserNodeListType.Attributes).Select(attr => attr.Type);
		}

		public ParserNode GetAttr(string type)
		{
			return GetAttrs(type, true).FirstOrDefault();
		}

		public IEnumerable<ParserNode> GetAttrs(string type, bool firstOnly = false)
		{
			var result = List(ParserNodeListType.Attributes).Where(attr => attr.Type == type);
			if (firstOnly)
				result = result.Take(1);
			return result;
		}

		public string GetAttrText(string type)
		{
			var attr = GetAttr(type);
			return attr == null ? null : attr.Text;
		}

		public IEnumerable<string> GetAttrsText(string type, bool firstOnly = false)
		{
			return GetAttrs(type, firstOnly).Select(attr => attr.Text);
		}

		void RemoveAttr(string type)
		{
			var toRemove = GetAttrs(type).ToList();
			foreach (var item in toRemove)
				children.Remove(item);
		}

		void SetAttrNode(string type, string value, int? start, int? end)
		{
			RemoveAttr(type);
			AddAttrNode(type, value, start, end);
		}

		public void SetAttr(string type, string value)
		{
			SetAttrNode(type, value, null, null);
		}

		public void SetAttr(string type, int start, int end)
		{
			SetAttrNode(type, null, start, end);
		}

		public void SetAttr(string type, string value, int start, int end)
		{
			SetAttrNode(type, value, start, end);
		}

		internal void SetAttr(string type, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			SetAttrNode(type, null, start, end);
		}

		internal void SetAttr(string type, string input, ParserRuleContext ctx)
		{
			var text = ctx.GetText(input);
			int start, end;
			ctx.GetBounds(out start, out end);
			SetAttrNode(type, text, start, end);
		}

		void AddAttrNode(string type, string value, int? start, int? end)
		{
			children.Add(new ParserNode(true) { Type = type, Text = value, start = start, end = end });
		}

		public void AddAttr(string type, string value)
		{
			AddAttrNode(type, value, null, null);
		}

		public void AddAttr(string type, int start, int end)
		{
			AddAttrNode(type, null, start, end);
		}

		public void AddAttr(string type, string value, int start, int end)
		{
			AddAttrNode(type, value, start, end);
		}

		internal void AddAttr(string type, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			AddAttrNode(type, null, start, end);
		}

		internal void AddAttr(string type, string input, ParserRuleContext ctx)
		{
			var text = ctx.GetText(input);
			int start, end;
			ctx.GetBounds(out start, out end);
			AddAttrNode(type, text, start, end);
		}

		public bool HasAttr(string type, Regex regex)
		{
			return List(ParserNodeListType.Attributes).Any(attr => regex.IsMatch(attr.Text));
		}

		List<string> rPrint()
		{
			var result = new List<string> { String.Format("{0}: {1}: {2}-{3} ({4})", IsAttr ? "Attr" : "Child", Type, start, end, Text) };
			result.AddRange(children.SelectMany(child => child.rPrint()).Select(str => String.Format(" {0}", str)));
			return result;
		}

		internal string Print()
		{
			return String.Join("", rPrint().Select(str => String.Format("{0}{1}", str, Environment.NewLine)));
		}

		internal void Save(string outputFile)
		{
			File.WriteAllText(outputFile, Print());
		}
	}
}
