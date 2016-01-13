grammar Columns;

columns : (line WS?)* EOF;
line    : DOUBLE item (SINGLE item)* DOUBLE ;
item    : WS? text WS? ;
text    : DATA (WS DATA)* | ;

DATA    : ~[\t \u2502\u2551\r\n]+ ;
WS      : [\t \r\n]+ ;
SINGLE  : '\u2502' ;
DOUBLE  : '\u2551' ;
