using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using NeoEdit.Common;

namespace NeoEdit.Common.Parsing
{
	public class ParserNode : ParserBase
	{

		public static List<string> GetAvailableAttrs(List<ParserNode> nodes, bool withLocation = false) => nodes.SelectMany(node => node.GetAttrTypes(withLocation)).Distinct().OrderBy().ToList();
		public static List<string> GetAvailableValues(List<ParserNode> nodes, string type) => nodes.SelectMany(node => node.GetAttrsText(type)).Distinct().OrderBy().ToList();

		public enum ParserNavigationDirectionEnum
		{
			Up,
			Down,
			Left,
			Right,
			Row,
			Column,
			Home,
			End,
			PgUp,
			PgDn,
		}

		public enum ParserNavigationTypeEnum
		{
			Regular,
			FirstChild,
			Cell,
		}

		public const string TYPE = "Type";

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
					parent = value;
					Depth = parent.Depth + 1;
					parent.children.Add(this);
				}
			}
		}

		public string Type
		{
			get { return GetAttrText(TYPE); }
			set { SetAttr(TYPE, value); }
		}
		public string Text { get; set; }
		public int Depth { get; set; }

		public ParserNavigationTypeEnum ParserNavigationType { get; set; } = ParserNavigationTypeEnum.Regular;

		readonly List<ParserNode> children = new List<ParserNode>();
		internal readonly List<ParserAttribute> attributes = new List<ParserAttribute>();

		public List<ParserNode> GetAllNodes()
		{
			var result = new List<ParserNode> { this };
			for (var ctr = 0; ctr < result.Count; ++ctr)
				result.AddRange(result[ctr].children);
			return result;
		}

		public IEnumerable<ParserNode> Parents()
		{
			for (var node = parent; node != null; node = node.parent)
				yield return node;
		}

		public IEnumerable<ParserNode> Children() => children;

		public IEnumerable<ParserNode> SelfAndChildren()
		{
			yield return this;
			foreach (var child in children)
				yield return child;
		}

		public IEnumerable<ParserNode> Descendants()
		{
			foreach (var child in children)
			{
				yield return child;
				foreach (var descendant in child.Descendants())
					yield return descendant;
			}
		}

		public IEnumerable<ParserNode> SelfAndDescendants()
		{
			yield return this;
			foreach (var descendant in Descendants())
				yield return descendant;
		}

		IEnumerable<ParserNode> NavigateRegular(ParserNavigationDirectionEnum direction, bool shiftDown, bool keepSelections)
		{
			switch (direction)
			{
				case ParserNavigationDirectionEnum.Up:
				case ParserNavigationDirectionEnum.Down:
					{
						if (shiftDown)
							yield return this;

						var index = parent?.children.IndexOf(this) ?? -1;
						if (index == -1)
						{
							if (keepSelections)
								yield return this;
							break;
						}
						index += direction == ParserNavigationDirectionEnum.Up ? -1 : 1;
						if (keepSelections)
							index = Math.Max(0, Math.Min(index, parent.children.Count - 1));
						if ((index >= 0) && (index < parent.children.Count))
							yield return parent.children[index];
					}
					break;
				case ParserNavigationDirectionEnum.Left:
					if ((parent != null) || (keepSelections))
						yield return parent ?? this;
					break;
				case ParserNavigationDirectionEnum.Right:
					if ((keepSelections) && (!children.Any()))
						yield return this;
					foreach (var child in children)
					{
						yield return child;
						if (!shiftDown)
							break;
					}
					break;
				case ParserNavigationDirectionEnum.Row:
				case ParserNavigationDirectionEnum.Column:
					if (parent == null)
					{
						if (keepSelections)
							yield return this;
						break;
					}
					foreach (var child in parent.children)
						yield return child;
					break;
				case ParserNavigationDirectionEnum.Home:
				case ParserNavigationDirectionEnum.PgUp:
					{
						if (parent == null)
						{
							if (keepSelections)
								yield return this;
							break;
						}
						var index = parent.children.IndexOf(this);
						if (!shiftDown)
							yield return parent.children.First();
						else
							foreach (var child in parent.children.Take(index + 1))
								yield return child;
					}
					break;
				case ParserNavigationDirectionEnum.End:
				case ParserNavigationDirectionEnum.PgDn:
					{
						if (parent == null)
						{
							if (keepSelections)
								yield return this;
							break;
						}
						var index = parent.children.IndexOf(this);
						if (!shiftDown)
							yield return parent.children.Last();
						else
							foreach (var child in parent.children.Skip(index))
								yield return child;
					}
					break;
				default:
					if (keepSelections)
						yield return this;
					break;
			}
		}

		IEnumerable<ParserNode> NavigateFirstChild()
		{
			var item = this;
			while ((item.ParserNavigationType == ParserNavigationTypeEnum.FirstChild) && (item.children.Count != 0))
				item = item.children[0];
			yield return item;
		}

		IEnumerable<ParserNode> NavigateCell(ParserNavigationDirectionEnum direction, bool shiftDown, bool keepSelections)
		{
			var startColumn = parent.children.IndexOf(this);
			var startRow = parent.parent.children.IndexOf(parent);
			var endColumn = startColumn;
			var endRow = startRow;

			switch (direction)
			{
				case ParserNavigationDirectionEnum.Up: endRow -= 1; break;
				case ParserNavigationDirectionEnum.Down: endRow += 1; break;
				case ParserNavigationDirectionEnum.Left: endColumn -= 1; break;
				case ParserNavigationDirectionEnum.Right: endColumn += 1; break;
				case ParserNavigationDirectionEnum.Home: endColumn = 0; break;
				case ParserNavigationDirectionEnum.End: endColumn = int.MaxValue; break;
				case ParserNavigationDirectionEnum.PgUp: endRow = 0; break;
				case ParserNavigationDirectionEnum.PgDn: endRow = int.MaxValue; break;
				case ParserNavigationDirectionEnum.Row: startColumn = 0; endColumn = int.MaxValue; break;
				case ParserNavigationDirectionEnum.Column: startRow = 0; endRow = int.MaxValue; break;
			}

			if (!shiftDown)
			{
				if (keepSelections)
				{
					endRow = Math.Max(0, Math.Min(endRow, parent.parent.children.Count - 1));
					endColumn = Math.Max(0, Math.Min(endColumn, parent.parent.children[endRow].children.Count - 1));
				}
				if ((endRow >= 0) && (endRow < parent.parent.children.Count))
					if ((endColumn >= 0) && (endColumn < parent.parent.children[endRow].children.Count))
						yield return parent.parent.children[endRow].children[endColumn];
				yield break;
			}

			var minRow = Math.Max(0, Math.Min(startRow, endRow));
			var minColumn = Math.Max(0, Math.Min(startColumn, endColumn));
			var maxRow = Math.Min(parent.parent.children.Count - 1, Math.Max(startRow, endRow));
			var maxColumn = Math.Max(startColumn, endColumn); // Can't force any upper bound; different rows could have different numbers of columns

			for (var row = minRow; row <= maxRow; ++row)
				for (var column = minColumn; column <= Math.Min(parent.parent.children[row].children.Count - 1, maxColumn); ++column)
					yield return parent.parent.children[row].children[column];
		}

		public IEnumerable<ParserNode> Navigate(ParserNavigationDirectionEnum direction, bool shiftDown, bool keepSelections)
		{
			switch (ParserNavigationType)
			{
				case ParserNavigationTypeEnum.Regular: return NavigateRegular(direction, shiftDown, keepSelections);
				case ParserNavigationTypeEnum.FirstChild: return NavigateFirstChild();
				case ParserNavigationTypeEnum.Cell: return NavigateCell(direction, shiftDown, keepSelections);
				default: throw new ArgumentException($"Invalid {nameof(ParserNavigationType)}");
			}
		}

		public IEnumerable<ParserAttribute> Attributes(bool withLocation = false)
		{
			var attrs = attributes as IEnumerable<ParserAttribute>;
			if (withLocation)
				attrs = attrs.Where(attr => attr.HasLocation);
			return attrs;
		}

		public IEnumerable<string> GetAttrTypes(bool withLocation = false) => Attributes(withLocation).Select(attr => attr.Type);

		public ParserAttribute GetAttr(string type) => GetAttrs(type, true).FirstOrDefault();

		public IEnumerable<ParserAttribute> GetAttrs(string type, bool firstOnly = false)
		{
			var result = Attributes().Where(attr => attr.Type == type);
			if (firstOnly)
				result = result.Take(1);
			return result;
		}

		public string GetAttrText(string type) => GetAttr(type)?.Text;

		public IEnumerable<string> GetAttrsText(string type, bool firstOnly = false) => GetAttrs(type, firstOnly).Select(attr => attr.Text);

		void RemoveAttr(string type) => attributes.RemoveAll(attr => attr.Type == type);

		void SetAttrNode(string type, string value, int? start, int? end)
		{
			RemoveAttr(type);
			AddAttrNode(type, value, start, end);
		}

		public void SetAttr(string type, string value) => SetAttrNode(type, value, null, null);
		public void SetAttr(string type, int start, int end) => SetAttrNode(type, null, start, end);
		public void SetAttr(string type, string value, int start, int end) => SetAttrNode(type, value, start, end);

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

		void AddAttrNode(string type, string value, int? start, int? end) => new ParserAttribute { Type = type, Parent = this, Text = value, start = start, end = end };
		public void AddAttr(string type, string value) => AddAttrNode(type, value, null, null);
		public void AddAttr(string type, int start, int end) => AddAttrNode(type, null, start, end);
		public void AddAttr(string type, string value, int start, int end) => AddAttrNode(type, value, start, end);

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

		public void AddAttr(string type, string input, ITerminalNode token) => AddAttr(type, input, token.Symbol);

		public void AddAttr(string type, string input, IToken token)
		{
			var text = token.Text;
			int start, end;
			token.GetBounds(out start, out end);
			AddAttrNode(type, text, start, end);
		}

		public bool HasAttr(string type) => GetAttrs(type).Any();

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
			var parentTypes = string.Join("->", parents.Select(node => node.Type));
			var attrs = string.Join(", ", attributes.Select(child => ($"{child.start}-{child.end} {child.Type} \"{(child.Text ?? "").Replace("\r", "").Replace("\n", "").Replace("\"", "\"\"")}\"")));
			var result = new List<string> { $"[{start}-{end}: {attrs} Path: \"{parentTypes}\"" };
			result.AddRange(children.SelectMany(child => child.Print()).Select(str => $" {str}"));
			if (result.Count == 1)
				result[0] += "]";
			else
				result.Add("]");
			return result;
		}

		public override string ToString() => string.Join("", Print().Select(str => $"{str}{Environment.NewLine}"));

		public void Save(string outputFile) => File.WriteAllText(outputFile, ToString());
	}
}
