lexer grammar SQLLexer;

ABSOLUTE        : A B S O L U T E                 ;
ACTION          : '$' A C T I O N                 ;
ADD             : A D D                           ;
AFTER           : A F T E R                       ;
ALL             : A L L                           ;
ALTER           : A L T E R                       ;
AND             : A N D                           ;
APPLY           : A P P L Y                       ;
AS              : A S                             ;
ASC             : A S C                           ;
AUTHORIZATION   : A U T H O R I Z A T I O N       ;
BACKUP          : B A C K U P                     ;
BEGIN           : B E G I N                       ;
BETWEEN         : B E T W E E N                   ;
BREAK           : B R E A K                       ;
BY              : B Y                             ;
CACHE           : C A C H E                       ;
CALLER          : C A L L E R                     ;
CASCADE         : C A S C A D E                   ;
CASE            : C A S E                         ;
CAST            : C A S T                         ;
CATCH           : C A T C H                       ;
CHANGE          : C H A N G E                     ;
CHECK           : C H E C K                       ;
CLOSE           : C L O S E                       ;
CLUSTERED       : C L U S T E R E D               ;
COLLATE         : C O L L A T E                   ;
COMMIT          : C O M M I T                     ;
COMMITTED       : C O M M I T T E D               ;
CONNECT         : C O N N E C T                   ;
CONSTRAINT      : C O N S T R A I N T             ;
CONTAINMENT     : C O N T A I N M E N T           ;
CONTINUE        : C O N T I N U E                 ;
CONTROL         : C O N T R O L                   ;
CREATE          : C R E A T E                     ;
CROSS           : C R O S S                       ;
CURSOR          : C U R S O R                     ;
DATABASE        : D A T A B A S E                 ;
DEALLOCATE      : D E A L L O C A T E             ;
DECLARE         : D E C L A R E                   ;
DEFAULT         : D E F A U L T                   ;
DEFAULT_SCHEMA  : D E F A U L T '_' S C H E M A   ;
DEFINITION      : D E F I N I T I O N             ;
DELAY           : D E L A Y                       ;
DELETE          : D E L E T E                     ;
DENY            : D E N Y                         ;
DESC            : D E S C                         ;
DISABLE         : D I S A B L E                   ;
DISTINCT        : D I S T I N C T                 ;
DROP            : D R O P                         ;
ELSE            : E L S E                         ;
END             : E N D                           ;
EXCEPT          : E X C E P T                     ;
EXECUTE         : E X E C (U T E)?                ;
EXISTS          : E X I S T S                     ;
FAST_FORWARD    : F A S T '_' F O R W A R D       ;
FETCH           : F E T C H                       ;
FILEGROWTH      : F I L E G R O W T H             ;
FILENAME        : F I L E N A M E                 ;
FILESTREAM      : F I L E S T R E A M             ;
FIRST           : F I R S T                       ;
FOR             : F O R                           ;
FOREIGN         : F O R E I G N                   ;
FORWARD_ONLY    : F O R W A R D '_' O N L Y       ;
FROM            : F R O M                         ;
FULL            : F U L L                         ;
FUNCTION        : F U N C T I O N                 ;
GB              : G B                             ;
GLOBAL          : G L O B A L                     ;
GO              : G O                             ;
GOTO            : G O T O                         ;
GRANT           : G R A N T                       ;
GROUP           : G R O U P                       ;
HAVING          : H A V I N G                     ;
IDENTITY        : I D E N T I T Y                 ;
IDENTITY_INSERT : I D E N T I T Y '_' I N S E R T ;
IF              : I F                             ;
IIF             : I I F                           ;
IN              : I N                             ;
INCLUDE         : I N C L U D E                   ;
INCREMENT       : I N C R E M E N T               ;
INDEX           : I N D E X                       ;
INNER           : I N N E R                       ;
INSERT          : I N S E R T                     ;
INTO            : I N T O                         ;
IS              : I S                             ;
ISOLATION       : I S O L A T I O N               ;
JOIN            : J O I N                         ;
KB              : K B                             ;
KEY             : K E Y                           ;
LAST            : L A S T                         ;
LEFT            : L E F T                         ;
LEVEL           : L E V E L                       ;
LIKE            : L I K E                         ;
LOCAL           : L O C A L                       ;
LOG             : L O G                           ;
LOGIN           : L O G I N                       ;
MATCHED         : M A T C H E D                   ;
MAX             : M A X                           ;
MAXDOP          : M A X D O P                     ;
MAXSIZE         : M A X S I Z E                   ;
MAXVALUE        : M A X V A L U E                 ;
MEMBER          : M E M B E R                     ;
MERGE           : M E R G E                       ;
MINVALUE        : M I N V A L U E                 ;
NEXT            : N E X T                         ;
NO_LOG          : N O '_' L O G                   ;
NOCHECK         : N O C H E C K                   ;
NOLOCK          : N O L O C K                     ;
NONCLUSTERED    : N O N C L U S T E R E D         ;
NONE            : N O N E                         ;
NOT             : N O T                           ;
NOWAIT          : N O W A I T                     ;
NULL            : N U L L                         ;
OFF             : O F F                           ;
ON              : O N                             ;
ONLY            : O N L Y                         ;
OPEN            : O P E N                         ;
OPTION          : O P T I O N                     ;
OR              : O R                             ;
ORDER           : O R D E R                       ;
OUTER           : O U T E R                       ;
OUTPUT          : O U T (P U T)?                  ;
OVER            : O V E R                         ;
OWNER           : O W N E R                       ;
OWNERSHIP       : O W N E R S H I P               ;
PARTITION       : P A R T I T I O N               ;
PATH            : P A T H                         ;
PIVOT           : P I V O T                       ;
PRIMARY         : P R I M A R Y                   ;
PRINT           : P R I N T                       ;
PRIOR           : P R I O R                       ;
PROCEDURE       : P R O C (E D U R E)?            ;
RAISERROR       : R A I S E R R O R               ;
READ            : R E A D                         ;
READONLY        : R E A D O N L Y                 ;
READPAST        : R E A D P A S T                 ;
RECOMPILE       : R E C O M P I L E               ;
REFERENCES      : R E F E R E N C E S             ;
RELATIVE        : R E L A T I V E                 ;
REPLICATION     : R E P L I C A T I O N           ;
RETURN          : R E T U R N                     ;
RETURNS         : R E T U R N S                   ;
ROLE            : R O L E                         ;
ROLLBACK        : R O L L B A C K                 ;
ROOT            : R O O T                         ;
ROWGUIDCOL      : R O W G U I D C O L             ;
ROWLOCK         : R O W L O C K                   ;
SAVE            : S A V E                         ;
SCHEMA          : S C H E M A                     ;
SCROLL          : S C R O L L                     ;
SECONDS         : S E C O N D S                   ;
SELECT          : S E L E C T                     ;
SELF            : S E L F                         ;
SEQUENCE        : S E Q U E N C E                 ;
SERIALIZABLE    : S E R I A L I Z A B L E         ;
SERVER          : S E R V E R                     ;
SET             : S E T                           ;
SIZE            : S I Z E                         ;
SOURCE          : S O U R C E                     ;
START           : S T A R T                       ;
STATIC          : S T A T I C                     ;
TABLE           : T A B L E                       ;
TAKE            : T A K E                         ;
TARGET          : T A R G E T                     ;
TEXTIMAGE_ON    : T E X T I M A G E '_' O N       ;
THEN            : T H E N                         ;
THROW           : T H R O W                       ;
TO              : T O                             ;
TOP             : T O P                           ;
TRACKING        : T R A C K I N G                 ;
TRANSACTION     : T R A N (S A C T I O N)?        ;
TRIGGER         : T R I G G E R                   ;
TRUNCATE        : T R U N C A T E                 ;
TRY             : T R Y                           ;
TRY_CAST        : T R Y '_' C A S T               ;
TYPE            : T Y P E                         ;
UNCOMMITTED     : U N C O M M I T T E D           ;
UNION           : U N I O N                       ;
UNIQUE          : U N I Q U E                     ;
UNLIMITED       : U N L I M I T E D               ;
UPDATE          : U P D A T E                     ;
UPDLOCK         : U P D L O C K                   ;
USE             : U S E                           ;
USER            : U S E R                         ;
USING           : U S I N G                       ;
VALUE           : V A L U E                       ;
VALUES          : V A L U E S                     ;
VIEW            : V I E W                         ;
WAITFOR         : W A I T F O R                   ;
WHEN            : W H E N                         ;
WHERE           : W H E R E                       ;
WHILE           : W H I L E                       ;
WINDOWS         : W I N D O W S                   ;
WITH            : W I T H                         ;
XLOCK           : X L O C K                       ;
XML             : X M L                           ;

