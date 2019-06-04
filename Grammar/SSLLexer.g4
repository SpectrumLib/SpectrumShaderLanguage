// This is the lexer grammar for the SpectrumShaderLanguage, used by Antlr to generate a lexer for
//    the language definition.
// This file is licensed under the MIT license.
// Copyright (c) Sean Moss 2019

lexer grammar SSLLexer;

// String literals
STRING_LITERAL
    : '"' (~('"'|'\n'|'\r'))* '"'
    ;

// Boolean literals
BOOLEAN_LITERAL
    : 'true'
    | 'false'
    ;

// Keywords
KW_ATTRIBUTES       : 'attributes' ;
KW_BLOCK            : 'block' ;
KW_BREAK            : 'break' ;
KW_CONST            : 'const' ;
KW_CONTINUE         : 'continue' ;
KW_DISCARD          : 'discard' ;
KW_DO               : 'do' ;
KW_ELIF             : 'elif' ;
KW_ELSE             : 'else' ;
KW_FLAT             : 'flat' ;
KW_FOR              : 'for' ;
KW_IF               : 'if' ;
KW_IN               : 'in' ;
KW_INOUT            : 'inout' ;
KW_INTERNALS        : 'internals' ;
KW_OUT              : 'out' ;
KW_OUTPUTS          : 'outputs' ;
KW_RETURN           : 'return' ;
KW_SHADER           : 'shader' ;
KW_UNIFORM          : 'uniform' ;
KW_VERSION          : 'version' ;
KW_WHILE            : 'while' ;

// Shader stage function keywords
KW_STAGE_VERT       : '@vert' ;
KW_STAGE_FRAG       : '@frag' ;

// Type keywords (value)
KWT_VOID        : 'void' ;
KWT_BOOL        : 'bool' ;
KWT_BOOL2       : 'bvec2' ;
KWT_BOOL3       : 'bvec3' ;
KWT_BOOL4       : 'bvec4' ;
KWT_INT         : 'int' ;
KWT_INT2        : 'ivec2' ;
KWT_INT3        : 'ivec3' ;
KWT_INT4        : 'ivec4' ;
KWT_UINT        : 'uint' ;
KWT_UINT2       : 'uvec2' ;
KWT_UINT3       : 'uvec3' ;
KWT_UINT4       : 'uvec4' ;
KWT_FLOAT       : 'float' ;
KWT_FLOAT2      : 'vec2' ;
KWT_FLOAT3      : 'vec3' ;
KWT_FLOAT4      : 'vec4' ;
KWT_MAT2        : 'mat2' ;
KWT_MAT3        : 'mat3' ;
KWT_MAT4        : 'mat4' ;

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

// Image format qualifiers
IFQ_F4  : 'f4' ;
IFQ_I4  : 'i4' ;
IFQ_U4  : 'u4' ;
IFQ_F2  : 'f2' ;
IFQ_I2  : 'i2' ;
IFQ_U2  : 'u2' ;
IFQ_F1  : 'f1' ;
IFQ_I1  : 'i1' ;
IFQ_U1  : 'u1' ;

// Built-in Functions
// Taken from https://www.khronos.org/files/opengl45-quick-reference-card.pdf, and adjusted to a smaller subset of
//    functions 
BIF_DEG2RAD     : 'deg2rad' ; // GLSL: radians
BIF_RAD2DEG     : 'rad2deg' ; // GLSL: degrees
BIF_SIN         : 'sin' ;
BIF_COS         : 'cos' ;
BIF_TAN         : 'tan' ;
BIF_ASIN        : 'asin' ;
BIF_ACOS        : 'acos' ;
BIF_ATAN        : 'atan' ;
BIF_ATAN2       : 'atan2' ; // GLSL: atan(x, y)
BIF_POW         : 'pow' ;
BIF_EXP         : 'exp' ;
BIF_LOG         : 'log' ;
BIF_EXP2        : 'exp2' ;
BIF_LOG2        : 'log2' ;
BIF_SQRT        : 'sqrt' ;
BIF_INVSQRT     : 'invsqrt' ; // GLSL: inversesqrt
BIF_ABS         : 'abs' ;
BIF_SIGN        : 'sign' ;
BIF_FLOOR       : 'floor' ;
BIF_TRUNC       : 'trunc' ;
BIF_ROUND       : 'round' ;
BIF_ROUNDEVEN   : 'roundEven' ;
BIF_CEIL        : 'ceil' ;
BIF_FRACT       : 'fract' ;
BIF_MOD         : 'mod' ;
BIF_MIN         : 'min' ;
BIF_MAX         : 'max' ;
BIF_CLAMP       : 'clamp';
BIF_MIX         : 'mix' ;
// TODO: 'select', which acts like glsl's `mix(vec, vec, bvec)`
BIF_STEP        : 'step' ;
BIF_SSTEP       : 'smoothstep' ;
// TODO: 'isnan' and 'isinf'
BIF_LENGTH      : 'length';
BIF_DISTANCE    : 'distance' ;
BIF_DOT         : 'dot' ;
BIF_CROSS       : 'cross' ;
BIF_NORMALIZE   : 'normalize' ;
BIF_FFORWARD    : 'faceforward' ;
BIF_REFLECT     : 'reflect' ;
BIF_REFRACT     : 'refract' ;
// TODO: 'outerProduct'
BIF_MATCOMPMUL  : 'matCompMul' ; // GLSL: matrixCompMult(mat, mat)
BIF_TRANSPOSE   : 'transpose' ;
BIF_INVERSE     : 'inverse' ;
BIF_DETERMINANT : 'determinant' ;
BIF_VECLT       : 'vecLT' ; // GLSL: lessThan(vec, vec)
BIF_VECLE       : 'vecLE' ; // GLSL: lessThanEqual(vec, vec)
BIF_VECGT       : 'vecGT' ; // GLSL: greaterThan(vec, vec)
BIF_VECGE       : 'vecGE' ; // GLSL: greaterThanEqual(vec, vec)
BIF_VECEQ       : 'vecEQ' ; // GLSL: equal(vec, vec)
BIF_VECNE       : 'vecNE' ; // GLSL: notEqual(vec, vec)
BIF_VECANY      : 'vecAny' ; // GLSL: any(bvec)
BIF_VECALL      : 'vecAll' ; // GLSL: all(bvec)
BIF_VECNOT      : 'vecNot' ; // GLSL: not(bvec)
BIF_TEXSIZE     : 'textureSize' ;
BIF_TEXTURE     : 'texture' ;
BIF_TEXFETCH    : 'texelFetch' ;
BIF_IMAGESIZE   : 'imageSize' ;
BIF_IMAGELOAD   : 'imageLoad' ;
BIF_IMAGESTORE  : 'imageStore' ;
BIF_SUBPASSLOAD : 'subpassLoad' ;

// Swizzles
SWIZZLE
    : '.' ([xyzw]+ | [rbga]+ | [stpq]+)
    ;

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
    : AlphaChar (AlphaNumericChar|'_')*
    | '$' AlphaChar+
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
OP_MOD_ASSIGN   : '%=' ;
OP_LS_ASSIGN    : '<<=' ;
OP_RS_ASSIGN    : '>>=' ;
OP_AND_ASSIGN   : '&=' ;
OP_OR_ASSIGN    : '|=' ;
OP_XOR_ASSIGN   : '^=' ;
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
fragment AlphaChar          : [a-zA-Z] ;
fragment DigitChar          : [0-9] ;
fragment AlphaNumericChar   : AlphaChar | DigitChar ;
fragment HexDigitChar       : [a-fA-F0-9];
