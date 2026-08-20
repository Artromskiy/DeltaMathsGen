# Delta.MathsGen

Declarative .NET 8 generator for `Delta.Maths`. It owns vector, matrix,
quaternion, lowercase `maths` APIs and `Maths/Vectors/shader-contract.json`.
Generated runtime code targets `netstandard2.0` and `netstandard2.1`.

## Model

- `ScalarDefinition` declares scalar capabilities.
- Function catalogs declare operations and target partial files.
- `ShaderContract` explicitly marks a symbol as `Builtin`, `Helper`, or
  `Unsupported`; GLSL names are never inferred.
- `ModelValidator` rejects inconsistent declarations before files are written.
- `.delta-generated-files` controls stale generated-file cleanup.

GPU-only operations such as derivatives belong to DeltaShader, not MathsGen.
`float4x4` uses four column vectors and column-vector multiplication;
`CreateTRS` is `T * R * S`. Do not introduce another matrix convention.

## Extending

Add vector operations to `VectorFunctionCatalog.Rules`, selecting them through
capabilities and dimension rather than scalar-name checks. Add an explicit
shader contract only after its GLSL lowering is proven. Use stable `delta_*`
names for helper lowering.

Add scalar types through `ScalarTypes.All`; the generator applies compatible
rules. Handwritten extensions must live outside generated files.

## Generate and verify

From the Furnace workspace:

```bash
dotnet build MathsGen/Delta.MathsGen.csproj -c Release \
  --disable-build-servers -m:1 /p:UseSharedCompilation=false
dotnet MathsGen/bin/Release/net8.0/Delta.MathsGen.dll Maths/Vectors
dotnet build Maths/Delta.Maths.csproj -c Release -f netstandard2.0
dotnet build Maths/Delta.Maths.csproj -c Release -f netstandard2.1
dotnet run --project Maths/Tests/Delta.Maths.Tests.csproj -c Release
git -C Maths diff --check
```

A second generator run must produce no diff. Full workspace verification and
benchmark policy are documented in `../REVIEW_PLAYBOOK.md` and `../README.md`.
