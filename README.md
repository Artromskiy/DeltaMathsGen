# DeltaMathsGen

DeltaMathsGen is a .NET 8 command-line generator for the public DeltaMaths
vector, matrix, quaternion and shader-contract sources.

## What it provides

- Generates the Delta.Maths vector and matrix API from declarative definitions.
- Produces the lowercase `maths` façade alongside typed APIs.
- Emits deterministic shader metadata consumed by DeltaShader.
- Validates declarations before producing output.
- Keeps CPU and GLSL naming and layout metadata in one source model.

## Quick start

Build the `DeltaMathsGen` executable and pass the DeltaMaths vectors source
directory as its only argument.

```text
DeltaMathsGen <DeltaMaths vectors directory>
```

## Core concepts

Declarations describe scalar capabilities, vector families and shader-visible
operations. The generator validates them, then emits runtime code and contract
metadata as one deterministic result. Matrix declarations follow DeltaMaths'
column-vector and column-major convention.

## Capabilities and limits

The generator targets the DeltaMaths API and shader contract; it does not
compile shaders or provide GPU-only intrinsics such as derivatives. See the
[DeltaMaths runtime](../DeltaMaths/docs/README.md) for supported targets.

## Packages and examples

DeltaMathsGen is an executable source tool, not the runtime `DeltaMaths`
package. Its output is consumed by the [DeltaMaths runtime](../DeltaMaths/docs/README.md).

## Further reading

- [DeltaMaths public API](../DeltaMaths/docs/README.md)
- [Generated shader contract](../DeltaMaths/src/DeltaMaths/Vectors/shader-contract.json)
