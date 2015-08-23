grammar RevRegEx;

revregex      : items EOF;
items         : item* ;
item          : LPAREN items RPAREN # Parens
              | item LBRACE count=repeatCount RBRACE # Repeat
              | (char | range) # Simple
              ;
repeatCount   : DIGIT+ ;
char          : BACKSLASH charval=(LBRACE | RBRACE | LBRACKET | RBRACKET | LPAREN | RPAREN | HYPHEN | BACKSLASH | DIGIT | CHAR) | charval=(HYPHEN | DIGIT | CHAR) ;
range         : LBRACKET (BACKSLASH? HYPHEN)? rangeItem* RBRACKET ;
rangeItem     : rangeChar | rangeStartEnd ;
rangeChar     : BACKSLASH charval=(LBRACE | RBRACE | LBRACKET | RBRACKET | LPAREN | RPAREN | HYPHEN | BACKSLASH | DIGIT | CHAR) | charval=(LBRACE | RBRACE | LBRACKET | LPAREN | RPAREN | DIGIT | CHAR) ;
rangeStartEnd : startchar=rangeChar HYPHEN endchar=rangeChar ;

LBRACE        : '{' ;
RBRACE        : '}' ;
LBRACKET      : '[' ;
RBRACKET      : ']' ;
LPAREN        : '(' ;
RPAREN        : ')' ;
HYPHEN        : '-' ;
BACKSLASH     : '\\' ;
DIGIT         : [0-9] ;
CHAR          : ~[-{}\[\]()0-9] ;
