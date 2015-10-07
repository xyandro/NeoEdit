parser grammar RevRegExParser;

options { tokenVocab = RevRegExLexer; }

// [-a-c \\[\]]{2}te st{2,3}\[q{,2}(qu|ee|r){2}

revregex      : items EOF;
items         : itemsList (PIPE itemsList)*;
itemsList     : item* ;
item          : LPAREN items RPAREN # Parens
              | item QUESTION # Optional
              | item LBRACE (mincount=COUNT? COMMA)? maxcount=COUNT RBRACE # Repeat
              | (char | range) # Simple
              ;
char          : val=CHAR ;
range         : LBRACKET HYPHEN? rangeItem* RBRACKET ;
rangeChar     : val=CHARS_CHAR ;
rangeItem     : rangeChar | rangeStartEnd ;
rangeStartEnd : startchar=rangeChar HYPHEN endchar=rangeChar ;
