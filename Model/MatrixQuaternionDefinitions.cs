using System.Collections.Generic;
using System.Linq;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal static class MatrixQuaternionDefinitions
    {
        public static TypeSpec[] Create()
        {
            return
            [
                CreateMatrix(),
                CreateQuaternion(),
            ];
        }

        private static TypeSpec CreateMatrix()
        {
            var members = new List<MemberSpec>
            {
                new FieldSpec { Name = "c0", Type = Type("float4"), Summary = "First column." },
                new FieldSpec { Name = "c1", Type = Type("float4"), Summary = "Second column." },
                new FieldSpec { Name = "c2", Type = Type("float4"), Summary = "Third column." },
                new FieldSpec { Name = "c3", Type = Type("float4"), Summary = "Fourth column." },
                new FieldSpec
                {
                    Name = "zero",
                    Type = Type("float4x4"),
                    Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
                    Initializer = "new float4x4(float4.zero, float4.zero, float4.zero, float4.zero)",
                    Summary = "A matrix whose components are all zero.",
                },
                new FieldSpec
                {
                    Name = "identity",
                    Type = Type("float4x4"),
                    Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
                    Initializer = "new float4x4(1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f, 0f, 0f, 0f, 0f, 1f)",
                    Summary = "The identity matrix.",
                },
                new ConstructorSpec
                {
                    Parameters = [Param("c0", Type("float4")), Param("c1", Type("float4")), Param("c2", Type("float4")), Param("c3", Type("float4"))],
                    Body = "this.c0 = c0;\nthis.c1 = c1;\nthis.c2 = c2;\nthis.c3 = c3;",
                    Summary = "Creates a matrix from its four columns.",
                },
                new ConstructorSpec
                {
                    Parameters =
                    [
                        Param("m11", Type("float")), Param("m12", Type("float")), Param("m13", Type("float")), Param("m14", Type("float")),
                        Param("m21", Type("float")), Param("m22", Type("float")), Param("m23", Type("float")), Param("m24", Type("float")),
                        Param("m31", Type("float")), Param("m32", Type("float")), Param("m33", Type("float")), Param("m34", Type("float")),
                        Param("m41", Type("float")), Param("m42", Type("float")), Param("m43", Type("float")), Param("m44", Type("float")),
                    ],
                    Body = "c0 = new float4(m11, m21, m31, m41);\nc1 = new float4(m12, m22, m32, m42);\nc2 = new float4(m13, m23, m33, m43);\nc3 = new float4(m14, m24, m34, m44);",
                    Summary = "Creates a matrix from row and column named components in mathematical order.",
                },
                new PropertySpec { Name = "Identity", Type = Type("float4x4"), Modifiers = Modifiers.Public | Modifiers.Static, Expression = "identity" },
                new FunctionSpec
                {
                    Name = "GetColumn",
                    ReturnType = Type("float4"),
                    Parameters = [Param("index", Type("int"))],
                    Part = TypePart.Core,
                    Body = "return index switch { 0 => c0, 1 => c1, 2 => c2, 3 => c3, _ => throw new ArgumentOutOfRangeException(nameof(index)) };",
                    Summary = "Returns a column using zero-based indexing.",
                },
                new FunctionSpec
                {
                    Name = "GetRow",
                    ReturnType = Type("float4"),
                    Parameters = [Param("index", Type("int"))],
                    Part = TypePart.Core,
                    Body = "return index switch { 0 => new float4(M11, M12, M13, M14), 1 => new float4(M21, M22, M23, M24), 2 => new float4(M31, M32, M33, M34), 3 => new float4(M41, M42, M43, M44), _ => throw new ArgumentOutOfRangeException(nameof(index)) };",
                    Summary = "Returns a row using zero-based indexing.",
                },
                new FunctionSpec
                {
                    Name = "GetElement",
                    ReturnType = Type("float"),
                    Parameters = [Param("column", Type("int")), Param("row", Type("int"))],
                    Part = TypePart.Core,
                    Body = "return GetColumn(column)[row];",
                },
                new FunctionSpec
                {
                    Name = "SetElement",
                    ReturnType = Type("void"),
                    Parameters = [Param("column", Type("int")), Param("row", Type("int")), Param("value", Type("float"))],
                    Part = TypePart.Core,
                    Body = "if ((uint)column >= 4u || (uint)row >= 4u) throw new ArgumentOutOfRangeException();\nvar columnValue = GetColumn(column);\ncolumnValue[row] = value;\nswitch (column) { case 0: c0 = columnValue; break; case 1: c1 = columnValue; break; case 2: c2 = columnValue; break; default: c3 = columnValue; break; }",
                },
                new FunctionSpec
                {
                    Name = "Transpose",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("value", Type("float4x4"))],
                    Part = TypePart.Common,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return new float4x4(value.M11, value.M21, value.M31, value.M41, value.M12, value.M22, value.M32, value.M42, value.M13, value.M23, value.M33, value.M43, value.M14, value.M24, value.M34, value.M44);",
                },
                new FunctionSpec
                {
                    Name = "Determinant",
                    ReturnType = Type("float"),
                    Parameters = [Param("value", Type("float4x4"))],
                    Part = TypePart.Common,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body =
                    """
                    static float minor(float a11, float a12, float a13, float a21, float a22, float a23, float a31, float a32, float a33) =>
                        a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);
                    return value.M11 * minor(value.M22, value.M23, value.M24, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44)
                        - value.M12 * minor(value.M21, value.M23, value.M24, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44)
                        + value.M13 * minor(value.M21, value.M22, value.M24, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44)
                        - value.M14 * minor(value.M21, value.M22, value.M23, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43);
                    """,
                },
                new FunctionSpec
                {
                    Name = "TryInverse",
                    ReturnType = Type("bool"),
                    Parameters = [Param("value", Type("float4x4")), Param("result", Type("float4x4"), "out")],
                    Part = TypePart.Common,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body =
                    """
                    static float minor(float a11, float a12, float a13, float a21, float a22, float a23, float a31, float a32, float a33) =>
                        a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);
                    var c11 = minor(value.M22, value.M23, value.M24, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44);
                    var c12 = -minor(value.M21, value.M23, value.M24, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44);
                    var c13 = minor(value.M21, value.M22, value.M24, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44);
                    var c14 = -minor(value.M21, value.M22, value.M23, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43);
                    var c21 = -minor(value.M12, value.M13, value.M14, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44);
                    var c22 = minor(value.M11, value.M13, value.M14, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44);
                    var c23 = -minor(value.M11, value.M12, value.M14, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44);
                    var c24 = minor(value.M11, value.M12, value.M13, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43);
                    var determinant = value.M11 * c11 + value.M12 * c12 + value.M13 * c13 + value.M14 * c14;
                    if (Maths.Abs(determinant) <= 1e-8f)
                    {
                        result = default;
                        return false;
                    }
                    var c31 = minor(value.M12, value.M13, value.M14, value.M22, value.M23, value.M24, value.M42, value.M43, value.M44);
                    var c32 = -minor(value.M11, value.M13, value.M14, value.M21, value.M23, value.M24, value.M41, value.M43, value.M44);
                    var c33 = minor(value.M11, value.M12, value.M14, value.M21, value.M22, value.M24, value.M41, value.M42, value.M44);
                    var c34 = -minor(value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M41, value.M42, value.M43);
                    var c41 = -minor(value.M12, value.M13, value.M14, value.M22, value.M23, value.M24, value.M32, value.M33, value.M34);
                    var c42 = minor(value.M11, value.M13, value.M14, value.M21, value.M23, value.M24, value.M31, value.M33, value.M34);
                    var c43 = -minor(value.M11, value.M12, value.M14, value.M21, value.M22, value.M24, value.M31, value.M32, value.M34);
                    var c44 = minor(value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M31, value.M32, value.M33);
                    var inverse = 1f / determinant;
                    result = new float4x4(
                        c11 * inverse, c21 * inverse, c31 * inverse, c41 * inverse,
                        c12 * inverse, c22 * inverse, c32 * inverse, c42 * inverse,
                        c13 * inverse, c23 * inverse, c33 * inverse, c43 * inverse,
                        c14 * inverse, c24 * inverse, c34 * inverse, c44 * inverse);
                    return true;
                    """,
                },
                new FunctionSpec
                {
                    Name = "Inverse",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("value", Type("float4x4"))],
                    Part = TypePart.Common,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return TryInverse(value, out var result) ? result : identity;",
                },
                new FunctionSpec
                {
                    Name = "CreateTranslation",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("translation", Type("float3"))],
                    Part = TypePart.Geometry,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return new float4x4(1f, 0f, 0f, translation.x, 0f, 1f, 0f, translation.y, 0f, 0f, 1f, translation.z, 0f, 0f, 0f, 1f);",
                },
                new FunctionSpec
                {
                    Name = "CreateScale",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("scale", Type("float3"))],
                    Part = TypePart.Geometry,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return new float4x4(scale.x, 0f, 0f, 0f, 0f, scale.y, 0f, 0f, 0f, 0f, scale.z, 0f, 0f, 0f, 0f, 1f);",
                },
                new FunctionSpec
                {
                    Name = "CreateScale",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("scale", Type("float"))],
                    Part = TypePart.Geometry,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return CreateScale(new float3(scale));",
                },
                new FunctionSpec
                {
                    Name = "CreateFromQuaternion",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("rotation", Type("quaternion"))],
                    Part = TypePart.Geometry,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body =
                    """
                    var xx = rotation.x * rotation.x;
                    var yy = rotation.y * rotation.y;
                    var zz = rotation.z * rotation.z;
                    var xy = rotation.x * rotation.y;
                    var xz = rotation.x * rotation.z;
                    var yz = rotation.y * rotation.z;
                    var wx = rotation.w * rotation.x;
                    var wy = rotation.w * rotation.y;
                    var wz = rotation.w * rotation.z;
                    return new float4x4(
                        1f - 2f * (yy + zz), 2f * (xy - wz), 2f * (xz + wy), 0f,
                        2f * (xy + wz), 1f - 2f * (xx + zz), 2f * (yz - wx), 0f,
                        2f * (xz - wy), 2f * (yz + wx), 1f - 2f * (xx + yy), 0f,
                        0f, 0f, 0f, 1f);
                    """,
                },
                new FunctionSpec
                {
                    Name = "CreateTRS",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("translation", Type("float3")), Param("rotation", Type("quaternion")), Param("scale", Type("float3"))],
                    Part = TypePart.Geometry,
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "delta_createTRS",
                        Mapping = ShaderMappingKind.Helper,
                        RequiredCapability = "matrix",
                    },
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return CreateTranslation(translation) * CreateFromQuaternion(rotation) * CreateScale(scale);",
                    Summary = "Creates a transform that applies scale, then rotation, then translation to a column vector.",
                },
                new FunctionSpec
                {
                    Name = "Multiply",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("left", Type("float4x4")), Param("right", Type("float4x4"))],
                    Part = TypePart.Operators,
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "*",
                        Mapping = ShaderMappingKind.Builtin,
                        RequiredCapability = "matrix",
                    },
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return left * right;",
                },
                new FunctionSpec
                {
                    Name = "TransformPoint",
                    ReturnType = Type("float3"),
                    Parameters = [Param("matrix", Type("float4x4")), Param("point", Type("float3"))],
                    Part = TypePart.Geometry,
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "delta_transformPoint",
                        Mapping = ShaderMappingKind.Helper,
                        RequiredCapability = "matrix",
                    },
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "var value = matrix * new float4(point, 1f);\nreturn value.w == 0f ? value.xyz : value.xyz / value.w;",
                },
                new FunctionSpec
                {
                    Name = "TransformDirection",
                    ReturnType = Type("float3"),
                    Parameters = [Param("matrix", Type("float4x4")), Param("direction", Type("float3"))],
                    Part = TypePart.Geometry,
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "delta_transformDirection",
                        Mapping = ShaderMappingKind.Helper,
                        RequiredCapability = "matrix",
                    },
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "return (matrix * new float4(direction, 0f)).xyz;",
                },
                new FunctionSpec
                {
                    Name = "CreateLookTo",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("eye", Type("float3")), Param("direction", Type("float3")), Param("up", Type("float3"))],
                    Part = TypePart.Geometry,
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "delta_createLookTo",
                        Mapping = ShaderMappingKind.Helper,
                        RequiredCapability = "matrix",
                    },
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "var zaxis = float3.NormalizeSafe(direction);\nvar xaxis = float3.NormalizeSafe(float3.Cross(up, zaxis));\nvar yaxis = float3.Cross(zaxis, xaxis);\nreturn new float4x4(xaxis.x, yaxis.x, zaxis.x, -float3.Dot(xaxis, eye), xaxis.y, yaxis.y, zaxis.y, -float3.Dot(yaxis, eye), xaxis.z, yaxis.z, zaxis.z, -float3.Dot(zaxis, eye), 0f, 0f, 0f, 1f);",
                },
                new FunctionSpec
                {
                    Name = "CreatePerspectiveFieldOfViewLeftHanded",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("fieldOfView", Type("float")), Param("aspectRatio", Type("float")), Param("nearPlaneDistance", Type("float")), Param("farPlaneDistance", Type("float"))],
                    Part = TypePart.Geometry,
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "delta_createPerspectiveFovLH",
                        Mapping = ShaderMappingKind.Helper,
                        RequiredCapability = "matrix",
                    },
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body = "var yScale = 1f / Maths.Tan(fieldOfView * 0.5f);\nvar xScale = yScale / aspectRatio;\nvar range = farPlaneDistance / (farPlaneDistance - nearPlaneDistance);\nreturn new float4x4(xScale, 0f, 0f, 0f, 0f, yScale, 0f, 0f, 0f, 0f, range, -nearPlaneDistance * range, 0f, 0f, 1f, 0f);",
                },
                new FunctionSpec
                {
                    Name = "Decompose",
                    ReturnType = Type("bool"),
                    Parameters = [Param("value", Type("float4x4")), Param("scale", Type("float3"), "out"), Param("rotation", Type("quaternion"), "out"), Param("translation", Type("float3"), "out")],
                    Part = TypePart.Geometry,
                    Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths,
                    Body =
                    """
                    translation = new float3(value.M14, value.M24, value.M34);
                    var x = new float3(value.M11, value.M21, value.M31);
                    var y = new float3(value.M12, value.M22, value.M32);
                    var z = new float3(value.M13, value.M23, value.M33);
                    var scaleX = float3.Length(x);
                    var scaleY = float3.Length(y);
                    var scaleZ = float3.Length(z);
                    if (scaleX <= 1e-20f || scaleY <= 1e-20f || scaleZ <= 1e-20f)
                    {
                        scale = default;
                        rotation = quaternion.identity;
                        return false;
                    }
                    if (float3.Dot(float3.Cross(x, y), z) < 0f)
                    {
                        scaleX = -scaleX;
                        x = -x;
                    }
                    scale = new float3(scaleX, scaleY, scaleZ);
                    x /= scaleX;
                    y /= scaleY;
                    z /= scaleZ;
                    var rotationMatrix = new float4x4(x.x, y.x, z.x, 0f, x.y, y.y, z.y, 0f, x.z, y.z, z.z, 0f, 0f, 0f, 0f, 1f);
                    rotation = quaternion.CreateFromRotationMatrix(rotationMatrix);
                    return true;
                    """,
                },
                new OperatorSpec
                {
                    Name = "Multiply",
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "*",
                    ReturnType = Type("float4x4"),
                    Parameters = [Param("left", Type("float4x4")), Param("right", Type("float4x4"))],
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "*",
                        Mapping = ShaderMappingKind.Builtin,
                        RequiredCapability = "matrix",
                    },
                    Body = "return new float4x4(left * right.c0, left * right.c1, left * right.c2, left * right.c3);",
                },
                new OperatorSpec
                {
                    Name = "Multiply",
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "*",
                    ReturnType = Type("float4"),
                    Parameters = [Param("left", Type("float4x4")), Param("right", Type("float4"))],
                    ShaderContract = new ShaderContract
                    {
                        GlslName = "*",
                        Mapping = ShaderMappingKind.Builtin,
                        RequiredCapability = "matrix",
                    },
                    Body = "return left.c0 * right.x + left.c1 * right.y + left.c2 * right.z + left.c3 * right.w;",
                },
                new OperatorSpec
                {
                    Name = "Equality",
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "==",
                    ReturnType = Type("bool"),
                    Parameters = [Param("left", Type("float4x4")), Param("right", Type("float4x4"))],
                    Body = "return left.c0 == right.c0 && left.c1 == right.c1 && left.c2 == right.c2 && left.c3 == right.c3;",
                },
                new OperatorSpec
                {
                    Name = "Inequality",
                    Part = TypePart.Operators,
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Operator = "!=",
                    ReturnType = Type("bool"),
                    Parameters = [Param("left", Type("float4x4")), Param("right", Type("float4x4"))],
                    Body = "return !(left == right);",
                },
                new FunctionSpec
                {
                    Name = "Equals",
                    ReturnType = Type("bool"),
                    Parameters = [Param("other", Type("float4x4"))],
                    Body = "return this == other;",
                },
                new FunctionSpec
                {
                    Name = "Equals",
                    ReturnType = Type("bool"),
                    Modifiers = Modifiers.Public | Modifiers.Override,
                    Parameters = [Param("obj", Type("object"))],
                    Body = "return obj is float4x4 other && Equals(other);",
                },
                new FunctionSpec
                {
                    Name = "GetHashCode",
                    ReturnType = Type("int"),
                    Modifiers = Modifiers.Public | Modifiers.Override,
                    Body = "unchecked { var hash = 17; hash = hash * 31 + c0.GetHashCode(); hash = hash * 31 + c1.GetHashCode(); hash = hash * 31 + c2.GetHashCode(); hash = hash * 31 + c3.GetHashCode(); return hash; }",
                },
                new FunctionSpec
                {
                    Name = "ToString",
                    ReturnType = Type("string"),
                    Modifiers = Modifiers.Public | Modifiers.Override,
                    Body = "return FormattableString.Invariant($\"[{M11}, {M12}, {M13}, {M14}; {M21}, {M22}, {M23}, {M24}; {M31}, {M32}, {M33}, {M34}; {M41}, {M42}, {M43}, {M44}]\");",
                },
            };

            AddMatrixProperties(members);
            MarkShaderFunctionsStatic(members);
            return new TypeSpec
            {
                Namespace = "Delta.Maths",
                Name = "float4x4",
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = ["IEquatable<float4x4>"],
                ShaderContract = new ShaderContract
                {
                    GlslName = "mat4",
                    Mapping = ShaderMappingKind.Builtin,
                    ColumnMajor = true,
                    Alignment = 16,
                    MatrixStride = 16,
                    RequiredCapability = "std430",
                },
                Comment = "A column-major 4x4 matrix represented by four float4 columns.",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }

        private static void AddMatrixProperties(List<MemberSpec> members)
        {
            var columns = new[] { "c0", "c1", "c2", "c3" };
            var components = new[] { "x", "y", "z", "w" };
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 4; column++)
            {
                var name = $"M{row + 1}{column + 1}";
                var columnName = columns[column];
                var component = components[row];
                members.Add(new PropertySpec
                {
                    Name = name,
                    Type = Type("float"),
                    Part = TypePart.Core,
                    Getter = $"{columnName}.{component}",
                    Setter = $"{columnName}.{component} = value",
                });
            }
        }

        private static void MarkShaderFunctionsStatic(List<MemberSpec> members)
        {
            foreach (var function in members.OfType<FunctionSpec>())
                if (function.Targets.HasFlag(FunctionTargets.ShaderMaths))
                    function.Modifiers |= Modifiers.Static;
        }

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
                    Body = "this.x = x;\nthis.y = y;\nthis.z = z;\nthis.w = w;",
                },
                new ConstructorSpec
                {
                    Parameters = [Param("value", Type("float4"))],
                    Body = "x = value.x;\ny = value.y;\nz = value.z;\nw = value.w;",
                },
                new PropertySpec { Name = "Identity", Type = Type("quaternion"), Modifiers = Modifiers.Public | Modifiers.Static, Expression = "identity" },
                new PropertySpec { Name = "X", Type = Type("float"), Getter = "x", Setter = "x = value" },
                new PropertySpec { Name = "Y", Type = Type("float"), Getter = "y", Setter = "y = value" },
                new PropertySpec { Name = "Z", Type = Type("float"), Getter = "z", Setter = "z = value" },
                new PropertySpec { Name = "W", Type = Type("float"), Getter = "w", Setter = "w = value" },
                new FunctionSpec { Name = "Dot", ReturnType = Type("float"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "return left.x * right.x + left.y * right.y + left.z * right.z + left.w * right.w;" },
                new FunctionSpec { Name = "LengthSquared", ReturnType = Type("float"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "return Dot(value, value);" },
                new FunctionSpec { Name = "Normalize", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, ShaderContract = new ShaderContract { GlslName = "normalize", Mapping = ShaderMappingKind.Builtin, RequiredCapability = "quaternion" }, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "return value / Maths.Sqrt(LengthSquared(value));" },
                new FunctionSpec { Name = "NormalizeSafe", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var lengthSquared = LengthSquared(value);\nreturn lengthSquared <= 1e-20f ? identity : value / Maths.Sqrt(lengthSquared);" },
                new FunctionSpec { Name = "Conjugate", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "return new quaternion(-value.x, -value.y, -value.z, value.w);" },
                new FunctionSpec { Name = "TryInverse", ReturnType = Type("bool"), Parameters = [Param("value", Type("quaternion")), Param("result", Type("quaternion"), "out")], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var lengthSquared = LengthSquared(value);\nif (lengthSquared <= 1e-20f) { result = default; return false; }\nresult = Conjugate(value) / lengthSquared;\nreturn true;" },
                new FunctionSpec { Name = "Inverse", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "return TryInverse(value, out var result) ? result : identity;" },
                new FunctionSpec { Name = "Lerp", ReturnType = Type("quaternion"), Parameters = [Param("start", Type("quaternion")), Param("end", Type("quaternion")), Param("amount", Type("float"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "if (Dot(start, end) < 0f) end = -end;\nreturn NormalizeSafe(start + (end - start) * amount);" },
                new FunctionSpec { Name = "Slerp", ReturnType = Type("quaternion"), Parameters = [Param("start", Type("quaternion")), Param("end", Type("quaternion")), Param("amount", Type("float"))], Part = TypePart.Common, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var dot = Dot(start, end);\nif (dot < 0f) { end = -end; dot = -dot; }\nif (dot > 0.9995f) return Lerp(start, end, amount);\ndot = Maths.Clamp(dot, -1f, 1f);\nvar angle = Maths.Acos(dot);\nvar scale = 1f / Maths.Sin(angle);\nreturn start * (Maths.Sin((1f - amount) * angle) * scale) + end * (Maths.Sin(amount * angle) * scale);" },
                new FunctionSpec { Name = "CreateFromAxisAngle", ReturnType = Type("quaternion"), Parameters = [Param("axis", Type("float3")), Param("angle", Type("float"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var normalizedAxis = float3.NormalizeSafe(axis);\nvar halfAngle = angle * 0.5f;\nvar sine = Maths.Sin(halfAngle);\nreturn new quaternion(-normalizedAxis.x * sine, -normalizedAxis.y * sine, -normalizedAxis.z * sine, Maths.Cos(halfAngle));" },
                new FunctionSpec { Name = "CreateFromYawPitchRoll", ReturnType = Type("quaternion"), Parameters = [Param("yaw", Type("float")), Param("pitch", Type("float")), Param("roll", Type("float"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var halfRoll = roll * 0.5f;\nvar halfPitch = pitch * 0.5f;\nvar halfYaw = yaw * 0.5f;\nvar sinRoll = Maths.Sin(halfRoll);\nvar cosRoll = Maths.Cos(halfRoll);\nvar sinPitch = Maths.Sin(halfPitch);\nvar cosPitch = Maths.Cos(halfPitch);\nvar sinYaw = Maths.Sin(halfYaw);\nvar cosYaw = Maths.Cos(halfYaw);\nreturn new quaternion(\n    -(cosYaw * sinPitch * cosRoll + sinYaw * cosPitch * sinRoll),\n    -(sinYaw * cosPitch * cosRoll - cosYaw * sinPitch * sinRoll),\n    -(cosYaw * cosPitch * sinRoll - sinYaw * sinPitch * cosRoll),\n    cosYaw * cosPitch * cosRoll + sinYaw * sinPitch * sinRoll);\n" },
                new FunctionSpec { Name = "Rotate", ReturnType = Type("float3"), Parameters = [Param("rotation", Type("quaternion")), Param("value", Type("float3"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var qv = new float3(rotation.x, rotation.y, rotation.z);\nvar t = 2f * float3.Cross(qv, value);\nreturn value + rotation.w * t + float3.Cross(qv, t);" },
                new FunctionSpec { Name = "CreateFromRotationMatrix", ReturnType = Type("quaternion"), Parameters = [Param("matrix", Type("float4x4"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var trace = matrix.M11 + matrix.M22 + matrix.M33;\nif (trace > 0f) { var s = Maths.Sqrt(trace + 1f) * 2f; return new quaternion((matrix.M32 - matrix.M23) / s, (matrix.M13 - matrix.M31) / s, (matrix.M21 - matrix.M12) / s, 0.25f * s); }\nif (matrix.M11 > matrix.M22 && matrix.M11 > matrix.M33) { var s = Maths.Sqrt(1f + matrix.M11 - matrix.M22 - matrix.M33) * 2f; return new quaternion(0.25f * s, (matrix.M12 + matrix.M21) / s, (matrix.M13 + matrix.M31) / s, (matrix.M32 - matrix.M23) / s); }\nif (matrix.M22 > matrix.M33) { var s = Maths.Sqrt(1f + matrix.M22 - matrix.M11 - matrix.M33) * 2f; return new quaternion((matrix.M12 + matrix.M21) / s, 0.25f * s, (matrix.M23 + matrix.M32) / s, (matrix.M13 - matrix.M31) / s); }\n{ var s = Maths.Sqrt(1f + matrix.M33 - matrix.M11 - matrix.M22) * 2f; return new quaternion((matrix.M13 + matrix.M31) / s, (matrix.M23 + matrix.M32) / s, 0.25f * s, (matrix.M21 - matrix.M12) / s); }" },
                new FunctionSpec { Name = "ToRotationMatrix", ReturnType = Type("float4x4"), Parameters = [Param("rotation", Type("quaternion"))], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "return float4x4.CreateFromQuaternion(rotation);" },
                new FunctionSpec { Name = "ToAxisAngle", ReturnType = Type("void"), Parameters = [Param("rotation", Type("quaternion")), Param("axis", Type("float3"), "out"), Param("angle", Type("float"), "out")], Part = TypePart.Geometry, Targets = FunctionTargets.Type | FunctionTargets.ShaderMaths, Body = "var normalized = NormalizeSafe(rotation);\nangle = 2f * Maths.Acos(Maths.Clamp(normalized.w, -1f, 1f));\nvar scale = Maths.Sqrt(Maths.Max(1e-20f, 1f - normalized.w * normalized.w));\naxis = scale <= 1e-10f ? new float3(1f, 0f, 0f) : new float3(-normalized.x / scale, -normalized.y / scale, -normalized.z / scale);" },
                new OperatorSpec { Name = "Multiply", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "*", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], ShaderContract = new ShaderContract { GlslName = "delta_quaternionMultiply", Mapping = ShaderMappingKind.Helper, RequiredCapability = "quaternion" }, Body = "return new quaternion(left.w * right.x + left.x * right.w + left.y * right.z - left.z * right.y, left.w * right.y - left.x * right.z + left.y * right.w + left.z * right.x, left.w * right.z + left.x * right.y - left.y * right.x + left.z * right.w, left.w * right.w - left.x * right.x - left.y * right.y - left.z * right.z);" },
                new OperatorSpec { Name = "Multiply", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "*", ReturnType = Type("float3"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("float3"))], ShaderContract = new ShaderContract { GlslName = "delta_quaternionRotate", Mapping = ShaderMappingKind.Helper, RequiredCapability = "quaternion" }, Body = "return Rotate(left, right);" },
                new OperatorSpec { Name = "Add", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "+", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return new quaternion(left.x + right.x, left.y + right.y, left.z + right.z, left.w + right.w);" },
                new OperatorSpec { Name = "Subtract", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "-", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return new quaternion(left.x - right.x, left.y - right.y, left.z - right.z, left.w - right.w);" },
                new OperatorSpec { Name = "Negate", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "-", ReturnType = Type("quaternion"), Parameters = [Param("value", Type("quaternion"))], Body = "return new quaternion(-value.x, -value.y, -value.z, -value.w);" },
                new OperatorSpec { Name = "Multiply", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "*", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("float"))], Body = "return new quaternion(left.x * right, left.y * right, left.z * right, left.w * right);" },
                new OperatorSpec { Name = "Divide", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "/", ReturnType = Type("quaternion"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("float"))], Body = "return new quaternion(left.x / right, left.y / right, left.z / right, left.w / right);" },
                new OperatorSpec { Name = "Equality", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "==", ReturnType = Type("bool"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return left.x == right.x && left.y == right.y && left.z == right.z && left.w == right.w;" },
                new OperatorSpec { Name = "Inequality", Part = TypePart.Operators, Modifiers = Modifiers.Public | Modifiers.Static, Operator = "!=", ReturnType = Type("bool"), Parameters = [Param("left", Type("quaternion")), Param("right", Type("quaternion"))], Body = "return !(left == right);" },
                new FunctionSpec { Name = "Equals", ReturnType = Type("bool"), Parameters = [Param("other", Type("quaternion"))], Body = "return this == other;" },
                new FunctionSpec { Name = "Equals", ReturnType = Type("bool"), Modifiers = Modifiers.Public | Modifiers.Override, Parameters = [Param("obj", Type("object"))], Body = "return obj is quaternion other && Equals(other);" },
                new FunctionSpec { Name = "GetHashCode", ReturnType = Type("int"), Modifiers = Modifiers.Public | Modifiers.Override, Body = "unchecked { var hash = 17; hash = hash * 31 + x.GetHashCode(); hash = hash * 31 + y.GetHashCode(); hash = hash * 31 + z.GetHashCode(); hash = hash * 31 + w.GetHashCode(); return hash; }" },
                new FunctionSpec { Name = "ToString", ReturnType = Type("string"), Modifiers = Modifiers.Public | Modifiers.Override, Body = "return FormattableString.Invariant($\"({x}, {y}, {z}, {w})\");" },
            };

            MarkShaderFunctionsStatic(members);
            return new TypeSpec
            {
                Namespace = "Delta.Maths",
                Name = "quaternion",
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = ["IEquatable<quaternion>"],
                ShaderContract = new ShaderContract
                {
                    GlslName = "vec4",
                    Mapping = ShaderMappingKind.Builtin,
                    Alignment = 16,
                    RequiredCapability = "std430",
                },
                Comment = "A left-handed quaternion stored as (x, y, z, w).",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }
    }
}
