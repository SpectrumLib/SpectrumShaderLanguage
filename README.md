# SpectrumShaderLanguage [![Build Status](https://travis-ci.org/SpectrumLib/SpectrumShaderLanguage.svg?branch=master)](https://travis-ci.org/SpectrumLib/SpectrumShaderLanguage)
A custom GPU shading language for Vulkan applications. It is designed primarily for use with the [Spectrum](https://github.com/SpectrumLib/Spectrum) graphics library, but can also be used standalone and be easily integrated with third party projects. It works by [transpiling](https://en.wikipedia.org/wiki/Source-to-source_compiler) the SSL source into GLSL, and then using the tools in the Vulkan SDK to compile the generated GLSL to SPIR-V bytecode.

While this project was originally born purely for use with the Spectrum library, its development shifted early to additionally supporting general-use by projects outside of the Spectrum environment. While GLSL is a powerful language, there are many limitations that make it difficult to robustly integrate with the content pipeline and runtime shader system present in Spectrum. A summary of these limitations (and general frustrations) is:

* GLSL uses confusing semantics designed for OpenGL that do not map well to Vulkan (e.g. `gl_*` naming)
* No standardized method for getting reflection information about the shader in the Vulkan runtime
* All stages have to be defined in separate files to properly compile them with the current Vulkan SDK
* Mappings between GLSL types and the types in the libraries changed between GLSL and Vulkan
* GLSL is needlessly verbose (`layout(...)` eveything), and prone to errors (e.g. mismatched `layout(location = x)` between stages)
* Vulkan-specific extensions to GLSL are poorly documented and can be hard for newcomers to understand

In general, the authors wanted a shading language that would be easier to work with, less tied to a older library, and still be powerful enough for most standard uses. A quick list of the design features and changes from GLSL to SSL:

* Simpler environment for compiling and using the shader files to help speed up and simplify integration
* Unified and explicit syntax for shader input, output, uniforms, and stages.
* Reduced set of types.
* All shader stages are described in the same source file.
* (Nearly) all of the common built-in functions of GLSL are supported.
* Smaller subset of common GLSL features, making programming against the generated SPIRV with Vulkan easier.

SSL is not designed as a full replacement for GLSL. Because it is primarily designed for use with a specific library, it will not be implementing the full set of features available in Vulkan GLSL. However, most common features and effects are still supported by SSL. We will be creating more detailed documentation on the limitations of SSL in the future.

## Using SSL

In order to compile SSL into SPIRV, the Vulkan SDK must be installed and in the PATH. If only converting to GLSL or producing reflection info, the Vulkan SDK is not required.

## Contributing

We welcome bugfixes, feature implementations, and optimizations through pull requests. When working on the source code, please follow the style and comment quality/quantity that you see in existing source code.

All changes, additions, and removals from the SSL grammar must be approved first by one of the lead developers. All changes must follow the goal of keeping SSL simpler than GLSL, and changes must be backwards compatible (no removals after the initial SSL interface is fully defined).

Before working on the source code, you must run the `build` script for your OS in the `Grammar/` folder to regenerate the Antlr source files. These files are very large, and are designed to be easily regenerated at any point, so they are not tracked in version control.

## Acknowledgements

This library uses [Antlr](https://www.antlr.org/) for lexing and parsing SSL for translation into GLSL.

The build process makes use of [ILRepack](https://github.com/gluck/il-repack) for combining .NET binaries for easier distribution.

The library uses the [VulkanSDK](https://www.lunarg.com/vulkan-sdk/) and [SPIRV-Tools](https://github.com/KhronosGroup/SPIRV-Tools) projects developed by LunarG and many open source developers.
