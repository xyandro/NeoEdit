grammar Expression;

expr : DEBUG? form EOF;

form
	: e # LongForm
	| op=(MULTOP | ADDOP | SHIFTOP | RELATIONALOP | EQUALITYOP | LOGICALAND | LOGICALXOR | LOGICALOR | CONDITIONALAND | CONDITIONALOR) # ShortForm
	| # DefaultOpForm
	;

e
	: method=METHOD LPAREN (e (COMMA e)*)? RPAREN # Method
	| val1=e op=DOT val2=e # Dot
	| op=(ADDOP | NOTOP) val=value # Unary
	| LPAREN type=CASTTYPE RPAREN val=value # Cast
	| val1=e op=MULTOP val2=e # Mult
	| val1=e op=ADDOP val2=e # Add
	| val1=e op=SHIFTOP val2=e # Shift
	| val1=e op=RELATIONALOP val2=e # Relational
	| val1=e op=EQUALITYOP val2=e # Equality
	| val1=e op=LOGICALAND val2=e # LogicalAnd
	| val1=e op=LOGICALXOR val2=e # LogicalXor
	| val1=e op=LOGICALOR val2=e # LogicalOr
	| val1=e op=CONDITIONALAND val2=e # ConditionalAnd
	| val1=e op=CONDITIONALOR val2=e # ConditionalOr
	| condition=e CONDITIONAL trueval=e ELSE falseval=e # Ternary
	| value # Simple
    ;

value
	: LPAREN val=e RPAREN # Expression
	| val=PARAM # Param
	| val=STRING # String
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
CASTTYPE: 'bool' | 'long' | 'double' | 'string';
MULTOP: [*/%];
ADDOP: '+' | '-';
SHIFTOP: '<<' | '>>';
RELATIONALOP: '<' | '>' | '<=' | '>=' | 'i<' | 'i>' | 'i<=' | 'i>=' | 'is';
EQUALITYOP: '==' | '!=' | 'i==' | 'i!=';
LOGICALAND: '&';
LOGICALXOR: '^';
LOGICALOR: '|';
CONDITIONALAND: '&&';
CONDITIONALOR: '||';
CONDITIONAL: '?';
ELSE: ':';
DOT: '.';
PARAM: '[' [0-9]+ ']';
STRING: '\'' .*? '\'';
TRUE: [Tt][Rr][Uu][Ee];
FALSE: [Ff][Aa][Ll][Ss][Ee];
NULL: [Nn][Uu][Ll][Ll];
FLOAT: [0-9]* '.'? [0-9]+ ([eE][-+]?[0-9]+)?;
HEX: '0x' [0-9a-fA-F]+;
VARIABLE: [a-zA-z][a-zA-Z0-9_]*;
WHITESPACE: [ \n\t\r]+ -> skip;
