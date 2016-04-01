lexer grammar CSharpLexer;

ABSTRACT                   : 'abstract'   ;
ADD                        : 'add'        ;
ALIAS                      : 'alias'      ;
ARGLIST                    : '__arglist'  ;
AS                         : 'as'         ;
ASCENDING                  : 'ascending'  ;
ASSEMBLY                   : 'assembly'   ;
ASYNC                      : 'async'      ;
AWAIT                      : 'await'      ;
BASE                       : 'base'       ;
BREAK                      : 'break'      ;
BY                         : 'by'         ;
CASE                       : 'case'       ;
CATCH                      : 'catch'      ;
CHECKED                    : 'checked'    ;
CLASS                      : 'class'      ;
CONST                      : 'const'      ;
CONTINUE                   : 'continue'   ;
DEFAULT                    : 'default'    ;
DELEGATE                   : 'delegate'   ;
DESCENDING                 : 'descending' ;
DO                         : 'do'         ;
ELSE                       : 'else'       ;
ENUM                       : 'enum'       ;
EQUALS                     : 'equals'     ;
EVENT                      : 'event'      ;
EXPLICIT                   : 'explicit'   ;
EXTERN                     : 'extern'     ;
FALSE                      : 'false'      ;
FIELD                      : 'field'      ;
FINALLY                    : 'finally'    ;
FIXED                      : 'fixed'      ;
FOR                        : 'for'        ;
FOREACH                    : 'foreach'    ;
FROM                       : 'from'       ;
GET                        : 'get'        ;
GOTO                       : 'goto'       ;
GROUP                      : 'group'      ;
IF                         : 'if'         ;
IMPLICIT                   : 'implicit'   ;
IN                         : 'in'         ;
INTERFACE                  : 'interface'  ;
INTERNAL                   : 'internal'   ;
INTO                       : 'into'       ;
IS                         : 'is'         ;
JOIN                       : 'join'       ;
LET                        : 'let'        ;
LOCK                       : 'lock'       ;
METHOD                     : 'method'     ;
MODULE                     : 'module'     ;
NAMESPACE                  : 'namespace'  ;
NEW                        : 'new'        ;
NULL                       : 'null'       ;
ON                         : 'on'         ;
OPERATOR                   : 'operator'   ;
ORDERBY                    : 'orderby'    ;
OUT                        : 'out'        ;
OVERRIDE                   : 'override'   ;
PARAM                      : 'param'      ;
PARAMS                     : 'params'     ;
PARTIAL                    : 'partial'    ;
PRIVATE                    : 'private'    ;
PROPERTY                   : 'property'   ;
PROTECTED                  : 'protected'  ;
PUBLIC                     : 'public'     ;
READONLY                   : 'readonly'   ;
REF                        : 'ref'        ;
REMOVE                     : 'remove'     ;
RETURN                     : 'return'     ;
SEALED                     : 'sealed'     ;
SELECT                     : 'select'     ;
SET                        : 'set'        ;
STACKALLOC                 : 'stackalloc' ;
STATIC                     : 'static'     ;
STRUCT                     : 'struct'     ;
SWITCH                     : 'switch'     ;
THIS                       : 'this'       ;
THROW                      : 'throw'      ;
TRUE                       : 'true'       ;
TRY                        : 'try'        ;
TYPE                       : 'type'       ;
TYPEOF                     : 'typeof'     ;
TYPEVAR                    : 'typevar'    ;
UNCHECKED                  : 'unchecked'  ;
UNSAFE                     : 'unsafe'     ;
USING                      : 'using'      ;
VIRTUAL                    : 'virtual'    ;
VOLATILE                   : 'volatile'   ;
WHERE                      : 'where'      ;
WHILE                      : 'while'      ;
YIELD                      : 'yield'      ;

PLUS                       : '+'   ;
PLUS_ASSIGN                : '+='  ;
AND                        : '&'   ;
AND_ASSIGN                 : '&='  ;
ASSIGN                     : '='   ;
BANG                       : '!'   ;
COALESCE                   : '??'  ;
COLON                      : ':'   ;
COMMA                      : ','   ;
DEC                        : '--'  ;
DIV                        : '/'   ;
DIV_ASSIGN                 : '/='  ;
DOT                        : '.'   ;
DOUBLE_COLON               : '::'  ;
EQ                         : '=='  ;
GE                         : '>='  ;
GT                         : '>'   ; // There is no '>>' because List<List<int>> would use it.
INC                        : '++'  ;
INTERR                     : '?'   ;
LAMBDA                     : '=>'  ;
LAND                       : '&&'  ;
LBRACE                     : '{'   ;
LBRACKET                   : '['   ;
LE                         : '<='  ;
LEFT_SHIFT                 : '<<'  ;
LEFT_SHIFT_ASSIGN          : '<<=' ;
LOR                        : '||'  ;
LPAREN                     : '('   ;
LT                         : '<'   ;
MOD                        : '%'   ;
MOD_ASSIGN                 : '%='  ;
MULT                       : '*'   ;
MULT_ASSIGN                : '*='  ;
NE                         : '!='  ;
OR                         : '|'   ;
OR_ASSIGN                  : '|='  ;
PTR                        : '->'  ;
RBRACE                     : '}'   ;
RBRACKET                   : ']'   ;
RPAREN                     : ')'   ;
SEMICOLON                  : ';'   ;
MINUS                      : '-'   ;
MINUS_ASSIGN               : '-='  ;
TILDE                      : '~'   ;
XOR                        : '^'   ;
XOR_ASSIGN                 : '^='  ;

