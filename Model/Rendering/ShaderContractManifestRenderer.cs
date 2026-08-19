using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Delta.MathsGen.CodeModel;

namespace Delta.MathsGen.Model.Rendering
{
    internal sealed class ShaderContractManifestRenderer
    {
        public string Render(TypeSpec[] types)
        {
            var functions = types
                .SelectMany(type => type.Members.OfType<FunctionSpec>(), (type, function) => new { type.Name, Function = function })
                .ToArray();
            var unnamed = functions.FirstOrDefault(item => string.IsNullOrWhiteSpace(item.Function.Name));
            if (unnamed != null)
                throw new InvalidOperationException($"Shader contract function on type '{unnamed.Name}' has no stable CLR identity.");

            var manifest = new Manifest
            {
                SchemaVersion = "1.0.0",
                Namespace = "Delta.Maths",
                Types = types
                    .OrderBy(type => type.Name, StringComparer.Ordinal)
                    .Select(type => new ManifestType
                    {
                        ClrName = type.Name,
                        GlslName = type.ShaderContract.GlslName,
                        Mapping = type.ShaderContract.Mapping.ToString(),
                        ColumnMajor = type.ShaderContract.ColumnMajor,
                        Alignment = type.ShaderContract.Alignment,
                        MatrixStride = type.ShaderContract.MatrixStride,
                        RequiredCapability = type.ShaderContract.RequiredCapability,
                    })
                    .ToArray(),
                Functions = functions
                    .OrderBy(item => item.Name, StringComparer.Ordinal)
                    .ThenBy(item => item.Function.Name, StringComparer.Ordinal)
                    .ThenBy(item => string.Join(",", item.Function.Parameters.Select(parameter => parameter.Type.Name)), StringComparer.Ordinal)
                    .ThenBy(item => item.Function.ReturnType.Name, StringComparer.Ordinal)
                    .Select(item => new ManifestFunction
                    {
                        TypeClrName = item.Name,
                        ClrName = ClrName(item.Function),
                        MathsName = item.Function.MathsName,
                        GlslName = item.Function.ShaderContract.GlslName,
                        Mapping = item.Function.ShaderContract.Mapping.ToString(),
                        RequiredCapability = item.Function.ShaderContract.RequiredCapability,
                    })
                    .ToArray(),
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            return JsonSerializer.Serialize(manifest, options);
        }

        private static string ClrName(FunctionSpec function)
        {
            if (function is not OperatorSpec operatorSpec)
                return function.Name;

            var unary = operatorSpec.Parameters.Length == 1;
            return operatorSpec.Operator switch
            {
                "implicit" => "op_Implicit",
                "explicit" => "op_Explicit",
                "+" => unary ? "op_UnaryPlus" : "op_Addition",
                "-" => unary ? "op_UnaryNegation" : "op_Subtraction",
                "*" => "op_Multiply",
                "/" => "op_Division",
                "%" => "op_Modulus",
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
                "++" => "op_Increment",
                "--" => "op_Decrement",
                _ => throw new InvalidOperationException($"Unsupported operator symbol '{operatorSpec.Operator}'."),
            };
        }

        private sealed record Manifest
        {
            [JsonPropertyName("schemaVersion")]
            public required string SchemaVersion { get; init; }

            [JsonPropertyName("namespace")]
            public required string Namespace { get; init; }

            [JsonPropertyName("types")]
            public required ManifestType[] Types { get; init; }

            [JsonPropertyName("functions")]
            public required ManifestFunction[] Functions { get; init; }
        }

        private sealed record ManifestType
        {
            [JsonPropertyName("clrName")]
            public required string ClrName { get; init; }

            [JsonPropertyName("glslName")]
            public string? GlslName { get; init; }

            [JsonPropertyName("mapping")]
            public required string Mapping { get; init; }

            [JsonPropertyName("columnMajor")]
            public bool? ColumnMajor { get; init; }

            [JsonPropertyName("alignment")]
            public int? Alignment { get; init; }

            [JsonPropertyName("matrixStride")]
            public int? MatrixStride { get; init; }

            [JsonPropertyName("requiredCapability")]
            public string? RequiredCapability { get; init; }
        }

        private sealed record ManifestFunction
        {
            [JsonPropertyName("typeClrName")]
            public required string TypeClrName { get; init; }

            [JsonPropertyName("clrName")]
            public required string ClrName { get; init; }

            [JsonPropertyName("mathsName")]
            public required string MathsName { get; init; }

            [JsonPropertyName("glslName")]
            public string? GlslName { get; init; }

            [JsonPropertyName("mapping")]
            public required string Mapping { get; init; }

            [JsonPropertyName("requiredCapability")]
            public string? RequiredCapability { get; init; }
        }
    }
}
