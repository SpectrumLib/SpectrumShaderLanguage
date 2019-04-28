# SpectrumShaderLanguage [![Build Status](https://travis-ci.org/SpectrumLib/SpectrumShaderLanguage.svg?branch=master)](https://travis-ci.org/SpectrumLib/SpectrumShaderLanguage)
A custom GPU shading language for Vulkan applications. It is designed primarily for use with the [Spectrum](https://github.com/SpectrumLib/Spectrum) graphics library, but can also be used standalone and be easily integrated with third party projects. It works by [transpiling](https://en.wikipedia.org/wiki/Source-to-source_compiler) the SSL source into GLSL, and then using the tools in the Vulkan SDK to compile the generated GLSL to SPIR-V bytecode.

While this project was originally born purely for use with the Spectrum library, its development shifted early to additionally supporting general-use by projects outside of the Spectrum environment. While GLSL is a powerful language, there are many limitations that make it difficult to robustly integrate with the content pipeline and runtime shader system present in Spectrum. More information can be found on the [Why SSL](https://github.com/SpectrumLib/SpectrumShaderLanguage/wiki/Why-SSL) page on the wiki.

## Using SSL

This project is split into three primary sections:

* `Compiler` - The C# library for compiling SSL programs from code.
* `SSLC` - The command line tool for compiling SSL. Currently requires .NET Core 2.1, but we will build .Net Native releases soon.
* `Reflection` - The reference implementation in C# for run-time loading of the shader refleciton files.

In order to compile SSL into SPIRV, the Vulkan SDK must be installed and in the PATH. If only converting to GLSL or producing reflection info, the Vulkan SDK is not required.

More information for the libraries and command line tool can be found on the Github project wiki.

## Contributing

We welcome bugfixes, feature implementations, and optimizations through pull requests. When working on the source code, please follow the style and comment quality/quantity that you see in existing source code.

All changes, additions, and removals from the SSL grammar must be approved first by one of the lead developers. All changes must follow the goal of keeping SSL simpler than GLSL, and changes must be backwards compatible (no removals after the initial SSL interface is fully defined).

Before working on the source code, you must run the `build` script for your OS in the `Grammar/` folder to regenerate the Antlr source files. These files are very large, and are designed to be easily regenerated at any point, so they are not tracked in version control.

## Acknowledgements

This library uses [Antlr](https://www.antlr.org/) for lexing and parsing SSL for translation into GLSL.

The build process makes use of [ILRepack](https://github.com/gluck/il-repack) for combining .NET binaries for easier distribution.

The library uses the [VulkanSDK](https://www.lunarg.com/vulkan-sdk/) and [SPIRV-Tools](https://github.com/KhronosGroup/SPIRV-Tools) projects developed by LunarG and many open source developers.
