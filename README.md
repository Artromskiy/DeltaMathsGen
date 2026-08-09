# KibiHex.MathsGen

The source generator for [KibiHex.Maths](../Maths/README.md).

KibiHex.MathsGen is a standalone .NET console application. It builds a complete in-memory model of every vector type and renders that model into readable C# partial files. The generator itself targets .NET 8 and uses C# 12; the generated runtime library remains compatible with `netstandard2.1` and C# 9.

The generator deliberately uses arrays, lists, object initializers, and raw string literals instead of long lazy `IEnumerable` pipelines. Its declarations are intended to be read and edited as ordinary data.

## Running the generator

From the `MathsGen` directory:

```bash
dotnet run --project KibiHex.MathsGen.csproj -- ../Maths/Vectors
```

The first argument is the output directory. Every generated file is always rewritten.

The generator also scans `Maths*.cs` in the parent directory of the output folder, so the normal output path must remain inside the Maths project if scalar `maths` forwarding methods should be generated.

Generated output includes:

```text
bool2.cs
bool2.operators.cs
bool2.swizzles.cs

float3.cs
float3.operators.cs
float3.geometry.cs
float3.swizzles.cs

maths.cs
maths.vectors.cs
```

To inspect the small model preview:

```bash
dotnet run --project KibiHex.MathsGen.csproj -- ../Maths/Vectors --model-preview
```

To print the public API of the generated vector types from an assembly:

```bash
dotnet run --project KibiHex.MathsGen.csproj -- --dump-api ../Maths/bin/Debug/netstandard2.1/KibiHex.Maths.dll
```

## Architecture

The generator has three main stages:

```text
declarations -> file layout -> renderers -> C# files
```

### Declarations

The declaration model is defined in `Model/Declarations.cs`.

`TypeSpec` describes a generated type. Its `Members` array contains declarations such as:

- `FieldSpec`
- `ConstructorSpec`
- `IndexerSpec`
- `PropertySpec`
- `FunctionSpec`
- `OperatorSpec`

A declaration contains semantic information and a readable function body, but does not know how to indent or format C#.

```csharp
new FunctionSpec
{
    Name = "Normalize",
    ReturnType = Type("float3"),
    Modifiers = Modifiers.Public | Modifiers.Static,
    Api = ApiSurface.Vector,
    Part = TypePart.Geometry,
    Parameters =
    [
        Param("value", Type("float3")),
    ],
    Body =
    """
    return value / Length(value);
    """,
}
```

Object initializers are preferred over constructors with long positional or named argument lists. Raw string literals are preferred for multiline generated bodies.

### Vector families

`Model/VectorFamily.cs` assembles one complete vector declaration from:

- scalar name: `bool`, `int`, `uint`, `float`, `double`, or `fix`;
- dimension: 2, 3, or 4;
- core fields, constructors, indexer, parsing, and object contract;
- operator declarations;
- function declarations;
- generated swizzles.

`Program` creates the Cartesian product of the six scalar types and three dimensions, producing 18 vector types.

### Function and operator catalogs

Reusable vector behaviour is kept outside `VectorFamily`:

- `Model/VectorFunctionCatalog.cs` contains geometry and component-wise numerical functions;
- `Model/VectorOperatorCatalog.cs` contains integer operations, masks, and numeric conversions.

The catalogs decide which declarations apply to a particular scalar and dimension. For example, `Cross` is emitted only for three-dimensional floating-point and fixed-point vectors.

### Type parts

Every member belongs to an explicit `TypePart`:

| Part | Output suffix | Purpose |
| --- | --- | --- |
| `Core` | `.cs` | fields, constructors, indexer, parsing, object contract |
| `Operators` | `.operators.cs` | operators and conversions |
| `Geometry` | `.geometry.cs` | vector and component-wise math functions |
| `Swizzles` | `.swizzles.cs` | component aliases and swizzle properties |

Always assign the part explicitly when a declaration does not belong in the core file:

```csharp
new PropertySpec
{
    Name = "xy",
    Type = Type("float2"),
    Part = TypePart.Swizzles,
    Getter = "new float2(x, y)",
}
```

`TypeFileLayout` groups declarations by part and creates only non-empty files.

### API surfaces

`ApiSurface` controls where a function appears:

