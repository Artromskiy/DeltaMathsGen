# DeltaMathsGen TODO

- Keep generator and DeltaMaths changes in one bounded contract update.
- Make the producer workflow build DeltaMathsGen, generate twice with no second
  diff, validate `shader-contract.json` layout/schema, then hand off to both
  DeltaMaths target builds/tests.

## GLSL 4.60 contract gaps

The current Delta.Maths shader contract intentionally covers the pure
single-precision vector/matrix subset used by the engine. The following is the
selected follow-up for closing the remaining GLSL 4.60 pure-math gaps. New
symbols must be declared here in the generator model, receive complete
metadata, and be regenerated into DeltaMaths; generated files are never edited
by hand.

### Pure functions and select semantics

- [x] Add shader metadata and CPU/GLSL conformance cases for vector relational
  functions `lessThan`, `lessThanEqual`, `greaterThan`, `greaterThanEqual`, and
  boolean-vector `not` for the supported `vec`, `ivec`, and `uvec` families.
- [x] Add `modf`, `frexp`, and `ldexp`, including their `out`/vector exponent
  signatures and defined zero/finite-value behavior.
- [x] Add the bit-preserving conversions
  `floatBitsToInt`, `floatBitsToUint`, `intBitsToFloat`, and
  `uintBitsToFloat` for matching scalar/vector widths.
- The first portable packing slice is implemented in the generator for
  `packUnorm2x16`, `unpackUnorm2x16`, `packSnorm2x16`, `unpackSnorm2x16`,
  `packUnorm4x8`, `unpackUnorm4x8`, `packSnorm4x8`, and `unpackSnorm4x8`.
  Keep the CPU conversion explicit and covered by bit-level conformance tests.
- [x] Add `packHalf2x16`/`unpackHalf2x16` using the shared netstandard IEEE-754
  `half` conversion; add `packDouble2x32`/`unpackDouble2x32` only together
  with the future double-precision shader capability.
- [x] Keep the public Delta naming `select`, not `mix`, for boolean-mask selection.
  Complete the `select` overloads that correspond to GLSL boolean-vector
  selection, while retaining `delta_select` as the helper mapping where a
  helper is required. Do not add a second `mix` API merely to mirror GLSL
  spelling.
- Keep `maths.round` mapped to `roundEven` until a separate, explicitly
  documented CPU contract for GLSL's implementation-dependent `round` is
  approved. `maths.roundEven` remains the exact nearest-even operation.

### Integer operators and built-ins

- [x] Publish metadata for the existing integer/vector operators `%`, `~`, `<<`,
  `>>`, `&`, `|`, and `^`; they currently exist in generated CPU types but are
  not shader-supported in the manifest.
- [x] Add the integer built-ins `uaddCarry`, `usubBorrow`, `umulExtended`,
  `imulExtended`, `bitfieldExtract`, `bitfieldInsert`, `bitfieldReverse`,
  `bitCount`, `findLSB`, and `findMSB`, with explicit handling for `out`
  parameters and undefined argument ranges.

### Double-precision capability

- [x] Publish shader-contract types `dvec2`, `dvec3`, `dvec4`, `dmat2`,
  `dmat3`, `dmat4`, and all rectangular `dmatCxR` forms. The CLR types are
  `double2/3/4` and `doubleCxR`; each published type carries the `float64`
  capability requirement and its exact std430 column layout.
- [x] Publish the corresponding double overloads that have a real CPU
  implementation for common, geometric, trigonometric, exponential,
  relational, and matrix operations. The manifest carries complete CLR/GLSL
  parameter and return types; DeltaShader must gate these symbols on device
  float64 support. Unsupported double overloads remain `Unsupported`.

### Explicitly outside Delta.Maths

Do not add these to the runtime maths library. DeltaShader owns their symbol
and stage lowering, and DeltaRender owns resource/ABI execution:

- opaque/resource types and operations: `sampler*`, `texture*`,
  `texelFetch*`, `textureGather*`, `image*`, `imageLoad`, `imageStore`,
  atomic counters, and atomic memory functions;
- stage-dependent operations: `dFdx`, `dFdy`, `dFdxFine`, `dFdyFine`,
  `dFdxCoarse`, `dFdyCoarse`, `fwidth`, interpolation-at functions,
  `subpassLoad`, invocation-group functions, `barrier`, and memory barriers;
- geometry-stage `EmitVertex`/`EndPrimitive` operations and GLSL built-in
  stage variables;
- shader language constructs such as interface blocks, storage/interpolation
  qualifiers, arrays/structs, control flow, specialization constants and
  descriptor bindings.

The current type metadata describes supported std430 scalar/vector/matrix
layouts; it is not yet a complete `std140`/array/struct/interface-block layout
model. Any such extension belongs to the shader ABI contract, not to ad-hoc
runtime types. Deprecated GLSL `noise1`-`noise4` are not a target: they are not
declared when generating SPIR-V.

Acceptance for this follow-up is deterministic generation, complete symbol and
overload identity in `shader-contract.json`, CPU cases for every newly
supported overload, and a DeltaShader consumer test for every published
mapping. Unsupported or capability-excluded variants must remain explicit.
