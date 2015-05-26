using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace NeoEdit.Parsing
{
	public class ParserNode
	{
		public class ParserNodeAttribute
		{
			public string Data { get; internal set; }
			public int Start { get; internal set; }
			public int End { get; internal set; }
		}

		ParserNode parent;
		public ParserNode Parent
		{
			get { return parent; }
			internal set
			{
				if (value == null)
					return;

				parent = value;
				Depth = value.Depth + 1;
				parent.children.Add(this);
			}
		}

		public ParserNodeAttribute Location { get { return GetAttribute("Location"); } }
		public int LocationStart
		{
			get
			{
				var location = GetAttribute("Location");
				if (location == null)
					return -1;
				return location.Start;
			}
			set
			{
				SetAttribute("Location", null, value, LocationEnd);
			}
		}
		public int LocationEnd
		{
			get
			{
				var location = GetAttribute("Location");
				if (location == null)
					return -1;
				return location.End;
			}
			set
			{
				SetAttribute("Location", null, LocationStart, value);
			}
		}
		internal ParserRuleContext LocationContext { set { AddAttribute("Location", null, value); } }
		public string Type { get { return GetAttributeText("Type"); } internal set { AddAttribute("Type", value, -1, -1); } }

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

		public List<string> GetAttributeNames()
		{
			return attributes.Keys.ToList();
		}

		public string GetAttributeText(string name)
		{
			if ((!attributes.ContainsKey(name)) || (attributes[name].Count == 0))
				return null;
			return attributes[name][0].Data;
		}

		public IEnumerable<string> GetAttributesText(string name, bool firstOnly = false)
		{
			if (!attributes.ContainsKey(name))
				yield break;
			foreach (var attr in attributes[name])
			{
				yield return attr.Data;
				if (firstOnly)
					break;
			}
		}

		public ParserNodeAttribute GetAttribute(string name)
		{
			if (!attributes.ContainsKey(name))
				return null;
			return attributes[name].FirstOrDefault();
		}

		public IEnumerable<ParserNodeAttribute> GetAttributes(string name, bool firstOnly = false)
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

		public void SetAttribute(string name, string value, int start, int end)
		{
			attributes[name] = new List<ParserNodeAttribute> { new ParserNodeAttribute { Data = value, Start = start, End = end } };
		}

		public void AddAttribute(string name, string value, int start, int end)
		{
			if (!attributes.ContainsKey(name))
				attributes[name] = new List<ParserNodeAttribute>();
			attributes[name].Add(new ParserNodeAttribute { Data = value, Start = start, End = end });
		}

		internal void AddAttribute(string name, string input, ParserRuleContext ctx)
		{
			int start, end;
			ctx.GetBounds(out start, out end);
			var data = ctx.GetText(input);
			AddAttribute(name, data, start, end);
		}

		public bool HasAttribute(string name, Regex regex)
		{
			if (!attributes.ContainsKey(name))
				return false;
			return attributes[name].Any(attr => regex.IsMatch(attr.Data));
		}

		List<string> rPrint()
		{
			var result = new List<string>();

			var attrs = new List<string> { Type.ToString() };
			attrs.AddRange(attributes.Select(attr => String.Format("{0}: {1}", attr.Key, String.Join(";", attr.Value.Select(attrValue => String.Format("{0}-{1} ({2})", attrValue.Start, attrValue.End, (attrValue.Data ?? "").Replace("\r", "").Replace("\n", "")))))));
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
