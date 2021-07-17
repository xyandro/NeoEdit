using System.Collections.Generic;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;
using NeoEdit.Common.Expressions.Parser;

namespace NeoEdit.Common.Expressions
{
	class NEExpressionVariableFinder : ExpressionParserBaseVisitor<object>
	{
		readonly List<NEVariableUse> variables = new List<NEVariableUse>();

		private NEExpressionVariableFinder() { }

		public override object VisitVariable([NotNull] ExpressionParser.VariableContext context)
		{
			variables.Add(new NEVariableUse(context.val.Text, context.repeat?.GetText() ?? ""));
			return base.VisitVariable(context);
		}

		public static List<NEVariableUse> GetVariables(IParseTree tree)
		{
			var finder = new NEExpressionVariableFinder();
			finder.Visit(tree);
			return finder.variables;
		}
	}
}
