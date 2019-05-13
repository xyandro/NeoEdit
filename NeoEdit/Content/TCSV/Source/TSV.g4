grammar TSV;

root : (row CR? LF)* row EOF ;
row  : cell (SPLIT cell)* ;
cell : STRING? ;

SPLIT  : '\t' ;
CR     : '\r' ;
LF     : '\n' ;
STRING : ('"' ('""'|~'"')* '"' | ~[\t\n\r"]) ~[\t\n\r]* ;
