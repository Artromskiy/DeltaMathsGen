using System;
using System.Collections.Generic;
using System.Linq;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal static class VectorOperatorCatalog
    {
        public static MemberSpec[] Create(VectorContext context)
        {
            var scalar = context.Scalar;
            var vector = context.Name;
            var fields = context.Components;
            var members = new List<MemberSpec>();

            if (scalar.Supports(ScalarCapabilities.Remainder))
            {
                members.Add(Binary(vector, scalar.Name, fields, "%", vector, vector));
                members.Add(Binary(vector, scalar.Name, fields, "%", vector, scalar.Name));
                members.Add(Binary(vector, scalar.Name, fields, "%", scalar.Name, vector));
            }

            if (scalar.Supports(ScalarCapabilities.Bitwise))
            {
                foreach (var symbol in new[] { "^", "|", "&" })
                {
                    members.Add(Binary(vector, scalar.Name, fields, symbol, vector, vector));
                    members.Add(Binary(vector, scalar.Name, fields, symbol, vector, scalar.Name));
                    members.Add(Binary(vector, scalar.Name, fields, symbol, scalar.Name, vector));
                }
                members.Add(new OperatorSpec
                {
                    Name = "OnesComplement",
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "~",
                    ReturnType = Type(vector),
                    Parameters = [Param("value", Type(vector))],
                    Body = $"return new({string.Join(", ", fields.Select(c => $"~value.{c}"))});",
                });
            }

            if (scalar.Supports(ScalarCapabilities.Shift))
            {
                members.Add(Binary(vector, "int", fields, "<<", vector, "int"));
                members.Add(Binary(vector, "int", fields, ">>", vector, "int"));
            }

            if (scalar.Supports(ScalarCapabilities.Boolean))
            {
                foreach (var symbol in new[] { "^", "|", "&" })
                {
                    members.Add(Binary(vector, scalar.Name, fields, symbol, vector, vector));
                    members.Add(Binary(vector, scalar.Name, fields, symbol, vector, scalar.Name));
                    members.Add(Binary(vector, scalar.Name, fields, symbol, scalar.Name, vector));
                }
                members.Add(Unary(vector, fields, "!"));
                members.Add(Aggregate("Any", vector, fields, "||"));
                members.Add(Aggregate("All", vector, fields, "&&"));
            }

            if (scalar.Supports(ScalarCapabilities.Ordered))
            {
                foreach (var symbol in new[] { "<", "<=", ">", ">=" })
                {
                    members.Add(Comparison(context, symbol, vector, vector));
                    members.Add(Comparison(context, symbol, vector, scalar.Name));
                    members.Add(Comparison(context, symbol, scalar.Name, vector));
                }
            }

            AddConversions(members, context, scalar.ImplicitTargets, "implicit");
            AddConversions(members, context, scalar.ExplicitTargets, "explicit");

            return members.ToArray();
        }

        private static void AddConversions(List<MemberSpec> members, VectorContext context, string[] targets, string kind)
        {
            foreach (var target in targets)
            {
                var targetVector = target + context.Dimension;
                members.Add(new OperatorSpec
                {
                    Name = OperatorName(kind),
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = kind,
                    ReturnType = Type(targetVector),
                    Parameters = [Param("value", Type(context.Name))],
                    Body = $"return new {targetVector}({string.Join(", ", context.Components.Select(c => $"({target})value.{c}"))});",
                });
            }
        }

        private static OperatorSpec Binary(string vector, string scalar, string fields, string symbol, string left, string right) => new()
        {
            Name = OperatorName(symbol),
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(vector),
            Parameters = [Param("left", Type(left == scalar ? scalar : vector)), Param("right", Type(right == scalar ? scalar : vector))],
            Body = $"return new({string.Join(", ", fields.Select(c => Operand("left", left, scalar, c) + " " + symbol + " " + Operand("right", right, scalar, c)))});",
        };

        private static OperatorSpec Unary(string vector, string fields, string symbol) => new()
        {
            Name = OperatorName(symbol),
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(vector),
            Parameters = [Param("value", Type(vector))],
            Body = $"return new({string.Join(", ", fields.Select(field => symbol + "value." + field))});",
        };

        private static OperatorSpec Comparison(VectorContext context, string symbol, string left, string right) => new()
        {
            Name = OperatorName(symbol),
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(context.BoolVectorName),
            Parameters =
            [
                Param("left", Type(left)),
                Param("right", Type(right)),
            ],
            Body = $"return new({string.Join(", ", context.Fields.Select(field =>
                Operand("left", left, context.Scalar.Name, field[0]) + " " + symbol + " " +
                Operand("right", right, context.Scalar.Name, field[0])))});",
        };

        private static string Operand(string name, string type, string scalar, char field) =>
            type == scalar ? name : $"{name}.{field}";

        private static string OperatorName(string symbol) => symbol switch
        {
            "implicit" => "op_Implicit",
            "explicit" => "op_Explicit",
            "+" => "op_Addition",
            "-" => "op_Subtraction",
            "*" => "op_Multiply",
            "/" => "op_Division",
            "%" => "op_Modulus",
            "++" => "op_Increment",
            "--" => "op_Decrement",
            "!" => "op_LogicalNot",
            "~" => "op_OnesComplement",
            "==" => "op_Equality",
            "!=" => "op_Inequality",
            "<" => "op_LessThan",
            "<=" => "op_LessThanOrEqual",
            ">" => "op_GreaterThan",
            ">=" => "op_GreaterThanOrEqual",
            "^" => "op_ExclusiveOr",
            "&" => "op_BitwiseAnd",
            "|" => "op_BitwiseOr",
            "<<" => "op_LeftShift",
            ">>" => "op_RightShift",
            _ => throw new InvalidOperationException($"Unsupported operator symbol '{symbol}'."),
        };

        private static FunctionSpec Aggregate(string name, string vector, string fields, string operation) => new()
        {
            Name = name,
            ReturnType = Type("bool"),
            Modifiers = Modifiers.Public | Modifiers.Static,
            Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
            Part = TypePart.Relational,
            Parameters = [Param("value", Type(vector))],
            Body = $"return {string.Join($" {operation} ", fields.Select(c => $"value.{c}"))};",
        };

    }
}
