grammar JSON;

json     : item* EOF ;
item     : object | array | string | number | constant ;
object   : LBRACE (pair (COMMA pair)*)? RBRACE ;
pair     : name=STRING COLON item ;
array    : LBRACKET (item (COMMA item)*)? RBRACKET ;
string   : STRING ;
number   : NUMBER ;
constant : CONSTANT ;

STRING   : '"' ('\\' . | ~["\\])* '"' ;
NUMBER   : [-+0-9.eE]+ ;
LBRACE   : '{' ;
RBRACE   : '}' ;
LBRACKET : '[' ;
RBRACKET : ']' ;
CONSTANT : ~["' \t\n\r\[\]{},:] ~[ \t\n\r\[\]{},:]* ;
COMMA    : ',' ;
COLON    : ':' ;
WS       : [ \t\n\r]+ -> skip ;
