grammar ExactColumns;

root : row* EOF;
row  : WS? DOUBLE (cell (SINGLE cell)*)? DOUBLE WS? ;
cell : WS? text WS? ;
text : TEXT? ;

TEXT   : '"' ('""' | ~'"')* '"' | ~[ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\u2502\u2551]+ (WS ~[ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\u2502\u2551]+)* ;
WS     : [ \t\r\n\f\u000b\u0085\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000]+ ;
SINGLE : '\u2502' ;
DOUBLE : '\u2551' ;
