grammar CSV;

root : (row CR? LF)* row EOF ;
row  : cell (SPLIT cell)* ;
cell : STRING? ;

SPLIT  : ',' ;
CR     : '\r' ;
LF     : '\n' ;
STRING : ('"' ('""'|~'"')* '"' | ~[,\n\r"]) ~[,\n\r]* ;
