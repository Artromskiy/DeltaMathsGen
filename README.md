# Delta.MathsGen

Standalone .NET 8 console generator for [Delta.Maths](../Maths/README.md). The generated runtime remains compatible with `netstandard2.0`, `netstandard2.1`, and C# 9.

The generator builds a complete model in memory, validates it, and renders readable partial C# files. Declarations use arrays, object initializers, and raw string literals; generated code is not assembled from lazy `IEnumerable` pipelines.

The model also declares the non-vector composite types `float4x4` and `quaternion`. Their generated files use the same partial-file pipeline as vectors, so matrix layout and quaternion algorithms remain generator-owned rather than handwritten in runtime output.

## Run

From the repository root:

```bash
dotnet run --project MathsGen/Delta.MathsGen.csproj -- Maths/Vectors
```

Every current file is rewritten. `.delta-generated-files` records ownership, so later runs remove obsolete generated files without touching handwritten partial extensions.

## The whole design

There are four concepts:

1. `ScalarDefinition` says what one scalar can do.
2. `VectorFunctionRule` says what capability and dimension a function needs.
3. `TypePart` says which partial file receives a member.
4. `FunctionTargets` says whether a function appears on the vector type, in lowercase `maths`, or both.

Shader mapping is explicit metadata on the same declaration. A declaration without a `ShaderContract` is emitted as `Unsupported`; the generator never guesses a GLSL name from a CLR name. Supported entries carry a GLSL symbol, `Builtin` or `Helper` kind, capability, and stage restrictions. The generated `Maths/Vectors/shader-contract.json` also records stable function identities, resolved GLSL parameter/result types, constructors, and standard swizzle metadata.

A rule applies when:

```csharp
scalar.Supports(rule.Requires) &&
(rule.RequiredDimension == 0 || rule.RequiredDimension == dimension)
```

There are no scalar-name checks in function declarations.

## Adding a vector function

Add one object to `VectorFunctionCatalog.Rules`:

```csharp
new()
{
    Name = "Perpendicular",
    Requires = ScalarCapabilities.Real,
    RequiredDimension = 2,
    Build = context =>
    [
        new FunctionSpec
        {
            Name = "Perpendicular",
            ReturnType = Type(context.Name),
            Parameters = [P("value", Type(context.Name))],
            Body =
            """
            return new(-value.y, value.x);
            """,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
            Part = TypePart.Geometry,
        },
    ],
},
```

That is enough. `Real` selects `float`, `double`, and `fix`; dimension `2` selects `*2`; `Targets` produces both `float2.Perpendicular(value)` and `maths.perpendicular(value)`. The lowercase name is derived automatically by changing only the first letter.

For repeated component-wise code, use `ComponentWise`:

```csharp
Build = context =>
[
    ComponentWise(
        context,
        "Saturate",
        Type(context.Name),
        Unary(context),
        field => $"Maths.Saturate(value.{field})"),
],
```

Templates remove component repetition; scalar selection remains explicit in the rule.

To expose a new function to shaders, add the runtime declaration first and then add an explicit contract only when its GLSL lowering is proven:

```csharp
ShaderContract = new ShaderContract
{
    GlslName = "smoothstep",
    Mapping = ShaderMappingKind.Builtin,
    RequiredCapability = "vector",
    Stages = ShaderStages.Vertex | ShaderStages.Fragment | ShaderStages.Compute,
},
```

Use `Helper` for a stable `delta_*` helper that Shad owns. Keep GPU-only intrinsics such as derivatives out of MathsGen; they belong to the Shad intrinsic registry and should not receive a CPU implementation.

## Adding a scalar type

Implement the runtime scalar, then add one entry to `ScalarTypes.All`:

```csharp
new()
{
    Name = "half",
    ZeroLiteral = "default",
    Capabilities =
        ScalarCapabilities.Arithmetic |
        ScalarCapabilities.Signed |
        ScalarCapabilities.Ordered |
        ScalarCapabilities.Real,
    ImplicitTargets = ["float"],
    ExplicitTargets = ["int"],
},
```

