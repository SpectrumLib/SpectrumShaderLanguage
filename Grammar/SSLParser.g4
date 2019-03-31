parser grammar SSLParser;

options {
    tokenVocab=SSLLexer;
}

// Top-level file unit
file
    : shaderMetaStatement? topLevelStatement* EOF
    ;

// Shader metadata statements
shaderMetaStatement
    : 'shader' Name=STRING_LITERAL ';'
    ;

// All top-level statements that can appear in the file scope
topLevelStatement
    : uniformStatement
    | inputStatement
    | outputStatement
    | localsStatement
    | stageFunction
    | standardFunction
    ;

// Uniform statements
uniformStatement
    : uniformBlockDeclare
    | uniformHandleDeclare
    ;
uniformBlockDeclare // Block of data types
    : uniformDeclare 'block' typeBlock
    ;
uniformHandleDeclare // A single opaque handle
    : uniformDeclare variableDeclaration
    ;
uniformDeclare
    : 'uniform' '(' Index=INTEGER_LITERAL ')'
    ;
typeBlock
    : '{' variableDeclaration* '}'
    ;

// Vertex shader input (attributes)
inputStatement
    : 'input' typeBlock
    ;

// Fragment shader output
outputStatement
    : 'output' typeBlock
    ;

// Locals (values passed between shader stages)
localsStatement
    : 'locals' typeBlock
    ;

// Stage functions
stageFunction
    : vertFunction
    | fragFunction
    ;
vertFunction
    : '@vert' block
    ;
fragFunction
    : '@frag' block
    ;

// Standard function (non-stage function)
standardFunction
    : type Name=IDENTIFIER '(' (Params=parameterList|'void')? ')' block
    ;
parameterList
    : PList+=parameter (',' PList+=parameter)*
    ;
parameter
    : Access=('in'|'out'|'inout')? type Name=IDENTIFIER
    ;

// Statements (basically, any line that can stand on its own in a function body)
block
    : '{' statement* '}'
    ;
statement
    : variableDeclaration
    | variableDefinition
    | assignment
    | ifStatement
    | forLoop
    | whileLoop
    | doLoop
    | controlFlowStatement
    ;

// Declaring new variables
variableDeclaration
    : type Name=IDENTIFIER arrayIndexer? ';'
    ;
variableDefinition
    : 'const'? type Name=IDENTIFIER '=' Value=expression ';'                   # ValueDefinition
    | 'const'? type Name=IDENTIFIER arrayIndexer '=' Value=arrayLiteral ';'    # ArrayDefinition
    ;
arrayLiteral
    : '{' Values+=expression (',' Values+=expression)* '}'
    ;

// Assigment
assignment
    : Name=IDENTIFIER arrayIndexer? SWIZZLE? Op=OP_ALL_ASSIGN Value=expression ';'
    ;

// Array indexer
arrayIndexer
    : '[' Index=INTEGER_LITERAL ']'
    ;

// Conditional statements
ifStatement
    : 'if' '(' Cond=expression ')' (IfBlock=block|IfStatement=statement) ('else' (ElseBlock=block|ElseStatement=statement))?
    ;

// Looping constructs
forLoop
    : 'for' '('
            (variableDefinition|assignment)* ';'
            Condition=expression? ';'
            (assignment)*
      ')' (block|statement)
    ;
whileLoop
    : 'while' '(' Condition=expression ')' (block|statement)
    ;
doLoop
    : 'do' block 'while' '(' Condition=expression ')' ';'
    ;

// Control flow statements
controlFlowStatement
    : 'return' RVal=expression? ';'
    | 'break' ';'
    | 'continue' ';'
    | 'discard' ';'
    ;

// Expressions (anything that can evaluate to a type value) (enforce order of operation)
// See https://www.khronos.org/files/opengl45-quick-reference-card.pdf for GLSL Order of Operations
expression
    : atom                              # AtomExpr
    // Unary operators
    | Expr=IDENTIFIER Op=('--'|'++')    # UnOpPostfix
    | Op=('--'|'++') Expr=IDENTIFIER    # UnOpPrefix
    | Op=('+'|'-') Expr=expression      # UnOpFactor
    | Op=('!'|'~') Expr=expression      # UnOpNegate
    // Binary operators
    | Left=expression Op=('*'|'/'|'%') Right=expression         # BinOpMulDivMod
    | Left=expression Op=('+'|'-') Right=expression             # BinOpAddSub
    | Left=expression Op=('<<'|'>>') Right=expression           # BinOpBitShift
    | Left=expression Op=('<'|'>'|'<='|'>=') Right=expression   # BinOpRelational
    | Left=expression Op=('=='|'!=') Right=expression           # BinOpEquality
    | Left=expression Op=('&'|'|'|'^') Right=expression         # BinOpBitLogic
    | Left=expression Op=('&&'|'||'|'^^') Right=expression      # BinOpBoolLogic
    // Ternary (selection) operator
    | Cond=expression '?' TVal=expression ':' FVal=expression   # SelectionExpr
    ;

// Atom expression (an expression that cannot be subdivided into further expressions)
atom
    : '(' expression ')' arrayIndexer? SWIZZLE?     # ParenAtom
    | typeConstruction arrayIndexer? SWIZZLE?       # ConstructionAtom
    | builtinFunctionCall arrayIndexer? SWIZZLE?    # BuiltinCallAtom
    | functionCall arrayIndexer? SWIZZLE?           # FunctionCallAtom
    | valueLiteral                                  # LiteralAtom
    | IDENTIFIER arrayIndexer? SWIZZLE?             # VariableAtom
    ;
typeConstruction // For built-in types, also how casting is performed
    : Type=type '(' Args+=expression (',' Args+=expression)* ')'
    ;
builtinFunctionCall
    : FName=BIF_ALL_ARG1 '(' A1=expression ')'                                      # BuiltinCall1
    | FName=BIF_ALL_ARG2 '(' A1=expression ',' A2=expression ')'                    # BuiltinCall2
    | FName=BIF_ALL_ARG3 '(' A1=expression ',' A2=expression ',' A3=expression ')'  # BuiltinCall3
    ;
functionCall
    : FName=IDENTIFIER '(' (Args+=expression (',' Args+=expression)*)? ')'
    ;
valueLiteral
    : INTEGER_LITERAL
    | FLOAT_LITERAL
    | BOOLEAN_LITERAL
    ;

// Type keywords
type
    : KWT_VOID
    | valueTypeKeyword
    ;
valueTypeKeyword
    : KWT_BOOL | KWT_INT | KWT_UINT | KWT_FLOAT
    | KWT_BOOL2 | KWT_INT2 | KWT_UINT2 | KWT_FLOAT2
    | KWT_BOOL3 | KWT_INT3 | KWT_UINT3 | KWT_FLOAT3
    | KWT_BOOL4 | KWT_INT4 | KWT_UINT4 | KWT_FLOAT4
    | KWT_MAT2 | KWT_MAT3 | KWT_MAT4
    ;