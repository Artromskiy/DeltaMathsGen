using System.Linq;
using System.Collections.Generic;
using System;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal sealed class VectorFamily
    {
        public required ScalarDefinition Scalar { get; init; }
        public required int Dimension { get; init; }

        public TypeSpec Create()
        {
            if (Dimension is < 2 or > 4)
                throw new ArgumentOutOfRangeException(nameof(Dimension), "Vector dimensions must be between 2 and 4.");

            var scalarName = Scalar.Name;
            var name = scalarName + Dimension;
            var fields = "xyzw"[..Dimension];
            var context = new VectorContext { Scalar = Scalar, Dimension = Dimension };
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

            if (Scalar.Supports(ScalarCapabilities.Arithmetic))
            {
                if (Scalar.Supports(ScalarCapabilities.UnaryPlus)) members.Add(CreateUnaryOperator(name, fields, "+"));
                if (Scalar.Supports(ScalarCapabilities.Signed)) members.Add(CreateUnaryOperator(name, fields, "-"));
                if (Scalar.Supports(ScalarCapabilities.Increment))
                {
                    members.Add(CreateUnaryOperator(name, fields, "++"));
                    members.Add(CreateUnaryOperator(name, fields, "--"));
                }
                members.Add(CreateBinaryOperator(name, fields, "+"));
                members.Add(CreateBinaryOperator(name, fields, "-"));
                members.Add(CreateBinaryOperator(name, fields, "*"));
                members.Add(CreateBinaryOperator(name, fields, "/"));
                members.AddRange(CreateScalarOperators(name, fields));
            }

            members.AddRange(VectorOperatorCatalog.Create(context));
            members.AddRange(VectorFunctionCatalog.Create(context));

            members.AddRange(CreateSwizzles(fields));

            return new TypeSpec
            {
                Namespace = "Delta.Maths",
                Name = name,
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = [$"IEquatable<{name}>", $"IComparable<{name}>"],
                Comment = $"A vector of type {scalarName} with {Dimension} components.",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }

        private FieldSpec[] CreateFields(string fields) =>
            fields.Select((component, index) => new FieldSpec
            {
                Name = component.ToString(),
                Type = Type(Scalar.Name),
                Attributes = [$"System.Runtime.Serialization.DataMember(Order = {index})"],
            }).ToArray();

        private MemberSpec[] CreateParseFunctions(string name, string fields)
        {
            var split = $$"""
                value = value.Trim();
                if (value.Length >= 2 && value[0] == '[' && value[value.Length - 1] == ']')
                    value = value.Substring(1, value.Length - 2);
                var values = value.Split(',');
                if (values.Length != {{Dimension}})
                    throw new FormatException("Expected {{Dimension}} vector components.");

                """;
            var parsed = string.Join(", ", Enumerable.Range(0, Dimension).Select(index => Scalar.ParseAcceptsFormatProvider
                ? $"{Scalar.Name}.Parse(values[{index}].Trim(), System.Globalization.CultureInfo.InvariantCulture)"
                : $"{Scalar.Name}.Parse(values[{index}].Trim())"));
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
            if (Scalar.ParseAcceptsFormatProvider)
            {
                var formatted = string.Join(", ", Enumerable.Range(0, Dimension).Select(index => $"{Scalar.Name}.Parse(values[{index}].Trim(), format)"));
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
            Parameters = fields.Select(component => Param(component.ToString(), Type(Scalar.Name))).ToArray(),
            Body = string.Join("\n", fields.Select(component => $"this.{component} = {component};")),
        };

        private ConstructorSpec CreateScalarConstructor(string name, string fields) => new()
        {
            Parameters = [Param("value", Type(Scalar.Name))],
            Body = string.Join("\n", fields.Select(component => $"{component} = value;")),
        };

        private ConstructorSpec[] CreateVectorConstructors(string name, string fields)
        {
            var constructors = new List<ConstructorSpec>();
            foreach (var sourceDimension in new[] { 2, 3, 4 })
            {
                var sourceName = Scalar.Name + sourceDimension;
                var assignments = fields.Select((component, index) =>
                    $"{component} = {(index < sourceDimension ? $"value.{"xyzw"[index]}" : DefaultValue())};");
                constructors.Add(new ConstructorSpec
                {
                    Parameters = [Param("value", Type(sourceName))],
                    Body = string.Join("\n", assignments),
                });
            }
            AddMixedConstructors(constructors, fields);
            return constructors.ToArray();
        }

        private void AddMixedConstructors(List<ConstructorSpec> constructors, string fields)
        {
            var scalar = Type(Scalar.Name);
            foreach (var partition in ComponentPartitions(Dimension))
            {
                if (partition.Length == 1 || partition.All(length => length == 1))
                    continue;

                var parameters = new List<ParameterSpec>();
                var assignments = new List<string>();
                var offset = 0;
                foreach (var length in partition)
                {
                    var parameterName = fields.Substring(offset, length);
                    parameters.Add(Param(parameterName, length == 1 ? scalar : Type(Scalar.Name + length)));
                    for (var component = 0; component < length; component++)
                    {
                        var target = fields[offset + component];
                        var source = length == 1 ? parameterName : parameterName + "." + "xyzw"[component];
                        assignments.Add($"this.{target} = {source};");
                    }
                    offset += length;
                }

                constructors.Add(new ConstructorSpec
                {
                    Parameters = parameters.ToArray(),
                    Body = string.Join("\n", assignments),
                });
            }
        }

        private static int[][] ComponentPartitions(int total)
        {
            var result = new List<int[]>();
            BuildPartition(total, [], result);
            return result.ToArray();
        }

        private static void BuildPartition(int remaining, int[] prefix, List<int[]> result)
        {
            if (remaining == 0)
            {
                result.Add(prefix);
                return;
            }

            for (var length = 1; length <= Math.Min(3, remaining); length++)
                BuildPartition(remaining - length, [.. prefix, length], result);
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
                    Body = $"return FormattableString.Invariant($\"{string.Join(", ", fields.Select(component => "{" + component + "}"))}\");",
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
            Type = Type(Scalar.Name),
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
            Name = UnaryOperatorName(symbol),
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type(name),
            Parameters = [Param("value", Type(name))],
            Body = $"return new({string.Join(", ", fields.Select(component => $"{symbol}value.{component}"))});",
        };

        private OperatorSpec CreateBinaryOperator(string name, string fields, string symbol) => new()
        {
            Name = OperatorName(symbol),
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
                    Name = OperatorName(symbol),
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = symbol,
                    ReturnType = Type(name),
                    Parameters = [Param("left", Type(name)), Param("right", Type(Scalar.Name))],
                    Body = $"return new({string.Join(", ", fields.Select(component => $"left.{component} {symbol} right"))});",
                });
                result.Add(new OperatorSpec
                {
                    Name = OperatorName(symbol),
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = symbol,
                    ReturnType = Type(name),
                    Parameters = [Param("left", Type(Scalar.Name)), Param("right", Type(name))],
                    Body = $"return new({string.Join(", ", fields.Select(component => $"left {symbol} right.{component}"))});",
                });
            }
            return result.ToArray();
        }

        private OperatorSpec CreateEqualityOperator(string name, string fields, string symbol) => new()
        {
            Name = OperatorName(symbol),
            Part = TypePart.Operators,
            Modifiers = Modifiers.Public | Modifiers.Static,
            Operator = symbol,
            ReturnType = Type("bool"),
            Parameters = [Param("left", Type(name)), Param("right", Type(name))],
            Body = $"return {string.Join(symbol == "==" ? " && " : " || ", fields.Select(component => $"left.{component} {symbol} right.{component}"))};",
        };

        private static string OperatorName(string symbol) => symbol switch
        {
            "+" => "op_Addition",
            "-" => "op_Subtraction",
            "*" => "op_Multiply",
            "/" => "op_Division",
            "==" => "op_Equality",
            "!=" => "op_Inequality",
            _ => throw new InvalidOperationException($"Unsupported operator symbol '{symbol}'."),
        };

        private static string UnaryOperatorName(string symbol) => symbol switch
        {
            "+" => "op_UnaryPlus",
            "-" => "op_UnaryNegation",
            "++" => "op_Increment",
            "--" => "op_Decrement",
            _ => throw new InvalidOperationException($"Unsupported unary operator symbol '{symbol}'."),
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
                var vectorType = Type(Scalar.Name + length);
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
                    Type = Type(Scalar.Name),
                    Part = TypePart.Swizzles,
                    Getter = fields[index].ToString(),
                    Setter = $"{fields[index]} = value",
                });
            }
        }

        private int[][] Combinations(int length)
        {
            var values = Enumerable.Range(-1, Dimension + 1).ToArray();
            var count = (int)System.Math.Pow(values.Length, length);
            var combinations = new int[count][];
            for (var number = 0; number < count; number++)
            {
                var current = number;
                var result = new int[length];
                for (var index = length - 1; index >= 0; index--)
                {
                    result[index] = values[current % values.Length];
                    current /= values.Length;
                }
                combinations[number] = result;
            }
            return combinations;
        }

        private string DefaultValue() => Scalar.ZeroLiteral;
    }
}
