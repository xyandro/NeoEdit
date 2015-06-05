grammar CSV;

csv: (fields CR? LF)* EOF ;
fields: field (COMMA field)* ;
field
	: TEXT   # Text
	| STRING # String
	|        # Empty;

COMMA: ',';
CR: '\r';
LF: '\n';
TEXT: ~[,\n\r"]+;
STRING: '"' ('""'|~'"')* '"';
