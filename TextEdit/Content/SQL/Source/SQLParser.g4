parser grammar SQLParser;

options { tokenVocab = SQLLexer; }

document           : stmt* EOF ;
selectentry        : select? EOF ;

stmt               : (ddl | tsql | trans | cursor | query) SEMICOLON? ;

ddl                : alterauthorization | alterdatabase | alterlogin | alterrole | altertable | createdatabase | createdefault | createfunc | createindex | createlogin | createproc | createrole | createschema | createsequence | createtable | createtrigger | createtype | createuser | createview | drop | grant ;
drop               : DROP (TABLE | TRIGGER | PROCEDURE | INDEX | VIEW | FUNCTION | TYPE | SEQUENCE | DEFAULT | SCHEMA | ROLE | USER | LOGIN | DATABASE) name (ON name)? withlimits? ;
alterauthorization : ALTER AUTHORIZATION ON ((DATABASE | ROLE | TYPE) COLON COLON)? name TO (name | SCHEMA OWNER) ;
alterdatabase      : ALTER DATABASE name set ;
alterlogin         : ALTER LOGIN name DISABLE ;
alterrole          : ALTER SERVER? ROLE name ADD MEMBER name ;
altertable         : ALTER TABLE name (WITH (CHECK | NOCHECK))? (ADD tabledecl | (CHECK | NOCHECK) CONSTRAINT name | DROP CONSTRAINT name) ;
tabledecl          : name type (DEFAULT expr)? (REFERENCES name LPAREN name RPAREN)? | constraint | fkconstraint | defaultfor ;
defaultfor         : (CONSTRAINT name)? DEFAULT expr FOR name ;
constraint         : (PRIMARY KEY | CONSTRAINT name (PRIMARY KEY | UNIQUE)?) (CLUSTERED | NONCLUSTERED)? (LPAREN constraintitem (COMMA constraintitem)* RPAREN | CHECK conditional) withlimits? onlimits ;
onlimits           : (ON name | TEXTIMAGE_ON name)* ;
constraintitem     : name ASC? ;
fkconstraint       : (FOREIGN KEY | CONSTRAINT name FOREIGN KEY) LPAREN name (COMMA name)* RPAREN REFERENCES name LPAREN name (COMMA name)* RPAREN (ON (UPDATE | DELETE) CASCADE)* withlimits? onlimits (NOT? FOR REPLICATION)? ;
createdatabase     : CREATE DATABASE name (CONTAINMENT EQUALS NONE ON | PRIMARY createdbparams | LOG ON createdbparams | COLLATE name)* ;
createdbparams     : LPAREN createdbparam (COMMA createdbparam)* RPAREN ;
createdbparam      : (NAME | FILENAME | SIZE | MAXSIZE | FILEGROWTH) EQUALS (expr | UNLIMITED | NUMBER (KB | GB)) ;
createdefault      : CREATE DEFAULT name AS expr ;
createfunc         : CREATE FUNCTION name LPAREN vardecls? RPAREN RETURNS name? type (WITH EXECUTE AS CALLER)? AS? stmt ;
createindex        : CREATE (UNIQUE | CLUSTERED | NONCLUSTERED)* INDEX name ON name LPAREN indexitem (COMMA indexitem)* RPAREN (INCLUDE LPAREN names RPAREN)? whereclause? withlimits? onlimits ;
indexitem          : name (ASC | DESC)? ;
createlogin        : CREATE LOGIN name (FROM WINDOWS)? WITH createloginparam (COMMA createloginparam)* ;
createloginparam   : name EQUALS (expr | OFF | ON) ;
createproc         : (CREATE | ALTER) PROCEDURE name LPAREN? vardecls? RPAREN? (WITH (EXECUTE AS SELF | RECOMPILE))? AS stmt ;
createrole         : CREATE ROLE name ;
createschema       : CREATE SCHEMA name (AUTHORIZATION name)? ;
createsequence     : CREATE SEQUENCE name AS name (START WITH expr | INCREMENT BY expr | MINVALUE expr | MAXVALUE expr | CACHE)* ;
createtable        : CREATE TABLE name LPAREN (tabledecl COMMA?)* RPAREN onlimits withlimits? ;
createtrigger      : CREATE TRIGGER name ON name (FOR | AFTER) triggertype (COMMA triggertype)* AS stmt ;
triggertype        : INSERT | UPDATE | DELETE ;
createtype         : (CREATE | ALTER) TYPE name AS type ;
createuser         : CREATE USER name (FOR LOGIN name | WITH DEFAULT_SCHEMA EQUALS name)* ;
createview         : CREATE VIEW name AS stmt ;
grant              : (GRANT | DENY) (ALTER | CONNECT | CONTROL | DELETE | EXECUTE | INSERT | REFERENCES | SELECT | TAKE OWNERSHIP | UPDATE | VIEW CHANGE TRACKING | VIEW DEFINITION) (ON ((SCHEMA | TYPE) COLON COLON)? name (LPAREN names RPAREN)?)? TO name (WITH GRANT OPTION)? (AS name)? ;
vardecls           : vardecl (COMMA vardecl)* ;
vardecl            : name AS? type (EQUALS expr)? READONLY? OUTPUT? ;
withlimits         : WITH LPAREN withlimit (COMMA withlimit)* RPAREN ;
withlimit          : name EQUALS (expr | ON | OFF) ;
type               : stdtype | tabletype | cursortype ;
stdtype            : name (LPAREN (NUMBER | MAX) (COMMA NUMBER)* RPAREN)? (COLLATE name | PRIMARY KEY | IDENTITY (LPAREN NUMBER COMMA NUMBER RPAREN)? | ROWGUIDCOL | NOT? NULL | NOT? FOR REPLICATION | CONSTRAINT name)* CLUSTERED? ;
tabletype          : TABLE (LPAREN tabledecl (COMMA tabledecl)* RPAREN)? onlimits ;
cursortype         : SCROLL? CURSOR (LOCAL | GLOBAL | SCROLL | FORWARD_ONLY | STATIC | FAST_FORWARD)* FOR select (FOR READ ONLY)? ;

