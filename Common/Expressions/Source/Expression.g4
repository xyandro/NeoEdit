grammar Expression;

expr : DEBUG? form EOF;

form
	: e # LongForm
	| op=(EXPOP | MULTOP | ADDOP | SHIFTOP | RELATIONALOP | EQUALITYOP | BITWISEAND | BITWISEXOR | BITWISEOR | LOGICALAND | LOGICALOR | NULLCOALESCE) # ShortForm
	| # DefaultOpForm
	;

e
	: method=METHOD LPAREN (e (COMMA e)*)? RPAREN # Method
	| val1=e op=DOT val2=e # Dot
	| op=(BITWISENOT | ADDOP | NOTOP) val=value # Unary
	| LPAREN type=CASTTYPE RPAREN val=value # Cast
	| val1=e op=EXPOP val2=e # Exp
	| val1=e op=MULTOP val2=e # Mult
	| val1=e op=ADDOP val2=e # Add
	| val1=e op=SHIFTOP val2=e # Shift
	| val1=e op=RELATIONALOP val2=e # Relational
	| val1=e op=EQUALITYOP val2=e # Equality
	| val1=e op=BITWISEAND val2=e # BitwiseAnd
	| val1=e op=BITWISEXOR val2=e # BitwiseXor
	| val1=e op=BITWISEOR val2=e # BitwiseOr
	| val1=e op=LOGICALAND val2=e # LogicalAnd
	| val1=e op=LOGICALOR val2=e # LogicalOr
	| val1=e op=NULLCOALESCE val2=e # NullCoalesce
	| condition=e CONDITIONAL trueval=e ELSE falseval=e # Ternary
	| value # Simple
    ;

value
	: LPAREN val=e RPAREN # Expression
	| val=PARAM # Param
	| val=STRING # String
	| val=CHAR # Char
	| TRUE # True
	| FALSE # False
	| NULL # Null
	| val=FLOAT # Float
	| val=HEX # Hex
	| val=VARIABLE # Variable
	;

DEBUG: '@';
LPAREN: '(';
RPAREN: ')';
COMMA: ',';
METHOD: 'Type' | 'ValidRE' | 'Eval' | 'FileName' | 'StrFormat';
NOTOP: '!';
CASTTYPE: 'bool' | 'char' | 'sbyte' | 'byte' | 'short' | 'ushort' | 'int' | 'uint' | 'long' | 'ulong' | 'float' | 'double' | 'string';
EXPOP: '^';
MULTOP: [*/%];
ADDOP: [-+];
SHIFTOP: '<<' | '>>';
RELATIONALOP: '<' | '>' | '<=' | '>=' | 'i<' | 'i>' | 'i<=' | 'i>=' | 'is';
EQUALITYOP: '==' | '!=' | 'i==' | 'i!=';
BITWISENOT: '~';
BITWISEAND: '&';
BITWISEXOR: '^^';
BITWISEOR: '|';
LOGICALAND: '&&';
LOGICALOR: '||';
NULLCOALESCE: '??';
CONDITIONAL: '?';
ELSE: ':';
DOT: '.';
PARAM: '[' [0-9]+ ']';
STRING: '"' ~'"'* '"';
CHAR: '\'' . '\'';
TRUE: [Tt]'rue' | 'TRUE';
FALSE: [Ff]'alse' | 'FALSE';
NULL: [Nn][Uu][Ll][Ll];
FLOAT: [0-9]* '.'? [0-9]+ ([eE][-+]?[0-9]+)?;
HEX: '0x' [0-9a-fA-F]+;
VARIABLE: [a-zA-Z][a-zA-Z0-9_]*;
WHITESPACE: [ \n\t\r]+ -> skip;
