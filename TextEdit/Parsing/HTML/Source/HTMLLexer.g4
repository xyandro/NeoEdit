lexer grammar HTMLLexer;

COMMENT       : '<!--' .*? '-->' ;
DTD           : '<!' .*? '>' ; 

SPECIAL       : '<?' .*? '?>' ;

OPENSCRIPT    : '<script' -> pushMode(SCRIPTMODE), pushMode(TAGMODE) ;
OPENSTYLE     : '<style' -> pushMode(STYLEMODE), pushMode(TAGMODE) ;
OPENTEXTAREA  : '<textarea' -> pushMode(TEXTAREAMODE), pushMode(TAGMODE) ;
OPENTITLE     : '<title' -> pushMode(TITLEMODE), pushMode(TAGMODE) ;

OPEN          : '<' -> pushMode(TAGMODE) ;

TEXT          : ~[<]+ ;

mode TAGMODE;

SLASHCLOSE    : '/>' -> popMode ;
CLOSE         : '>' -> popMode ;
SLASH         : '/' ;
EQUALS        : '=' -> pushMode(ATTRVALUEMODE) ;
VOIDNAME      : 'area' | 'base' | 'br' | 'col' | 'embed' | 'hr' | 'img' | 'input' | 'keygen' | 'link' | 'meta' | 'param' | 'source' | 'track' | 'wbr' ;
NAME          : ~[/=> \t\r\n;] ~[=> \t\r\n;]* ;
WS1           : [ \t\r\n]+ -> skip ;

mode ATTRVALUEMODE;
ATTRSTRING    : ('"' ~[<"]* '"'+ | '\'' ~[<']* '\''+) -> popMode ;
ATTRTEXT      : ~[> \t\r\n'"]+ -> popMode ;
WS2           : [ \t\r\n]+ -> skip ;

mode SCRIPTMODE;
SCRIPTBODY    : .*? ('</script>' | '</>') -> popMode ;

mode STYLEMODE;
STYLEBODY     : .*? ('</style>' | '</>') -> popMode ;

mode TEXTAREAMODE;
TEXTAREABODY  : .*? ('</textarea>' | '</>') -> popMode ;

mode TITLEMODE;
TITLEBODY     : .*? ('</title>' | '</>') -> popMode ;
