parser grammar ExpressionParser ;

options { tokenVocab = ExpressionLexer; }

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
	| (normalstring | verbatimstring | interpolatedstring) # String
	| val=(DATE | DATETIME) # Date
	| val=TIME # Time
	| charval # Char
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

unit : val=(CONSTANT | FALSE | METHOD1 | METHOD1VAR | METHOD2 | METHODVAR | NULL | TRUE | VARIABLE | CURRENCY) ;

charval : CHARSTART val=charoptions CHAREND ;
charoptions : charany | charescape | charunicode ;
charany : val=CHARANY ;
charescape : val=CHARESCAPE ;
charunicode : val=CHARUNICODE ;

normalstring : STRSTART val=strcontent STREND ;
strcontent : (strchars | charescape | charunicode)* ;
strchars : val=STRCHARS ;

verbatimstring : VSTRSTART val=vstrcontent VSTREND ;
vstrcontent : (vstrchars | vstrquote)* ;
vstrchars : val=VSTRCHARS ;
vstrquote : VSTRQUOTE ;

interpolatedstring : ISTRSTART val=istrcontent ISTREND ;
istrcontent : (istrchars | charescape | charunicode | istrliteral | istrinter)* ;
istrchars : val=ISTRCHARS ;
istrliteral : val=ISTRLITERAL ;
istrinter : ISTRINTERSTA val=e ISTRINTEREND ;
