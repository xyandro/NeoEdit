grammar TSV;

root : (row EOL)* row EOF ;
row  : cell (SPLIT cell)* ;
cell : STRING? ;

SPLIT  : '\t' ;
EOL    : [\r\n\u0085\u2028\u2029]+ ;
STRING : ('"' ('""'|~'"')* '"' | ~[\t\r\n\u0085\u2028\u2029"]) ~[\t\r\n\u0085\u2028\u2029]* ;
