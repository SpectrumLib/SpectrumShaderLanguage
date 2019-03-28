lexer grammar SSLLexer;

// String literals
STRING_LITERAL
	: '"' (~('"'|'\n'|'\r'))* '"'
	;

// Keywords
KW_SHADER : 'shader' ;

// Punctuators
SEMI_COLON : ';' ;

// Operators

// Character Types
fragment AlphaChar : [a-zA-Z] ;
fragment DigitChar : [0-9] ;
fragment AlphaNumericChar : AlphaChar | DigitChar ;

// Whitespace And Comments(ignore)
WS 
	: [ \t\r\n\u000C]+ -> channel(HIDDEN)
	;

COMMENT
	: '/*' .*? '*/' -> channel(HIDDEN)
	;

LINECOMMENT
	: '//' ~[\r\n]* '\r'? '\n' -> channel(HIDDEN)
	;
