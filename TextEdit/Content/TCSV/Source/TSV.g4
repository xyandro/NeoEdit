grammar TSV;

doc    : (row CR? LF)* row? EOF ;
row    : field (SPLIT field)* ;
field  : STRING? ;

SPLIT  : '\t' ;
CR     : '\r' ;
LF     : '\n' ;
STRING : ('"' ('""'|~'"')* '"' | ~[\t\n\r"]) ~[\t\n\r]* ;