The generator creates `half2`, `half3`, and `half4`, then applies every compatible rule. A normal new scalar does not require edits to `VectorFamily`, the catalogs, or `Program`.

Capabilities are deliberately specific. `Arithmetic` does not imply `UnaryPlus`, `Increment`, `Trigonometry`, or `Exponential`; this prevents generation of code the scalar cannot compile.

## API and file placement

Functions normally use:

```csharp
Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths;
```

The implementation lives on the type; `maths` only forwards:

```csharp
float3.Normalize(value);
maths.normalize(value);
```

`TypePart` controls only file placement:

| Part | File suffix |
| --- | --- |
| `Core` | `.cs` |
| `Operators` | `.operators.cs` |
| `Common` | `.common.cs` |
| `Geometry` | `.geometry.cs` |
| `Trigonometry` | `.trigonometry.cs` |
| `Exponential` | `.exponential.cs` |
| `Relational` | `.relational.cs` |
| `Swizzles` | `.swizzles.cs` |

Handwritten extensions can live in any separate file because vector structs and both maths classes are partial.

## Matrix convention

`float4x4` is four sequential `float4` columns (`c0`–`c3`) and uses column-vector multiplication. `CreateTRS` emits `T * R * S` (scale → rotation → translation in that order for a column-vector model). This convention is part of the API contract and is covered by layout tests; generated code must not introduce a second matrix style.

`quaternion` is a left-handed `(x, y, z, w)` Hamilton-style storage used by this API. Its matrix conversion and axis-angle conventions match the matrix conventions above.

## Pipeline

```text
ScalarDefinition + rules
        -> TypeSpec[]
        -> ModelValidator
        -> TypeFileLayout
        -> focused renderers
        -> GeneratedFileWriter

The shader contract renderer consumes the same `TypeSpec[]` and writes `shader-contract.json`. This keeps C# signatures, layout metadata, and shader identities in one model. A second generation run must produce no diff.
```

`ModelValidator` rejects unknown conversion targets, duplicate types or members, duplicate `maths` overloads, missing function targets, and invalid dimension rules before anything is written.

Rendering is split by responsibility: `TypeRenderer` selects a partial type, `MemberRenderer` renders one member, `BodyRenderer` indents bodies, the two maths renderers create forwarding APIs, and `GeneratedFileWriter` owns rewriting and stale-file cleanup.

## Verify

```bash
dotnet build MathsGen/Delta.MathsGen.csproj --no-restore
dotnet run --project MathsGen/Delta.MathsGen.csproj --no-build -- Maths/Vectors
dotnet run --project Maths/Tests/Delta.Maths.Tests.csproj --no-restore
dotnet build Core/Core.csproj --no-restore
```

Generated files begin with `// <auto-generated />` and should not be edited directly.


## CI, tests, and benchmarks

The GitHub Actions workflow is [`.github/workflows/ci.yml`](.github/workflows/ci.yml).
Pull requests and pushes to `main` build in Release, run correctness tests, and
perform BenchmarkDotNet discovery only; they do not record performance numbers.
Measured benchmarks run only from **Actions → Build, tests and benchmarks → Run
workflow** with `run_benchmarks=true`. Results are uploaded from
`artifacts/benchmarks` for 30 days.

Repository conventions:

- correctness projects are named `*.Tests.csproj`; projects using
  `Microsoft.NET.Test.Sdk` run through `dotnet test`, while custom executable
  harnesses must be listed explicitly in the workflow and return a non-zero exit
  code on failure;
- BenchmarkDotNet projects are named `*.Benchmarks.csproj`; this filename is how
  the workflow discovers them;
- their entry point must forward CLI arguments with
  `BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args)`;
- mark the Delta implementation with `[Benchmark(Baseline = true)]` within every
  comparable benchmark category; use exactly one baseline per category;
- add sibling repositories to the checkout steps whenever a
  `ProjectReference` escapes this repository.

A benchmark added without the naming convention or without CLI argument
forwarding is not registered and must not be treated as CI coverage. Shared
GitHub runners are suitable for comparisons within one run, not for small
cross-run regression claims.