NEWLINE                    : ('\r' | '\n' | '\r\n' | '\u0085' | '\u2028' | '\u2029' | EOF) -> skip ;
WHITESPACE                 : ('\u0009' | '\u000b'..'\u000c' | '\u0020' | '\u00a0' | '\u1680' | '\u2000'..'\u200a' | '\u202f' | '\u205f' | '\u3000')+ -> skip ;

COMMENT                    : (SINGLE_COMMENT | DELIMITED_COMMENT) -> skip ;
fragment SINGLE_COMMENT    : '//' ~[\r\n\u0085\u2028\u2029]* ;
fragment DELIMITED_COMMENT : '/*' .*? '*/' ;

IDENTIFIER                 : '@'? IDFIRST IDNEXT* ;
fragment HEXDIGIT          : [0-9A-Fa-f] ;
fragment HEXID             : '\\u' HEXDIGIT HEXDIGIT HEXDIGIT HEXDIGIT | '\\U' HEXDIGIT HEXDIGIT HEXDIGIT HEXDIGIT HEXDIGIT HEXDIGIT HEXDIGIT HEXDIGIT ;
fragment IDFIRST           : HEXID | '\u0041'..'\u005a' | '\u005f' | '\u0061'..'\u007a' | '\u00aa' | '\u00b5' | '\u00ba' | '\u00c0'..'\u00d6' | '\u00d8'..'\u00f6' | '\u00f8'..'\u02c1' | '\u02c6'..'\u02d1' | '\u02e0'..'\u02e4' | '\u02ec' | '\u02ee' | '\u0370'..'\u0374' | '\u0376'..'\u0377' | '\u037a'..'\u037d' | '\u037f' | '\u0386' | '\u0388'..'\u038a' | '\u038c' | '\u038e'..'\u03a1' | '\u03a3'..'\u03f5' | '\u03f7'..'\u0481' | '\u048a'..'\u052f' | '\u0531'..'\u0556' | '\u0559' | '\u0561'..'\u0587' | '\u05d0'..'\u05ea' | '\u05f0'..'\u05f2' | '\u0620'..'\u064a' | '\u066e'..'\u066f' | '\u0671'..'\u06d3' | '\u06d5' | '\u06e5'..'\u06e6' | '\u06ee'..'\u06ef' | '\u06fa'..'\u06fc' | '\u06ff' | '\u0710' | '\u0712'..'\u072f' | '\u074d'..'\u07a5' | '\u07b1' | '\u07ca'..'\u07ea' | '\u07f4'..'\u07f5' | '\u07fa' | '\u0800'..'\u0815' | '\u081a' | '\u0824' | '\u0828' | '\u0840'..'\u0858' | '\u08a0'..'\u08b4' | '\u0904'..'\u0939' | '\u093d' | '\u0950' | '\u0958'..'\u0961' | '\u0971'..'\u0980' | '\u0985'..'\u098c' | '\u098f'..'\u0990' | '\u0993'..'\u09a8' | '\u09aa'..'\u09b0' | '\u09b2' | '\u09b6'..'\u09b9' | '\u09bd' | '\u09ce' | '\u09dc'..'\u09dd' | '\u09df'..'\u09e1' | '\u09f0'..'\u09f1' | '\u0a05'..'\u0a0a' | '\u0a0f'..'\u0a10' | '\u0a13'..'\u0a28' | '\u0a2a'..'\u0a30' | '\u0a32'..'\u0a33' | '\u0a35'..'\u0a36' | '\u0a38'..'\u0a39' | '\u0a59'..'\u0a5c' | '\u0a5e' | '\u0a72'..'\u0a74' | '\u0a85'..'\u0a8d' | '\u0a8f'..'\u0a91' | '\u0a93'..'\u0aa8' | '\u0aaa'..'\u0ab0' | '\u0ab2'..'\u0ab3' | '\u0ab5'..'\u0ab9' | '\u0abd' | '\u0ad0' | '\u0ae0'..'\u0ae1' | '\u0af9' | '\u0b05'..'\u0b0c' | '\u0b0f'..'\u0b10' | '\u0b13'..'\u0b28' | '\u0b2a'..'\u0b30' | '\u0b32'..'\u0b33' | '\u0b35'..'\u0b39' | '\u0b3d' | '\u0b5c'..'\u0b5d' | '\u0b5f'..'\u0b61' | '\u0b71' | '\u0b83' | '\u0b85'..'\u0b8a' | '\u0b8e'..'\u0b90' | '\u0b92'..'\u0b95' | '\u0b99'..'\u0b9a' | '\u0b9c' | '\u0b9e'..'\u0b9f' | '\u0ba3'..'\u0ba4' | '\u0ba8'..'\u0baa' | '\u0bae'..'\u0bb9' | '\u0bd0' | '\u0c05'..'\u0c0c' | '\u0c0e'..'\u0c10' | '\u0c12'..'\u0c28' | '\u0c2a'..'\u0c39' | '\u0c3d' | '\u0c58'..'\u0c5a' | '\u0c60'..'\u0c61' | '\u0c85'..'\u0c8c' | '\u0c8e'..'\u0c90' | '\u0c92'..'\u0ca8' | '\u0caa'..'\u0cb3' | '\u0cb5'..'\u0cb9' | '\u0cbd' | '\u0cde' | '\u0ce0'..'\u0ce1' | '\u0cf1'..'\u0cf2' | '\u0d05'..'\u0d0c' | '\u0d0e'..'\u0d10' | '\u0d12'..'\u0d3a' | '\u0d3d' | '\u0d4e' | '\u0d5f'..'\u0d61' | '\u0d7a'..'\u0d7f' | '\u0d85'..'\u0d96' | '\u0d9a'..'\u0db1' | '\u0db3'..'\u0dbb' | '\u0dbd' | '\u0dc0'..'\u0dc6' | '\u0e01'..'\u0e30' | '\u0e32'..'\u0e33' | '\u0e40'..'\u0e46' | '\u0e81'..'\u0e82' | '\u0e84' | '\u0e87'..'\u0e88' | '\u0e8a' | '\u0e8d' | '\u0e94'..'\u0e97' | '\u0e99'..'\u0e9f' | '\u0ea1'..'\u0ea3' | '\u0ea5' | '\u0ea7' | '\u0eaa'..'\u0eab' | '\u0ead'..'\u0eb0' | '\u0eb2'..'\u0eb3' | '\u0ebd' | '\u0ec0'..'\u0ec4' | '\u0ec6' | '\u0edc'..'\u0edf' | '\u0f00' | '\u0f40'..'\u0f47' | '\u0f49'..'\u0f6c' | '\u0f88'..'\u0f8c' | '\u1000'..'\u102a' | '\u103f' | '\u1050'..'\u1055' | '\u105a'..'\u105d' | '\u1061' | '\u1065'..'\u1066' | '\u106e'..'\u1070' | '\u1075'..'\u1081' | '\u108e' | '\u10a0'..'\u10c5' | '\u10c7' | '\u10cd' | '\u10d0'..'\u10fa' | '\u10fc'..'\u1248' | '\u124a'..'\u124d' | '\u1250'..'\u1256' | '\u1258' | '\u125a'..'\u125d' | '\u1260'..'\u1288' | '\u128a'..'\u128d' | '\u1290'..'\u12b0' | '\u12b2'..'\u12b5' | '\u12b8'..'\u12be' | '\u12c0' | '\u12c2'..'\u12c5' | '\u12c8'..'\u12d6' | '\u12d8'..'\u1310' | '\u1312'..'\u1315' | '\u1318'..'\u135a' | '\u1380'..'\u138f' | '\u13a0'..'\u13f5' | '\u13f8'..'\u13fd' | '\u1401'..'\u166c' | '\u166f'..'\u167f' | '\u1681'..'\u169a' | '\u16a0'..'\u16ea' | '\u16ee'..'\u16f8' | '\u1700'..'\u170c' | '\u170e'..'\u1711' | '\u1720'..'\u1731' | '\u1740'..'\u1751' | '\u1760'..'\u176c' | '\u176e'..'\u1770' | '\u1780'..'\u17b3' | '\u17d7' | '\u17dc' | '\u1820'..'\u1877' | '\u1880'..'\u18a8' | '\u18aa' | '\u18b0'..'\u18f5' | '\u1900'..'\u191e' | '\u1950'..'\u196d' | '\u1970'..'\u1974' | '\u1980'..'\u19ab' | '\u19b0'..'\u19c9' | '\u1a00'..'\u1a16' | '\u1a20'..'\u1a54' | '\u1aa7' | '\u1b05'..'\u1b33' | '\u1b45'..'\u1b4b' | '\u1b83'..'\u1ba0' | '\u1bae'..'\u1baf' | '\u1bba'..'\u1be5' | '\u1c00'..'\u1c23' | '\u1c4d'..'\u1c4f' | '\u1c5a'..'\u1c7d' | '\u1ce9'..'\u1cec' | '\u1cee'..'\u1cf1' | '\u1cf5'..'\u1cf6' | '\u1d00'..'\u1dbf' | '\u1e00'..'\u1f15' | '\u1f18'..'\u1f1d' | '\u1f20'..'\u1f45' | '\u1f48'..'\u1f4d' | '\u1f50'..'\u1f57' | '\u1f59' | '\u1f5b' | '\u1f5d' | '\u1f5f'..'\u1f7d' | '\u1f80'..'\u1fb4' | '\u1fb6'..'\u1fbc' | '\u1fbe' | '\u1fc2'..'\u1fc4' | '\u1fc6'..'\u1fcc' | '\u1fd0'..'\u1fd3' | '\u1fd6'..'\u1fdb' | '\u1fe0'..'\u1fec' | '\u1ff2'..'\u1ff4' | '\u1ff6'..'\u1ffc' | '\u2071' | '\u207f' | '\u2090'..'\u209c' | '\u2102' | '\u2107' | '\u210a'..'\u2113' | '\u2115' | '\u2119'..'\u211d' | '\u2124' | '\u2126' | '\u2128' | '\u212a'..'\u212d' | '\u212f'..'\u2139' | '\u213c'..'\u213f' | '\u2145'..'\u2149' | '\u214e' | '\u2160'..'\u2188' | '\u2c00'..'\u2c2e' | '\u2c30'..'\u2c5e' | '\u2c60'..'\u2ce4' | '\u2ceb'..'\u2cee' | '\u2cf2'..'\u2cf3' | '\u2d00'..'\u2d25' | '\u2d27' | '\u2d2d' | '\u2d30'..'\u2d67' | '\u2d6f' | '\u2d80'..'\u2d96' | '\u2da0'..'\u2da6' | '\u2da8'..'\u2dae' | '\u2db0'..'\u2db6' | '\u2db8'..'\u2dbe' | '\u2dc0'..'\u2dc6' | '\u2dc8'..'\u2dce' | '\u2dd0'..'\u2dd6' | '\u2dd8'..'\u2dde' | '\u2e2f' | '\u3005'..'\u3007' | '\u3021'..'\u3029' | '\u3031'..'\u3035' | '\u3038'..'\u303c' | '\u3041'..'\u3096' | '\u309d'..'\u309f' | '\u30a1'..'\u30fa' | '\u30fc'..'\u30ff' | '\u3105'..'\u312d' | '\u3131'..'\u318e' | '\u31a0'..'\u31ba' | '\u31f0'..'\u31ff' | '\u3400' | '\u4db5' | '\u4e00' | '\u9fd5' | '\ua000'..'\ua48c' | '\ua4d0'..'\ua4fd' | '\ua500'..'\ua60c' | '\ua610'..'\ua61f' | '\ua62a'..'\ua62b' | '\ua640'..'\ua66e' | '\ua67f'..'\ua69d' | '\ua6a0'..'\ua6ef' | '\ua717'..'\ua71f' | '\ua722'..'\ua788' | '\ua78b'..'\ua7ad' | '\ua7b0'..'\ua7b7' | '\ua7f7'..'\ua801' | '\ua803'..'\ua805' | '\ua807'..'\ua80a' | '\ua80c'..'\ua822' | '\ua840'..'\ua873' | '\ua882'..'\ua8b3' | '\ua8f2'..'\ua8f7' | '\ua8fb' | '\ua8fd' | '\ua90a'..'\ua925' | '\ua930'..'\ua946' | '\ua960'..'\ua97c' | '\ua984'..'\ua9b2' | '\ua9cf' | '\ua9e0'..'\ua9e4' | '\ua9e6'..'\ua9ef' | '\ua9fa'..'\ua9fe' | '\uaa00'..'\uaa28' | '\uaa40'..'\uaa42' | '\uaa44'..'\uaa4b' | '\uaa60'..'\uaa76' | '\uaa7a' | '\uaa7e'..'\uaaaf' | '\uaab1' | '\uaab5'..'\uaab6' | '\uaab9'..'\uaabd' | '\uaac0' | '\uaac2' | '\uaadb'..'\uaadd' | '\uaae0'..'\uaaea' | '\uaaf2'..'\uaaf4' | '\uab01'..'\uab06' | '\uab09'..'\uab0e' | '\uab11'..'\uab16' | '\uab20'..'\uab26' | '\uab28'..'\uab2e' | '\uab30'..'\uab5a' | '\uab5c'..'\uab65' | '\uab70'..'\uabe2' | '\uac00' | '\ud7a3' | '\ud7b0'..'\ud7c6' | '\ud7cb'..'\ud7fb' | '\uf900'..'\ufa6d' | '\ufa70'..'\ufad9' | '\ufb00'..'\ufb06' | '\ufb13'..'\ufb17' | '\ufb1d' | '\ufb1f'..'\ufb28' | '\ufb2a'..'\ufb36' | '\ufb38'..'\ufb3c' | '\ufb3e' | '\ufb40'..'\ufb41' | '\ufb43'..'\ufb44' | '\ufb46'..'\ufbb1' | '\ufbd3'..'\ufd3d' | '\ufd50'..'\ufd8f' | '\ufd92'..'\ufdc7' | '\ufdf0'..'\ufdfb' | '\ufe70'..'\ufe74' | '\ufe76'..'\ufefc' | '\uff21'..'\uff3a' | '\uff41'..'\uff5a' | '\uff66'..'\uffbe' | '\uffc2'..'\uffc7' | '\uffca'..'\uffcf' | '\uffd2'..'\uffd7' | '\uffda'..'\uffdc' | '\ud800' ( '\udc00'..'\udc0b' | '\udc0d'..'\udc26' | '\udc28'..'\udc3a' | '\udc3c'..'\udc3d' | '\udc3f'..'\udc4d' | '\udc50'..'\udc5d' | '\udc80'..'\udcfa' | '\udd40'..'\udd74' | '\ude80'..'\ude9c' | '\udea0'..'\uded0' | '\udf00'..'\udf1f' | '\udf30'..'\udf4a' | '\udf50'..'\udf75' | '\udf80'..'\udf9d' | '\udfa0'..'\udfc3' | '\udfc8'..'\udfcf' | '\udfd1'..'\udfd5' ) | '\ud801' ( '\udc00'..'\udc9d' | '\udd00'..'\udd27' | '\udd30'..'\udd63' | '\ude00'..'\udf36' | '\udf40'..'\udf55' | '\udf60'..'\udf67' ) | '\ud802' ( '\udc00'..'\udc05' | '\udc08' | '\udc0a'..'\udc35' | '\udc37'..'\udc38' | '\udc3c' | '\udc3f'..'\udc55' | '\udc60'..'\udc76' | '\udc80'..'\udc9e' | '\udce0'..'\udcf2' | '\udcf4'..'\udcf5' | '\udd00'..'\udd15' | '\udd20'..'\udd39' | '\udd80'..'\uddb7' | '\uddbe'..'\uddbf' | '\ude00' | '\ude10'..'\ude13' | '\ude15'..'\ude17' | '\ude19'..'\ude33' | '\ude60'..'\ude7c' | '\ude80'..'\ude9c' | '\udec0'..'\udec7' | '\udec9'..'\udee4' | '\udf00'..'\udf35' | '\udf40'..'\udf55' | '\udf60'..'\udf72' | '\udf80'..'\udf91' ) | '\ud803' ( '\udc00'..'\udc48' | '\udc80'..'\udcb2' | '\udcc0'..'\udcf2' ) | '\ud804' ( '\udc03'..'\udc37' | '\udc83'..'\udcaf' | '\udcd0'..'\udce8' | '\udd03'..'\udd26' | '\udd50'..'\udd72' | '\udd76' | '\udd83'..'\uddb2' | '\uddc1'..'\uddc4' | '\uddda' | '\udddc' | '\ude00'..'\ude11' | '\ude13'..'\ude2b' | '\ude80'..'\ude86' | '\ude88' | '\ude8a'..'\ude8d' | '\ude8f'..'\ude9d' | '\ude9f'..'\udea8' | '\udeb0'..'\udede' | '\udf05'..'\udf0c' | '\udf0f'..'\udf10' | '\udf13'..'\udf28' | '\udf2a'..'\udf30' | '\udf32'..'\udf33' | '\udf35'..'\udf39' | '\udf3d' | '\udf50' | '\udf5d'..'\udf61' ) | '\ud805' ( '\udc80'..'\udcaf' | '\udcc4'..'\udcc5' | '\udcc7' | '\udd80'..'\uddae' | '\uddd8'..'\udddb' | '\ude00'..'\ude2f' | '\ude44' | '\ude80'..'\udeaa' | '\udf00'..'\udf19' ) | '\ud806' ( '\udca0'..'\udcdf' | '\udcff' | '\udec0'..'\udef8' ) | '\ud808' '\udc00'..'\udf99' | '\ud809' ( '\udc00'..'\udc6e' | '\udc80'..'\udd43' ) | '\ud80c' '\udc00'..'\udfff' | '\ud80d' '\udc00'..'\udc2e' | '\ud811' '\udc00'..'\ude46' | '\ud81a' ( '\udc00'..'\ude38' | '\ude40'..'\ude5e' | '\uded0'..'\udeed' | '\udf00'..'\udf2f' | '\udf40'..'\udf43' | '\udf63'..'\udf77' | '\udf7d'..'\udf8f' ) | '\ud81b' ( '\udf00'..'\udf44' | '\udf50' | '\udf93'..'\udf9f' ) | '\ud82c' '\udc00'..'\udc01' | '\ud82f' ( '\udc00'..'\udc6a' | '\udc70'..'\udc7c' | '\udc80'..'\udc88' | '\udc90'..'\udc99' ) | '\ud835' ( '\udc00'..'\udc54' | '\udc56'..'\udc9c' | '\udc9e'..'\udc9f' | '\udca2' | '\udca5'..'\udca6' | '\udca9'..'\udcac' | '\udcae'..'\udcb9' | '\udcbb' | '\udcbd'..'\udcc3' | '\udcc5'..'\udd05' | '\udd07'..'\udd0a' | '\udd0d'..'\udd14' | '\udd16'..'\udd1c' | '\udd1e'..'\udd39' | '\udd3b'..'\udd3e' | '\udd40'..'\udd44' | '\udd46' | '\udd4a'..'\udd50' | '\udd52'..'\udea5' | '\udea8'..'\udec0' | '\udec2'..'\udeda' | '\udedc'..'\udefa' | '\udefc'..'\udf14' | '\udf16'..'\udf34' | '\udf36'..'\udf4e' | '\udf50'..'\udf6e' | '\udf70'..'\udf88' | '\udf8a'..'\udfa8' | '\udfaa'..'\udfc2' | '\udfc4'..'\udfcb' ) | '\ud83a' '\udc00'..'\udcc4' | '\ud83b' ( '\ude00'..'\ude03' | '\ude05'..'\ude1f' | '\ude21'..'\ude22' | '\ude24' | '\ude27' | '\ude29'..'\ude32' | '\ude34'..'\ude37' | '\ude39' | '\ude3b' | '\ude42' | '\ude47' | '\ude49' | '\ude4b' | '\ude4d'..'\ude4f' | '\ude51'..'\ude52' | '\ude54' | '\ude57' | '\ude59' | '\ude5b' | '\ude5d' | '\ude5f' | '\ude61'..'\ude62' | '\ude64' | '\ude67'..'\ude6a' | '\ude6c'..'\ude72' | '\ude74'..'\ude77' | '\ude79'..'\ude7c' | '\ude7e' | '\ude80'..'\ude89' | '\ude8b'..'\ude9b' | '\udea1'..'\udea3' | '\udea5'..'\udea9' | '\udeab'..'\udebb' ) | '\ud840' '\udc00' | '\ud869' ( '\uded6' | '\udf00' ) | '\ud86d' ( '\udf34' | '\udf40' ) | '\ud86e' ( '\udc1d' | '\udc20' ) | '\ud873' '\udea1' | '\ud87e' '\udc00'..'\ude1d' ;
fragment IDNEXT            : IDFIRST | '\u0030'..'\u0039' | '\u00ad' | '\u0300'..'\u036f' | '\u0483'..'\u0487' | '\u0591'..'\u05bd' | '\u05bf' | '\u05c1'..'\u05c2' | '\u05c4'..'\u05c5' | '\u05c7' | '\u0600'..'\u0605' | '\u0610'..'\u061a' | '\u061c' | '\u064b'..'\u0669' | '\u0670' | '\u06d6'..'\u06dd' | '\u06df'..'\u06e4' | '\u06e7'..'\u06e8' | '\u06ea'..'\u06ed' | '\u06f0'..'\u06f9' | '\u070f' | '\u0711' | '\u0730'..'\u074a' | '\u07a6'..'\u07b0' | '\u07c0'..'\u07c9' | '\u07eb'..'\u07f3' | '\u0816'..'\u0819' | '\u081b'..'\u0823' | '\u0825'..'\u0827' | '\u0829'..'\u082d' | '\u0859'..'\u085b' | '\u08e3'..'\u0903' | '\u093a'..'\u093c' | '\u093e'..'\u094f' | '\u0951'..'\u0957' | '\u0962'..'\u0963' | '\u0966'..'\u096f' | '\u0981'..'\u0983' | '\u09bc' | '\u09be'..'\u09c4' | '\u09c7'..'\u09c8' | '\u09cb'..'\u09cd' | '\u09d7' | '\u09e2'..'\u09e3' | '\u09e6'..'\u09ef' | '\u0a01'..'\u0a03' | '\u0a3c' | '\u0a3e'..'\u0a42' | '\u0a47'..'\u0a48' | '\u0a4b'..'\u0a4d' | '\u0a51' | '\u0a66'..'\u0a71' | '\u0a75' | '\u0a81'..'\u0a83' | '\u0abc' | '\u0abe'..'\u0ac5' | '\u0ac7'..'\u0ac9' | '\u0acb'..'\u0acd' | '\u0ae2'..'\u0ae3' | '\u0ae6'..'\u0aef' | '\u0b01'..'\u0b03' | '\u0b3c' | '\u0b3e'..'\u0b44' | '\u0b47'..'\u0b48' | '\u0b4b'..'\u0b4d' | '\u0b56'..'\u0b57' | '\u0b62'..'\u0b63' | '\u0b66'..'\u0b6f' | '\u0b82' | '\u0bbe'..'\u0bc2' | '\u0bc6'..'\u0bc8' | '\u0bca'..'\u0bcd' | '\u0bd7' | '\u0be6'..'\u0bef' | '\u0c00'..'\u0c03' | '\u0c3e'..'\u0c44' | '\u0c46'..'\u0c48' | '\u0c4a'..'\u0c4d' | '\u0c55'..'\u0c56' | '\u0c62'..'\u0c63' | '\u0c66'..'\u0c6f' | '\u0c81'..'\u0c83' | '\u0cbc' | '\u0cbe'..'\u0cc4' | '\u0cc6'..'\u0cc8' | '\u0cca'..'\u0ccd' | '\u0cd5'..'\u0cd6' | '\u0ce2'..'\u0ce3' | '\u0ce6'..'\u0cef' | '\u0d01'..'\u0d03' | '\u0d3e'..'\u0d44' | '\u0d46'..'\u0d48' | '\u0d4a'..'\u0d4d' | '\u0d57' | '\u0d62'..'\u0d63' | '\u0d66'..'\u0d6f' | '\u0d82'..'\u0d83' | '\u0dca' | '\u0dcf'..'\u0dd4' | '\u0dd6' | '\u0dd8'..'\u0ddf' | '\u0de6'..'\u0def' | '\u0df2'..'\u0df3' | '\u0e31' | '\u0e34'..'\u0e3a' | '\u0e47'..'\u0e4e' | '\u0e50'..'\u0e59' | '\u0eb1' | '\u0eb4'..'\u0eb9' | '\u0ebb'..'\u0ebc' | '\u0ec8'..'\u0ecd' | '\u0ed0'..'\u0ed9' | '\u0f18'..'\u0f19' | '\u0f20'..'\u0f29' | '\u0f35' | '\u0f37' | '\u0f39' | '\u0f3e'..'\u0f3f' | '\u0f71'..'\u0f84' | '\u0f86'..'\u0f87' | '\u0f8d'..'\u0f97' | '\u0f99'..'\u0fbc' | '\u0fc6' | '\u102b'..'\u103e' | '\u1040'..'\u1049' | '\u1056'..'\u1059' | '\u105e'..'\u1060' | '\u1062'..'\u1064' | '\u1067'..'\u106d' | '\u1071'..'\u1074' | '\u1082'..'\u108d' | '\u108f'..'\u109d' | '\u135d'..'\u135f' | '\u1712'..'\u1714' | '\u1732'..'\u1734' | '\u1752'..'\u1753' | '\u1772'..'\u1773' | '\u17b4'..'\u17d3' | '\u17dd' | '\u17e0'..'\u17e9' | '\u180b'..'\u180e' | '\u1810'..'\u1819' | '\u18a9' | '\u1920'..'\u192b' | '\u1930'..'\u193b' | '\u1946'..'\u194f' | '\u19d0'..'\u19d9' | '\u1a17'..'\u1a1b' | '\u1a55'..'\u1a5e' | '\u1a60'..'\u1a7c' | '\u1a7f'..'\u1a89' | '\u1a90'..'\u1a99' | '\u1ab0'..'\u1abd' | '\u1b00'..'\u1b04' | '\u1b34'..'\u1b44' | '\u1b50'..'\u1b59' | '\u1b6b'..'\u1b73' | '\u1b80'..'\u1b82' | '\u1ba1'..'\u1bad' | '\u1bb0'..'\u1bb9' | '\u1be6'..'\u1bf3' | '\u1c24'..'\u1c37' | '\u1c40'..'\u1c49' | '\u1c50'..'\u1c59' | '\u1cd0'..'\u1cd2' | '\u1cd4'..'\u1ce8' | '\u1ced' | '\u1cf2'..'\u1cf4' | '\u1cf8'..'\u1cf9' | '\u1dc0'..'\u1df5' | '\u1dfc'..'\u1dff' | '\u200b'..'\u200f' | '\u202a'..'\u202e' | '\u203f'..'\u2040' | '\u2054' | '\u2060'..'\u2064' | '\u2066'..'\u206f' | '\u20d0'..'\u20dc' | '\u20e1' | '\u20e5'..'\u20f0' | '\u2cef'..'\u2cf1' | '\u2d7f' | '\u2de0'..'\u2dff' | '\u302a'..'\u302f' | '\u3099'..'\u309a' | '\ua620'..'\ua629' | '\ua66f' | '\ua674'..'\ua67d' | '\ua69e'..'\ua69f' | '\ua6f0'..'\ua6f1' | '\ua802' | '\ua806' | '\ua80b' | '\ua823'..'\ua827' | '\ua880'..'\ua881' | '\ua8b4'..'\ua8c4' | '\ua8d0'..'\ua8d9' | '\ua8e0'..'\ua8f1' | '\ua900'..'\ua909' | '\ua926'..'\ua92d' | '\ua947'..'\ua953' | '\ua980'..'\ua983' | '\ua9b3'..'\ua9c0' | '\ua9d0'..'\ua9d9' | '\ua9e5' | '\ua9f0'..'\ua9f9' | '\uaa29'..'\uaa36' | '\uaa43' | '\uaa4c'..'\uaa4d' | '\uaa50'..'\uaa59' | '\uaa7b'..'\uaa7d' | '\uaab0' | '\uaab2'..'\uaab4' | '\uaab7'..'\uaab8' | '\uaabe'..'\uaabf' | '\uaac1' | '\uaaeb'..'\uaaef' | '\uaaf5'..'\uaaf6' | '\uabe3'..'\uabea' | '\uabec'..'\uabed' | '\uabf0'..'\uabf9' | '\ufb1e' | '\ufe00'..'\ufe0f' | '\ufe20'..'\ufe2f' | '\ufe33'..'\ufe34' | '\ufe4d'..'\ufe4f' | '\ufeff' | '\uff10'..'\uff19' | '\uff3f' | '\ufff9'..'\ufffb' | '\ud800' ( '\uddfd' | '\udee0' | '\udf76'..'\udf7a' ) | '\ud801' '\udca0'..'\udca9' | '\ud802' ( '\ude01'..'\ude03' | '\ude05'..'\ude06' | '\ude0c'..'\ude0f' | '\ude38'..'\ude3a' | '\ude3f' | '\udee5'..'\udee6' ) | '\ud804' ( '\udc00'..'\udc02' | '\udc38'..'\udc46' | '\udc66'..'\udc6f' | '\udc7f'..'\udc82' | '\udcb0'..'\udcba' | '\udcbd' | '\udcf0'..'\udcf9' | '\udd00'..'\udd02' | '\udd27'..'\udd34' | '\udd36'..'\udd3f' | '\udd73' | '\udd80'..'\udd82' | '\uddb3'..'\uddc0' | '\uddca'..'\uddcc' | '\uddd0'..'\uddd9' | '\ude2c'..'\ude37' | '\udedf'..'\udeea' | '\udef0'..'\udef9' | '\udf00'..'\udf03' | '\udf3c' | '\udf3e'..'\udf44' | '\udf47'..'\udf48' | '\udf4b'..'\udf4d' | '\udf57' | '\udf62'..'\udf63' | '\udf66'..'\udf6c' | '\udf70'..'\udf74' ) | '\ud805' ( '\udcb0'..'\udcc3' | '\udcd0'..'\udcd9' | '\uddaf'..'\uddb5' | '\uddb8'..'\uddc0' | '\udddc'..'\udddd' | '\ude30'..'\ude40' | '\ude50'..'\ude59' | '\udeab'..'\udeb7' | '\udec0'..'\udec9' | '\udf1d'..'\udf2b' | '\udf30'..'\udf39' ) | '\ud806' '\udce0'..'\udce9' | '\ud81a' ( '\ude60'..'\ude69' | '\udef0'..'\udef4' | '\udf30'..'\udf36' | '\udf50'..'\udf59' ) | '\ud81b' ( '\udf51'..'\udf7e' | '\udf8f'..'\udf92' ) | '\ud82f' ( '\udc9d'..'\udc9e' | '\udca0'..'\udca3' ) | '\ud834' ( '\udd65'..'\udd69' | '\udd6d'..'\udd82' | '\udd85'..'\udd8b' | '\uddaa'..'\uddad' | '\ude42'..'\ude44' ) | '\ud835' '\udfce'..'\udfff' | '\ud836' ( '\ude00'..'\ude36' | '\ude3b'..'\ude6c' | '\ude75' | '\ude84' | '\ude9b'..'\ude9f' | '\udea1'..'\udeaf' ) | '\ud83a' '\udcd0'..'\udcd6' | '\udb40' ( '\udc01' | '\udc20'..'\udc7f' | '\udd00'..'\uddef' ) ;