| Value | Result |
| --- | --- |
| `Type` | method on the generated vector type |
| `Maths` | conventional static maths surface reserved for generated declarations |
| `ShaderMaths` | lowercase overload in `maths` |
| `Extension` | forwarding member supported by `ExtensionRenderer` |
| `Vector` | shorthand for `Type | ShaderMaths` |
| `Static` | shorthand for `Maths | ShaderMaths` |

Vector functions normally use `ApiSurface.Vector`:

```csharp
new FunctionSpec
{
    Name = "Dot",
    Api = ApiSurface.Vector,
    // ...
}
```

This produces both:

```csharp
float3.Dot(a, b);
maths.dot(a, b);
```

`MathsName` is derived automatically by lowercasing the first character. Do not duplicate the lowercase name in every declaration.

### Renderers

Rendering is split by responsibility:

- `CSharpRenderer` writes the file header and namespace;
- `TypeRenderer` writes a partial type and selects members for one part;
- `MemberRenderer` renders individual declarations;
- `BodyRenderer` renders and indents multiline bodies;
- `SyntaxFormatter` formats modifiers and parameters;
- `ShaderMathsRenderer` generates vector forwarding functions in `maths`;
- `ScalarMathsRenderer` generates scalar forwarding functions in `maths`;
- `ExtensionRenderer` supports a future extension-method surface.

`CodeModel/CodeWriter.cs` is the shared indentation-aware writer. Declaration code should not manually manage file-level indentation.

## `Maths` and `maths`

The runtime library intentionally exposes two related APIs:

- `Maths` is the hand-written conventional PascalCase scalar API;
- `maths` is the generated lowercase shader-like API.

`ScalarMathsScanner` scans public static methods from the runtime `Maths*.cs` files and creates forwarding overloads such as:

```csharp
public static float sin(float x) => Maths.Sin(x);
```

`ShaderMathsRenderer` adds vector overloads from declarations marked with `ApiSurface.ShaderMaths`:

```csharp
public static float3 normalize(float3 value) => float3.Normalize(value);
```

The original method body stays on the vector type. The `maths` layer is only a forwarding API and does not duplicate the implementation.

## Adding a vector function

1. Decide which scalar types and dimensions support it.
2. Add a `FunctionSpec` in `VectorFunctionCatalog`.
3. Set `Modifiers.Public | Modifiers.Static`.
4. Set `Api = ApiSurface.Vector` if both type and lowercase APIs are required.
5. Select the appropriate `TypePart`.
6. Run the generator.
7. Build the runtime library.

Example:

```csharp
Add(
    members,
    "Distance",
    scalar,
    [P("a", vector), P("b", vector)],
    "return Length(a - b);");
```

## Adding a scalar type

Adding a new scalar family requires more than placing its name in `Program.cs`:

1. Define its default literal in `VectorFamily.DefaultValue`.
2. Decide which constructors and operators are valid.
3. Add operator-catalog rules and conversions.
4. Add supported function-catalog rules.
5. Verify parsing and formatting support.
6. Add the scalar to the family list in `Program`.
7. Generate and compile all dimensions.

Keep scalar capabilities explicit. Avoid assuming that every scalar supports negation, square roots, trigonometry, or implicit conversion.

## Adding a new file group

1. Add a `TypePart` value with its file suffix.
2. Add it to `TypeFileLayout.Parts` in the desired order.
3. Assign declarations to the new part.

For example, trigonometric functions could later use:

```csharp
public static readonly TypePart Trigonometry =
    new("Trigonometry", ".trigonometry");
```

## Validation

`Validation/ApiSurfaceReader.cs` reads the public surface of all generated vector types from a compiled assembly. `ApiSurfaceComparer` can compare two snapshots and report missing or additional members.

Normal verification after changing declarations is:

```bash
dotnet build KibiHex.MathsGen.csproj
dotnet run --project KibiHex.MathsGen.csproj -- ../Maths/Vectors
dotnet build ../Maths/KibiHex.Maths.csproj
dotnet build ../Core/Core.csproj
```

Generated files should never be edited manually. Change the declaration or renderer and regenerate them instead.

## Design principles

- declarations are complete data models, not chains of rendering operations;
- generated bodies should be readable C#;
- multiline bodies use raw string literals;
- object initializers are preferred for declarations;
- each renderer has one narrow responsibility;
- type capabilities are selected explicitly;
- one implementation may be projected onto several API surfaces;
- generated runtime code must remain compatible with the runtime project's language version;
- the generator may use a newer .NET and C# version than the runtime library.
