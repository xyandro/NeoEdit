grammar CSV;

doc    : (row CR? LF)* row? EOF ;
row    : field (SPLIT field)* ;
field  : TEXT   # Text
       | STRING # String
       |        # Empty
       ;

SPLIT  : ',' ;
CR     : '\r' ;
LF     : '\n' ;
STRING : '"' ('""'|~'"')* '"' ;
TEXT   : ~[,\n\r"]+ ;
