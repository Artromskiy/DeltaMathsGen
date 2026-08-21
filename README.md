# Delta.MathsGen

Declarative .NET 8 generator for `Delta.Maths`. It owns vectors, matrices,
quaternions, lowercase `maths` APIs and the generated
`Maths/Vectors/shader-contract.json`.

The model is intentionally explicit:

- `ScalarDefinition` declares scalar capabilities;
- function catalogs select operations by capability and dimension;
- shader symbols are marked `Builtin`, `Helper` or `Unsupported` and never
  inferred from CLR names;
- `ModelValidator` rejects inconsistent declarations before writing files;
- `.delta-generated-files` owns stale generated-file cleanup.

GPU-only operations such as derivatives belong to DeltaShader. `float4x4` uses
four column vectors and `CreateTRS` is `T * R * S`; generator changes must not
introduce another convention.

Add scalar types through `ScalarTypes.All` and operations through the relevant
catalog. Handwritten extensions live outside generated files. Use
[WORKFLOW.md](WORKFLOW.md) to regenerate and verify, [TODO.md](TODO.md) for
selected work and [AGENTS.md](AGENTS.md) for task routing.
