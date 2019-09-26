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
NAME          : ~[/=> \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000;] ~[=> \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000;]* ;
WS1           : [ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;

mode ATTRVALUEMODE;
ATTRSTRING    : ('"' ~[<"]* '"'+ | '\'' ~[<']* '\''+) -> popMode ;
ATTRTEXT      : ~[> \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000'"]+ -> popMode ;
WS2           : [ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;

mode SCRIPTMODE;
SCRIPTBODY    : .*? ('</script>' | '</>') -> popMode ;

mode STYLEMODE;
STYLEBODY     : .*? ('</style>' | '</>') -> popMode ;

mode TEXTAREAMODE;
TEXTAREABODY  : .*? ('</textarea>' | '</>') -> popMode ;

mode TITLEMODE;
TITLEBODY     : .*? ('</title>' | '</>') -> popMode ;
