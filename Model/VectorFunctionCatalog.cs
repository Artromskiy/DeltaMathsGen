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
        private static readonly FunctionTargets PublicApi =
            FunctionTargets.Type | FunctionTargets.ShaderMaths;

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
                    ComponentWise(context, "Min", Type(context.Name), Binary(context), field => $"Maths.Min(a.{field}, b.{field})"),
                    ComponentWise(context, "Min", Type(context.Name), [P("a", Type(context.Name)), P("b", Type(context.Scalar.Name))], field => $"Maths.Min(a.{field}, b)"),
                    ComponentWise(context, "Min", Type(context.Name), [P("a", Type(context.Scalar.Name)), P("b", Type(context.Name))], field => $"Maths.Min(a, b.{field})"),
                    ComponentWise(context, "Max", Type(context.Name), Binary(context), field => $"Maths.Max(a.{field}, b.{field})"),
                    ComponentWise(context, "Max", Type(context.Name), [P("a", Type(context.Name)), P("b", Type(context.Scalar.Name))], field => $"Maths.Max(a.{field}, b)"),
                    ComponentWise(context, "Max", Type(context.Name), [P("a", Type(context.Scalar.Name)), P("b", Type(context.Name))], field => $"Maths.Max(a, b.{field})"),
                    ComponentWise(context, "Clamp", Type(context.Name),
                        [P("value", Type(context.Name)), P("min", Type(context.Scalar.Name)), P("max", Type(context.Scalar.Name))],
                        field => $"Maths.Clamp(value.{field}, min, max)"),
                    ComponentWise(context, "Clamp", Type(context.Name),
                        [P("value", Type(context.Name)), P("min", Type(context.Name)), P("max", Type(context.Name))],
                        field => $"Maths.Clamp(value.{field}, min.{field}, max.{field})"),
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
                Name = "AbsSign",
                Requires = ScalarCapabilities.Signed,
                Build = context =>
                [
                    UnaryMaths(context, "Abs"),
                    UnaryMaths(context, "Sign"),
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
                        field => $"Maths.Lerp(a.{field}, b.{field}, t)"),
                    ComponentWise(context, "Lerp", Type(context.Name),
                        [P("a", Type(context.Name)), P("b", Type(context.Name)), P("t", Type(context.Name))],
                        field => $"Maths.Lerp(a.{field}, b.{field}, t.{field})"),
                    ComponentWise(context, "InvLerp", Type(context.Name),
                        [P("edge0", Type(context.Name)), P("edge1", Type(context.Name)), P("value", Type(context.Name))],
                        field => $"Maths.InvLerp(edge0.{field}, edge1.{field}, value.{field})"),
                    ComponentWise(context, "InvLerp", Type(context.Name),
                        [P("edge0", Type(context.Name)), P("edge1", Type(context.Name)), P("value", Type(context.Scalar.Name))],
                        field => $"Maths.InvLerp(edge0.{field}, edge1.{field}, value)"),
                    ComponentWise(context, "SmoothStep", Type(context.Name),
                        [P("edge0", Type(context.Name)), P("edge1", Type(context.Name)), P("value", Type(context.Name))],
                        field => $"Maths.SmoothStep(edge0.{field}, edge1.{field}, value.{field})"),
                    ComponentWise(context, "SmoothStep", Type(context.Name),
                        [P("edge0", Type(context.Scalar.Name)), P("edge1", Type(context.Scalar.Name)), P("value", Type(context.Name))],
                        field => $"Maths.SmoothStep(edge0, edge1, value.{field})"),
                    ComponentWise(context, "Step", Type(context.Name),
                        [P("edge", Type(context.Name)), P("value", Type(context.Name))],
                        field => $"Maths.Step(edge.{field}, value.{field})"),
                    ComponentWise(context, "Step", Type(context.Name),
                        [P("edge", Type(context.Scalar.Name)), P("value", Type(context.Name))],
                        field => $"Maths.Step(edge, value.{field})"),
                    ComponentWise(context, "Saturate", Type(context.Name), Unary(context),
                        field => $"Maths.Saturate(value.{field})"),
                    ComponentWise(context, "Fma", Type(context.Name),
                        [P("a", Type(context.Name)), P("b", Type(context.Name)), P("c", Type(context.Name))],
                        field => $"Maths.Fma(a.{field}, b.{field}, c.{field})"),
                    ComponentWise(context, "Remap", Type(context.Name),
                        [P("value", Type(context.Name)), P("sourceFrom", Type(context.Name)), P("sourceTo", Type(context.Name)), P("targetFrom", Type(context.Name)), P("targetTo", Type(context.Name))],
                        field => $"Maths.Remap(value.{field}, sourceFrom.{field}, sourceTo.{field}, targetFrom.{field}, targetTo.{field})"),
                ],
            },
            new()
            {
                Name = "Rounding",
                Requires = ScalarCapabilities.Rounding,
                Build = context => Names("Floor", "Ceil", "Round", "Truncate", "Fract")
                    .Select(name => UnaryMaths(context, name, TypePart.Common)).ToArray(),
            },
            new()
            {
                Name = "Angles",
                Requires = ScalarCapabilities.Real,
                Build = context => Names("Radians", "Degrees")
                    .Select(name => UnaryMaths(context, name, TypePart.Common)).ToArray(),
            },
            new()
            {
                Name = "Trigonometry",
                Requires = ScalarCapabilities.Trigonometry,
                Build = context =>
                [
                    .. Names("Sin", "Cos", "Tan", "Asin", "Acos", "Atan")
                        .Select(name => UnaryMaths(context, name, TypePart.Trigonometry)),
                    .. BinaryMathsOverloads(context, "Atan2", "y", "x", TypePart.Trigonometry),
                ],
            },
            new()
            {
                Name = "Hyperbolic",
                Requires = ScalarCapabilities.Hyperbolic,
                Build = context => Names("Sinh", "Cosh", "Tanh", "Asinh", "Acosh", "Atanh")
                    .Select(name => UnaryMaths(context, name, TypePart.Trigonometry)).ToArray(),
            },
            new()
            {
                Name = "Exponential",
                Requires = ScalarCapabilities.Exponential,
                Build = context =>
                [
                    .. Names("Exp", "Exp2", "Log", "Log2", "Log10", "Sqrt", "InverseSqrt", "Cbrt")
                        .Select(name => UnaryMaths(context, name, TypePart.Exponential)),
                    .. BinaryMathsOverloads(context, "Pow", "a", "b", TypePart.Exponential),
                ],
            },
            new()
            {
                Name = "FixedSqrt",
                Requires = ScalarCapabilities.FixedPoint,
                Build = context =>
                [
                    UnaryMaths(context, "Sqrt", TypePart.Exponential),
                    UnaryMaths(context, "InverseSqrt", TypePart.Exponential),
                ],
            },
            new()
            {
                Name = "Classification",
                Requires = ScalarCapabilities.FloatingPoint,
                Build = context =>
                [
                    BoolUnaryMaths(context, "IsNaN"),
                    BoolUnaryMaths(context, "IsInfinity"),
                    BoolUnaryMaths(context, "IsFinite"),
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
                    members.AddRange(rule.Build(context));
            }
            return members.ToArray();
        }

        private static FunctionSpec[] Geometry(VectorContext context)
        {
            var vector = Type(context.Name);
            var scalar = Type(context.Scalar.Name);
            return
            [
                Function("Length", scalar, [P("value", vector)], "return Maths.Sqrt(SqrLength(value));", TypePart.Geometry, context),
                Function("Distance", scalar, [P("a", vector), P("b", vector)], "return Length(a - b);", TypePart.Geometry, context),
                Function("SqrDistance", scalar, [P("a", vector), P("b", vector)], "return SqrLength(a - b);", TypePart.Geometry),
                Function("Normalize", vector, [P("value", vector)], "return value / Length(value);", TypePart.Geometry, context),
                Function("NormalizeSafe", vector, [P("value", vector)],
                    $$"""
                    var sqrLength = SqrLength(value);
                    return sqrLength <= {{context.Scalar.NormalizeSafeThreshold}} ? zero : value * Maths.InverseSqrt(sqrLength);
                    """, TypePart.Geometry),
                Function("NormalizeSafe", vector, [P("value", vector), P("fallback", vector)],
                    $$"""
                    var sqrLength = SqrLength(value);
                    return sqrLength <= {{context.Scalar.NormalizeSafeThreshold}} ? fallback : value * Maths.InverseSqrt(sqrLength);
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
                    return eta * I - (eta * dNI + Maths.Sqrt(k)) * N;
                    """, TypePart.Geometry),
                Function("Project", vector, [P("value", vector), P("onto", vector)],
                    "return onto * (Dot(value, onto) / SqrLength(onto));", TypePart.Geometry),
                Function("ProjectSafe", vector, [P("value", vector), P("onto", vector)],
                    """
                    var denominator = SqrLength(onto);
                    return denominator == 0 ? zero : onto * (Dot(value, onto) / denominator);
                    """, TypePart.Geometry),
                Function("ClampLength", vector, [P("value", vector), P("maxLength", scalar)],
                    """
                    maxLength = Maths.Max(maxLength, 0);
                    var sqrLength = SqrLength(value);
                    if (sqrLength > maxLength * maxLength)
                    {
                        return value * maxLength * Maths.InverseSqrt(sqrLength);
                    }
                    return value;
                    """, TypePart.Geometry),
                Function("MoveTowards", vector, [P("current", vector), P("target", vector), P("maxDelta", scalar)],
                    """
                    maxDelta = Maths.Max(maxDelta, 0);
                    var delta = target - current;
                    var sqrDistance = SqrLength(delta);
                    if (sqrDistance == 0 || sqrDistance <= maxDelta * maxDelta)
                        return target;
                    return current + delta * maxDelta * Maths.InverseSqrt(sqrDistance);
                    """, TypePart.Geometry),
                Function("SmoothDamp", vector,
                    [P("source", vector), P("target", vector), P("velocity", vector, "ref"), P("smoothTime", scalar), P("deltaTime", scalar)],
                    $"return new({string.Join(", ", context.Fields.Select(field => $"Maths.SmoothDamp(source.{field}, target.{field}, ref velocity.{field}, smoothTime, deltaTime)"))});",
                    TypePart.Geometry, context),
            ];
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

        private static FunctionSpec UnaryMaths(VectorContext context, string name, TypePart part = TypePart.Common) =>
            ComponentWise(context, name, Type(context.Name), Unary(context), field => $"Maths.{name}(value.{field})", part);

        private static FunctionSpec BoolUnaryMaths(VectorContext context, string name) =>
            ComponentWise(context, name, Type(context.BoolVectorName), Unary(context), field => $"Maths.{name}(value.{field})", TypePart.Relational);

        private static FunctionSpec BinaryMaths(VectorContext context, string name, string left, string right, TypePart part) =>
            ComponentWise(context, name, Type(context.Name),
                [P(left, Type(context.Name)), P(right, Type(context.Name))],
                field => $"Maths.{name}({left}.{field}, {right}.{field})", part);

        private static FunctionSpec[] BinaryMathsOverloads(VectorContext context, string name, string left, string right, TypePart part) =>
        [
            BinaryMaths(context, name, left, right, part),
            ComponentWise(context, name, Type(context.Name),
                [P(left, Type(context.Name)), P(right, Type(context.Scalar.Name))],
                field => $"Maths.{name}({left}.{field}, {right})", part),
            ComponentWise(context, name, Type(context.Name),
                [P(left, Type(context.Scalar.Name)), P(right, Type(context.Name))],
                field => $"Maths.{name}({left}, {right}.{field})", part),
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
            ShaderContract = context == null ? new ShaderContract() : CreateShaderContract(context, name, parameters),
        };

        private static ParameterSpec[] Unary(VectorContext context) =>
            [P("value", Type(context.Name))];

        private static ParameterSpec[] Binary(VectorContext context) =>
            [P("a", Type(context.Name)), P("b", Type(context.Name))];

        private static ParameterSpec P(string name, TypeRef type, string? modifier = null) =>
            new() { Name = name, Type = type, Modifier = modifier };

        private static string[] Names(params string[] names) => names;

        private static ShaderContract CreateShaderContract(VectorContext context, string name, ParameterSpec[] parameters)
        {
            var scalar = context.Scalar.Name;
            var shaderScalar = scalar is "bool" or "int" or "uint" or "float";
            if (!shaderScalar)
                return new ShaderContract();

            return name switch
            {
                "Select" when parameters.Length == 3 && parameters[0].Type.Name == context.Name &&
                    parameters[1].Type.Name == context.Name && parameters[2].Type.Name == context.BoolVectorName
                    && scalar != "bool" => Helper("delta_select", "vector"),
                "Equal" when parameters.All(parameter => parameter.Type.Name == context.Name) => Builtin("equal", "vector"),
                "NotEqual" when parameters.All(parameter => parameter.Type.Name == context.Name) => Builtin("notEqual", "vector"),
                "Min" or "Max" or "Clamp" when scalar != "bool" => Builtin(name.ToLowerInvariant(), "vector"),
                "Abs" when scalar is "float" or "int" => Builtin("abs", "vector"),
                "Lerp" when scalar == "float" => Builtin("mix", "vector"),
                "SmoothStep" when scalar == "float" => Builtin("smoothstep", "vector"),
                "Step" when scalar == "float" => Builtin("step", "vector"),
                "Dot" when scalar == "float" => Builtin("dot", "vector"),
                "Length" or "Distance" when scalar == "float" => Builtin(name.ToLowerInvariant(), "vector"),
                "Normalize" when scalar == "float" => Builtin("normalize", "vector"),
                "FaceForward" when scalar == "float" => Builtin("faceforward", "vector"),
                "Reflect" when scalar == "float" => Builtin("reflect", "vector"),
                "Refract" when scalar == "float" => Builtin("refract", "vector"),
                "Cross" when scalar == "float" && context.Dimension == 3 => Builtin("cross", "vector"),
                _ => new ShaderContract(),
            };
        }

        private static ShaderContract Builtin(string name, string capability) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Builtin,
            RequiredCapability = capability,
            Stages = ShaderStages.All,
        };

        private static ShaderContract Helper(string name, string capability) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Helper,
            RequiredCapability = capability,
            Stages = ShaderStages.All,
        };

    }
}
