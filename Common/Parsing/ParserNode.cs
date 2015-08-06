using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace NeoEdit.Common.Parsing
{
	public class ParserNode
	{
		public const string TYPE = "Type";

		public ParserNode(bool isAttr = false)
		{
			IsAttr = isAttr;
		}

		ParserNode parent;
		public ParserNode Parent
		{
			get { return parent; }
			set
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
		public int Start { get { return start.Value; } set { start = value; } }
		public int End { get { return end.Value; } set { end = value; } }
		public int Length { get { return End - Start; } }
		public bool HasLocation { get { return (start.HasValue) && (end.HasValue); } }

		public ParserRuleContext LocationParserRule
		{
			set
			{
				int lStart, lEnd;
				value.GetBounds(out lStart, out lEnd);
				start = lStart;
				end = lEnd;
			}
		}

		public ITerminalNode LocationTerminalNode
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
			get { return IsAttr ? type : GetAttrText(TYPE); }
			set
			{
				if (IsAttr)
					type = value;
				else
					SetAttr(TYPE, value);
			}
		}
		public string Text { get; set; }
		public int Depth { get; set; }

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

		public List<ParserNode> GetAllNodes()
		{
			var result = new List<ParserNode> { this };
			for (var ctr = 0; ctr < result.Count; ++ctr)
			{
				foreach (var child in result[ctr].children)
					result.Add(child);
			}
			return result;
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

		public void SetAttr(string type, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			SetAttrNode(type, null, start, end);
		}

		public void SetAttr(string type, string input, ParserRuleContext ctx)
		{
			var text = ctx.GetText(input);
			int start, end;
			ctx.GetBounds(out start, out end);
			SetAttrNode(type, text, start, end);
		}

		void AddAttrNode(string type, string value, int? start, int? end)
		{
			new ParserNode(true) { Type = type, Parent = this, Text = value, start = start, end = end };
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

		public void AddAttr(string type, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			AddAttrNode(type, null, start, end);
		}

		public void AddAttr(string type, ITerminalNode token)
		{
			int start, end;
			token.GetBounds(out start, out end);
			AddAttrNode(type, null, start, end);
		}

		public void AddAttr(string type, IToken token)
		{
			int start, end;
			token.GetBounds(out start, out end);
			AddAttrNode(type, null, start, end);
		}

		public void AddAttr(string type, string input, ParserRuleContext ctx)
		{
			var text = ctx.GetText(input);
			int start, end;
			ctx.GetBounds(out start, out end);
			AddAttrNode(type, text, start, end);
		}

		public void AddAttr(string type, string input, ITerminalNode token)
		{
			AddAttr(type, input, token.Symbol);
		}

		public void AddAttr(string type, string input, IToken token)
		{
			var text = token.Text;
			int start, end;
			token.GetBounds(out start, out end);
			AddAttrNode(type, text, start, end);
		}

		public bool HasAttr(string type)
		{
			return GetAttrs(type).Any();
		}

		public bool HasAttr(string type, Regex regex, bool invert)
		{
			var attrs = GetAttrs(type);
			if (!attrs.Any())
				return invert;
			return attrs.Any(attr => regex.IsMatch(attr.Text) != invert);
		}

		List<string> Print()
		{
			var parents = new List<ParserNode>();
			for (var parent = this; parent != null; parent = parent.Parent)
				parents.Insert(0, parent);
			var parentTypes = String.Join("->", parents.Select(node => node.Type));
			var attrs = String.Join(", ", children.Where(child => child.IsAttr).Select(child => (String.Format("{0}-{1} {2} \"{3}\"", child.start, child.end, child.type, (child.Text ?? "").Replace("\r", "").Replace("\n", "").Replace("\"", "\"\"")))));
			var result = new List<string> { String.Format("[{0}-{1}: {2} Path: \"{3}\"", start, end, attrs, parentTypes) };
			result.AddRange(children.Where(child => !child.IsAttr).SelectMany(child => child.Print()).Select(str => String.Format(" {0}", str)));
			if (result.Count == 1)
				result[0] += "]";
			else
				result.Add("]");
			return result;
		}

		public override string ToString()
		{
			return String.Join("", Print().Select(str => String.Format("{0}{1}", str, Environment.NewLine)));
		}

		public void Save(string outputFile)
		{
			File.WriteAllText(outputFile, ToString());
		}
	}
}
