lexer grammar SSLLexer;

// String literals
STRING_LITERAL
    : '"' (~('"'|'\n'|'\r'))* '"'
    ;

// Numeric literals
INTEGER_LITERAL
    : DecimalLiteral
    | HexLiteral
    ;
FLOAT_LITERAL
    : DigitChar* '.' DigitChar+ ExponentPart?
    ;
fragment DecimalLiteral     : DigitChar+ ;
fragment HexLiteral         : '0x' HexDigitChar+ ;
fragment ExponentPart       : [eE] ('-'|'+')? DigitChar+ ;

// Keywords
KW_BLOCK        : 'block' ;
KW_SHADER       : 'shader' ;
KW_UNIFORM      : 'uniform' ;

// Type keywords
TYPE_KEYWORD
    : DATA_TYPE_KEYWORD
    ;
DATA_TYPE_KEYWORD
    : KWT_BOOL | KWT_INT | KWT_UINT | KWT_FLOAT | KWT_DOUBLE
    | KWT_BOOL2 | KWT_INT2 | KWT_UINT2 | KWT_FLOAT2 | KWT_DOUBLE2
    | KWT_BOOL3 | KWT_INT3 | KWT_UINT3 | KWT_FLOAT3 | KWT_DOUBLE3
    | KWT_BOOL4 | KWT_INT4 | KWT_UINT4 | KWT_FLOAT4 | KWT_DOUBLE4
    | KWT_MAT2 | KWT_MAT3 | KWT_MAT4
    ;
KWT_BOOL        : 'bool' ;
KWT_INT         : 'int' ;
KWT_UINT        : 'uint' ;
KWT_FLOAT       : 'float' ;
KWT_DOUBLE      : 'double' ;
KWT_BOOL2       : 'bvec2' ;
KWT_INT2        : 'ivec2' ;
KWT_UINT2       : 'uvec2' ;
KWT_FLOAT2      : 'vec2' ;
KWT_DOUBLE2     : 'dvec2' ;
KWT_BOOL3       : 'bvec3' ;
KWT_INT3        : 'ivec3' ;
KWT_UINT3       : 'uvec3' ;
KWT_FLOAT3      : 'vec3' ;
KWT_DOUBLE3     : 'dvec3' ;
KWT_BOOL4       : 'bvec4' ;
KWT_INT4        : 'ivec4' ;
KWT_UINT4       : 'uvec4' ;
KWT_FLOAT4      : 'vec4' ;
KWT_DOUBLE4     : 'dvec4' ;
KWT_MAT2        : 'mat2' ;
KWT_MAT3        : 'mat3' ;
KWT_MAT4        : 'mat4' ;

// Identifiers (variable and member names)
IDENTIFIER
    : AlphaChar (AlphaNumericChar|'_')*
    ;

// Punctuators
LBRACE          : '{' ;
RBRACE          : '}' ;
LBRACKET        : '[' ;
RBRACKET        : ']' ;
COMMA           : ',' ;
DOUBLE_QUOTE    : '"' ;
LPAREN          : '(' ;
RPAREN          : ')' ;
PERIOD          : '.' ;
SEMI_COLON      : ';' ;

// Operators
OP_ASSIGN       : '=' ;

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

// Character Types
fragment AlphaChar          : [a-zA-Z] ;
fragment DigitChar          : [0-9] ;
fragment AlphaNumericChar   : AlphaChar | DigitChar ;
fragment HexDigitChar       : [a-fA-F0-9];
