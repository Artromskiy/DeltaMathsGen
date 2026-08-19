using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Delta.MathsGen.CodeModel;

namespace Delta.MathsGen.Model.Rendering
{
    internal sealed class ShaderContractManifestRenderer
    {
        private static readonly IReadOnlyDictionary<string, string> ScalarGlslNames =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bool"] = "bool",
                ["int"] = "int",
                ["uint"] = "uint",
                ["float"] = "float",
            };

        public string Render(TypeSpec[] types)
        {
            var typeByName = types.ToDictionary(type => type.Name, StringComparer.Ordinal);
            var functions = types
                .SelectMany(type => type.Members.OfType<FunctionSpec>(), (type, function) => new { Type = type, Function = function })
                .OrderBy(item => item.Type.Name, StringComparer.Ordinal)
                .ThenBy(item => item.Function.Name, StringComparer.Ordinal)
                .ThenBy(item => string.Join(",", item.Function.Parameters.Select(parameter => parameter.Type.Name)), StringComparer.Ordinal)
                .ThenBy(item => item.Function.ReturnType.Name, StringComparer.Ordinal)
                .ToArray();

            var functionManifests = functions.Select(item => ToManifestFunction(item.Type, item.Function, typeByName)).ToArray();
            var duplicateFunction = functionManifests
                .GroupBy(function => function.Identity, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateFunction != null)
                throw new InvalidOperationException($"Duplicate shader contract function identity '{duplicateFunction.Key}'.");

            var manifest = new Manifest
            {
                SchemaVersion = "1.1.0",
                Namespace = "Delta.Maths",
                Types = types
                    .OrderBy(type => type.Name, StringComparer.Ordinal)
                    .Select(type => ToManifestType(type, typeByName))
                    .ToArray(),
                Functions = functionManifests,
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(manifest, options);
        }

        private static ManifestType ToManifestType(TypeSpec type, IReadOnlyDictionary<string, TypeSpec> types)
        {
            var constructors = type.Members
                .OfType<ConstructorSpec>()
                .OrderBy(constructor => string.Join(",", constructor.Parameters.Select(parameter => parameter.Type.Name)), StringComparer.Ordinal)
                .Select(constructor => new ManifestConstructor
                {
                    Identity = type.Name + Parameters(constructor.Parameters),
                    ParameterClrNames = constructor.Parameters.Select(parameter => parameter.Type.Name).ToArray(),
                    ParameterGlslTypes = constructor.Parameters.Select(parameter => GlslType(parameter.Type.Name, types)).ToArray(),
                })
                .ToArray();
            var duplicateConstructor = constructors
                .GroupBy(constructor => constructor.Identity, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateConstructor != null)
                throw new InvalidOperationException($"Duplicate shader contract constructor identity '{duplicateConstructor.Key}'.");

            return new ManifestType
            {
                ClrName = type.Name,
                GlslName = type.ShaderContract.GlslName,
                Mapping = type.ShaderContract.Mapping.ToString(),
                ShaderZone = ShaderZone(type.ShaderContract),
                Stages = StageNames(type.ShaderContract),
                ColumnMajor = type.ShaderContract.ColumnMajor,
                Alignment = type.ShaderContract.Alignment,
                MatrixStride = type.ShaderContract.MatrixStride,
                RequiredCapability = type.ShaderContract.RequiredCapability,
                Swizzles = type.Members
                    .OfType<PropertySpec>()
                    .Where(property => property.Part == TypePart.Swizzles && !property.Name.Contains('_'))
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new ManifestSwizzle
                    {
                        Name = property.Name,
                        ClrTypeName = property.Type.Name,
                        GlslType = GlslType(property.Type.Name, types),
                        Writable = property.Setter != null,
                    })
                    .ToArray(),
                Constructors = constructors,
            };
        }

        private static ManifestFunction ToManifestFunction(
            TypeSpec type,
            FunctionSpec function,
            IReadOnlyDictionary<string, TypeSpec> types)
        {
            var clrName = ClrName(function);
            return new ManifestFunction
            {
                Identity = type.Name + "." + clrName + Parameters(function.Parameters) + ":" + function.ReturnType.Name,
                TypeClrName = type.Name,
                ClrName = clrName,
                MathsName = function.MathsName,
                ParameterClrNames = function.Parameters.Select(parameter => parameter.Type.Name).ToArray(),
                GlslParameterTypes = function.Parameters.Select(parameter => GlslType(parameter.Type.Name, types)).ToArray(),
                ReturnClrName = function.ReturnType.Name,
                GlslReturnType = GlslType(function.ReturnType.Name, types),
                GlslName = function.ShaderContract.GlslName,
                Mapping = function.ShaderContract.Mapping.ToString(),
                ShaderZone = ShaderZone(function.ShaderContract),
                Stages = StageNames(function.ShaderContract),
                RequiredCapability = function.ShaderContract.RequiredCapability,
            };
        }

