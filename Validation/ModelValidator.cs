using System;
using System.Collections.Generic;
using System.Linq;
using Delta.MathsGen.Model;

namespace Delta.MathsGen.Validation
{
    internal static class ModelValidator
    {
        public static void Validate(ScalarDefinition[] scalars, TypeSpec[] types)
        {
            ValidateScalars(scalars);
            ValidateRules(VectorFunctionCatalog.Rules);

            var duplicateType = types.GroupBy(type => type.Name, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateType != null)
                Fail($"Type '{duplicateType.Key}' is declared more than once.");

            foreach (var type in types)
                ValidateType(type);

            ValidateShaderMaths(types);
            ValidateShaderContracts(types);
        }

        private static void ValidateRules(VectorFunctionRule[] rules)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules)
            {
                if (string.IsNullOrWhiteSpace(rule.Name))
                    Fail("A vector function rule has no name.");
                if (!names.Add(rule.Name))
                    Fail($"Vector function rule '{rule.Name}' is declared more than once.");
                if (rule.RequiredDimension != 0 && rule.RequiredDimension is < 2 or > 4)
                    Fail($"Rule '{rule.Name}' requires unsupported dimension {rule.RequiredDimension}.");
            }
        }

        private static void ValidateScalars(ScalarDefinition[] scalars)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var scalar in scalars)
            {
                if (string.IsNullOrWhiteSpace(scalar.Name))
                    Fail("A scalar type has no name.");
                if (!names.Add(scalar.Name))
                    Fail($"Scalar type '{scalar.Name}' is declared more than once.");
            }

            foreach (var scalar in scalars)
            foreach (var target in scalar.ImplicitTargets.Concat(scalar.ExplicitTargets))
                if (!names.Contains(target))
                    Fail($"Conversion from '{scalar.Name}' targets unknown scalar '{target}'.");
        }

        private static void ValidateType(TypeSpec type)
        {
            if (string.IsNullOrWhiteSpace(type.Name))
                Fail("A generated type has no name.");

            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in type.Members)
            {
                if (member is FunctionSpec function)
                {
                    if (function.Targets == FunctionTargets.None)
                        Fail($"Function '{type.Name}.{function.Name}' has no target API.");
                    if (function.Targets.HasFlag(FunctionTargets.ShaderMaths) &&
                        !function.Targets.HasFlag(FunctionTargets.Type))
                        Fail($"Function '{type.Name}.{function.Name}' cannot forward to maths without a type implementation.");
                }

                var signature = Signature(member);
                if (!signatures.Add(signature))
                    Fail($"Duplicate member '{signature}' in type '{type.Name}'.");
            }
        }

        private static void ValidateShaderMaths(TypeSpec[] types)
        {
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var type in types)
            foreach (var function in type.Members.OfType<FunctionSpec>())
            {
                if (!function.Targets.HasFlag(FunctionTargets.ShaderMaths))
                    continue;

                var signature = function.MathsName + Parameters(function.Parameters);
                if (!signatures.Add(signature))
                    Fail($"Duplicate maths overload '{signature}', contributed by '{type.Name}.{function.Name}'.");
            }
        }

        private static void ValidateShaderContracts(TypeSpec[] types)
        {
            foreach (var type in types)
            {
                var typeContract = type.ShaderContract;
                if (typeContract.Mapping != ShaderMappingKind.Unsupported &&
                    (string.IsNullOrWhiteSpace(typeContract.GlslName) || string.IsNullOrWhiteSpace(typeContract.RequiredCapability)))
                    Fail($"Shader type contract '{type.Name}' must define GLSL name and capability.");

                foreach (var function in type.Members.OfType<FunctionSpec>())
                {
                    var contract = function.ShaderContract;
                    if (contract.Mapping == ShaderMappingKind.Unsupported)
                        continue;
                    if (string.IsNullOrWhiteSpace(function.Name) ||
                        string.IsNullOrWhiteSpace(contract.GlslName) ||
                        string.IsNullOrWhiteSpace(contract.RequiredCapability))
                        Fail($"Shader function '{type.Name}.{function.Name}' has incomplete contract metadata.");
                }
            }
        }

        private static string Signature(MemberSpec member) => member switch
        {
            ConstructorSpec constructor => ".ctor" + Parameters(constructor.Parameters),
            OperatorSpec conversion when conversion.Operator is "implicit" or "explicit" =>
                conversion.Operator + " " + conversion.ReturnType + Parameters(conversion.Parameters),
            OperatorSpec operation => "operator " + operation.Operator + Parameters(operation.Parameters),
            FunctionSpec function => function.Name + Parameters(function.Parameters),
            PropertySpec property => "property " + property.Name,
            IndexerSpec indexer => "this" + Parameters([indexer.Parameter]),
            FieldSpec field => "field " + field.Name,
            _ => throw new InvalidOperationException($"Unknown member declaration '{member.GetType().Name}'."),
        };

        private static string Parameters(ParameterSpec[] parameters) =>
            "(" + string.Join(",", parameters.Select(parameter =>
                (string.IsNullOrWhiteSpace(parameter.Modifier) ? "" : "&") + parameter.Type.Name)) + ")";

        private static void Fail(string message) => throw new InvalidOperationException("Invalid generation model: " + message);
    }
}