NUMBER                     : [-+]? (DECIMAL_DIGIT* DOT? DECIMAL_DIGIT+ ([Ee] [-+]? DECIMAL_DIGIT+)? | '0' [Xx] HEX_DIGIT+) [UuLlFfDdMm]* ;
fragment DECIMAL_DIGIT     : '0'..'9' ;
fragment HEX_DIGIT         : '0'..'9' | 'A'..'F' | 'a'..'f' ;

STR                        : REGULAR_STRING | VERBATIM_STRING ;
fragment REGULAR_STRING    : '"' (~["\\\r\n\u0085\u2028\u2029] | ESCAPE)* '"' ;
fragment VERBATIM_STRING   : '@' '"' (~["] | '""')* '"' ;
fragment ESCAPE            : '\\' ~[xUu] | '\\x' HEX_DIGIT+ | '\\u' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT | '\\U' HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT HEX_DIGIT ;

INTERPOLATED_START         : '$"' -> more, pushMode(INTERPOLATED) ;

CHARACTER                  : '\'' (~['\\\r\n\u0085\u2028\u2029] | ESCAPE) '\'' ;

PREPROCESSOR               : '#' (~[\r\n\u0085\u2028\u2029])* -> skip ;

mode INTERPOLATED;
INTERPOLATED_BRACES        : '{{' -> more ;
INTERPOLATED_BRACE         : '{' -> more, pushMode(INTERPOLATED_EXP) ;
INTERPOLATED_CHARS         : (~["{\\\r\n\u0085\u2028\u2029] | ESCAPE)+ -> more ;
INTERPOLATED_END           : '"' -> type(STR), popMode ;

mode INTERPOLATED_SUB;
INTERPOLATED_SUB_BRACE     : '{' -> more, pushMode(INTERPOLATED_EXP) ;
INTERPOLATED_SUB_CHARS     : (~["{\\\r\n\u0085\u2028\u2029] | ESCAPE)+ -> more ;
INTERPOLATED_SUB_STRING    : '"' -> more, popMode ;

mode INTERPOLATED_EXP;
INTERPOLATED_EXP_BRACE     : '{' -> more, pushMode(INTERPOLATED_EXP) ;
INTERPOLATED_EXP_CHARS     : (STR | ~["@${}]+) -> more ;
INTERPOLATED_EXP_START     : '$"' -> more, pushMode(INTERPOLATED_SUB) ;
INTERPOLATED_EXP_END       : '}' -> more, popMode;
