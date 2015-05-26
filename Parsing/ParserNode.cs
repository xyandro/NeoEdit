using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;

namespace NeoEdit
{
	namespace Parsing
	{
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

		public class ParserNode<T>
		{
			ParserNode<T> parent;
			public ParserNode<T> Parent
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
			public T NodeType { get; internal set; }

			public Tuple<int, int> Location { get { return GetAttributeLocation("Location"); } }
			internal ParserRuleContext LocationContext { set { AddAttribute("Location", null, value); } }

			public int Depth { get; set; }

			readonly List<ParserNode<T>> children = new List<ParserNode<T>>();
			readonly Dictionary<string, List<Tuple<string, int, int>>> attributes = new Dictionary<string, List<Tuple<string, int, int>>>();

			public IEnumerable<ParserNode<T>> List(ParserNodeListType list)
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
				return attributes[name][0].Item1;
			}

			public Tuple<int, int> GetAttributeLocation(string name)
			{
				if ((!attributes.ContainsKey(name)) || (attributes[name].Count == 0))
					return null;
				return Tuple.Create(attributes[name][0].Item2, attributes[name][0].Item3);
			}

			public IEnumerable<string> GetAttributesText(string name, bool firstOnly = false)
			{
				if (!attributes.ContainsKey(name))
					yield break;
				foreach (var attr in attributes[name])
				{
					yield return attr.Item1;
					if (firstOnly)
						break;
				}
			}

			public IEnumerable<Tuple<int, int>> GetAttributesLocation(string name, bool firstOnly = false)
			{
				if (!attributes.ContainsKey(name))
					yield break;
				foreach (var attr in attributes[name])
				{
					yield return Tuple.Create(attr.Item2, attr.Item3);
					if (firstOnly)
						break;
				}
			}

			public Tuple<string, int, int> GetAttribute(string name)
			{
				if (!attributes.ContainsKey(name))
					return null;
				return attributes[name].FirstOrDefault();
			}

			public IEnumerable<Tuple<string, int, int>> GetAttributes(string name, bool firstOnly = false)
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

			public void AddAttribute(string name, string value, int start, int end)
			{
				if (!attributes.ContainsKey(name))
					attributes[name] = new List<Tuple<string, int, int>>();
				attributes[name].Add(Tuple.Create(value, start, end));
			}

			internal void AddAttribute(string name, string input, ParserRuleContext ctx)
			{
				var start = ctx.Start.StartIndex;
				var end = Math.Max(start, ctx.Stop == null ? 0 : ctx.Stop.StopIndex + 1);
				var data = input == null ? null : input.Substring(start, end - start);
				AddAttribute(name, data, start, end);
			}

			public bool HasAttribute(string name, Regex regex)
			{
				if (!attributes.ContainsKey(name))
					return false;
				return attributes[name].Any(attr => regex.IsMatch(attr.Item1));
			}

			List<string> rPrint()
			{
				var result = new List<string>();

				var attrs = new List<string> { NodeType.ToString() };
				attrs.AddRange(attributes.Select(attr => String.Format("{0}: {1}", attr.Key, String.Join(";", attr.Value.Select(tuple => String.Format("{0}-{1} ({2})", tuple.Item2, tuple.Item3, (tuple.Item1 ?? "").Replace("\r", "").Replace("\n", "")))))));
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
}