COMMENT1        : '--' ~[\r\n\u0085\u2028\u2029]* -> skip ;
COMMENT2        : '/*' .*? '*/' -> skip ;
WS              : [ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ -> skip ;
LPAREN          : '(' ;
RPAREN          : ')' ;
SEMICOLON       : ';' ;
COLON           : ':' ;
EQUALS          : '=' ;
PLUS            : '+' ;
MINUS           : '-' ;
TILDE           : '~' ;
EXPROP          : [/%&|] ;
CONDITIONALOP   : [<>] '='? | '<>' | '!=' ;
NUMBER          : [0-9]+ ('.' [0-9]*)? | '.' [0-9]+ ;
HEX             : '0x' [0-9a-fA-F]+ ;
ASTERISK        : '*' ;
COMMA           : ',' ;
DOT             : '.' ;
NAME            : '#'? [@a-zA-Z_] [@0-9a-zA-Z_]* | '[' ~']'* ']' | '"' ~'"'* '"' ;
STRING          : 'N'? '\'' (~'\'' | '\'\'')* '\'' ;

fragment A      : [Aa] ;
fragment B      : [Bb] ;
fragment C      : [Cc] ;
fragment D      : [Dd] ;
fragment E      : [Ee] ;
fragment F      : [Ff] ;
fragment G      : [Gg] ;
fragment H      : [Hh] ;
fragment I      : [Ii] ;
fragment J      : [Jj] ;
fragment K      : [Kk] ;
fragment L      : [Ll] ;
fragment M      : [Mm] ;
fragment N      : [Nn] ;
fragment O      : [Oo] ;
fragment P      : [Pp] ;
fragment Q      : [Qq] ;
fragment R      : [Rr] ;
fragment S      : [Ss] ;
fragment T      : [Tt] ;
fragment U      : [Uu] ;
fragment V      : [Vv] ;
fragment W      : [Ww] ;
fragment X      : [Xx] ;
fragment Y      : [Yy] ;
fragment Z      : [Zz] ;
