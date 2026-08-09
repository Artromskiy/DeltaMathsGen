using System.Linq;
using static KibiHex.MathsGen.Model.DeclarationHelpers;

namespace KibiHex.MathsGen.Model
{
    internal sealed class VectorFamily
    {
        public string ScalarName { get; init; }
        public int Dimension { get; init; }

        public TypeSpec Create()
        {
            var name = ScalarName + Dimension;
            var fields = "xyzw"[..Dimension];

            return new TypeSpec
            {
                Namespace = "KibiHex",
                Name = name,
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = [$"IEquatable<{name}>", $"IComparable<{name}>"],
                Members =
                [
                    ..CreateFields(fields),
                    CreateConstructor(name, fields),
                    CreateUnaryOperator(name, fields, "-"),
                    CreateBinaryOperator(name, fields, "+"),
                    CreateBinaryOperator(name, fields, "-"),
                    CreateBinaryOperator(name, fields, "*"),
                    CreateBinaryOperator(name, fields, "/"),
                    CreateDot(name, fields),
                    CreateLength(name),
                    CreateNormalize(name),
                    CreateLerp(name, fields),
                ],
            };
        }

        private FieldSpec[] CreateFields(string fields) =>
            fields.Select(component => new FieldSpec
            {
                Name = component.ToString(),
                Type = Type(ScalarName),
            }).ToArray();

        private ConstructorSpec CreateConstructor(string name, string fields) => new()
        {
            Parameters = fields.Select(component => Param(component.ToString(), Type(ScalarName))).ToArray(),
            Body = string.Join("\n", fields.Select(component => $"this.{component} = {component};")),
        };

        private OperatorSpec CreateUnaryOperator(string name, string fields, string symbol) => new()
        {
            Operator = symbol,
            ReturnType = Type(name),
            Parameters = [Param("value", Type(name))],
            Body = $"return new({string.Join(", ", fields.Select(component => $"{symbol}value.{component}"))});",
        };

        private OperatorSpec CreateBinaryOperator(string name, string fields, string symbol) => new()
        {
            Operator = symbol,
            ReturnType = Type(name),
            Parameters = [Param("left", Type(name)), Param("right", Type(name))],
            Body = $"return new({string.Join(", ", fields.Select(component => $"left.{component} {symbol} right.{component}"))});",
        };

        private FunctionSpec CreateDot(string name, string fields) => new()
        {
            Name = "Dot",
            ReturnType = Type(ScalarName),
            Modifiers = Modifiers.Public | Modifiers.Static,
            Api = ApiSurface.Vector,
            Parameters = [Param("left", Type(name)), Param("right", Type(name))],
            Body = $"return {string.Join(" + ", fields.Select(component => $"left.{component} * right.{component}"))};",
            Part = TypePart.Geometry,
        };

        private FunctionSpec CreateLength(string name) => new()
        {
            Name = "Length",
            ReturnType = Type(ScalarName),
            Modifiers = Modifiers.Public | Modifiers.Static,
            Api = ApiSurface.Vector,
            Parameters = [Param("value", Type(name))],
            Body = "return Maths.Sqrt(Dot(value, value));",
            Part = TypePart.Geometry,
        };

        private FunctionSpec CreateNormalize(string name) => new()
        {
            Name = "Normalize",
            ReturnType = Type(name),
            Modifiers = Modifiers.Public | Modifiers.Static,
            Api = ApiSurface.Vector,
            Parameters = [Param("value", Type(name))],
            Body = "return value / Length(value);",
            Part = TypePart.Geometry,
        };

        private FunctionSpec CreateLerp(string name, string fields) => new()
        {
            Name = "Lerp",
            ReturnType = Type(name),
            Modifiers = Modifiers.Public | Modifiers.Static,
            Api = ApiSurface.Vector,
            Parameters = [Param("a", Type(name)), Param("b", Type(name)), Param("t", Type(ScalarName))],
            Body = $"return new({string.Join(", ", fields.Select(component => $"Maths.Lerp(a.{component}, b.{component}, t)"))});",
            Part = TypePart.Geometry,
        };
    }
}
