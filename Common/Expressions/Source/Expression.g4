grammar Expression ;

expr : DEBUG? form EOF ;

form
	: e # LongForm
	| op=(EXPOP | MULTOP | ADDOP | SHIFTOP | RELATIONALOP | EQUALITYOP | BITWISEAND | BITWISEXOR | BITWISEOR | LOGICALAND | LOGICALOR | NULLCOALESCE) # ShortForm
	| # DefaultOpForm
	;

e
	:	(
			method=METHOD1 LPAREN e RPAREN | 
			method=METHOD1VAR LPAREN e (COMMA e)* RPAREN | 
			method=METHOD2 LPAREN e COMMA e RPAREN | 
			method=METHODVAR LPAREN (e (COMMA e)*)? RPAREN
		) # Method
	| LPAREN val=e RPAREN # Parens
	| val1=e unitsVal=units # AddUnits
	| val1=e op=DOT val2=e # Dot
	| op=(BITWISENOT | ADDOP | BANG) val=e # Unary
	| val=value op=BANG # UnaryEnd
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
	| val1=e op=CONVERSION val2=units # UnitConversion
	| condition=e CONDITIONAL trueval=e ELSE falseval=e # Ternary
	| constant=CONSTANT # Constant
	| value # Simple
	;

value
	: val=PARAM # Param
	| val=STRING # String
	| val=(DATE | DATETIME) # Date
	| val=TIME # Time
	| val=CHAR # Char
	| TRUE # True
	| FALSE # False
	| NULL # Null
	| val=INTEGER # Integer
	| val=FLOAT # Float
	| val=HEX # Hex
	| val=VARIABLE # Variable
	;

units
	: base1=units op=EXPOP power=INTEGER # UnitExp
	| val1=units op=MULTOP val2=units # UnitMult
	| LPAREN units RPAREN # UnitParen
	| unit # UnitSimple
	;

unit : val=(CONSTANT | FALSE | METHOD1 | METHOD1VAR | METHOD2 | METHODVAR | NULL | TRUE | VARIABLE) ;

fragment A: [Aa] ;
fragment B: [Bb] ;
fragment C: [Cc] ;
fragment D: [Dd] ;
fragment E: [Ee] ;
fragment F: [Ff] ;
fragment G: [Gg] ;
fragment H: [Hh] ;
fragment I: [Ii] ;
fragment J: [Jj] ;
fragment K: [Kk] ;
fragment L: [Ll] ;
fragment M: [Mm] ;
fragment N: [Nn] ;
fragment O: [Oo] ;
fragment P: [Pp] ;
fragment Q: [Qq] ;
fragment R: [Rr] ;
fragment S: [Ss] ;
fragment T: [Tt] ;
fragment U: [Uu] ;
fragment V: [Vv] ;
fragment W: [Ww] ;
fragment X: [Xx] ;
fragment Y: [Yy] ;
fragment Z: [Zz] ;

DEBUG: '@' ;
LPAREN: '(' ;
RPAREN: ')' ;
COMMA: ',' ;
METHOD1: A B S | A C O S | A S I N | A T A N | C O N J U G A T E | C O S | C O S H | E V A L | F I L E N A M E | F R O M W O R D S | I M A G I N A R Y | L N | L O G | M A G N I T U D E | P H A S E | R E A L | R E C I P R O C A L | S I N | S I N H | S Q R T | T A N | T A N H | T O W O R D S | T Y P E | V A L I D R E ;
METHOD1VAR: S T R F O R M A T;
METHOD2: F R O M P O L A R | L O G | R O O T ;
METHODVAR: M A X | M I N ;
BANG: '!' ;
EXPOP: '^' ;
MULTOP: [*/%] | '//' ;
ADDOP: [-+] ;
SHIFTOP: '<<' | '>>' ;
RELATIONALOP: '<' | '>' | '<=' | '>=' | 'i<' | 'i>' | 'i<=' | 'i>=' | I S ;
EQUALITYOP: '=' | '==' | '!=' | '<>' | 'i==' | 'i!=' | 'i<>' ;
CONVERSION: '=>' ;
BITWISENOT: '~' ;
BITWISEAND: '&' ;
BITWISEXOR: '^^' ;
BITWISEOR: '|' ;
LOGICALAND: '&&' ;
LOGICALOR: '||' ;
NULLCOALESCE: '??' ;
CONDITIONAL: '?' ;
ELSE: ':' ;
DOT: '.' ;
CONSTANT: P I | E | I | N O W | U T C N O W | T O D A Y | U T C T O D A Y ;
PARAM: '[' [0-9]+ ']' ;
STRING: '"' ~'"'* '"' ;
CHAR: '\'' . '\'' ;
DATE: '\'' ([0-9][0-9][0-9][0-9] [-/])? [01]?[0-9] [-/] [0-3]?[0-9] (Z | [-+] [0-2]?[0-9] (':' [0-5]?[0-9])? )? '\'' ;
TIME: '\'' '-'? ([0-9]+ ':')? [0-2]?[0-9] ':' [0-5][0-9] (':' [0-5][0-9] ('.' [0-9]+)?)? ([ \t]* (A M | P M))? '\'' ;
DATETIME: '\'' [0-9][0-9][0-9][0-9] [-/] [01]?[0-9] [-/] [0-3]?[0-9] [ \tT]+ [0-2]?[0-9] ':' [0-5]?[0-9] (':' [0-5]?[0-9] ('.' [0-9]+)?)? (Z | [-+] [0-2]?[0-9] (':' [0-5]?[0-9])? )? ([ \t]* (A M | P M))? '\'' ;
TRUE: T R U E ;
FALSE: F A L S E ;
NULL: N U L L ;
INTEGER: [0-9]+ ;
FLOAT: [0-9]* '.'? [0-9]+ ([eE][-+]?[0-9]+)? ;
HEX: '0x' [0-9a-fA-F]+ ;
VARIABLE: [a-zA-Z][a-zA-Z0-9_]* ;
WHITESPACE: [ \n\t\r]+ -> skip ;
