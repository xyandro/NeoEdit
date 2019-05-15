grammar Balanced;

balanced : data* EOF;

data : LANGLE data* RANGLE     # Angles
     | LBRACE data* RBRACE     # Braces
     | LBRACKET data* RBRACKET # Brackets
     | LPAREN data* RPAREN     # Parens
     ;

LANGLE   : '<' ;
RANGLE   : '>' ;
LBRACE   : '{' ;
RBRACE   : '}' ;
LBRACKET : '[' ;
RBRACKET : ']' ;
LPAREN   : '(' ;
RPAREN   : ')' ;

STRING   : '"' (~'"' | '""')* '"' -> skip ;
CONTENT  : ~[<>{}\[\]()"]+ -> skip ;
