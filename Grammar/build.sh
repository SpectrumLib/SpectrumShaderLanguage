#!/bin/sh
# This is the grammar build script for Unix.
# This file is public domain.

java -jar antlr-4.7.2-complete.jar  \
    -no-listener                    \
    -visitor                        \
    -o ../Generated/                \
    -package SSLang.Generated       \
    -Xexact-output-dir              \
    -Dlanguage=CSharp               \
    SSL.g4
