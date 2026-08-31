using System;
using System.Linq;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal static class VectorShaderContractFactory
    {
        internal static ShaderContract Create(VectorContext context, string name, ParameterSpec[] parameters)
        {
            var scalar = context.Scalar.Name;
            var shaderScalar = scalar is "bool" or "int" or "uint" or "float" or "half" or "double";
            if (!shaderScalar)
            {
                return new ShaderContract();
            }

            return name switch
            {
                "Select" when parameters.Length == 3 && parameters[0].Type.Name == context.Name &&
                    parameters[1].Type.Name == context.Name && parameters[2].Type.Name == context.BoolVectorName
                    && scalar != "bool" => Helper("delta_select", "vector", context),
                "LessThan" or "LessThanOrEqual" or "GreaterThan" or "GreaterThanOrEqual"
                    when (scalar is "float" or "half" or "double" or "int" or "uint") && parameters.Length == 2 && AllParametersAreVector(context, parameters)
                    => Builtin(name switch
                    {
                        "LessThanOrEqual" => "lessThanEqual",
                        "GreaterThanOrEqual" => "greaterThanEqual",
                        _ => LowercaseFirst(name),
                    }, "vector", context),
                "Not" when scalar == "bool" && parameters.Length == 1 && parameters[0].Type.Name == context.Name
                    => Builtin("not", "vector", context),
                "Equal" when parameters.All(parameter => parameter.Type.Name == context.Name) => Builtin("equal", "vector", context),
                "NotEqual" when parameters.All(parameter => parameter.Type.Name == context.Name) => Builtin("notEqual", "vector", context),
                "Min" or "Max" when scalar != "bool" && FirstParameterIsVector(context, parameters)
                    => Builtin(LowercaseFirst(name), "vector", context),
                "Clamp" when scalar != "bool" && FirstParameterIsVector(context, parameters)
                    => Builtin("clamp", "vector", context),
                "Abs" when scalar is "float" or "half" or "double" or "int" => Builtin("abs", "vector", context),
                "Sign" when scalar is "float" or "half" or "double" or "int" => Builtin("sign", "vector", context),
                "Mod" when scalar is "float" or "half" or "double" => Builtin("mod", "vector", context),
                "Modf" when (scalar is "float" or "half" or "double") && parameters.Length == 2 && parameters[1].Modifier == ParameterModifier.Out
                    => Builtin("modf", "vector", context),
                "Frexp" when (scalar is "float" or "half" or "double") && parameters.Length == 2 && parameters[1].Modifier == ParameterModifier.Out
                    => Builtin("frexp", "vector", context),
                "Ldexp" when (scalar is "float" or "half" or "double") && parameters.Length == 2 && parameters[1].Type.Name == "int" + context.Dimension
                    => Builtin("ldexp", "vector", context),
                "FloatBitsToInt" when scalar == "float" => Builtin("floatBitsToInt", "vector"),
                "FloatBitsToUint" when scalar == "float" => Builtin("floatBitsToUint", "vector"),
                "IntBitsToFloat" when scalar == "float" => Builtin("intBitsToFloat", "vector"),
                "UintBitsToFloat" when scalar == "float" => Builtin("uintBitsToFloat", "vector"),
                "BitCount" or "FindLSB" or "FindMSB" or "BitfieldReverse" or "BitfieldExtract" or "BitfieldInsert"
                    when scalar is "int" or "uint" => Builtin(LowercaseFirst(name), "vector"),
                "UaddCarry" or "UsubBorrow" when scalar == "uint" => Builtin(LowercaseFirst(name), "vector"),
                "UmulExtended" when scalar == "uint" => Builtin("umulExtended", "vector"),
                "ImulExtended" when scalar == "int" => Builtin("imulExtended", "vector"),
                "Fract" when scalar is "float" or "half" or "double" => Builtin("fract", "vector", context),
                "InverseSqrt" when scalar is "float" or "half" or "double" => Builtin("inversesqrt", "vector", context),
                "PackUnorm2x16" or "UnpackUnorm2x16" or "PackSnorm2x16" or "UnpackSnorm2x16" or "PackHalf2x16" or "UnpackHalf2x16"
                    when scalar == "float" && context.Dimension == 2 => Builtin(LowercaseFirst(name), "vector"),
                "PackUnorm4x8" or "UnpackUnorm4x8" or "PackSnorm4x8" or "UnpackSnorm4x8"
                    when scalar == "float" && context.Dimension == 4 => Builtin(LowercaseFirst(name), "vector"),
                "Radians" or "Degrees" when scalar is "float" or "half" or "double" => Builtin(LowercaseFirst(name), "vector", context),
                "Floor" or "Ceil" or "Round" or "RoundEven" or "Truncate" when scalar is "float" or "half" or "double"
                    => Builtin(name switch
                    {
                        "Round" or "RoundEven" => "roundEven",
                        "Truncate" => "trunc",
                        _ => LowercaseFirst(name),
                    }, "vector", context),
                "Sin" or "Cos" or "Tan" or "Asin" or "Acos" or "Atan"
                    when scalar is "float" or "half" or "double" => Builtin(LowercaseFirst(name), "vector", context),
                "Sinh" or "Cosh" or "Tanh" or "Asinh" or "Acosh" or "Atanh"
                    when scalar is "float" or "half" or "double" => Builtin(LowercaseFirst(name), "vector", context),
                "Exp" or "Exp2" or "Log" or "Log2" or "Sqrt"
                    when scalar is "float" or "half" or "double" => Builtin(LowercaseFirst(name), "vector", context),
                "Pow" when scalar is "float" or "half" or "double" && AllParametersAreVector(context, parameters)
                    => Builtin("pow", "vector", context),
                "Fma" when scalar is "float" or "half" or "double" && AllParametersAreVector(context, parameters)
                    => Builtin("fma", "vector", context),
                "Lerp" when scalar is "float" or "half" or "double" => Builtin("mix", "vector", context),
                "Smoothstep" when scalar is "float" or "half" or "double" => Builtin("smoothstep", "vector", context),
                "Step" when scalar is "float" or "half" or "double" => Builtin("step", "vector", context),
                "Dot" when scalar is "float" or "half" or "double" => Builtin("dot", "vector", context),
                "Length" or "Distance" when scalar is "float" or "half" or "double" => Builtin(LowercaseFirst(name), "vector", context),
                "Atan" when scalar is "float" or "half" or "double" => Builtin("atan", "vector", context),
                "Atan2" when scalar is "float" or "half" or "double" && FirstParameterIsVector(context, parameters) => Builtin("atan", "vector", context),
                "Normalize" when scalar is "float" or "half" or "double" => Builtin("normalize", "vector", context),
                "FaceForward" when scalar is "float" or "half" or "double" => Builtin("faceforward", "vector", context),
                "Reflect" when scalar is "float" or "half" or "double" => Builtin("reflect", "vector", context),
                "Refract" when scalar is "float" or "half" or "double" => Builtin("refract", "vector", context),
                "Cross" when scalar is "float" or "half" or "double" && context.Dimension == 3 => Builtin("cross", "vector", context),
                "IsNaN" when scalar is "float" or "half" or "double" => Builtin("isnan", "vector", context),
                "IsInfinity" when scalar is "float" or "half" or "double" => Builtin("isinf", "vector", context),
                _ => new ShaderContract(),
            };
        }

        private static bool AllParametersAreVector(VectorContext context, ParameterSpec[] parameters) =>
            parameters.Length != 0 && parameters.All(parameter => parameter.Type.Name == context.Name);

        private static bool FirstParameterIsVector(VectorContext context, ParameterSpec[] parameters) =>
            parameters.Length > 0 && parameters[0].Type.Name == context.Name;

        private static ShaderContract Builtin(string name, string capability, VectorContext? context = null) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Builtin,
            Capability = ShaderCapabilityFor(context, capability),
            Stages = ShaderStages.All,
        };

        private static ShaderContract Helper(string name, string capability, VectorContext? context = null) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Helper,
            Capability = ShaderCapabilityFor(context, capability),
            Stages = ShaderStages.All,
        };

        private static ShaderCapability ShaderCapabilityFor(VectorContext? context, string capability) =>
            context?.Scalar.Name switch
            {
                "half" => ShaderCapability.Float16,
                "double" => ShaderCapability.Float64,
                _ => ParseCapability(capability),
            };

        private static ShaderCapability ParseCapability(string capability) => capability switch
        {
            "vector" => ShaderCapability.Vector,
            "matrix" => ShaderCapability.Matrix,
            "quaternion" => ShaderCapability.Quaternion,
            "std430" => ShaderCapability.Std430,
            _ => throw new InvalidOperationException($"Unsupported shader capability '{capability}'."),
        };
    }
}
