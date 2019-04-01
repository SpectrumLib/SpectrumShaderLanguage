# SpectrumShaderLanguage
A custom GPU shading language designed for use with the [Spectrum](https://github.com/SpectrumLib/Spectrum) graphics library. It works by [transpiling](https://en.wikipedia.org/wiki/Source-to-source_compiler) the SSL source into GLSL, and then using the tools in the Vulkan SDK to compile the generated GLSL to SPIR-V bytecode.

The current version of GLSL for Vulkan uses confusing semantics (designed for OpenGL), provides far more types than is used in 95% of shader programs, and there is no standardized procedure for getting reflection info about shaders. All of these make GLSL far more complex than is required for nearly all developers and shader authors, and would make a full parsing and reflecting system for Spectrum too complex to easily develop and manage. Therefore, we designed SSL as a minimal and simpler shading language, similar to GLSL, but should still work for most situtations.

A quick list of the design features and changes from GLSL to SSL:

* Designed for use with Vulkan, instead of being hacked together from another library like GLSL is.
* Unified and explicit syntax for shader input, output, uniforms, and stages.
* Greatly reduced set of types.
* All shader stages are described in the same source file.
* (Nearly) all of the built-in functions of GLSL are supported.
* Smaller subset of common GLSL features, making programming against the generated SPIRV with Vulkan easier.

Because it has the ability to generate GLSL source instead of compiling directly to SPIRV, this tool can additionally be used outside of the Spectrum environment for any project. This is supported by the licensing (MIT), and is encouraged by us, the authors.

In order to compile SSL into SPIRV, the Vulkan SDK must be installed and in the PATH. If only converting to GLSL, the Vulkan SDK is not required.

More information will be provided as development progresses.

## Contributing

We welcome feature implementations and bugfixes through pull requests. When working on the source code, please follow the style and comment quality/quantity that you see in existing source code.

All changes, additions, and removals from the SSL grammar must be approved first by one of the lead developers. All changes must follow the goal of keeping SSL simpler than GLSL, and changes must be backwards compatible (no removals after the initial SSL interface is fully defined).

Before working on the source code, you must run the `build` script for your OS in the `Grammar/` folder to regenerate the Antlr source files. These files are very large, and are designed to be easily regenerated at any point, so they are not tracked in version control.

## Limitations

This language is designed to minimally cover only the features that are required by Spectrum. This makes it unable to support advanced features and shading tricks. However, it can be used for a larget subset of standard shader effects, such as texturing, lighting, shadows, deferred rendering, ect...

## Acknowledgements

This library uses [Antlr](https://www.antlr.org/) for lexing and parsing SSL for translation into GLSL.
