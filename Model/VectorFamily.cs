using System.Linq;
using System.Collections.Generic;
using System;
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
            var members = new List<MemberSpec>();
            members.AddRange(CreateFields(fields));
            members.Add(new FieldSpec
            {
                Name = "zero",
                Type = Type(name),
                Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
                Initializer = $"new {name}({string.Join(", ", Enumerable.Repeat(DefaultValue(), Dimension))})",
            });
            members.Add(CreateConstructor(name, fields));
            members.Add(CreateScalarConstructor(name, fields));
            members.AddRange(CreateVectorConstructors(name, fields));
            members.Add(CreateIndexer(fields));
            members.Add(new PropertySpec
            {
                Name = "Count",
                Type = Type("int"),
                Expression = Dimension.ToString(),
            });
            members.Add(CreateEqualityOperator(name, fields, "=="));
            members.Add(CreateEqualityOperator(name, fields, "!="));
            members.AddRange(CreateObjectContract(name, fields));
            members.AddRange(CreateParseFunctions(name, fields));

            if (ScalarName != "bool")
            {
                if (ScalarName != "uint") members.Add(CreateUnaryOperator(name, fields, "-"));
                members.Add(CreateBinaryOperator(name, fields, "+"));
                members.Add(CreateBinaryOperator(name, fields, "-"));
                members.Add(CreateBinaryOperator(name, fields, "*"));
                members.Add(CreateBinaryOperator(name, fields, "/"));
                members.AddRange(CreateScalarOperators(name, fields));
            }

            members.AddRange(VectorOperatorCatalog.Create(ScalarName, Dimension));
            members.AddRange(VectorFunctionCatalog.Create(ScalarName, Dimension));

            members.AddRange(CreateSwizzles(fields));

            return new TypeSpec
            {
                Namespace = "KibiHex",
                Name = name,
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = [$"IEquatable<{name}>", $"IComparable<{name}>"],
                Comment = $"A vector of type {ScalarName} with {Dimension} components.",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }

        private FieldSpec[] CreateFields(string fields) =>
            fields.Select((component, index) => new FieldSpec
            {
                Name = component.ToString(),
                Type = Type(ScalarName),
                Attributes = [$"System.Runtime.Serialization.DataMember(Order = {index})"],
            }).ToArray();

        private MemberSpec[] CreateParseFunctions(string name, string fields)
        {
            var split = "var values = value.Split(',');\n";
            var parsed = string.Join(", ", Enumerable.Range(0, Dimension).Select(index => $"{ScalarName}.Parse(values[{index}])"));
            var result = new List<MemberSpec>
            {
                new FunctionSpec
                {
                    Name = "Parse",
                    ReturnType = Type(name),
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Parameters = [Param("value", Type("string"))],
                    Body = split + $"return new({parsed});",
                },
            };
            if (ScalarName != "bool")
            {
                var formatted = string.Join(", ", Enumerable.Range(0, Dimension).Select(index => $"{ScalarName}.Parse(values[{index}], format)"));
                result.Add(new FunctionSpec
                {
                    Name = "Parse",
                    ReturnType = Type(name),
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Parameters = [Param("value", Type("string")), Param("format", Type("IFormatProvider"))],
                    Body = split + $"return new({formatted});",
                });
            }
            return result.ToArray();
        }

        private ConstructorSpec CreateConstructor(string name, string fields) => new()
        {
            Parameters = fields.Select(component => Param(component.ToString(), Type(ScalarName))).ToArray(),
            Body = string.Join("\n", fields.Select(component => $"this.{component} = {component};")),
        };

        private ConstructorSpec CreateScalarConstructor(string name, string fields) => new()
        {
            Parameters = [Param("value", Type(ScalarName))],
            Body = string.Join("\n", fields.Select(component => $"{component} = value;")),
        };

        private ConstructorSpec[] CreateVectorConstructors(string name, string fields)
        {
            var constructors = new List<ConstructorSpec>();
            foreach (var sourceDimension in new[] { 2, 3, 4 })
            {
                var sourceName = ScalarName + sourceDimension;
                var assignments = fields.Select((component, index) =>
                    $"{component} = {(index < sourceDimension ? $"value.{"xyzw"[index]}" : DefaultValue())};");
                constructors.Add(new ConstructorSpec
                {
                    Parameters = [Param("value", Type(sourceName))],
                    Body = string.Join("\n", assignments),
                });
            }
            return constructors.ToArray();
        }

        private MemberSpec[] CreateObjectContract(string name, string fields)
        {
            var equality = string.Join(" && ", fields.Select(component => $"{component}.Equals(other.{component})"));
            var hashBody = "unchecked\n{\n    var hash = 17;\n" +
                string.Join("\n", fields.Select(component => $"    hash = hash * 31 + {component}.GetHashCode();")) +
                "\n    return hash;\n}";
            return
            [
                new FunctionSpec
                {
                    Name = "Equals",
                    ReturnType = Type("bool"),
                    Parameters = [Param("other", Type(name))],
                    Body = $"return {equality};",
                },
                new FunctionSpec
                {
                    Name = "Equals",
                    ReturnType = Type("bool"),
                    Modifiers = Modifiers.Public | Modifiers.Override,
                    Parameters = [Param("obj", Type("object"))],
                    Body = $"return obj is {name} other && Equals(other);",
                },
                new FunctionSpec
                {
                    Name = "GetHashCode",
                    ReturnType = Type("int"),
                    Modifiers = Modifiers.Public | Modifiers.Override,
                    Body = hashBody,
                },
                new FunctionSpec
                {
                    Name = "CompareTo",
                    ReturnType = Type("int"),
                    Parameters = [Param("other", Type(name))],
                    Body = CompareBody(fields),
                },
                new FunctionSpec
                {
                    Name = "ToString",
                    ReturnType = Type("string"),
                    Modifiers = Modifiers.Public | Modifiers.Override,
                    Body = $"return $\"[{string.Join(", ", fields.Select(component => "{" + component + "}"))}]\";",
                },
            ];
        }

        private static string CompareBody(string fields)
        {
            var lines = new List<string>();
            foreach (var component in fields)
            {
                lines.Add($"var {component}Comparison = {component}.CompareTo(other.{component});");
                lines.Add($"if ({component}Comparison != 0) return {component}Comparison;");
            }
            lines.Add("return 0;");
            return string.Join("\n", lines);
        }

        private IndexerSpec CreateIndexer(string fields) => new()
        {
            Type = Type(ScalarName),
            Parameter = Param("index", Type("int")),
            Getter =
            """
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return Unsafe.Add(ref x, index);
            """,
            Setter =
            """
            if ((uint)index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            Unsafe.Add(ref x, index) = value;
            """,
        };

        private OperatorSpec CreateUnaryOperator(string name, string fields, string symbol) => new()
        {
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(name),
            Parameters = [Param("value", Type(name))],
            Body = $"return new({string.Join(", ", fields.Select(component => $"{symbol}value.{component}"))});",
        };

        private OperatorSpec CreateBinaryOperator(string name, string fields, string symbol) => new()
        {
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(name),
            Parameters = [Param("left", Type(name)), Param("right", Type(name))],
            Body = $"return new({string.Join(", ", fields.Select(component => $"left.{component} {symbol} right.{component}"))});",
        };

        private OperatorSpec[] CreateScalarOperators(string name, string fields)
        {
            var result = new List<OperatorSpec>();
            foreach (var symbol in new[] { "+", "-", "*", "/" })
            {
                result.Add(new OperatorSpec
                {
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = symbol,
                    ReturnType = Type(name),
                    Parameters = [Param("left", Type(name)), Param("right", Type(ScalarName))],
                    Body = $"return new({string.Join(", ", fields.Select(component => $"left.{component} {symbol} right"))});",
                });
                result.Add(new OperatorSpec
                {
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = symbol,
                    ReturnType = Type(name),
                    Parameters = [Param("left", Type(ScalarName)), Param("right", Type(name))],
                    Body = $"return new({string.Join(", ", fields.Select(component => $"left {symbol} right.{component}"))});",
                });
            }
            return result.ToArray();
        }

        private OperatorSpec CreateEqualityOperator(string name, string fields, string symbol) => new()
        {
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type("bool"),
            Parameters = [Param("left", Type(name)), Param("right", Type(name))],
            Body = $"return {string.Join(symbol == "==" ? " && " : " || ", fields.Select(component => $"left.{component} {symbol} right.{component}"))};",
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

        private PropertySpec[] CreateSwizzles(string fields)
        {
            var result = new List<PropertySpec>();
            AddComponentAliases(result, fields, "rgba");
            AddComponentAliases(result, fields, "stpq");

            foreach (var alphabet in new[] { "xyzw", "rgba", "stpq" })
            foreach (var length in new[] { 2, 3, 4 })
            foreach (var indices in Combinations(length))
            {
                if (indices.All(index => index < 0))
                    continue;

                var propertyName = string.Concat(indices.Select(index => index < 0 ? '_' : alphabet[index]));
                var vectorType = Type(ScalarName + length);
                var values = indices.Select(index => index < 0 ? DefaultValue() : fields[index].ToString()).ToArray();
                var canWrite = indices.All(index => index >= 0) && indices.Distinct().Count() == indices.Length;

                result.Add(new PropertySpec
                {
                    Name = propertyName,
                    Type = vectorType,
                    Part = TypePart.Swizzles,
                    Attributes = ["System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)"],
                    Getter = $"new {vectorType}({string.Join(", ", values)})",
                    Setter = canWrite
                        ? string.Join("\n", indices.Select((index, component) => $"{fields[index]} = value.{"xyzw"[component]};"))
                        : null,
                });
            }

            return result.ToArray();
        }

        private void AddComponentAliases(List<PropertySpec> result, string fields, string alphabet)
        {
            for (var index = 0; index < Dimension; index++)
            {
                result.Add(new PropertySpec
                {
                    Name = alphabet[index].ToString(),
                    Type = Type(ScalarName),
                    Part = TypePart.Swizzles,
                    Getter = fields[index].ToString(),
                    Setter = $"{fields[index]} = value",
                });
            }
        }

        private IEnumerable<int[]> Combinations(int length)
        {
            var values = Enumerable.Range(-1, Dimension + 1).ToArray();
            var count = (int)System.Math.Pow(values.Length, length);
            for (var number = 0; number < count; number++)
            {
                var current = number;
                var result = new int[length];
                for (var index = length - 1; index >= 0; index--)
                {
                    result[index] = values[current % values.Length];
                    current /= values.Length;
                }
                yield return result;
            }
        }

        private string DefaultValue() => ScalarName switch
        {
            "float" => "0f",
            "double" => "0.0",
            "uint" => "0u",
            "bool" => "false",
            _ => "0",
        };
    }
}
