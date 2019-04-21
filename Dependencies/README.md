This directory contains the libraries and tools used by SSL.

## Antlr4.Runtime.Standard.dll
The .NETStandard binary library for the Antlr4 runtime. This is taken from the standard C# target nuget package.

## ILRepack.exe
The ILRepack tool used to pack .NET binaries together for easier distribution.

## spirv-link
An executable developed by the SPIRV-Tools contributors for linking together multiple SPIR-V modules into a single module. This single module can contain all of the entry points, variables, and functions required to full specify all stages of a shader pipeline.

While part of the SPIRV-Tools project, it is not included in the offical Vulkan SDK because of its pre-release nature. However, it is complete enough for the simple uses SSL requires. Because it is a native library, there is a binary for each of Windows, Linux, and MacOS. These are included as embedded resources in the compiler library, and are extracted at runtime for use.

They are named `spirv-link.X`, where `X` is one of `w`, `l`, or `m` and signifies the platform the binary was built for. Each library is built as x64, using MinSizeRel.