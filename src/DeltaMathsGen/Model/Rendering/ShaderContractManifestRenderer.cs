using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Delta.MathsGen.Model.Rendering
{
    internal static class ShaderContractManifestRenderer
    {
        private static readonly Dictionary<string, string> ScalarGlslNames =
            new(StringComparer.Ordinal)
            {
                ["bool"] = "bool",
                ["int"] = "int",
                ["uint"] = "uint",
                ["float"] = "float",
                ["double"] = "double",
                ["half"] = "float16_t",
            };

        private static readonly Dictionary<string, string> IntegerScalarGlslNames =
            new(StringComparer.Ordinal)
            {
                ["UaddCarry|uint|uint,uint,uint"] = "uaddCarry",
                ["UsubBorrow|uint|uint,uint,uint"] = "usubBorrow",
                ["UmulExtended|void|uint,uint,uint,uint"] = "umulExtended",
                ["ImulExtended|void|int,int,int,int"] = "imulExtended",
                ["BitCount|int|int"] = "bitCount",
                ["BitCount|int|uint"] = "bitCount",
                ["FindLSB|int|int"] = "findLSB",
                ["FindLSB|int|uint"] = "findLSB",
                ["FindMSB|int|int"] = "findMSB",
                ["FindMSB|int|uint"] = "findMSB",
                ["BitfieldReverse|int|int"] = "bitfieldReverse",
                ["BitfieldReverse|uint|uint"] = "bitfieldReverse",
                ["BitfieldExtract|int|int,int,int"] = "bitfieldExtract",
                ["BitfieldExtract|uint|uint,int,int"] = "bitfieldExtract",
                ["BitfieldInsert|int|int,int,int,int"] = "bitfieldInsert",
                ["BitfieldInsert|uint|uint,uint,int,int"] = "bitfieldInsert",
            };

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string Render(TypeSpec[] types, ScalarMathMethod[] scalarMethods)
        {
            ArgumentNullException.ThrowIfNull(types);
            ArgumentNullException.ThrowIfNull(scalarMethods);
            var typeByName = types.ToDictionary(type => type.Name, StringComparer.Ordinal);
            var functions = types
                .SelectMany(type => type.Members.OfType<FunctionSpec>(), (type, function) => new { Type = type, Function = function })
                .OrderBy(item => item.Type.Name, StringComparer.Ordinal)
                .ThenBy(item => item.Function.Name, StringComparer.Ordinal)
                .ThenBy(item => string.Join(",", item.Function.Parameters.Select(parameter => parameter.Type.Name)), StringComparer.Ordinal)
                .ThenBy(item => item.Function.ReturnType.Name, StringComparer.Ordinal)
                .ToArray();

            var functionManifests = functions
                .Select(item => ToManifestFunction(item.Type, item.Function, typeByName))
                .Concat(functions
                    .Where(item =>
                        item.Function.Targets.HasFlag(FunctionTargets.ShaderDeltaMaths) &&
                        item.Function.ShaderContract.Mapping != ShaderMappingKind.Unsupported)
                    .Select(item => ToManifestFacadeFunction(item.Function, typeByName)))
                .Concat(ShaderScalarFunctions(scalarMethods, typeByName))
                .ToArray();
            var duplicateFunction = functionManifests
                .GroupBy(function => function.Identity, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateFunction != null)
            {
                throw new InvalidOperationException($"Duplicate shader contract function identity '{duplicateFunction.Key}'.");
            }

            var manifest = new Manifest
            {
                SchemaVersion = "1.1.0",
                Namespace = "Delta",
                Types = types
                    .OrderBy(type => type.Name, StringComparer.Ordinal)
                    .Select(type => ToManifestType(type, typeByName))
                    .ToArray(),
                Functions = functionManifests,
            };

            return JsonSerializer.Serialize(manifest, JsonOptions);
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
            {
                throw new InvalidOperationException($"Duplicate shader contract constructor identity '{duplicateConstructor.Key}'.");
            }

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
                MatrixColumns = type.ShaderContract.MatrixColumns,
                MatrixRows = type.ShaderContract.MatrixRows,
                ElementGlslType = type.ShaderContract.ElementGlslType,
                Size = type.ShaderContract.Size,
                RequiredCapability = RequiredCapability(type.ShaderContract),
                Swizzles = type.Members
                    .OfType<PropertySpec>()
                    .Where(property => property.Part == TypePart.Swizzles && !property.Name.Contains('_', StringComparison.Ordinal))
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
                DeltaMathsName = function.DeltaMathsName,
                ParameterClrNames = function.Parameters.Select(parameter => parameter.Type.Name).ToArray(),
                GlslParameterTypes = function.Parameters.Select(parameter => GlslType(parameter.Type.Name, types)).ToArray(),
                ParameterModifiers = function.Parameters.Select(parameter => ShaderMetadata.ModifierName(parameter.Modifier)).ToArray(),
                ReturnClrName = function.ReturnType.Name,
                GlslReturnType = GlslType(function.ReturnType.Name, types),
                GlslName = function.ShaderContract.GlslName,
                Mapping = function.ShaderContract.Mapping.ToString(),
                ShaderZone = ShaderZone(function.ShaderContract),
                Stages = StageNames(function.ShaderContract),
                RequiredCapability = RequiredCapability(function.ShaderContract),
            };
        }

        private static ManifestFunction ToManifestScalarFunction(
            ScalarMathMethod method,
            string glslName,
            IReadOnlyDictionary<string, TypeSpec> types)
        {
            var parameters = ParseScalarParameters(method.Parameters);
            return new ManifestFunction
            {
                Identity = "maths." + DeclarationHelpers.LowercaseFirst(method.Name) + StringParameters(parameters.Types) + ":" + method.ReturnType,
                TypeClrName = "maths",
                ClrName = DeclarationHelpers.LowercaseFirst(method.Name),
                DeltaMathsName = DeclarationHelpers.LowercaseFirst(method.Name),
                ParameterClrNames = parameters.Types,
                GlslParameterTypes = parameters.Types.Select(type => GlslType(type, types)).ToArray(),
                ParameterModifiers = parameters.Modifiers,
                ReturnClrName = method.ReturnType,
                GlslReturnType = GlslType(method.ReturnType, types),
                GlslName = glslName,
                Mapping = ShaderMappingKind.Builtin.ToString(),
                ShaderZone = ShaderMetadata.ZoneName(ShaderZoneKind.DeltaMaths),
                Stages = StageNames(new ShaderContract
                {
                    Mapping = ShaderMappingKind.Builtin,
                    Stages = ShaderStages.All,
                }),
                RequiredCapability = ShaderMetadata.CapabilityName(ScalarCapability(method.ReturnType, parameters.Types)),
            };
        }

        private static ShaderCapability ScalarCapability(string returnType, string[] parameterTypes) =>
            returnType == "double" || parameterTypes.Any(type => type == "double")
                ? ShaderCapability.Float64
                : returnType == "half" || parameterTypes.Any(type => type == "half")
                    ? ShaderCapability.Float16
                    : ShaderCapability.Scalar;

        private static ManifestFunction ToManifestFacadeFunction(
            FunctionSpec function,
            IReadOnlyDictionary<string, TypeSpec> types)
        {
            return new ManifestFunction
            {
                Identity = "maths." + function.DeltaMathsName + Parameters(function.Parameters) + ":" + function.ReturnType.Name,
                TypeClrName = "maths",
                ClrName = function.DeltaMathsName,
                DeltaMathsName = function.DeltaMathsName,
                ParameterClrNames = function.Parameters.Select(parameter => parameter.Type.Name).ToArray(),
                GlslParameterTypes = function.Parameters.Select(parameter => GlslType(parameter.Type.Name, types)).ToArray(),
                ParameterModifiers = function.Parameters.Select(parameter => ShaderMetadata.ModifierName(parameter.Modifier)).ToArray(),
                ReturnClrName = function.ReturnType.Name,
                GlslReturnType = GlslType(function.ReturnType.Name, types),
                GlslName = function.ShaderContract.GlslName,
                Mapping = function.ShaderContract.Mapping.ToString(),
                ShaderZone = ShaderZone(function.ShaderContract),
                Stages = StageNames(function.ShaderContract),
                RequiredCapability = RequiredCapability(function.ShaderContract),
            };
        }

        private static IEnumerable<ManifestFunction> ShaderScalarFunctions(
            ScalarMathMethod[] methods,
            IReadOnlyDictionary<string, TypeSpec> types)
        {
            foreach (var method in methods)
            {
                if (TryGetScalarGlslName(method, out var glslName))
                {
                    yield return ToManifestScalarFunction(method, glslName, types);
                }
            }
        }

        private static bool TryGetScalarGlslName(ScalarMathMethod method, out string glslName)
        {
            glslName = string.Empty;
            var parameters = ParseScalarParameters(method.Parameters);
            if (TryGetIntegerScalarGlslName(method, parameters.Types, out glslName))
            {
                return true;
            }

            if (method.Name == "PackDouble2x32"
                && method.ReturnType == "double"
                && parameters.Types.SequenceEqual(["uint2"], StringComparer.Ordinal)
                && parameters.Modifiers.SequenceEqual(["none"], StringComparer.Ordinal))
            {
                glslName = "packDouble2x32";
                return true;
            }

            if (method.Name == "UnpackDouble2x32"
                && method.ReturnType == "uint2"
                && parameters.Types.SequenceEqual(["double"], StringComparer.Ordinal)
                && parameters.Modifiers.SequenceEqual(["none"], StringComparer.Ordinal))
            {
                glslName = "unpackDouble2x32";
                return true;
            }

            if (method.ReturnType is "float" or "half" or "double")
            {
                var floatingType = method.ReturnType;
                glslName = method.Name switch
                {
                    "Modf" when parameters.Types.SequenceEqual([floatingType, floatingType], StringComparer.Ordinal)
                        && parameters.Modifiers.SequenceEqual(["none", "out"], StringComparer.Ordinal) => "modf",
                    "Frexp" when parameters.Types.SequenceEqual([floatingType, "int"], StringComparer.Ordinal)
                        && parameters.Modifiers.SequenceEqual(["none", "out"], StringComparer.Ordinal) => "frexp",
                    "Ldexp" when parameters.Types.SequenceEqual([floatingType, "int"], StringComparer.Ordinal)
                        && parameters.Modifiers.SequenceEqual(["none", "none"], StringComparer.Ordinal) => "ldexp",
                    "IntBitsToFloat" when parameters.Types.SequenceEqual(["int"], StringComparer.Ordinal) => "intBitsToFloat",
                    "UintBitsToFloat" when parameters.Types.SequenceEqual(["uint"], StringComparer.Ordinal) => "uintBitsToFloat",
                    _ => string.Empty,
                };
                if (glslName.Length != 0)
                {
                    return true;
                }
            }

            if (method.ReturnType is "int" or "uint"
                && parameters.Types.SequenceEqual(["float"], StringComparer.Ordinal)
                && parameters.Modifiers.SequenceEqual(["none"], StringComparer.Ordinal))
            {
                glslName = method.Name switch
                {
                    "FloatBitsToInt" when method.ReturnType == "int" => "floatBitsToInt",
                    "FloatBitsToUint" when method.ReturnType == "uint" => "floatBitsToUint",
                    _ => string.Empty,
                };
                if (glslName.Length != 0)
                {
                    return true;
                }
            }

            if (method.ReturnType is not ("float" or "half" or "double" or "bool"))
            {
                return false;
            }

            var floatingParameterType = method.ReturnType is "float" or "half" or "double"
                ? method.ReturnType
                : parameters.Types.FirstOrDefault(parameterType => parameterType is "float" or "half" or "double");
            if (floatingParameterType is not ("float" or "half" or "double")
                || parameters.Types.Any(parameterType => parameterType != floatingParameterType))
            {
                return false;
            }

            glslName = method.Name switch
            {
                "Lerp" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType, floatingParameterType], StringComparer.Ordinal) => "mix",
                "Mod" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType], StringComparer.Ordinal) => "mod",
                "Abs" when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) => "abs",
                "Fract" or "InverseSqrt" or "Radians" or "Degrees" or "Floor" or "Ceil"
                    when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) =>
                    method.Name switch
                    {
                        "InverseSqrt" => "inversesqrt",
                        _ => DeclarationHelpers.LowercaseFirst(method.Name),
                    },
                "Sin" or "Cos" or "Tan" or "Asin" or "Acos" or "Atan"
                    when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) =>
                    DeclarationHelpers.LowercaseFirst(method.Name),
                "Sinh" or "Cosh" or "Tanh" or "Asinh" or "Acosh" or "Atanh"
                    when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) =>
                    DeclarationHelpers.LowercaseFirst(method.Name),
                "Exp" or "Exp2" or "Log" or "Log2" or "Sqrt"
                    when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) =>
                    DeclarationHelpers.LowercaseFirst(method.Name),
                "Pow" or "Fma" when parameters.Types.All(parameter => parameter == floatingParameterType) && parameters.Types.Length > 1 =>
                    DeclarationHelpers.LowercaseFirst(method.Name),
                "RoundEven" when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) => "roundEven",
                "Truncate" when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) => "trunc",
                "Round" when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) => "roundEven",
                "Atan2" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType], StringComparer.Ordinal) => "atan",
                "Step" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType], StringComparer.Ordinal) => "step",
                "Smoothstep" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType, floatingParameterType], StringComparer.Ordinal) => "smoothstep",
                "Min" or "Max" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType], StringComparer.Ordinal) =>
                    DeclarationHelpers.LowercaseFirst(method.Name),
                "Clamp" when parameters.Types.SequenceEqual([floatingParameterType, floatingParameterType, floatingParameterType], StringComparer.Ordinal) => "clamp",
                "IsNaN" when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) => "isnan",
                "IsInfinity" when parameters.Types.SequenceEqual([floatingParameterType], StringComparer.Ordinal) => "isinf",
                _ => string.Empty,
            };
            return glslName.Length != 0;
        }

        private static bool TryGetIntegerScalarGlslName(
            ScalarMathMethod method,
            string[] parameterTypes,
            out string glslName)
        {
            if (IntegerScalarGlslNames.TryGetValue(
                    method.Name + "|" + method.ReturnType + "|" + string.Join(",", parameterTypes),
                    out var mappedName)
                && mappedName is not null)
            {
                glslName = mappedName;
                return true;
            }

            glslName = string.Empty;
            return false;
        }

        private static ScalarParameterLists ParseScalarParameters(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                return new([], []);
            }

            var declarations = parameters.Split(',');
            var types = new string[declarations.Length];
            var modifiers = new string[declarations.Length];
            for (var index = 0; index < declarations.Length; index++)
            {
                var declaration = declarations[index];
                var tokens = declaration.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2)
                {
                    throw new InvalidOperationException($"Unable to parse scalar parameter declaration '{declaration}'.");
                }

                types[index] = tokens[^2];
                modifiers[index] = tokens[0] switch
                {
                    "out" => "out",
                    "ref" => "ref",
                    _ => "none",
                };
            }

            return new(types, modifiers);
        }

        private static string? GlslType(string clrName, IReadOnlyDictionary<string, TypeSpec> types)
        {
            if (ScalarGlslNames.TryGetValue(clrName, out var scalarName))
            {
                return scalarName;
            }

            if (clrName == "void")
            {
                return "void";
            }

            return types.TryGetValue(clrName, out var type)
                && type.ShaderContract.Mapping != ShaderMappingKind.Unsupported
                ? type.ShaderContract.GlslName
                : null;
        }

        private static string? ShaderZone(ShaderContract contract) =>
            contract.Mapping == ShaderMappingKind.Unsupported ? null : ShaderMetadata.ZoneName(contract.Zone);

        private static string? RequiredCapability(ShaderContract contract) =>
            contract.Mapping == ShaderMappingKind.Unsupported ? null : ShaderMetadata.CapabilityName(contract.Capability);

        private static string[] StageNames(ShaderContract contract)
        {
            var stages = contract.Mapping == ShaderMappingKind.Unsupported
                ? ShaderStages.None
                : contract.Stages == ShaderStages.None ? ShaderStages.All : contract.Stages;
            var names = new List<string>(3);
            if (stages.HasFlag(ShaderStages.Vertex))
            {
                names.Add("vertex");
            }

            if (stages.HasFlag(ShaderStages.Fragment))
            {
                names.Add("fragment");
            }

            if (stages.HasFlag(ShaderStages.Compute))
            {
                names.Add("compute");
            }

            return names.ToArray();
        }

        private static string ClrName(FunctionSpec function)
        {
            if (function is not OperatorSpec operatorSpec)
            {
                return function.Name;
            }

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

        private static string StringParameters(string[] parameters) =>
            "(" + string.Join(",", parameters) + ")";

        private readonly record struct ScalarParameterLists(string[] Types, string[] Modifiers);

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
            [JsonPropertyName("matrixColumns")] public int? MatrixColumns { get; init; }
            [JsonPropertyName("matrixRows")] public int? MatrixRows { get; init; }
            [JsonPropertyName("elementGlslType")] public string? ElementGlslType { get; init; }
            [JsonPropertyName("size")] public int? Size { get; init; }
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
            [JsonPropertyName("mathsName")] public required string DeltaMathsName { get; init; }
            [JsonPropertyName("parameterClrNames")] public required string[] ParameterClrNames { get; init; }
            [JsonPropertyName("parameterGlslTypes")] public required string?[] GlslParameterTypes { get; init; }
            [JsonPropertyName("parameterModifiers")] public required string[] ParameterModifiers { get; init; }
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
