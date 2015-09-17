grammar Columns;

columns : line* EOF;
line    : items EOL ;
items   : itemws  (EOC itemws )* ;
itemws  : WS? item WS? ;
item    : DATA (WS DATA)* | ;

DATA    : ~[\t \u2502\r\n]+ ;
WS      : [\t ]+ ;
EOC     : '\u2502' ;
EOL     : '\r' '\n' | '\r' | '\n' ;