        private static string? GlslType(string clrName, IReadOnlyDictionary<string, TypeSpec> types)
        {
            if (ScalarGlslNames.TryGetValue(clrName, out var scalarName))
                return scalarName;
            if (clrName == "void")
                return "void";
            return types.TryGetValue(clrName, out var type)
                && type.ShaderContract.Mapping != ShaderMappingKind.Unsupported
                ? type.ShaderContract.GlslName
                : null;
        }

        private static string? ShaderZone(ShaderContract contract) =>
            contract.Mapping == ShaderMappingKind.Unsupported ? null : contract.Zone ?? "Delta.Maths";

        private static string[] StageNames(ShaderContract contract)
        {
            var stages = contract.Mapping == ShaderMappingKind.Unsupported
                ? ShaderStages.None
                : contract.Stages == ShaderStages.None ? ShaderStages.All : contract.Stages;
            var names = new List<string>(3);
            if (stages.HasFlag(ShaderStages.Vertex)) names.Add("vertex");
            if (stages.HasFlag(ShaderStages.Fragment)) names.Add("fragment");
            if (stages.HasFlag(ShaderStages.Compute)) names.Add("compute");
            return names.ToArray();
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

        private static string Parameters(ParameterSpec[] parameters) =>
            "(" + string.Join(",", parameters.Select(parameter => parameter.Type.Name)) + ")";

        private sealed record Manifest
        {
            [JsonPropertyName("schemaVersion")] public required string SchemaVersion { get; init; }
            [JsonPropertyName("namespace")] public required string Namespace { get; init; }
            [JsonPropertyName("types")] public required ManifestType[] Types { get; init; }
            [JsonPropertyName("functions")] public required ManifestFunction[] Functions { get; init; }
        }

        private sealed record ManifestType
        {
            [JsonPropertyName("clrName")] public required string ClrName { get; init; }
            [JsonPropertyName("glslName")] public string? GlslName { get; init; }
            [JsonPropertyName("mapping")] public required string Mapping { get; init; }
            [JsonPropertyName("shaderZone")] public string? ShaderZone { get; init; }
            [JsonPropertyName("stages")] public required string[] Stages { get; init; }
            [JsonPropertyName("columnMajor")] public bool? ColumnMajor { get; init; }
            [JsonPropertyName("alignment")] public int? Alignment { get; init; }
            [JsonPropertyName("matrixStride")] public int? MatrixStride { get; init; }
            [JsonPropertyName("requiredCapability")] public string? RequiredCapability { get; init; }
            [JsonPropertyName("constructors")] public required ManifestConstructor[] Constructors { get; init; }
            [JsonPropertyName("swizzles")] public required ManifestSwizzle[] Swizzles { get; init; }
        }

        private sealed record ManifestConstructor
        {
            [JsonPropertyName("identity")] public required string Identity { get; init; }
            [JsonPropertyName("parameterClrNames")] public required string[] ParameterClrNames { get; init; }
            [JsonPropertyName("parameterGlslTypes")] public required string?[] ParameterGlslTypes { get; init; }
        }

        private sealed record ManifestSwizzle
        {
            [JsonPropertyName("name")] public required string Name { get; init; }
            [JsonPropertyName("clrTypeName")] public required string ClrTypeName { get; init; }
            [JsonPropertyName("glslType")] public string? GlslType { get; init; }
            [JsonPropertyName("writable")] public bool Writable { get; init; }
        }

        private sealed record ManifestFunction
        {
            [JsonPropertyName("identity")] public required string Identity { get; init; }
            [JsonPropertyName("typeClrName")] public required string TypeClrName { get; init; }
            [JsonPropertyName("clrName")] public required string ClrName { get; init; }
            [JsonPropertyName("mathsName")] public required string MathsName { get; init; }
            [JsonPropertyName("parameterClrNames")] public required string[] ParameterClrNames { get; init; }
            [JsonPropertyName("parameterGlslTypes")] public required string?[] GlslParameterTypes { get; init; }
            [JsonPropertyName("returnClrName")] public required string ReturnClrName { get; init; }
            [JsonPropertyName("returnGlslType")] public string? GlslReturnType { get; init; }
            [JsonPropertyName("glslName")] public string? GlslName { get; init; }
            [JsonPropertyName("mapping")] public required string Mapping { get; init; }
            [JsonPropertyName("shaderZone")] public string? ShaderZone { get; init; }
            [JsonPropertyName("stages")] public required string[] Stages { get; init; }
            [JsonPropertyName("requiredCapability")] public string? RequiredCapability { get; init; }
        }
    }
}
