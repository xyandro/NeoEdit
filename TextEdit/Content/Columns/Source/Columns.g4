grammar Columns;

columns : line* EOF;
line    : items EOL ;
items   : item WS? (EOC item WS?)* ;
item    : DATA (WS DATA)* | ;

DATA    : ~[\t \u2502\r\n]+ ;
WS      : [\t ]+ ;
EOC     : '\u2502' ;
EOL     : '\r' '\n' | '\r' | '\n' ;
