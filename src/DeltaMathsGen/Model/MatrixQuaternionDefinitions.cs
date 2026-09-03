using System;
using System.Collections.Generic;
using System.Linq;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal static class MatrixQuaternionDefinitions
    {
        public static TypeSpec[] Create()
        {
            var types = new List<TypeSpec>(10);
            types.AddRange(MatrixDefinitions.Create());
            types.Add(CreateQuaternion());
            return types.ToArray();
        }

        private static void MarkShaderFunctionsStatic(List<MemberSpec> members, string typeName)
        {
            foreach (var function in members.OfType<FunctionSpec>())
            {
                if (function.Targets.HasFlag(FunctionTargets.ShaderDeltaMaths))
                {
                    function.Modifiers |= Modifiers.Static;
                    if (function.ShaderContract.Mapping == ShaderMappingKind.Unsupported)
                    {
                        function.ShaderContract = CreateDefaultShaderContract(typeName, function);
                    }
                }
            }
        }

        private static ShaderContract CreateDefaultShaderContract(string typeName, FunctionSpec function)
        {
            if (typeName != "quaternion")
            {
                return new ShaderContract();
            }

            return function.Name switch
            {
                "Conjugate" => Helper("delta_quaternionConjugate", "quaternion"),
                "Normalize" => Helper("delta_quaternionNormalize", "quaternion"),
                "Inverse" => Helper("delta_quaternionInverse", "quaternion"),
                "Lerp" => Helper("delta_quaternionLerp", "quaternion"),
                "Slerp" => Helper("delta_quaternionSlerp", "quaternion"),
                "CreateFromAxisAngle" => Helper("delta_quaternionFromAxisAngle", "quaternion"),
                "CreateFromYawPitchRoll" => Helper("delta_quaternionFromYawPitchRoll", "quaternion"),
                "Rotate" => Helper("delta_quaternionRotate", "quaternion"),
                "CreateFromRotationMatrix" => Helper("delta_quaternionFromMatrix", "quaternion"),
                "ToRotationMatrix" => Helper("delta_quaternionToMatrix", "quaternion"),
                _ => new ShaderContract(),
            };
        }

        private static ShaderContract Helper(string name, string capability) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Helper,
            Capability = ParseCapability(capability),
            Stages = ShaderStages.All,
        };

        private static ShaderCapability ParseCapability(string capability) => capability switch
        {
            "quaternion" => ShaderCapability.Quaternion,
            _ => throw new InvalidOperationException($"Unsupported shader capability '{capability}'."),
        };

        private static TypeSpec CreateQuaternion()
        {
            var members = new List<MemberSpec>
            {
                new FieldSpec { Name = "x", Type = Type("float") },
                new FieldSpec { Name = "y", Type = Type("float") },
                new FieldSpec { Name = "z", Type = Type("float") },
                new FieldSpec { Name = "w", Type = Type("float") },
                new FieldSpec
                {
                    Name = "identity",
                    Type = Type("quaternion"),
                    Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
                    Initializer = "new quaternion(0f, 0f, 0f, 1f)",
                    Summary = "The identity rotation.",
                },
                new ConstructorSpec
                {
                    Parameters = [Param("x", Type("float")), Param("y", Type("float")), Param("z", Type("float")), Param("w", Type("float"))],
                    Body =
                    """
                    this.x = x;
                    this.y = y;
                    this.z = z;
                    this.w = w;
                    """,
                },
                new ConstructorSpec
                {
                    Parameters = [Param("value", Type("float4"))],
                    Body =
                    """
                    x = value.x;
                    y = value.y;
                    z = value.z;
                    w = value.w;
                    """,
                },
                new PropertySpec { Name = "Identity", Type = Type("quaternion"), Modifiers = Modifiers.Public | Modifiers.Static, Expression = "identity" },
                new PropertySpec { Name = "X", Type = Type("float"), Getter = "x", Setter = "x = value" },
                new PropertySpec { Name = "Y", Type = Type("float"), Getter = "y", Setter = "y = value" },
                new PropertySpec { Name = "Z", Type = Type("float"), Getter = "z", Setter = "z = value" },
                new PropertySpec { Name = "W", Type = Type("float"), Getter = "w", Setter = "w = value" },
                new FunctionSpec { Name = "Dot", ReturnType = Type("float"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body = "return left.x * right.x + left.y * right.y + left.z * right.z + left.w * right.w;" },
                new FunctionSpec { Name = "LengthSquared", ReturnType = Type("float"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body = "return Dot(value, value);" },
                new FunctionSpec { Name = "Normalize", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body = "return value / DeltaMaths.Sqrt(LengthSquared(value));" },
                new FunctionSpec { Name = "NormalizeSafe", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var lengthSquared = LengthSquared(value);
                    return lengthSquared <= 1e-20f ? identity : value / DeltaMaths.Sqrt(lengthSquared);
                    """ },
                new FunctionSpec { Name = "Conjugate", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body = "return new quaternion(-value.x, -value.y, -value.z, value.w);" },
                new FunctionSpec { Name = "TryInverse", ReturnType = Type("bool"), Parameters = [Param("value", Type("quaternion")), Param("result", Type("quaternion"), ParameterModifier.Out)], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var lengthSquared = LengthSquared(value);
                    if (lengthSquared <= 1e-20f) { result = default; return false; }
                    result = Conjugate(value) / lengthSquared;
                    return true;
                    """ },
                new FunctionSpec { Name = "Inverse", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body = "return TryInverse(value, out var result) ? result : identity;" },
                new FunctionSpec { Name = "Lerp", ReturnType = Type("quaternion"), Parameters = [Param("start", Type("quaternion")), Param("end", Type("quaternion")), Param("amount", Type("float"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    if (Dot(start, end) < 0f) end = -end;
                    return NormalizeSafe(start + (end - start) * amount);
                    """ },
                new FunctionSpec { Name = "Slerp", ReturnType = Type("quaternion"), Parameters = [Param("start", Type("quaternion")), Param("end", Type("quaternion")), Param("amount", Type("float"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var dot = Dot(start, end);
                    if (dot < 0f) { end = -end; dot = -dot; }
                    if (dot > 0.9995f) return Lerp(start, end, amount);
                    dot = DeltaMaths.Clamp(dot, -1f, 1f);
                    var angle = DeltaMaths.Acos(dot);
                    var scale = 1f / DeltaMaths.Sin(angle);
                    return start * (DeltaMaths.Sin((1f - amount) * angle) * scale) + end * (DeltaMaths.Sin(amount * angle) * scale);
                    """ },
                new FunctionSpec { Name = "CreateFromAxisAngle", ReturnType = Type("quaternion"), Parameters = [Param("axis", Type("float3")), Param("angle", Type("float"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var normalizedAxis = float3.NormalizeSafe(axis);
                    var halfAngle = angle * 0.5f;
                    var sine = DeltaMaths.Sin(halfAngle);
                    return new quaternion(-normalizedAxis.x * sine, -normalizedAxis.y * sine, -normalizedAxis.z * sine, DeltaMaths.Cos(halfAngle));
                    """ },
                new FunctionSpec { Name = "CreateFromYawPitchRoll", ReturnType = Type("quaternion"), Parameters = [Param("yaw", Type("float")), Param("pitch", Type("float")), Param("roll", Type("float"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var halfRoll = roll * 0.5f;
                    var halfPitch = pitch * 0.5f;
                    var halfYaw = yaw * 0.5f;
                    var sinRoll = DeltaMaths.Sin(halfRoll);
                    var cosRoll = DeltaMaths.Cos(halfRoll);
                    var sinPitch = DeltaMaths.Sin(halfPitch);
                    var cosPitch = DeltaMaths.Cos(halfPitch);
                    var sinYaw = DeltaMaths.Sin(halfYaw);
                    var cosYaw = DeltaMaths.Cos(halfYaw);
                    return new quaternion(
                        -(cosYaw * sinPitch * cosRoll + sinYaw * cosPitch * sinRoll),
                        -(sinYaw * cosPitch * cosRoll - cosYaw * sinPitch * sinRoll),
                        -(cosYaw * cosPitch * sinRoll - sinYaw * sinPitch * cosRoll),
                        cosYaw * cosPitch * cosRoll + sinYaw * sinPitch * sinRoll);
                    """ },
                new FunctionSpec { Name = "Rotate", ReturnType = Type("float3"), Parameters = [Param("rotation", Type("quaternion")), Param("value", Type("float3"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var qv = new float3(rotation.x, rotation.y, rotation.z);
                    var t = 2f * float3.Cross(qv, value);
                    return value + rotation.w * t + float3.Cross(qv, t);
                    """ },
                new FunctionSpec { Name = "CreateFromRotationMatrix", ReturnType = Type("quaternion"), Parameters = [Param("matrix", Type("float4x4"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var trace = matrix.M11 + matrix.M22 + matrix.M33;
                    if (trace > 0f) { var s = DeltaMaths.Sqrt(trace + 1f) * 2f; return new quaternion((matrix.M32 - matrix.M23) / s, (matrix.M13 - matrix.M31) / s, (matrix.M21 - matrix.M12) / s, 0.25f * s); }
                    if (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33) { var s = DeltaMaths.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33) * 2f; return new quaternion(0.25f * s, (matrix.M12 + matrix.M21) / s, (matrix.M13 + matrix.M31) / s, (matrix.M32 - matrix.M23) / s); }
                    if (matrix.M22 > matrix.M33) { var s = DeltaMaths.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33) * 2f; return new quaternion((matrix.M12 + matrix.M21) / s, 0.25f * s, (matrix.M23 + matrix.M32) / s, (matrix.M13 - matrix.M31) / s); }
                    { var s = DeltaMaths.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22) * 2f; return new quaternion((matrix.M13 + matrix.M31) / s, (matrix.M23 + matrix.M32) / s, 0.25f * s, (matrix.M21 - matrix.M12) / s); }
                    """ },
                new FunctionSpec { Name = "ToRotationMatrix", ReturnType = Type("float4x4"), Parameters = [Param("rotation", Type("quaternion"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body = "return float4x4.CreateFromQuaternion(rotation);" },
                new FunctionSpec { Name = "ToAxisAngle", ReturnType = Type("void"), Parameters = [Param("rotation", Type("quaternion")), Param("axis", Type("float3"), ParameterModifier.Out), Param("angle", Type("float"), ParameterModifier.Out)], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths, Body =
                    """
                    var normalized = NormalizeSafe(rotation);
                    angle = 2f * DeltaMaths.Acos(DeltaMaths.Clamp(normalized.w, -1f, 1f));
                    var scale = DeltaMaths.Sqrt(DeltaMaths.Max(1e-20f, 1f - normalized.w * normalized.w));
                    axis = scale <= 1e-10f ? new float3(1f, 0f, 0f) : new float3(-normalized.x / scale, -normalized.y / scale, -normalized.z / scale);
                    """ },
                new OperatorSpec { Name = "Multiply", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "*", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], ShaderContract = new ShaderContract { GlslName = "delta_quaternionMultiply", Mapping = ShaderMappingKind.Helper, Capability = ShaderCapability.Quaternion }, Body = "return new quaternion(left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y, left.w * right.y - left.x * right.z + left.y * right.w + left.z * right.x, left.w * right.z + left.x * right.y - left.y * right.x + left.z * right.w, left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z);" },
                new OperatorSpec { Name = "Multiply", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "*", ReturnType = Type("float3"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("float3"))], Body = "return Rotate(left, right);" },
                new OperatorSpec { Name = "Add", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "+", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return new quaternion(left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w);" },
                new OperatorSpec { Name = "Subtract", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "-", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return new quaternion(left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w);" },
                new OperatorSpec { Name = "Negate", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "-", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Body = "return new quaternion(-value.x, -value.y, -value.z, -value.w);" },
                new OperatorSpec { Name = "Multiply", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "*", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("float"))], Body = "return new quaternion(left.x * right, left.y * right, left.z * right, left.w * right);" },
                new OperatorSpec { Name = "Divide", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "/", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("float"))], Body = "return new quaternion(left.x / right, left.y / right, left.z / right, left.w / right);" },
                new OperatorSpec { Name = "Equality", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "==", ReturnType = Type("bool"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return left.x == right.x && left.y == right.y && left.z == right.z && left.w == right.w;" },
                new OperatorSpec { Name = "Inequality", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "!=", ReturnType = Type("bool"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return !(left == right);" },
                new FunctionSpec { Name = "Equals", ReturnType = Type("bool"), Parameters = [Param("other", Type("quaternion"))], Body = "return this == other;" },
                new FunctionSpec { Name = "Equals", ReturnType = Type("bool"), Modifiers = Modifiers.Public | Modifiers.Override, Parameters = [Param("obj", Type("object?"))], Body = "return obj is quaternion other && Equals(other);" },
                new FunctionSpec { Name = "GetHashCode", ReturnType = Type("int"), Modifiers = Modifiers.Public | Modifiers.Override, Body = "unchecked { var hash = 17; hash = hash * 31 + x.GetHashCode(); hash = hash * 31 + y.GetHashCode(); hash = hash * 31 + z.GetHashCode(); hash = hash * 31 + w.GetHashCode(); return hash; }" },
                new FunctionSpec { Name = "ToString", ReturnType = Type("string"), Modifiers = Modifiers.Public | Modifiers.Override, Body = "return FormattableString.Invariant($\"({x}, {y}, {z}, {w})\");" },
            };

            MarkShaderFunctionsStatic(members, "quaternion");
            return new TypeSpec
            {
                Namespace = "Delta",
                Name = "quaternion",
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = ["IEquatable<quaternion>"],
                ShaderContract = new ShaderContract
                {
                    GlslName = "vec4",
                    Mapping = ShaderMappingKind.Builtin,
                    Alignment = 16,
                    Capability = ShaderCapability.Std430,
                },
                Comment = "A left-handed quaternion stored as (x, y, z, w).",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }
    }
}
