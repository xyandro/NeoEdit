grammar TSV;

doc    : (row CR? LF)* row? EOF ;
row    : field (SPLIT field)* ;
field  : TEXT   # Text
       | STRING # String
       |        # Empty
       ;

SPLIT  : '\t' ;
CR     : '\r' ;
LF     : '\n' ;
STRING : '"' ('""'|~'"')* '"' ;
TEXT   : ~[\t\n\r"]+ ;
