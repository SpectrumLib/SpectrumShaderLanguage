@rem This is the grammar build script for Windows.
@rem This file is public domain.

@echo off

java -jar antlr-4.7.2-complete.jar  ^
    -no-listener                    ^
    -visitor                        ^
    -o ../Generated/                ^
    -package SSLang.Generated       ^
    -Xexact-output-dir              ^
    -Dlanguage=CSharp               ^
    SSLLexer.g4

java -jar antlr-4.7.2-complete.jar  ^
    -no-listener                    ^
    -visitor                        ^
    -o ../Generated/                ^
    -package SSLang.Generated       ^
    -Xexact-output-dir              ^
    -Dlanguage=CSharp               ^
    SSL.g4
