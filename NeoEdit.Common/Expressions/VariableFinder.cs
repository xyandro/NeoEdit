﻿using System.Collections.Generic;
using Antlr4.Runtime.Tree;
using NeoEdit.Common.Expressions.Parser;

namespace NeoEdit.Common.Expressions
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