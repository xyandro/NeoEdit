using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace NeoEdit.Parsing
{
	public class ParserNode : IEnumerable
	{
		public const string TypeStr = "Type";
		public const string LocationStr = "Location";

		public class ParserNodeAttribute
		{
			public string Text { get; internal set; }
			public int? Start { get; internal set; }
			public int? End { get; internal set; }
		}

		public ParserNode(string type, ParserNode parent = null)
		{
			Type = type;
			Parent = parent;
		}

		public IEnumerator GetEnumerator()
		{
			return null;
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
					parent = value;
					Depth = parent.Depth + 1;
					parent.children.Add(this);
				}
			}
		}

		public int Start
		{
			get { return GetAttr(LocationStr).Start.Value; }
			set { Set(LocationStr, null, value, End); }
		}
		public int End
		{
			get { return GetAttr(LocationStr).End.Value; }
			set { Set(LocationStr, null, Start, value); }
		}
		public string Type { get { return GetAttrText(TypeStr); } internal set { Set(TypeStr, value); } }
		public int Depth { get; set; }

		readonly List<ParserNode> children = new List<ParserNode>();
		readonly Dictionary<string, List<ParserNodeAttribute>> attributes = new Dictionary<string, List<ParserNodeAttribute>>();

		[Flags]
		public enum ParserNodeListType
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

		public IEnumerable<ParserNode> List(ParserNodeListType list)
		{
			if (list.HasFlag(ParserNodeListType.Self))
				yield return this;
			if (list.HasFlag(ParserNodeListType.Parents))
			{
				for (var parent = this.Parent; parent != null; parent = parent.Parent)
					yield return parent;
			}
			if ((list.HasFlag(ParserNodeListType.Children)) || (list.HasFlag(ParserNodeListType.Descendants)))
			{
				foreach (var child in children)
				{
					yield return child;
					if (list.HasFlag(ParserNodeListType.Descendants))
						foreach (var childChild in child.List(ParserNodeListType.Descendants))
							yield return childChild;
				}
			}
		}

		public List<string> GetAttrNames()
		{
			return attributes.Keys.ToList();
		}

		public ParserNodeAttribute GetAttr(string name)
		{
			if (!attributes.ContainsKey(name))
				return null;
			return attributes[name].FirstOrDefault();
		}

		public IEnumerable<ParserNodeAttribute> GetAttrs(string name, bool firstOnly = false)
		{
			if (!attributes.ContainsKey(name))
				yield break;
			foreach (var attr in attributes[name])
			{
				yield return attr;
				if (firstOnly)
					break;
			}
		}

		public string GetAttrText(string name)
		{
			var attr = GetAttr(name);
			return attr == null ? null : attr.Text;
		}

		public IEnumerable<string> GetAttrsText(string name, bool firstOnly = false)
		{
			return GetAttrs(name, firstOnly).Select(attr => attr.Text);
		}

		public void SetAttr(string name, string value, int? start, int? end)
		{
			attributes[name] = new List<ParserNodeAttribute> { new ParserNodeAttribute { Text = value, Start = start, End = end } };
		}

		public void Set(string name, string value)
		{
			SetAttr(name, value, null, null);
		}

		public void Set(string name, string value, int start, int end)
		{
			SetAttr(name, value, start, end);
		}

		internal void Set(string name, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			SetAttr(name, null, start, end);
		}

		internal void Set(string name, string input, ParserRuleContext ctx)
		{
			var text = ctx.GetText(input);
			int start, end;
			ctx.GetBounds(out start, out end);
			SetAttr(name, text, start, end);
		}

		void AddAttr(string name, string value, int? start, int? end)
		{
			if (!attributes.ContainsKey(name))
				attributes[name] = new List<ParserNodeAttribute>();
			attributes[name].Add(new ParserNodeAttribute { Text = value, Start = start, End = end });
		}

		public void Add(string name, string value)
		{
			AddAttr(name, value, null, null);
		}

		public void Add(string name, string value, int start, int end)
		{
			AddAttr(name, value, start, end);
		}

		internal void Add(string name, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			AddAttr(name, null, start, end);
		}

		internal void Add(string name, string input, ParserRuleContext ctx)
		{
			var text = ctx.GetText(input);
			int start, end;
			ctx.GetBounds(out start, out end);
			AddAttr(name, text, start, end);
		}

		public bool HasAttr(string name, Regex regex)
		{
			if (!attributes.ContainsKey(name))
				return false;
			return attributes[name].Any(attr => regex.IsMatch(attr.Text));
		}

		List<string> rPrint()
		{
			var result = new List<string>();

			var attrs = new List<string> { };
			attrs.AddRange(attributes.Select(attr => String.Format("{0}: {1}", attr.Key, String.Join(";", attr.Value.Select(attrValue => String.Format("{0}-{1} ({2})", attrValue.Start, attrValue.End, (attrValue.Text ?? "").Replace("\r", "").Replace("\n", "")))))));
			result.Add(String.Join(", ", attrs));

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
