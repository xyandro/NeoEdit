grammar ExactColumns;

root : row* EOF;
row  : WS? DOUBLE (cell (SINGLE cell)*)? DOUBLE WS? ;
cell : WS? text WS? ;
text : TEXT? ;

TEXT   : '"' ('""' | ~'"')* '"' | ~[\t \u2502\u2551\r\n]+ (WS ~[\t \u2502\u2551\r\n]+)* ;
WS     : [\t \r\n]+ ;
SINGLE : '\u2502' ;
DOUBLE : '\u2551' ;
