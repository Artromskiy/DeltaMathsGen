using System.Collections.Generic;
using System.Linq;
using static KibiHex.MathsGen.Model.DeclarationHelpers;

namespace KibiHex.MathsGen.Model
{
    // Kept as a model declaration for renderers that want to distinguish casts
    // from ordinary operators.  The current renderer can render the equivalent
    // OperatorSpec returned by Create below.
    internal sealed class ConversionSpec : MemberSpec
    {
        public TypeRef FromType { get; init; }
        public TypeRef ToType { get; init; }
        public bool Implicit { get; init; } = true;
        public string Body { get; init; } = "";
    }

    internal static class VectorOperatorCatalog
    {
        public static MemberSpec[] Create(string scalarName, int dimension)
        {
            var vector = scalarName + dimension;
            var fields = "xyzw"[..dimension];
            var members = new List<MemberSpec>();

            if (scalarName is "int" or "uint")
            {
                members.Add(new FunctionSpec
                {
                    Name = "Clamp",
                    ReturnType = Type(vector),
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Api = ApiSurface.Vector,
                    Part = TypePart.Geometry,
                    Parameters = [Param("value", Type(vector)), Param("min", Type(scalarName)), Param("max", Type(scalarName))],
                    Body = $"return new({string.Join(", ", fields.Select(c => $"Maths.Clamp(value.{c}, min, max)"))});",
                });
                foreach (var symbol in new[] { "%", "^", "|", "&", "<<", ">>" })
                {
                    if (symbol is "<<" or ">>")
                    {
                        members.Add(Binary(vector, "int", fields, symbol, vector, "int"));
                        continue;
                    }

                    members.Add(Binary(vector, scalarName, fields, symbol, vector, vector));
                    members.Add(Binary(vector, scalarName, fields, symbol, vector, scalarName));
                    if (symbol is "%" or "^" or "|" or "&")
                        members.Add(Binary(vector, scalarName, fields, symbol, scalarName, vector));
                }
                members.Add(new OperatorSpec
                {
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "~",
                    ReturnType = Type(vector),
                    Parameters = [Param("value", Type(vector))],
                    Body = $"return new({string.Join(", ", fields.Select(c => $"~value.{c}"))});",
                });
            }

            if (scalarName == "bool")
            {
                members.Add(Aggregate("Any", vector, fields, "||", "false"));
                members.Add(Aggregate("All", vector, fields, "&&", "true"));
            }

            foreach (var target in UpcastTargets(scalarName))
            {
                var targetVector = target + dimension;
                members.Add(new OperatorSpec
                {
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "implicit",
                    ReturnType = Type(targetVector),
                    Parameters = [Param("value", Type(vector))],
                    Body = $"return new {targetVector}({string.Join(", ", fields.Select(c => scalarName == "int" && target == "uint" ? $"(uint)value.{c}" : $"value.{c}"))});",
                });
            }

            return members.ToArray();
        }

        private static OperatorSpec Binary(string vector, string scalar, string fields, string symbol, string left, string right) => new()
        {
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(vector),
            Parameters = [Param("left", Type(left == scalar ? scalar : vector)), Param("right", Type(right == scalar ? scalar : vector))],
            Body = $"return new({string.Join(", ", fields.Select(c => Operand("left", left, c) + " " + symbol + " " + Operand("right", right, c)))});",
        };

        private static string Operand(string name, string type, char field) =>
            type == "int" || type == "uint" ? name : $"{name}.{field}";

        private static FunctionSpec Aggregate(string name, string vector, string fields, string operation, string empty) => new()
        {
            Name = name,
            ReturnType = Type("bool"),
            Modifiers = Modifiers.Public | Modifiers.Static,
            Api = ApiSurface.Vector,
            Parameters = [Param("value", Type(vector))],
            Body = $"return {string.Join($" {operation} ", fields.Select(c => $"value.{c}"))};",
        };

        private static IEnumerable<string> UpcastTargets(string scalar) => scalar switch
        {
            "int" => new[] { "uint", "float", "double" },
            "uint" => new[] { "float", "double" },
            "float" => new[] { "double" },
            _ => Enumerable.Empty<string>(),
        };
    }
}
