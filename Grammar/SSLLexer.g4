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
KW_BREAK            : 'break' ;
KW_CONST            : 'const' ;
KW_CONTINUE         : 'continue' ;
KW_DISCARD          : 'discard' ;
KW_DO               : 'do' ;
KW_ELSE             : 'else' ;
KW_FOR              : 'for' ;
KW_IF               : 'if' ;
KW_IN               : 'in' ;
KW_INOUT            : 'inout' ;
KW_INPUT            : 'input' ;
KW_LOCALS           : 'locals' ;
KW_OUT              : 'out' ;
KW_OUTPUT           : 'output' ;
KW_RETURN           : 'return' ;
KW_SHADER           : 'shader' ;
KW_UNIFORM          : 'uniform' ;
KW_WHILE            : 'while' ;

// Shader stage function keywords
KW_STAGE_VERT       : '@vert' ;
KW_STAGE_FRAG       : '@frag' ;

// Type keywords (data)
KWT_VOID        : 'void' ;
KWT_BOOL        : 'bool' ;
KWT_INT         : 'int' ;
KWT_UINT        : 'uint' ;
KWT_FLOAT       : 'float' ;
KWT_BOOL2       : 'bvec2' ;
KWT_INT2        : 'ivec2' ;
KWT_UINT2       : 'uvec2' ;
KWT_FLOAT2      : 'vec2' ;
KWT_BOOL3       : 'bvec3' ;
KWT_INT3        : 'ivec3' ;
KWT_UINT3       : 'uvec3' ;
KWT_FLOAT3      : 'vec3' ;
KWT_BOOL4       : 'bvec4' ;
KWT_INT4        : 'ivec4' ;
KWT_UINT4       : 'uvec4' ;
KWT_FLOAT4      : 'vec4' ;
KWT_MAT2        : 'mat2' ;
KWT_MAT3        : 'mat3' ;
KWT_MAT4        : 'mat4' ;

// Type keywords (handle)
KWT_SAMP1       : 'sampler1D' ;
KWT_SAMP2       : 'sampler2D' ;
KWT_SAMP3       : 'sampler3D' ;
KWT_SAMPCUBE    : 'samplerCube' ;
KWT_SAMP1ARR    : 'sampler1DArray' ;
KWT_SAMP2ARR    : 'sampler2DArray' ;
KWT_IMAGE1      : 'image1D' ;
KWT_IMAGE2      : 'image2D' ;
KWT_IMAGE3      : 'image3D' ;
KWT_IMAGECUBE   : 'imageCube' ;
KWT_IMAGE1ARR   : 'image1DArray' ;
KWT_IMAGE2ARR   : 'image2DArray' ;

// Built-in Functions
// Taken from https://www.khronos.org/files/opengl45-quick-reference-card.pdf, and adjusted to a smaller subset of
//    functions 
BIF_ALL_ARG1 // All 1-argument builtin functions
    : BIF_DEG2RAD | BIF_RAD2DEG | BIF_SIN | BIF_COS | BIF_TAN | BIF_ASIN | BIF_ACOS | BIF_ATAN | BIF_EXP
    | BIF_LOG | BIF_EXP2 | BIF_LOG2 | BIF_SQRT | BIF_INVSQRT | BIF_ABS | BIF_SIGN | BIF_FLOOR | BIF_TRUNC
    | BIF_ROUND | BIF_ROUNDEVEN | BIF_CEIL | BIF_FRACT | BIF_LENGTH | BIF_NORMALIZE | BIF_TRANSPOSE
    | BIF_DETERMINANT | BIF_INVERSE | BIF_VECANY | BIF_VECALL | BIF_VECNOT | BIF_TEXSIZE | BIF_IMAGESIZE
    ;
BIF_ALL_ARG2 // All 2-argument builtin functions
    : BIF_ATAN2 | BIF_POW | BIF_MOD | BIF_MIN | BIF_MAX | BIF_STEP | BIF_DISTANCE | BIF_DOT | BIF_CROSS
    | BIF_REFLECT | BIF_MATCOMPMUL | BIF_VECLT | BIF_VECLE | BIF_VECGT | BIF_VECGE | BIF_VECEQ | BIF_VECNE
    | BIF_TEXTURE | BIF_TEXFETCH | BIF_IMAGELOAD
    ;
BIF_ALL_ARG3 // All 3-argument builtin functions
    : BIF_CLAMP | BIF_MIX | BIF_SSTEP | BIF_FFORWARD | BIF_REFRACT | BIF_IMAGESTORE
    ;
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
BIF_STEP        : 'step' ;
BIF_SSTEP       : 'smoothstep' ;
BIF_LENGTH      : 'length';
BIF_DISTANCE    : 'distance' ;
BIF_DOT         : 'dot' ;
BIF_CROSS       : 'cross' ;
BIF_NORMALIZE   : 'normalize' ;
BIF_FFORWARD    : 'faceforward' ;
BIF_REFLECT     : 'reflect' ;
BIF_REFRACT     : 'refract' ;
BIF_MATCOMPMUL  : 'matCompMul' ; // GLSL: matrixCompMult(vec, vec)
BIF_TRANSPOSE   : 'transpose' ;
BIF_DETERMINANT : 'determinant' ;
BIF_INVERSE     : 'inverse' ;
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
OP_ALL_ASSIGN
    : OP_ASSIGN | OP_ADD_ASSIGN | OP_SUB_ASSIGN | OP_MUL_ASSIGN | OP_DIV_ASSIGN | OP_MOD_ASSIGN
    | OP_LS_ASSIGN | OP_RS_ASSIGN | OP_AND_ASSIGN | OP_OR_ASSIGN | OP_XOR_ASSIGN
    ;
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
