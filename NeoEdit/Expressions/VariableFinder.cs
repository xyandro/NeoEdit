using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using NeoEdit.Program.Expressions.Parser;

namespace NeoEdit.Program.Expressions
{
	class VariableFinder : ExpressionParserBaseVisitor<object>
	{
		readonly HashSet<string> variables = new HashSet<string>();

		private VariableFinder() { }

		public override object VisitVariable(ExpressionParser.VariableContext context)
		{
			variables.Add(context.val.Text);
			return base.VisitVariable(context);
		}

		public static HashSet<string> GetVariables(IParseTree tree)
		{
			var finder = new VariableFinder();
			finder.Visit(tree);
			return finder.variables;
		}
	}
}
