// This is the lexer grammar for the SpectrumShaderLanguage, used by Antlr to generate a lexer for
//    the language definition.
// This file is licensed under the MIT license.
// Copyright (c) Sean Moss 2019

lexer grammar SSLLexer;

// Boolean literals
BOOLEAN_LITERAL
    : 'true'
    | 'false'
    ;

// Keywords
KW_ATTR         : 'attr' ;
KW_BLOCK        : 'block' ;
KW_BREAK        : 'break' ;
KW_CONST        : 'const' ;
KW_CONTINUE     : 'continue' ;
KW_DISCARD      : 'discard' ;
KW_DO           : 'do' ;
KW_ELIF         : 'elif' ;
KW_ELSE         : 'else' ;
KW_FLAT         : 'flat' ;
KW_FOR          : 'for' ;
KW_IF           : 'if' ;
KW_IN           : 'in' ;
KW_INOUT        : 'inout' ;
KW_LOCAL        : 'local' ;
KW_OUT          : 'out' ;
KW_RETURN       : 'return' ;
KW_UNIF         : 'unif' ;
KW_VERSION      : 'version' ;
KW_WHILE        : 'while' ;

// Shader stage function keywords
KW_STAGE_VERT   : '@vert' ;
KW_STAGE_FRAG   : '@frag' ;

// Type keywords (value)
KWT_VOID    : 'void' ;
KWT_BOOL    : 'bool' ;
KWT_BOOL2   : 'bvec2' ;
KWT_BOOL3   : 'bvec3' ;
KWT_BOOL4   : 'bvec4' ;
KWT_INT     : 'int' ;
KWT_INT2    : 'ivec2' ;
KWT_INT3    : 'ivec3' ;
KWT_INT4    : 'ivec4' ;
KWT_UINT    : 'uint' ;
KWT_UINT2   : 'uvec2' ;
KWT_UINT3   : 'uvec3' ;
KWT_UINT4   : 'uvec4' ;
KWT_FLOAT   : 'float' ;
KWT_FLOAT2  : 'vec2' ;
KWT_FLOAT3  : 'vec3' ;
KWT_FLOAT4  : 'vec4' ;
KWT_MAT2    : 'mat2' ;
KWT_MAT3    : 'mat3' ;
KWT_MAT4    : 'mat4' ;

// Type keywords (handle)
KWT_TEX1D           : 'tex1D' ;
KWT_TEX2D           : 'tex2D' ;
KWT_TEX3D           : 'tex3D' ;
KWT_TEXCUBE         : 'texCube' ;
KWT_TEX1D_ARR       : 'tex1DArray' ;
KWT_TEX2D_ARR       : 'tex2DArray' ;
KWT_IMAGE1D         : 'image1D' ;
KWT_IMAGE2D         : 'image2D' ;
KWT_IMAGE3D         : 'image3D' ;
KWT_IMAGE1D_ARR     : 'image1DArray' ;
KWT_IMAGE2D_ARR     : 'image2DArray' ;
KWT_SUBPASSINPUT    : 'subpassInput';

// Swizzles
SWIZZLE
    : '.' ([xyzw]+ | [rbga]+ | [stpq]+)
    ;
fragment SwizzleCharPos         : [xyzw] ;
fragment SwizzleCharColor       : [rgba] ;
fragment SwizzleCharTexCoord    : [stpq] ;

// Numeric literals
INTEGER_LITERAL
    : '-'? DecimalLiteral ('u'|'U')?
    | '-'? HexLiteral ('u'|'U')?
    ;
FLOAT_LITERAL
    : '-'? DigitChar* '.' DigitChar+ ExponentPart?
    ;
VERSION_LITERAL
    : DigitChar+ '.' DigitChar+ '.' DigitChar+
    ;
fragment DecimalLiteral     : DigitChar+ ;
fragment HexLiteral         : '0x' HexDigitChar+ ;
fragment ExponentPart       : [eE] ('-'|'+')? DigitChar+ ;

// Identifiers (variable and member names), also includes the built-in variables
IDENTIFIER
    : (AlphaChar|'_') (AlnumChar|'_')*
    | '$' AlphaChar+
    ;

// Punctuators
LBRACE          : '{' ;
RBRACE          : '}' ;
LBRACKET        : '[' ;
RBRACKET        : ']' ;
COLON           : ':' ;
COMMA           : ',' ;
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
OP_MOD          : '%' ;
OP_LSHIFT       : '<<' ;
OP_RSHIFT       : '>>' ;
OP_AND          : '&&' ;
OP_OR           : '||' ;
OP_XOR          : '^^' ;
OP_BITAND       : '&' ;
OP_BITOR        : '|' ;
OP_BITXOR       : '^' ;
OP_INC          : '++' ;
OP_DEC          : '--' ;
OP_BANG         : '!' ;
OP_BITNEG       : '~' ;
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
fragment AlphaChar      : [a-zA-Z] ;
fragment DigitChar      : [0-9] ;
fragment AlnumChar      : AlphaChar | DigitChar ;
fragment HexDigitChar   : [a-fA-F0-9] ;