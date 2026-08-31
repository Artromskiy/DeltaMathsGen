using System;
using System.Collections.Generic;
using System.Linq;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal sealed class VectorFunctionRule
    {
        public required string Name { get; init; }
        public ScalarCapabilities Requires { get; init; }
        public int RequiredDimension { get; init; }
        public required Func<VectorContext, FunctionSpec[]> Build { get; init; }

        public bool AppliesTo(VectorContext context) =>
            context.Scalar.Supports(Requires) &&
            (RequiredDimension == 0 || RequiredDimension == context.Dimension);
    }

    internal static class VectorFunctionCatalog
    {
        private const FunctionTargets PublicApi =
            FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths;

        public static readonly VectorFunctionRule[] Rules =
        [
            new()
            {
                Name = "Select",
                Build = context =>
                [
                    ComponentWise(context, "Select", Type(context.Name),
                        [P("falseValue", Type(context.Name)), P("trueValue", Type(context.Name)), P("mask", Type(context.BoolVectorName))],
                        field => $"mask.{field} ? trueValue.{field} : falseValue.{field}", TypePart.Relational),
                    ComponentWise(context, "Select", Type(context.Name),
                        [P("falseValue", Type(context.Name)), P("trueValue", Type(context.Name)), P("mask", Type("bool"))],
                        field => $"mask ? trueValue.{field} : falseValue.{field}", TypePart.Relational),
                    ComponentWise(context, "Select", Type(context.Name),
                        [P("falseValue", Type(context.Scalar.Name)), P("trueValue", Type(context.Scalar.Name)), P("mask", Type(context.BoolVectorName))],
                        field => $"mask.{field} ? trueValue : falseValue", TypePart.Relational),
                ],
            },
            new()
            {
                Name = "Equal",
                Build = context => Masks(context, "Equal", "=="),
            },
            new()
            {
                Name = "NotEqual",
                Build = context => Masks(context, "NotEqual", "!="),
            },
            new()
            {
                Name = "MinMaxClamp",
                Requires = ScalarCapabilities.Ordered,
                Build = context =>
                [
                    ComponentWise(context, "Min", Type(context.Name), Binary(context), field => $"DeltaMaths.Min(a.{field}, b.{field})"),
                    ComponentWise(context, "Min", Type(context.Name), [P("a", Type(context.Name)), P("b", Type(context.Scalar.Name))], field => $"DeltaMaths.Min(a.{field}, b)"),
                    ComponentWise(context, "Min", Type(context.Name), [P("a", Type(context.Scalar.Name)), P("b", Type(context.Name))], field => $"DeltaMaths.Min(a, b.{field})"),
                    ComponentWise(context, "Max", Type(context.Name), Binary(context), field => $"DeltaMaths.Max(a.{field}, b.{field})"),
                    ComponentWise(context, "Max", Type(context.Name), [P("a", Type(context.Name)), P("b", Type(context.Scalar.Name))], field => $"DeltaMaths.Max(a.{field}, b)"),
                    ComponentWise(context, "Max", Type(context.Name), [P("a", Type(context.Scalar.Name)), P("b", Type(context.Name))], field => $"DeltaMaths.Max(a, b.{field})"),
                    ComponentWise(context, "Clamp", Type(context.Name),
                        [P("value", Type(context.Name)), P("min", Type(context.Scalar.Name)), P("max", Type(context.Scalar.Name))],
                        field => $"DeltaMaths.Clamp(value.{field}, min, max)"),
                    ComponentWise(context, "Clamp", Type(context.Name),
                        [P("value", Type(context.Name)), P("min", Type(context.Name)), P("max", Type(context.Name))],
                        field => $"DeltaMaths.Clamp(value.{field}, min.{field}, max.{field})"),
                ],
            },
            new()
            {
                Name = "OrderingMasks",
                Requires = ScalarCapabilities.Ordered,
                Build = context =>
                [
                    .. Masks(context, "LessThan", "<"),
                    .. Masks(context, "LessThanOrEqual", "<="),
                    .. Masks(context, "GreaterThan", ">"),
                    .. Masks(context, "GreaterThanOrEqual", ">="),
                ],
            },
            new()
            {
                Name = "BooleanNot",
                Requires = ScalarCapabilities.Boolean,
                Build = context => context.Scalar.Name == "bool"
                    ? [ComponentWise(context, "Not", Type(context.Name), Unary(context), field => $"!value.{field}", TypePart.Relational)]
                    : [],
            },
            new()
            {
                Name = "AbsSign",
                Requires = ScalarCapabilities.Signed,
                Build = context =>
                [
                    UnaryDeltaMaths(context, "Abs"),
                    UnaryDeltaMaths(context, "Sign"),
                ],
            },
            new()
            {
                Name = "Algebra",
                Requires = ScalarCapabilities.Arithmetic,
                Build = Algebra,
            },
            new()
            {
                Name = "RealCommon",
                Requires = ScalarCapabilities.Real,
                Build = context =>
                [
                    ComponentWise(context, "Lerp", Type(context.Name),
                        [P("a", Type(context.Name)), P("b", Type(context.Name)), P("t", Type(context.Scalar.Name))],
                        field => $"DeltaMaths.Lerp(a.{field}, b.{field}, t)"),
                    ComponentWise(context, "Lerp", Type(context.Name),
                        [P("a", Type(context.Name)), P("b", Type(context.Name)), P("t", Type(context.Name))],
                        field => $"DeltaMaths.Lerp(a.{field}, b.{field}, t.{field})"),
                    ComponentWise(context, "InvLerp", Type(context.Name),
                        [P("edge0", Type(context.Name)), P("edge1", Type(context.Name)), P("value", Type(context.Name))],
                        field => $"DeltaMaths.InvLerp(edge0.{field}, edge1.{field}, value.{field})"),
                    ComponentWise(context, "InvLerp", Type(context.Name),
                        [P("edge0", Type(context.Name)), P("edge1", Type(context.Name)), P("value", Type(context.Scalar.Name))],
                        field => $"DeltaMaths.InvLerp(edge0.{field}, edge1.{field}, value)"),
                    ComponentWise(context, "Smoothstep", Type(context.Name),
                        [P("edge0", Type(context.Name)), P("edge1", Type(context.Name)), P("value", Type(context.Name))],
                        field => $"DeltaMaths.Smoothstep(edge0.{field}, edge1.{field}, value.{field})"),
                    ComponentWise(context, "Smoothstep", Type(context.Name),
                        [P("edge0", Type(context.Scalar.Name)), P("edge1", Type(context.Scalar.Name)), P("value", Type(context.Name))],
                        field => $"DeltaMaths.Smoothstep(edge0, edge1, value.{field})"),
                    ComponentWise(context, "Step", Type(context.Name),
                        [P("edge", Type(context.Name)), P("value", Type(context.Name))],
                        field => $"DeltaMaths.Step(edge.{field}, value.{field})"),
                    ComponentWise(context, "Step", Type(context.Name),
                        [P("edge", Type(context.Scalar.Name)), P("value", Type(context.Name))],
                        field => $"DeltaMaths.Step(edge, value.{field})"),
                    ComponentWise(context, "Saturate", Type(context.Name), Unary(context),
                        field => $"DeltaMaths.Saturate(value.{field})"),
                    ComponentWise(context, "Fma", Type(context.Name),
                        [P("a", Type(context.Name)), P("b", Type(context.Name)), P("c", Type(context.Name))],
                        field => $"DeltaMaths.Fma(a.{field}, b.{field}, c.{field})"),
                    ComponentWise(context, "Remap", Type(context.Name),
                        [P("value", Type(context.Name)), P("sourceFrom", Type(context.Name)), P("sourceTo", Type(context.Name)), P("targetFrom", Type(context.Name)), P("targetTo", Type(context.Name))],
                        field => $"DeltaMaths.Remap(value.{field}, sourceFrom.{field}, sourceTo.{field}, targetFrom.{field}, targetTo.{field})"),
                ],
            },
            new()
            {
                Name = "FloatingRemainder",
                Requires = ScalarCapabilities.FloatingPoint,
                Build = context => context.Scalar.Name is "float" or "half" or "double"
                    ?
                    [
                        ComponentWise(context, "Mod", Type(context.Name),
                            [P("x", Type(context.Name)), P("y", Type(context.Name))],
                            field => $"DeltaMaths.Mod(x.{field}, y.{field})", TypePart.Common),
                        ComponentWise(context, "Mod", Type(context.Name),
                            [P("x", Type(context.Name)), P("y", Type(context.Scalar.Name))],
                            field => $"DeltaMaths.Mod(x.{field}, y)", TypePart.Common),
                    ]
                    : [],
            },
            new()
            {
                Name = "FloatingDecomposition",
                Requires = ScalarCapabilities.FloatingPoint,
                Build = context => context.Scalar.Name is "float" or "half"
                    ? FloatingDecomposition(context)
                    : [],
            },
            new()
            {
                Name = "Packing",
                Requires = ScalarCapabilities.FloatingPoint,
                Build = Packing,
            },
            new()
            {
                Name = "BitPreservingConversions",
                Requires = ScalarCapabilities.FloatingPoint,
                Build = context => context.Scalar.Name == "float"
                    ? BitPreservingConversions(context)
                    : [],
            },
            new()
            {
                Name = "IntegerBitOperations",
                Requires = ScalarCapabilities.Bitwise,
                Build = IntegerBitOperations,
            },
            new()
            {
                Name = "Rounding",
                Requires = ScalarCapabilities.Rounding,
                Build = context => (context.Scalar.Name == "fix"
                    ? Names("Floor", "Ceil", "Round", "Truncate", "Fract")
                    : Names("Floor", "Ceil", "Round", "RoundEven", "Truncate", "Fract"))
                    .Select(name => UnaryDeltaMaths(context, name, TypePart.Common)).ToArray(),
            },
            new()
            {
                Name = "Angles",
                Requires = ScalarCapabilities.Real,
                Build = context => Names("Radians", "Degrees")
                    .Select(name => UnaryDeltaMaths(context, name, TypePart.Common)).ToArray(),
            },
            new()
            {
                Name = "Trigonometry",
                Requires = ScalarCapabilities.Trigonometry,
                Build = context =>
                [
                    .. Names("Sin", "Cos", "Tan", "Asin", "Acos", "Atan")
                        .Select(name => UnaryDeltaMaths(context, name, TypePart.Trigonometry)),
                    .. BinaryDeltaMathsOverloads(context, "Atan2", "y", "x", TypePart.Trigonometry),
                ],
            },
            new()
            {
                Name = "Hyperbolic",
                Requires = ScalarCapabilities.Hyperbolic,
                Build = context => Names("Sinh", "Cosh", "Tanh", "Asinh", "Acosh", "Atanh")
                    .Select(name => UnaryDeltaMaths(context, name, TypePart.Trigonometry)).ToArray(),
            },
            new()
            {
                Name = "Exponential",
                Requires = ScalarCapabilities.Exponential,
                Build = context =>
                [
                    .. Names("Exp", "Exp2", "Log", "Log2", "Log10", "Sqrt", "InverseSqrt", "Cbrt")
                        .Select(name => UnaryDeltaMaths(context, name, TypePart.Exponential)),
                    .. BinaryDeltaMathsOverloads(context, "Pow", "a", "b", TypePart.Exponential),
                ],
            },
            new()
            {
                Name = "FixedSqrt",
                Requires = ScalarCapabilities.FixedPoint,
                Build = context =>
                [
                    UnaryDeltaMaths(context, "Sqrt", TypePart.Exponential),
                    UnaryDeltaMaths(context, "InverseSqrt", TypePart.Exponential),
                ],
            },
            new()
            {
                Name = "Classification",
                Requires = ScalarCapabilities.FloatingPoint,
                Build = context =>
                [
                    BoolUnaryDeltaMaths(context, "IsNaN"),
                    BoolUnaryDeltaMaths(context, "IsInfinity"),
                    BoolUnaryDeltaMaths(context, "IsFinite"),
                ],
            },
            new()
            {
                Name = "Geometry",
                Requires = ScalarCapabilities.Real,
                Build = Geometry,
            },
            new()
            {
                Name = "Cross",
                Requires = ScalarCapabilities.Real,
                RequiredDimension = 3,
                Build = context => [Cross(context)],
            },
        ];

        public static MemberSpec[] Create(VectorContext context)
        {
            var members = new List<MemberSpec>();
            foreach (var rule in Rules)
            {
                if (rule.AppliesTo(context))
                {
                    members.AddRange(rule.Build(context));
                }
            }
            return members.ToArray();
        }

        private static FunctionSpec[] Geometry(VectorContext context)
        {
            var vector = Type(context.Name);
            var scalar = Type(context.Scalar.Name);
            return
            [
                Function("Length", scalar, [P("value", vector)], "return DeltaMaths.Sqrt(SqrLength(value));", TypePart.Geometry, context),
                Function("Distance", scalar, [P("a", vector), P("b", vector)], "return Length(a - b);", TypePart.Geometry, context),
                Function("SqrDistance", scalar, [P("a", vector), P("b", vector)], "return SqrLength(a - b);", TypePart.Geometry),
                Function("Normalize", vector, [P("value", vector)], "return value / Length(value);", TypePart.Geometry, context),
                Function("NormalizeSafe", vector, [P("value", vector)],
                    $$"""
                    var sqrLength = SqrLength(value);
                    return sqrLength <= {{context.Scalar.NormalizeSafeThreshold}} ? zero : value * DeltaMaths.InverseSqrt(sqrLength);
                    """, TypePart.Geometry),
                Function("NormalizeSafe", vector, [P("value", vector), P("fallback", vector)],
                    $$"""
                    var sqrLength = SqrLength(value);
                    return sqrLength <= {{context.Scalar.NormalizeSafeThreshold}} ? fallback : value * DeltaMaths.InverseSqrt(sqrLength);
                    """, TypePart.Geometry),
                Function("FaceForward", vector, [P("N", vector), P("I", vector), P("Nref", vector)],
                    "return Dot(Nref, I) < 0 ? N : -N;", TypePart.Geometry, context),
                Function("Reflect", vector, [P("I", vector), P("N", vector)],
                    "return I - 2 * Dot(N, I) * N;", TypePart.Geometry, context),
                Function("Refract", vector, [P("I", vector), P("N", vector), P("eta", scalar)],
                    """
                    var dNI = Dot(N, I);
                    var k = 1 - eta * eta * (1 - dNI * dNI);
                    if (k < 0) return zero;
                    return eta * I - (eta * dNI + DeltaMaths.Sqrt(k)) * N;
                    """, TypePart.Geometry, context),
                Function("Project", vector, [P("value", vector), P("onto", vector)],
                    "return onto * (Dot(value, onto) / SqrLength(onto));", TypePart.Geometry),
                Function("ProjectSafe", vector, [P("value", vector), P("onto", vector)],
                    """
                    var denominator = SqrLength(onto);
                    return denominator == 0 ? zero : onto * (Dot(value, onto) / denominator);
                    """, TypePart.Geometry),
                Function("ClampLength", vector, [P("value", vector), P("maxLength", scalar)],
                    """
                    maxLength = DeltaMaths.Max(maxLength, 0);
                    var sqrLength = SqrLength(value);
                    if (sqrLength > maxLength * maxLength)
                    {
                        return value * maxLength * DeltaMaths.InverseSqrt(sqrLength);
                    }
                    return value;
                    """, TypePart.Geometry),
                Function("MoveTowards", vector, [P("current", vector), P("target", vector), P("maxDelta", scalar)],
                    """
                    maxDelta = DeltaMaths.Max(maxDelta, 0);
                    var delta = target - current;
                    var sqrDistance = SqrLength(delta);
                    if (sqrDistance == 0 || sqrDistance <= maxDelta * maxDelta)
                        return target;
                    return current + delta * maxDelta * DeltaMaths.InverseSqrt(sqrDistance);
                    """, TypePart.Geometry),
                Function("SmoothDamp", vector,
                    [P("source", vector), P("target", vector), P("velocity", vector, ParameterModifier.Ref), P("smoothTime", scalar), P("deltaTime", scalar)],
                    $"return new({string.Join(", ", context.Fields.Select(field => $"DeltaMaths.SmoothDamp(source.{field}, target.{field}, ref velocity.{field}, smoothTime, deltaTime)"))});",
                    TypePart.Geometry, context),
            ];
        }

        private static FunctionSpec[] Packing(VectorContext context)
        {
            if (context.Scalar.Name != "float" || context.Dimension is not (2 or 4))
            {
                return [];
            }

            return context.Dimension == 2 ?
            [
                Function("PackUnorm2x16", Type("uint"), [P("value", Type(context.Name))],
                    $$"""
                    var x = (uint)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.x, 0f, 1f) * 65535f);
                    var y = (uint)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.y, 0f, 1f) * 65535f);
                    return x | (y << 16);
                    """, TypePart.Common, context),
                Function("UnpackUnorm2x16", Type(context.Name), [P("value", Type("uint"))],
                    "return new((value & 0xffffu) / 65535f, ((value >> 16) & 0xffffu) / 65535f);",
                    TypePart.Common, context),
                Function("PackHalf2x16", Type("uint"), [P("value", Type(context.Name))],
                    "return DeltaMaths.PackHalf(value.x) | (DeltaMaths.PackHalf(value.y) << 16);",
                    TypePart.Common, context),
                Function("UnpackHalf2x16", Type(context.Name), [P("value", Type("uint"))],
                    "return new(DeltaMaths.UnpackHalf(value & 0xffffu), DeltaMaths.UnpackHalf(value >> 16));",
                    TypePart.Common, context),
                Function("PackSnorm2x16", Type("uint"), [P("value", Type(context.Name))],
                    $$"""
                    var x = unchecked((uint)(ushort)(short)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.x, -1f, 1f) * 32767f));
                    var y = unchecked((uint)(ushort)(short)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.y, -1f, 1f) * 32767f));
                    return x | (y << 16);
                    """, TypePart.Common, context),
                Function("UnpackSnorm2x16", Type(context.Name), [P("value", Type("uint"))],
                    $$"""
                    var x = unchecked((short)(value & 0xffffu));
                    var y = unchecked((short)(value >> 16));
                    return new(
                        DeltaMaths.Clamp(x / 32767f, -1f, 1f),
                        DeltaMaths.Clamp(y / 32767f, -1f, 1f));
                    """, TypePart.Common, context),
            ] :
            [
                Function("PackUnorm4x8", Type("uint"), [P("value", Type(context.Name))],
                    $$"""
                    var x = (uint)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.x, 0f, 1f) * 255f);
                    var y = (uint)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.y, 0f, 1f) * 255f);
                    var z = (uint)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.z, 0f, 1f) * 255f);
                    var w = (uint)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.w, 0f, 1f) * 255f);
                    return x | (y << 8) | (z << 16) | (w << 24);
                    """, TypePart.Common, context),
                Function("UnpackUnorm4x8", Type(context.Name), [P("value", Type("uint"))],
                    "return new((value & 0xffu) / 255f, ((value >> 8) & 0xffu) / 255f, ((value >> 16) & 0xffu) / 255f, ((value >> 24) & 0xffu) / 255f);",
                    TypePart.Common, context),
                Function("PackSnorm4x8", Type("uint"), [P("value", Type(context.Name))],
                    $$"""
                    var x = unchecked((uint)(byte)(sbyte)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.x, -1f, 1f) * 127f));
                    var y = unchecked((uint)(byte)(sbyte)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.y, -1f, 1f) * 127f));
                    var z = unchecked((uint)(byte)(sbyte)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.z, -1f, 1f) * 127f));
                    var w = unchecked((uint)(byte)(sbyte)DeltaMaths.RoundEven(DeltaMaths.Clamp(value.w, -1f, 1f) * 127f));
                    return x | (y << 8) | (z << 16) | (w << 24);
                    """, TypePart.Common, context),
                Function("UnpackSnorm4x8", Type(context.Name), [P("value", Type("uint"))],
                    $$"""
                    var x = unchecked((sbyte)(value & 0xffu));
                    var y = unchecked((sbyte)(value >> 8));
                    var z = unchecked((sbyte)(value >> 16));
                    var w = unchecked((sbyte)(value >> 24));
                    return new(
                        DeltaMaths.Clamp(x / 127f, -1f, 1f),
                        DeltaMaths.Clamp(y / 127f, -1f, 1f),
                        DeltaMaths.Clamp(z / 127f, -1f, 1f),
                        DeltaMaths.Clamp(w / 127f, -1f, 1f));
                    """, TypePart.Common, context),
            ];
        }

        private static FunctionSpec[] FloatingDecomposition(VectorContext context)
        {
            var vector = Type(context.Name);
            var integerVector = Type("int" + context.Dimension);
            var lines = context.Fields
                .Select(field => $"var fractional{field} = DeltaMaths.Modf(value.{field}, out var integer{field});")
                .ToList();
            lines.Add($"integerPart = new({string.Join(", ", context.Fields.Select(field => "integer" + field))});");
            lines.Add($"return new({string.Join(", ", context.Fields.Select(field => "fractional" + field))});");
            var modf = Function("Modf", vector,
                [P("value", vector), P("integerPart", vector, ParameterModifier.Out)],
                string.Join("\n", lines), TypePart.Common, context);

            lines = context.Fields
                .Select(field => $"var mantissa{field} = DeltaMaths.Frexp(value.{field}, out var exponent{field});")
                .ToList();
            lines.Add($"exponent = new({string.Join(", ", context.Fields.Select(field => "exponent" + field))});");
            lines.Add($"return new({string.Join(", ", context.Fields.Select(field => "mantissa" + field))});");
            var frexp = Function("Frexp", vector,
                [P("value", vector), P("exponent", integerVector, ParameterModifier.Out)],
                string.Join("\n", lines), TypePart.Common, context);

            var ldexp = ComponentWise(context, "Ldexp", vector,
                [P("value", vector), P("exponent", integerVector)],
                field => $"DeltaMaths.Ldexp(value.{field}, exponent.{field})", TypePart.Common);
            return [modf, frexp, ldexp];
        }

        private static FunctionSpec[] BitPreservingConversions(VectorContext context)
        {
            var vector = Type(context.Name);
            return
            [
                ComponentWise(context, "FloatBitsToInt", Type("int" + context.Dimension), Unary(context),
                    field => $"DeltaMaths.FloatBitsToInt(value.{field})", TypePart.Common),
                ComponentWise(context, "FloatBitsToUint", Type("uint" + context.Dimension), Unary(context),
                    field => $"DeltaMaths.FloatBitsToUint(value.{field})", TypePart.Common),
                ComponentWise(context, "IntBitsToFloat", vector, [P("value", Type("int" + context.Dimension))],
                    field => $"DeltaMaths.IntBitsToFloat(value.{field})", TypePart.Common),
                ComponentWise(context, "UintBitsToFloat", vector, [P("value", Type("uint" + context.Dimension))],
                    field => $"DeltaMaths.UintBitsToFloat(value.{field})", TypePart.Common),
            ];
        }

        private static FunctionSpec[] IntegerBitOperations(VectorContext context)
        {
            var vector = Type(context.Name);
            var bitCount = ComponentWise(context, "BitCount", Type("int" + context.Dimension), Unary(context),
                field => $"DeltaMaths.BitCount(value.{field})", TypePart.Common);
            var findLsb = ComponentWise(context, "FindLSB", Type("int" + context.Dimension), Unary(context),
                field => $"DeltaMaths.FindLSB(value.{field})", TypePart.Common);
            var findMsb = ComponentWise(context, "FindMSB", Type("int" + context.Dimension), Unary(context),
                field => $"DeltaMaths.FindMSB(value.{field})", TypePart.Common);
            var bitfieldReverse = ComponentWise(context, "BitfieldReverse", vector, Unary(context),
                field => $"DeltaMaths.BitfieldReverse(value.{field})", TypePart.Common);
            var bitfieldExtract = ComponentWise(context, "BitfieldExtract", vector,
                [P("value", vector), P("offset", Type("int")), P("bits", Type("int"))],
                field => $"DeltaMaths.BitfieldExtract(value.{field}, offset, bits)", TypePart.Common);
            var bitfieldInsert = ComponentWise(context, "BitfieldInsert", vector,
                [P("baseValue", vector), P("insert", vector), P("offset", Type("int")), P("bits", Type("int"))],
                field => $"DeltaMaths.BitfieldInsert(baseValue.{field}, insert.{field}, offset, bits)", TypePart.Common);
            var functions = new List<FunctionSpec>
            {
                bitCount,
                findLsb,
                findMsb,
                bitfieldReverse,
                bitfieldExtract,
                bitfieldInsert,
            };

            if (context.Scalar.Name == "uint")
            {
                functions.Add(ExtendedAdd(context, "UaddCarry", "carry"));
                functions.Add(ExtendedSubtract(context));
                functions.Add(ExtendedMultiply(context, "UmulExtended"));
            }
            else
            {
                functions.Add(ExtendedMultiply(context, "ImulExtended"));
            }

            return functions.ToArray();
        }

        private static FunctionSpec ExtendedAdd(VectorContext context, string name, string outputName)
        {
            var vector = Type(context.Name);
            var lines = context.Fields
                .Select(field => $"var result{field} = DeltaMaths.{name}(a.{field}, b.{field}, out var {outputName}{field});")
                .ToList();
            lines.Add($"{outputName} = new({string.Join(", ", context.Fields.Select(field => outputName + field))});");
            lines.Add($"return new({string.Join(", ", context.Fields.Select(field => "result" + field))});");
            return Function(name, vector,
                [P("a", vector), P("b", vector), P(outputName, vector, ParameterModifier.Out)],
                string.Join("\n", lines), TypePart.Common, context);
        }

        private static FunctionSpec ExtendedSubtract(VectorContext context) =>
            ExtendedAdd(context, "UsubBorrow", "borrow");

        private static FunctionSpec ExtendedMultiply(VectorContext context, string name)
        {
            var vector = Type(context.Name);
            var lines = context.Fields
                .Select(field => $"DeltaMaths.{name}(a.{field}, b.{field}, out var msb{field}, out var lsb{field});")
                .ToList();
            lines.Add($"msb = new({string.Join(", ", context.Fields.Select(field => "msb" + field))});");
            lines.Add($"lsb = new({string.Join(", ", context.Fields.Select(field => "lsb" + field))});");
            return Function(name, Type("void"),
                [
                    P("a", Type(context.Name)),
                    P("b", Type(context.Name)),
                    P("msb", Type(context.Name), ParameterModifier.Out),
                    P("lsb", Type(context.Name), ParameterModifier.Out),
                ],
                string.Join("\n", lines), TypePart.Common, context);
        }

        private static FunctionSpec[] Algebra(VectorContext context)
        {
            var vector = Type(context.Name);
            var scalar = Type(context.Scalar.Name);
            var dot = string.Join(" + ", context.Fields.Select(field => $"a.{field} * b.{field}"));
            var square = string.Join(" + ", context.Fields.Select(field => $"value.{field} * value.{field}"));
            var sum = string.Join(" + ", context.Fields.Select(field => $"value.{field}"));
            return
            [
                Function("Dot", scalar, [P("a", vector), P("b", vector)], $"return {dot};", TypePart.Geometry, context),
                Function("SqrLength", scalar, [P("value", vector)], $"return {square};", TypePart.Geometry),
                Function("Sum", scalar, [P("value", vector)], $"return {sum};", TypePart.Common),
            ];
        }

        private static FunctionSpec Cross(VectorContext context) => Function(
            "Cross", Type(context.Name), [P("a", Type(context.Name)), P("b", Type(context.Name))],
            "return new(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);",
            TypePart.Geometry, context);

        private static FunctionSpec UnaryDeltaMaths(VectorContext context, string name, TypePart part = TypePart.Common) =>
            ComponentWise(context, name, Type(context.Name), Unary(context), field => $"DeltaMaths.{name}(value.{field})", part);

        private static FunctionSpec BoolUnaryDeltaMaths(VectorContext context, string name) =>
            ComponentWise(context, name, Type(context.BoolVectorName), Unary(context), field => $"DeltaMaths.{name}(value.{field})", TypePart.Relational);

        private static FunctionSpec BinaryDeltaMaths(VectorContext context, string name, string left, string right, TypePart part) =>
            ComponentWise(context, name, Type(context.Name),
                [P(left, Type(context.Name)), P(right, Type(context.Name))],
                field => $"DeltaMaths.{name}({left}.{field}, {right}.{field})", part);

        private static FunctionSpec[] BinaryDeltaMathsOverloads(VectorContext context, string name, string left, string right, TypePart part) =>
        [
            BinaryDeltaMaths(context, name, left, right, part),
            ComponentWise(context, name, Type(context.Name),
                [P(left, Type(context.Name)), P(right, Type(context.Scalar.Name))],
                field => $"DeltaMaths.{name}({left}.{field}, {right})", part),
            ComponentWise(context, name, Type(context.Name),
                [P(left, Type(context.Scalar.Name)), P(right, Type(context.Name))],
                field => $"DeltaMaths.{name}({left}, {right}.{field})", part),
        ];

        private static FunctionSpec Mask(VectorContext context, string name, string operation) =>
            ComponentWise(context, name, Type(context.BoolVectorName), Binary(context),
                field => $"a.{field} {operation} b.{field}", TypePart.Relational);

        private static FunctionSpec[] Masks(VectorContext context, string name, string operation) =>
        [
            Mask(context, name, operation),
            ComponentWise(context, name, Type(context.BoolVectorName),
                [P("a", Type(context.Name)), P("b", Type(context.Scalar.Name))],
                field => $"a.{field} {operation} b", TypePart.Relational),
            ComponentWise(context, name, Type(context.BoolVectorName),
                [P("a", Type(context.Scalar.Name)), P("b", Type(context.Name))],
                field => $"a {operation} b.{field}", TypePart.Relational),
        ];

        private static FunctionSpec ComponentWise(
            VectorContext context,
            string name,
            TypeRef returnType,
            ParameterSpec[] parameters,
            Func<string, string> expression,
            TypePart part = TypePart.Common) =>
            Function(name, returnType, parameters,
                $"return new({string.Join(", ", context.Fields.Select(expression))});", part, context);

        private static FunctionSpec Function(
            string name,
            TypeRef returnType,
            ParameterSpec[] parameters,
            string body,
            TypePart part,
            VectorContext? context = null) => new()
            {
                Name = name,
                ReturnType = returnType,
                Parameters = parameters,
                Body = body,
                Modifiers = Modifiers.Public | Modifiers.Static,
                Targets = PublicApi,
                Part = part,
                ShaderContract = context == null
                    ? new ShaderContract()
                    : VectorShaderContractFactory.Create(context, name, parameters),
            };

        private static ParameterSpec[] Unary(VectorContext context) =>
            [P("value", Type(context.Name))];

        private static ParameterSpec[] Binary(VectorContext context) =>
            [P("a", Type(context.Name)), P("b", Type(context.Name))];

        private static ParameterSpec P(string name, TypeRef type, ParameterModifier modifier = ParameterModifier.None) =>
            new() { Name = name, Type = type, Modifier = modifier };

        private static string[] Names(params string[] names) => names;

    }
}