tsql               : backup | begincatch | begintry | block | break | continue | declare | endcatch | endtry | execute | go | goto | if | label | print | raiserror | return | set | throw | use | waitfor | while ;
backup             : BACKUP LOG name WITH NO_LOG ;
begincatch         : BEGIN CATCH ;
begintry           : BEGIN TRY ;
block              : BEGIN stmt* END ;
break              : BREAK ;
continue           : CONTINUE ;
declare            : DECLARE vardecls ;
endcatch           : END CATCH ;
endtry             : END TRY ;
execute            : EXECUTE ((name EQUALS)? name (executeparam (COMMA executeparam)*)? | LPAREN expr RPAREN) ;
executeparam       : (name EQUALS)? (expr | DEFAULT) OUTPUT? ;
go                 : GO ;
goto               : GOTO name ;
if                 : IF conditional (stmt | expr) (ELSE stmt)? ;
label              : name COLON ;
print              : PRINT expr ;
raiserror          : RAISERROR LPAREN exprs RPAREN (WITH NOWAIT)? ;
return             : RETURN expr? ;
set                : SET (TRANSACTION ISOLATION LEVEL (READ (COMMITTED | UNCOMMITTED) | SERIALIZABLE) | (names | IDENTITY_INSERT name) (ON | OFF | GLOBAL) | name EQUALS? (expr | NUMBER SECONDS) | FILESTREAM LPAREN name EQUALS (ON | OFF) RPAREN) ;
throw              : THROW expr COMMA expr COMMA expr ;
use                : USE name ;
waitfor            : WAITFOR DELAY expr ;
while              : WHILE conditional stmt ;

trans              : begintrans | commit | rollback | savetrans ;
begintrans         : BEGIN TRANSACTION name? ;
commit             : COMMIT TRANSACTION? name? ;
rollback           : ROLLBACK TRANSACTION? name? ;
savetrans          : SAVE TRANSACTION name? ;

cursor             : close | deallocate | fetch | open ;
close              : CLOSE name ;
deallocate         : DEALLOCATE name ;
fetch              : FETCH (NEXT | PRIOR | FIRST | LAST | ABSOLUTE expr | RELATIVE expr) FROM name INTO names ;
open               : OPEN name ;

