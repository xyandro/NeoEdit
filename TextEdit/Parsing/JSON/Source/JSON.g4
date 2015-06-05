/** Taken from "The Definitive ANTLR 4 Reference" by Terence Parr */

// Derived from http://json.org

grammar JSON;

json:   object
    |   array
    ;

object
    :   '{' pair (',' pair)* '}'
    |   '{' '}' // empty object
    ;

pair:   pairid ':' item ;

pairid: STRING ;

array
    :   '[' item (',' item)* ']'
    |   '[' ']' // empty array
    ;

item
	: recurse
	| value
	;

recurse
    :   object
    |   array
    ;

value
    :   str=STRING
    |   NUMBER
    |   'true'  // keywords
    |   'false'
    |   'null'
    ;

STRING :  '"' (ESC | ~["\\])* '"' ;

fragment ESC :   '\\' (["\\/bfnrt] | UNICODE) ;
fragment UNICODE : 'u' HEX HEX HEX HEX ;
fragment HEX : [0-9a-fA-F] ;

NUMBER
    :   '-'? INT '.' [0-9]+ EXP? // 1.35, 1.35E-9, 0.3, -4.5
    |   '-'? INT EXP             // 1e10 -3e4
    |   '-'? INT                 // -3, 45
    ;

fragment INT :   '0' | [1-9] [0-9]* ; // no leading zeros
fragment EXP :   [Ee] [+\-]? INT ; // \- since - means "range" inside [...]

WS  :   [ \t\n\r]+ -> skip ;
