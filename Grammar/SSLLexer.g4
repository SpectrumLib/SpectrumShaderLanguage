lexer grammar SSLLexer;

// String literals
STRING_LITERAL
    : '"' (~('"'|'\n'|'\r'))* '"'
    ;

// Boolean literals
BOOLEAN_LITERAL
    : KW_TRUE
    | KW_FALSE
    ;
KW_TRUE     : 'true' ;
KW_FALSE    : 'false' ;

// Keywords
KW_BLOCK            : 'block' ;
KW_CONST            : 'const' ;
KW_IN               : 'in' ;
KW_INOUT            : 'inout' ;
KW_INPUT            : 'input' ;
KW_LOCALS           : 'locals' ;
KW_OUT              : 'out' ;
KW_OUTPUT           : 'output' ;
KW_SHADER           : 'shader' ;
KW_UNIFORM          : 'uniform' ;

// Shader stage function keywords
KW_STAGE_VERT       : '@vert' ;
KW_STAGE_FRAG       : '@frag' ;

// Type keywords
KWT_VOID        : 'void' ;
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

// Swizzles
SWIZZLE
    : '.' ([xyzw]+ | [rbga]+ | [stpq]+)
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

// Identifiers (variable and member names)
IDENTIFIER
    : AlphaChar (AlphaNumericChar|'_')*
    ;

// Punctuators
LBRACE          : '{' ;
RBRACE          : '}' ;
LBRACKET        : '[' ;
RBRACKET        : ']' ;
COLON           : ':' ;
COMMA           : ',' ;
DOUBLE_QUOTE    : '"' ;
LPAREN          : '(' ;
Q_MARK          : '?' ;
RPAREN          : ')' ;
PERIOD          : '.' ;
SEMI_COLON      : ';' ;

// Operators
OP_ASSIGN       : '=' ;
OP_ADD_ASSIGN   : '+=' ;
OP_SUB_ASSIGN   : '-=' ;
OP_MUL_ASSIGN   : '*=' ;
OP_DIV_ASSIGN   : '/=' ;
OP_ADD          : '+' ;
OP_SUB          : '-' ;
OP_MUL          : '*' ;
OP_DIV          : '/' ;
OP_AND          : '&&' ;
OP_OR           : '||' ;
OP_XOR          : '^^' ;
OP_INC          : '++' ;
OP_DEC          : '--' ;
OP_BANG         : '!' ;
OP_LT           : '<' ;
OP_GT           : '>' ;
OP_LE           : '<=' ;
OP_GE           : '>=' ;
OP_EQ           : '==' ;
OP_NE           : '!=' ;

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
