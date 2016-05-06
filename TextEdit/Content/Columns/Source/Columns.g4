grammar Columns;

root : (row WS?)* EOF;
row  : DOUBLE cell (SINGLE cell)* DOUBLE ;
cell : WS? text WS? ;
text : TEXT (WS TEXT)* | ;

TEXT   : ~[\t \u2502\u2551\r\n]+ ;
WS     : [\t \r\n]+ ;
SINGLE : '\u2502' ;
DOUBLE : '\u2551' ;
