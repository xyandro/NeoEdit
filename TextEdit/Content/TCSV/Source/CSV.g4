grammar CSV;

doc    : (row CR? LF)* row? EOF ;
row    : field (SPLIT field)* ;
field  : STRING? ;

SPLIT  : ',' ;
CR     : '\r' ;
LF     : '\n' ;
STRING : ('"' ('""'|~'"')* '"' | ~[,\n\r"]) ~[,\n\r]* ;