query              : (LPAREN query RPAREN | delete | insert | merge | select | truncate | update | with) ((UNION | EXCEPT) ALL? query)? ;
delete             : DELETE FROM? name outputclause? sourceclause? whereclause? ;
insert             : INSERT INTO? name (LPAREN names RPAREN)? outputclause? (VALUES LPAREN exprs RPAREN (COMMA LPAREN exprs RPAREN)* | select | LPAREN select RPAREN | execute) ;
merge              : MERGE INTO? selecttable alias? USING selecttable (ON conditional)? alias? (LPAREN names RPAREN)? ON conditional (WHEN conditional THEN (UPDATE setclauses | DELETE | INSERT (LPAREN names RPAREN)? VALUES LPAREN exprs RPAREN))* outputclause? ;
select             : selects (INTO name)? sourceclause? whereclause? groupbyclause? havingclause? orderbyclause? forxml? optionclause? ;
selects            : SELECT DISTINCT? (TOP (LPAREN expr RPAREN | expr))? selected (COMMA selected)* ;
selected           : (name EQUALS)? Selected=expr alias? ;
forxml             : FOR XML PATH LPAREN expr RPAREN (COMMA TYPE)? (COMMA ROOT)? ;
truncate           : TRUNCATE TABLE name ;
update             : UPDATE (TOP LPAREN expr RPAREN)? selecttable setclauses outputclause? sourceclause? whereclause? ;
with               : SEMICOLON? WITH withclause (COMMA withclause)* stmt ;
withclause         : name (LPAREN names RPAREN)? AS LPAREN stmt RPAREN ;
sourceclause       : FROM (fromclause | joinclause | applyclause | pivotclause)+ ;
fromclause         : selecttable (COMMA selecttable)* ;
joinclause         : JoinType=jointype selecttable (ON Condition=conditional)? ;
jointype           : ((FULL | LEFT) OUTER? | INNER | CROSS)? JOIN ;
applyclause        : ApplyType=applytype selecttable ;
applytype          : (CROSS | OUTER) APPLY ;
pivotclause        : PIVOT LPAREN expr FOR name IN LPAREN names RPAREN RPAREN alias ;
selecttable        : Table=expr alias? (WITH? LPAREN selectwith (COMMA selectwith)* RPAREN)? ;
selectwith         : NOLOCK | READPAST | ROWLOCK | UPDLOCK | XLOCK ;
whereclause        : WHERE Condition=conditional ;
groupbyclause      : GROUP BY groupbyitem (COMMA groupbyitem)* ;
groupbyitem        : expr ;
havingclause       : HAVING Condition=conditional ;
orderbyclause      : ORDER BY orderbyitem (COMMA orderbyitem)* ;
orderbyitem        : Expr=expr Direction=(ASC | DESC)? ;
optionclause       : OPTION LPAREN (RECOMPILE | MAXDOP NUMBER) RPAREN ;
outputclause       : OUTPUT outputitems (INTO name (LPAREN names RPAREN)?)? ;
outputitems        : outputitem (COMMA outputitem)* ;
outputitem         : expr alias? ;
setclauses         : SET setclause (COMMA setclause)* ;
setclause          : name EQUALS expr ;
alias              : AS? Alias=aliastext ;
aliastext          : name (LPAREN names RPAREN)? ;

identifier         : NAME | STRING | ABSOLUTE | AFTER | APPLY | CACHE | CALLER | CAST | CATCH | CHANGE | COMMITTED | CONNECT | CONTAINMENT | CONTROL | DEFAULT_SCHEMA | DEFINITION | DELAY | DISABLE | FAST_FORWARD | FILEGROWTH | FILENAME | FILESTREAM | FIRST | GB | GLOBAL | GO | IIF | INCLUDE | INCREMENT | ISOLATION | KB | LAST | LEVEL | LOCAL | LOG | LOGIN | MATCHED | MAX | MAXDOP | MAXSIZE | MAXVALUE | MEMBER | MINVALUE | NEXT | NO_LOG | NOLOCK | NONE | NOWAIT | OUTPUT | OWNER | OWNERSHIP | PARTITION | PATH | PRIOR | READONLY | READPAST | RECOMPILE | RELATIVE | RETURNS | ROLE | ROOT | ROWLOCK | SCROLL | SECONDS | SELF | SEQUENCE | SERIALIZABLE | SERVER | SIZE | SOURCE | START | STATIC | TAKE | TARGET | TEXTIMAGE_ON | THROW | TRACKING | TRY | TRY_CAST | TYPE | UNCOMMITTED | UNLIMITED | UPDLOCK | VALUE | WINDOWS | XLOCK | XML ;
name               : DOT* identifier (DOT+ identifier)* ;
names              : name (COMMA name)* ;

expr               : LPAREN expr RPAREN
                   | LPAREN stmt RPAREN
                   | LPAREN VALUES LPAREN exprs RPAREN (COMMA LPAREN exprs RPAREN)* RPAREN
                   | (PLUS | MINUS | TILDE) expr
                   | expr (EXPROP | ASTERISK | PLUS | MINUS) expr
                   | (name | LEFT) LPAREN (DISTINCT? exprs)? RPAREN
                   | expr DOT (name | LEFT) LPAREN (exprs)? RPAREN
                   | CASE (WHEN conditional THEN expr)+ (ELSE expr)? END
                   | CASE expr (WHEN expr THEN expr)+ (ELSE expr)? END
                   | (CAST | TRY_CAST) LPAREN expr AS type RPAREN
                   | IIF LPAREN conditional COMMA expr COMMA expr RPAREN
                   | expr OVER LPAREN (PARTITION BY exprs | orderbyclause)* RPAREN
                   | NEXT VALUE FOR name
                   | name
                   | name DOT ASTERISK
                   | ACTION
                   | ASTERISK
                   | STRING
                   | NUMBER
                   | HEX
                   | NULL
                   ;
exprs              : expr (COMMA expr)* ;

conditional        : LPAREN conditional RPAREN
                   | NOT conditional
                   | conditional (AND | OR) conditional
                   | UPDATE LPAREN name RPAREN
                   | expr (CONDITIONALOP | EQUALS) expr
                   | expr NOT? IN (expr | LPAREN exprs RPAREN)
                   | expr BETWEEN expr AND expr
                   | expr IS NOT? NULL
                   | NOT? EXISTS LPAREN select RPAREN
                   | expr NOT? LIKE expr
                   | NOT? MATCHED (BY (TARGET | SOURCE))?
                   ;
