grammar CSV;

root : (row EOL)* row EOF ;
row  : cell (SPLIT cell)* ;
cell : STRING? ;

SPLIT  : ',' ;
EOL    : [\r\n\u0085\u2028\u2029]+ ;
STRING : ('"' ('""'|~'"')* '"' | ~[,\r\n\u0085\u2028\u2029"]) ~[,\r\n\u0085\u2028\u2029]* ;
