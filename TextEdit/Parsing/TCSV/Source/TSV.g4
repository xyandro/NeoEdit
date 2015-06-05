grammar TSV;

tsv: (fields CR? LF)* EOF ;
fields: field (TAB field)* ;
field
	: TEXT   # Text
	| STRING # String
	|        # Empty;

TAB: '\t';
CR: '\r';
LF: '\n';
TEXT: ~[\t\n\r"]+;
STRING: '"' ('""'|~'"')* '"';
