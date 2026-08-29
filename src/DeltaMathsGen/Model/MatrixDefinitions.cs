using System;
using System.Collections.Generic;
using System.Linq;
using static Delta.MathsGen.Model.DeclarationHelpers;

namespace Delta.MathsGen.Model
{
    internal static class MatrixDefinitions
    {
        public static TypeSpec[] Create()
        {
            var types = new List<TypeSpec>(18);
            foreach (var scalar in new[] { "float", "double" })
            {
                for (var columns = 2; columns <= 4; columns++)
                {
                    for (var rows = 2; rows <= 4; rows++)
                    {
                        types.Add(CreateMatrix(scalar, columns, rows));
                    }
                }
            }

            return types.ToArray();
        }

        private static TypeSpec CreateMatrix(string scalar, int columns, int rows)
        {
            var name = MatrixName(scalar, columns, rows);
            var vector = VectorName(scalar, rows);
            var members = new List<MemberSpec>();

            for (var column = 0; column < columns; column++)
            {
                members.Add(new FieldSpec
                {
                    Name = ColumnName(column),
                    Type = Type(vector),
                    Summary = $"Column {column}.",
                });
                if (NeedsMatrixPadding(scalar, rows))
                {
                    members.Add(new FieldSpec
                    {
                        Name = PaddingName(column),
                        Type = Type(scalar),
                        Modifiers = Modifiers.Private,
                    });
                }
            }

            members.Add(new FieldSpec
            {
                Name = "zero",
                Type = Type(name),
                Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
                Initializer = $"new {name}({string.Join(", ", Enumerable.Repeat(vector + ".zero", columns))})",
                Summary = "A matrix whose components are all zero.",
            });

            if (columns == rows)
            {
                members.Add(new FieldSpec
                {
                    Name = "identity",
                    Type = Type(name),
                    Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Readonly,
                    Initializer = $"new {name}({One(scalar)})",
                    Summary = "The identity matrix.",
                });
            }

            members.Add(new ConstructorSpec
            {
                Parameters = Enumerable.Range(0, columns)
                    .Select(column => Param(ColumnName(column), Type(vector)))
                    .ToArray(),
                Body = AssignColumns(scalar, columns, NeedsMatrixPadding(scalar, rows)),
                Summary = "Creates a matrix from its columns in column-major order.",
            });
            members.Add(new ConstructorSpec
            {
                Parameters = [Param("value", Type(scalar))],
                Body = ScalarConstructorBody(scalar, columns, rows),
                Summary = "Creates a matrix with value on the diagonal and zero elsewhere.",
            });
            members.Add(new ConstructorSpec
            {
                Parameters = RowMajorParameters(scalar, columns, rows),
                Body = RowMajorConstructorBody(scalar, columns, rows),
                Summary = "Creates a matrix from row-major named components in mathematical order.",
            });
            AddMatrixConversions(members, scalar, columns, rows);

            if (columns == rows)
            {
                members.Add(new PropertySpec
                {
                    Name = "Identity",
                    Type = Type(name),
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Expression = "identity",
                });
            }

            AddMatrixAccessors(members, scalar, columns, rows);
            AddMatrixProperties(members, scalar, columns, rows);
            AddMatrixAlgebra(members, scalar, columns, rows);
            if (columns == rows)
            {
                AddSquareAlgebra(members, scalar, columns, rows);
            }

            if (scalar == "float" && columns == 4 && rows == 4)
            {
                AddFloat4x4Geometry(members);
            }

            AddMatrixOperators(members, scalar, columns, rows);
            members.Add(new FunctionSpec
            {
                Name = "Equals",
                ReturnType = Type("bool"),
                Parameters = [Param("other", Type(name))],
                Body = "return " + string.Join(" && ", Enumerable.Range(0, columns).Select(column => $"c{column} == other.c{column}")) + ";",
            });
            members.Add(new FunctionSpec
            {
                Name = "Equals",
                ReturnType = Type("bool"),
                Modifiers = Modifiers.Public | Modifiers.Override,
                Parameters = [Param("obj", Type("object?"))],
                Body = $"return obj is {name} other && Equals(other);",
            });
            members.Add(new FunctionSpec
            {
                Name = "GetHashCode",
                ReturnType = Type("int"),
                Modifiers = Modifiers.Public | Modifiers.Override,
                Body = HashCodeBody(columns),
            });
            members.Add(new FunctionSpec
            {
                Name = "ToString",
                ReturnType = Type("string"),
                Modifiers = Modifiers.Public | Modifiers.Override,
                Body = ToStringBody(columns, rows),
            });

            MarkShaderFunctionsStatic(members, name);
            var stride = rows == 2 ? ScalarSize(scalar) * 2 : ScalarSize(scalar) * 4;
            return new TypeSpec
            {
                Namespace = "Delta.Maths",
                Name = name,
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = [$"IEquatable<{name}>"],
                ShaderContract = new ShaderContract
                {
                    GlslName = GlslName(scalar, columns, rows),
                    Mapping = ShaderMappingKind.Builtin,
                    ColumnMajor = true,
                    Alignment = stride,
                    MatrixStride = stride,
                    MatrixColumns = columns,
                    MatrixRows = rows,
                    ElementGlslType = scalar,
                    Size = columns * stride,
                    Capability = scalar == "double" ? ShaderCapability.Float64 : ShaderCapability.Std430,
                },
                Comment = $"A column-major {columns}x{rows} matrix represented by {columns} {scalar}{rows} columns.",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }

        private static void AddMatrixAccessors(List<MemberSpec> members, string scalar, int columns, int rows)
        {
            var vector = VectorName(scalar, rows);
            members.Add(new FunctionSpec
            {
                Name = "GetColumn",
                ReturnType = Type(vector),
                Parameters = [Param("index", Type("int"))],
                Part = TypePart.Core,
                Body = "return index switch { " + string.Join(", ", Enumerable.Range(0, columns).Select(column => $"{column} => c{column}")) + ", _ => throw new ArgumentOutOfRangeException(nameof(index)) };",
                Summary = "Returns a column using zero-based indexing.",
            });
            members.Add(new FunctionSpec
            {
                Name = "GetRow",
                ReturnType = Type(VectorName(scalar, columns)),
                Parameters = [Param("index", Type("int"))],
                Part = TypePart.Core,
                Body = "return index switch { " + string.Join(", ", Enumerable.Range(0, rows).Select(row => $"{row} => new {VectorName(scalar, columns)}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"c{column}.{Component(row)}"))})")) + ", _ => throw new ArgumentOutOfRangeException(nameof(index)) };",
                Summary = "Returns a row using zero-based indexing.",
            });
            members.Add(new FunctionSpec
            {
                Name = "GetElement",
                ReturnType = Type(scalar),
                Parameters = [Param("column", Type("int")), Param("row", Type("int"))],
                Part = TypePart.Core,
                Body = "return GetColumn(column)[row];",
            });
            members.Add(new FunctionSpec
            {
                Name = "SetElement",
                ReturnType = Type("void"),
                Parameters = [Param("column", Type("int")), Param("row", Type("int")), Param("value", Type(scalar))],
                Part = TypePart.Core,
                Body = SetElementBody(columns, rows),
            });
        }

        private static void AddMatrixConversions(List<MemberSpec> members, string scalar, int columns, int rows)
        {
            foreach (var sourceScalar in new[] { "float", "double" })
            {
                for (var sourceColumns = 2; sourceColumns <= 4; sourceColumns++)
                {
                    for (var sourceRows = 2; sourceRows <= 4; sourceRows++)
                    {
                        var source = MatrixName(sourceScalar, sourceColumns, sourceRows);
                        members.Add(new ConstructorSpec
                        {
                            Parameters = [Param("value", Type(source))],
                            Body = MatrixConversionBody(scalar, columns, rows, sourceScalar, sourceColumns, sourceRows),
                            Summary = $"Creates a {MatrixName(scalar, columns, rows)} from a {source} using GLSL matrix conversion rules.",
                        });
                    }
                }
            }
        }

        private static void AddMatrixProperties(List<MemberSpec> members, string scalar, int columns, int rows)
        {
            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < columns; column++)
                {
                    var columnName = ColumnName(column);
                    var component = Component(row);
                    members.Add(new PropertySpec
                    {
                        Name = $"M{row + 1}{column + 1}",
                        Type = Type(scalar),
                        Part = TypePart.Core,
                        Getter = $"{columnName}.{component}",
                        Setter = $"{columnName}.{component} = value",
                    });
                }
            }
        }

        private static void AddMatrixAlgebra(List<MemberSpec> members, string scalar, int columns, int rows)
        {
            var name = MatrixName(scalar, columns, rows);
            var transposed = MatrixName(scalar, rows, columns);
            var vector = VectorName(scalar, rows);
            var sourceVector = VectorName(scalar, columns);
            members.Add(new FunctionSpec
            {
                Name = "Transpose",
                ReturnType = Type(transposed),
                Parameters = [Param("value", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("transpose", MatrixCapability(scalar)),
                Body = "return new " + transposed + "(" + string.Join(", ", Enumerable.Range(0, rows).Select(row => $"new {sourceVector}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"value.c{column}.{Component(row)}"))})")) + ");",
            });
            members.Add(new FunctionSpec
            {
                Name = "MatrixCompMult",
                ReturnType = Type(name),
                Parameters = [Param("left", Type(name)), Param("right", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("matrixCompMult", MatrixCapability(scalar)),
                Body = "return new " + name + "(" + string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} * right.c{column}")) + ");",
            });
            members.Add(new FunctionSpec
            {
                Name = "OuterProduct",
                ReturnType = Type(name),
                Parameters = [Param("c", Type(vector)), Param("r", Type(sourceVector))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("outerProduct", MatrixCapability(scalar)),
                Body = "return new " + name + "(" + string.Join(", ", Enumerable.Range(0, columns).Select(column => $"c * r.{Component(column)}")) + ");",
            });
        }

        private static void AddSquareAlgebra(List<MemberSpec> members, string scalar, int size, int rows)
        {
            var name = MatrixName(scalar, size, rows);
            members.Add(new FunctionSpec
            {
                Name = "Determinant",
                ReturnType = Type(scalar),
                Parameters = [Param("value", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("determinant", MatrixCapability(scalar)),
                Body = DeterminantBody(scalar, size),
            });
            members.Add(new FunctionSpec
            {
                Name = "TryInverse",
                ReturnType = Type("bool"),
                Parameters = [Param("value", Type(name)), Param("result", Type(name), ParameterModifier.Out)],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = InverseBody(scalar, size),
            });
            members.Add(new FunctionSpec
            {
                Name = "Inverse",
                ReturnType = Type(name),
                Parameters = [Param("value", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("inverse", MatrixCapability(scalar)),
                Body = $"return TryInverse(value, out var result) ? result : {name}.identity;",
            });
        }

        private static void AddMatrixOperators(List<MemberSpec> members, string scalar, int columns, int rows)
        {
            var name = MatrixName(scalar, columns, rows);
            var vector = VectorName(scalar, rows);
            var sourceVector = VectorName(scalar, columns);
            AddOperator(members, scalar, "Add", "+", name, [Param("left", Type(name)), Param("right", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} + right.c{column}"))});");
            AddOperator(members, scalar, "Subtract", "-", name, [Param("left", Type(name)), Param("right", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} - right.c{column}"))});");
            AddOperator(members, scalar, "Negate", "-", name, [Param("value", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"-value.c{column}"))});");
            AddOperator(members, scalar, "Multiply", "*", name, [Param("left", Type(name)), Param("right", Type(scalar))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} * right"))});");
            AddOperator(members, scalar, "Multiply", "*", name, [Param("left", Type(scalar)), Param("right", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left * right.c{column}"))});");
            AddOperator(members, scalar, "Divide", "/", name, [Param("left", Type(name)), Param("right", Type(scalar))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} / right"))});");
            AddOperator(members, scalar, "Multiply", "*", vector, [Param("left", Type(name)), Param("right", Type(sourceVector))], $"return new {vector}({string.Join(", ", Enumerable.Range(0, rows).Select(row => string.Join(" + ", Enumerable.Range(0, columns).Select(column => $"left.c{column}.{Component(row)} * right.{Component(column)}"))))});");
            AddOperator(members, scalar, "Multiply", "*", VectorName(scalar, columns), [Param("left", Type(VectorName(scalar, rows))), Param("right", Type(name))], $"return new {VectorName(scalar, columns)}({string.Join(", ", Enumerable.Range(0, columns).Select(column => string.Join(" + ", Enumerable.Range(0, rows).Select(row => $"left.{Component(row)} * right.c{column}.{Component(row)}"))))});");
            AddOperator(members, scalar, "Equality", "==", "bool", [Param("left", Type(name)), Param("right", Type(name))], "return " + string.Join(" && ", Enumerable.Range(0, columns).Select(column => $"left.c{column} == right.c{column}")) + ";");
            AddOperator(members, scalar, "Inequality", "!=", "bool", [Param("left", Type(name)), Param("right", Type(name))], "return !(left == right);");

            for (var rightColumns = 2; rightColumns <= 4; rightColumns++)
            {
                var right = MatrixName(scalar, rightColumns, columns);
                var result = MatrixName(scalar, rightColumns, rows);
                AddOperator(
                    members,
                    scalar,
                    "Multiply",
                    "*",
                    result,
                    [Param("left", Type(name)), Param("right", Type(right))],
                    $"return new {result}({string.Join(", ", Enumerable.Range(0, rightColumns).Select(column => $"left * right.c{column}"))});");
            }
        }

        private static void AddOperator(List<MemberSpec> members, string scalar, string name, string symbol, string returnType, ParameterSpec[] parameters, string body)
        {
            members.Add(new OperatorSpec
            {
                Name = name,
                Part = TypePart.Operators,
                Modifiers = Modifiers.Public | Modifiers.Static,
                Operator = symbol,
                ReturnType = Type(returnType),
                Parameters = parameters,
                ShaderContract = Builtin(symbol, MatrixCapability(scalar)),
                Body = body,
            });
        }

        private static void AddFloat4x4Geometry(List<MemberSpec> members)
        {
            members.Add(new FunctionSpec
            {
                Name = "CreateTranslation",
                ReturnType = Type("float4x4"),
                Parameters = [Param("translation", Type("float3"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "return new float4x4(1f, 0f, 0f, translation.x, 0f, 1f, 0f, translation.y, 0f, 0f, 1f, translation.z, 0f, 0f, 0f, 1f);",
            });
            members.Add(new FunctionSpec
            {
                Name = "CreateScale",
                ReturnType = Type("float4x4"),
                Parameters = [Param("scale", Type("float3"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "return new float4x4(scale.x, 0f, 0f, 0f, 0f, scale.y, 0f, 0f, 0f, 0f, scale.z, 0f, 0f, 0f, 0f, 1f);",
            });
            members.Add(new FunctionSpec
            {
                Name = "CreateScale",
                ReturnType = Type("float4x4"),
                Parameters = [Param("scale", Type("float"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "return CreateScale(new float3(scale, scale, scale));",
            });
            members.Add(new FunctionSpec
            {
                Name = "CreateFromQuaternion",
                ReturnType = Type("float4x4"),
                Parameters = [Param("rotation", Type("quaternion"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
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
                return new float4x4(1f - 2f * (yy + zz), 2f * (xy - wz), 2f * (xz + wy), 0f, 2f * (xy + wz), 1f - 2f * (xx + zz), 2f * (yz - wx), 0f, 2f * (xz - wy), 2f * (yz + wx), 1f - 2f * (xx + yy), 0f, 0f, 0f, 0f, 1f);
                """,
            });
            members.Add(new FunctionSpec
            {
                Name = "CreateTRS",
                ReturnType = Type("float4x4"),
                Parameters = [Param("translation", Type("float3")), Param("rotation", Type("quaternion")), Param("scale", Type("float3"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "return CreateTranslation(translation) * CreateFromQuaternion(rotation) * CreateScale(scale);",
            });
            members.Add(new FunctionSpec
            {
                Name = "TransformPoint",
                ReturnType = Type("float3"),
                Parameters = [Param("matrix", Type("float4x4")), Param("point", Type("float3"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body =
                """
                var transformed = matrix * new float4(point, 1f);
                return transformed.w == 0f ? transformed.xyz : transformed.xyz / transformed.w;
                """,
            });
            members.Add(new FunctionSpec
            {
                Name = "TransformDirection",
                ReturnType = Type("float3"),
                Parameters = [Param("matrix", Type("float4x4")), Param("direction", Type("float3"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "return (matrix * new float4(direction, 0f)).xyz;",
            });
            members.Add(new FunctionSpec
            {
                Name = "CreateLookTo",
                ReturnType = Type("float4x4"),
                Parameters = [Param("eye", Type("float3")), Param("direction", Type("float3")), Param("up", Type("float3"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body =
                """
                var zaxis = float3.NormalizeSafe(direction);
                var xaxis = float3.NormalizeSafe(float3.Cross(up, zaxis));
                var yaxis = float3.Cross(zaxis, xaxis);
                return new float4x4(xaxis.x, yaxis.x, zaxis.x, -float3.Dot(xaxis, eye), xaxis.y, yaxis.y, zaxis.y, -float3.Dot(yaxis, eye), xaxis.z, yaxis.z, zaxis.z, -float3.Dot(zaxis, eye), 0f, 0f, 0f, 1f);
                """,
            });
            members.Add(new FunctionSpec
            {
                Name = "CreatePerspectiveFieldOfViewLeftHanded",
                ReturnType = Type("float4x4"),
                Parameters = [Param("fieldOfView", Type("float")), Param("aspectRatio", Type("float")), Param("nearPlaneDistance", Type("float")), Param("farPlaneDistance", Type("float"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body =
                """
                var yScale = 1f / DeltaMaths.Tan(fieldOfView * 0.5f);
                var xScale = yScale / aspectRatio;
                var range = farPlaneDistance / (farPlaneDistance - nearPlaneDistance);
                return new float4x4(xScale, 0f, 0f, 0f, 0f, yScale, 0f, 0f, 0f, 0f, range, -nearPlaneDistance * range, 0f, 0f, 1f, 0f);
                """,
            });
            members.Add(new FunctionSpec
            {
                Name = "Decompose",
                ReturnType = Type("bool"),
                Parameters = [Param("value", Type("float4x4")), Param("scale", Type("float3"), ParameterModifier.Out), Param("rotation", Type("quaternion"), ParameterModifier.Out), Param("translation", Type("float3"), ParameterModifier.Out)],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body =
                """
                translation = new float3(value.M14, value.M24, value.M34);
                var x = new float3(value.M11, value.M21, value.M31);
                var y = new float3(value.M12, value.M22, value.M32);
                var z = new float3(value.M13, value.M23, value.M33);
                scale = new float3(float3.Length(x), float3.Length(y), float3.Length(z));
                if (scale.x <= 1e-20f || scale.y <= 1e-20f || scale.z <= 1e-20f) { rotation = quaternion.identity; return false; }
                x /= scale.x;
                y /= scale.y;
                z /= scale.z;
                if (float3.Dot(float3.Cross(x, y), z) < 0f) { scale.x = -scale.x; x = -x; }
                var rotationMatrix = new float4x4(x.x, y.x, z.x, 0f, x.y, y.y, z.y, 0f, x.z, y.z, z.z, 0f, 0f, 0f, 0f, 1f);
                rotation = quaternion.NormalizeSafe(quaternion.CreateFromRotationMatrix(rotationMatrix));
                return true;
                """,
            });
        }

        private static string DeterminantBody(string scalar, int size) => size switch
        {
            2 => "return value.M11 * value.M22 - value.M12 * value.M21;",
            3 => "return value.M11 * (value.M22 * value.M33 - value.M23 * value.M32) - value.M12 * (value.M21 * value.M33 - value.M23 * value.M31) + value.M13 * (value.M21 * value.M32 - value.M22 * value.M31);",
            4 => $"""
            static {scalar} minor({scalar} a11, {scalar} a12, {scalar} a13, {scalar} a21, {scalar} a22, {scalar} a23, {scalar} a31, {scalar} a32, {scalar} a33) => a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);
            return value.M11 * minor(value.M22, value.M23, value.M24, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44) - value.M12 * minor(value.M21, value.M23, value.M24, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44) + value.M13 * minor(value.M21, value.M22, value.M24, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44) - value.M14 * minor(value.M21, value.M22, value.M23, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43);
            """,
            _ => throw new ArgumentOutOfRangeException(nameof(size)),
        };

        private static string InverseBody(string scalar, int size)
        {
            var name = MatrixName(scalar, size, size);
            var one = One(scalar);
            var threshold = InverseThreshold(scalar);
            return size switch
            {
                2 => $$"""
                var determinant = Determinant(value);
                if (DeltaMaths.Abs(determinant) <= {{threshold}}) { result = default; return false; }
                var inverse = {{one}} / determinant;
                result = new {{name}}(value.M22 * inverse, -value.M12 * inverse, -value.M21 * inverse, value.M11 * inverse);
                return true;
                """,
                3 => $$"""
                var c11 = value.M22 * value.M33 - value.M23 * value.M32;
                var c12 = value.M13 * value.M32 - value.M12 * value.M33;
                var c13 = value.M12 * value.M23 - value.M13 * value.M22;
                var c21 = value.M23 * value.M31 - value.M21 * value.M33;
                var c22 = value.M11 * value.M33 - value.M13 * value.M31;
                var c23 = value.M13 * value.M21 - value.M11 * value.M23;
                var c31 = value.M21 * value.M32 - value.M22 * value.M31;
                var c32 = value.M12 * value.M31 - value.M11 * value.M32;
                var c33 = value.M11 * value.M22 - value.M12 * value.M21;
                var determinant = value.M11 * c11 + value.M12 * c12 + value.M13 * c13;
                if (DeltaMaths.Abs(determinant) <= {{threshold}}) { result = default; return false; }
                var inverse = {{one}} / determinant;
                result = new {{name}}(c11 * inverse, c21 * inverse, c31 * inverse, c12 * inverse, c22 * inverse, c32 * inverse, c13 * inverse, c23 * inverse, c33 * inverse);
                return true;
                """,
                4 => InverseFourBody(scalar),
                _ => throw new ArgumentOutOfRangeException(nameof(size)),
            };
        }

        private static string InverseFourBody(string scalar)
        {
            var name = MatrixName(scalar, 4, 4);
            var one = One(scalar);
            var threshold = InverseThreshold(scalar);
            return $$"""
            var determinant = Determinant(value);
            if (DeltaMaths.Abs(determinant) <= {{threshold}}) { result = default; return false; }
            var inverse = {{one}} / determinant;
            static {{scalar}} minor({{scalar}} a11, {{scalar}} a12, {{scalar}} a13, {{scalar}} a21, {{scalar}} a22, {{scalar}} a23, {{scalar}} a31, {{scalar}} a32, {{scalar}} a33) => a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);
            result = new {{name}}(
                minor(value.M22, value.M23, value.M24, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44) * inverse,
                -minor(value.M12, value.M13, value.M14, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44) * inverse,
                minor(value.M12, value.M13, value.M14, value.M22, value.M23, value.M24, value.M42, value.M43, value.M44) * inverse,
                -minor(value.M12, value.M13, value.M14, value.M22, value.M23, value.M24, value.M32, value.M33, value.M34) * inverse,
                -minor(value.M21, value.M23, value.M24, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44) * inverse,
                minor(value.M11, value.M13, value.M14, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44) * inverse,
                -minor(value.M11, value.M13, value.M14, value.M21, value.M23, value.M24, value.M41, value.M43, value.M44) * inverse,
                minor(value.M11, value.M13, value.M14, value.M21, value.M23, value.M24, value.M31, value.M33, value.M34) * inverse,
                minor(value.M21, value.M22, value.M24, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44) * inverse,
                -minor(value.M11, value.M12, value.M14, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44) * inverse,
                minor(value.M11, value.M12, value.M14, value.M21, value.M22, value.M24, value.M41, value.M42, value.M44) * inverse,
                -minor(value.M11, value.M12, value.M14, value.M21, value.M22, value.M24, value.M31, value.M32, value.M34) * inverse,
                -minor(value.M21, value.M22, value.M23, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43) * inverse,
                minor(value.M11, value.M12, value.M13, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43) * inverse,
                -minor(value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M41, value.M42, value.M43) * inverse,
                minor(value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M31, value.M32, value.M33) * inverse);
            return true;
            """;
        }

        private static string AssignColumns(string scalar, int columns, bool includePadding)
        {
            var lines = new List<string>(columns * (includePadding ? 2 : 1));
            for (var column = 0; column < columns; column++)
            {
                lines.Add($"this.c{column} = c{column};");
                if (includePadding)
                {
                    lines.Add($"this._padding{column} = {Zero(scalar)};");
                }
            }

            return string.Join("\n", lines.Where(line => includePadding || !line.Contains("padding", StringComparison.Ordinal)));
        }

        private static string ScalarConstructorBody(string scalar, int columns, int rows)
        {
            var lines = new List<string>(columns + 1);
            for (var column = 0; column < columns; column++)
            {
                var values = Enumerable.Range(0, rows).Select(row => row == column ? "value" : Zero(scalar));
                lines.Add($"c{column} = new {VectorName(scalar, rows)}({string.Join(", ", values)});");
                if (NeedsMatrixPadding(scalar, rows))
                {
                    lines.Add($"_padding{column} = {Zero(scalar)};");
                }
            }

            return string.Join("\n", lines);
        }

        private static string MatrixConversionBody(string scalar, int columns, int rows, string sourceScalar, int sourceColumns, int sourceRows)
        {
            var lines = new List<string>(columns + 1);
            for (var column = 0; column < columns; column++)
            {
                var values = Enumerable.Range(0, rows).Select(row =>
                    row < sourceRows && column < sourceColumns
                        ? sourceScalar == scalar
                            ? $"value.c{column}.{Component(row)}"
                            : $"({scalar})value.c{column}.{Component(row)}"
                        : row == column ? One(scalar) : Zero(scalar));
                lines.Add($"c{column} = new {VectorName(scalar, rows)}({string.Join(", ", values)});");
                if (NeedsMatrixPadding(scalar, rows))
                {
                    lines.Add($"_padding{column} = {Zero(scalar)};");
                }
            }

            return string.Join("\n", lines);
        }

        private static ParameterSpec[] RowMajorParameters(string scalar, int columns, int rows) =>
            Enumerable.Range(0, rows)
                .SelectMany(row => Enumerable.Range(0, columns).Select(column => Param($"m{row + 1}{column + 1}", Type(scalar))))
                .ToArray();

        private static string RowMajorConstructorBody(string scalar, int columns, int rows)
        {
            var lines = new List<string>(columns + 1);
            for (var column = 0; column < columns; column++)
            {
                var values = Enumerable.Range(0, rows).Select(row => $"m{row + 1}{column + 1}");
                lines.Add($"c{column} = new {VectorName(scalar, rows)}({string.Join(", ", values)});");
                if (NeedsMatrixPadding(scalar, rows))
                {
                    lines.Add($"_padding{column} = {Zero(scalar)};");
                }
            }

            return string.Join("\n", lines);
        }

        private static string SetElementBody(int columns, int rows)
        {
            var cases = string.Join(" ", Enumerable.Range(0, columns).Select(column => $"case {column}: c{column} = columnValue; break;"));
            return $$"""
            if ((uint)column >= {{columns}}u || (uint)row >= {{rows}}u) throw new ArgumentOutOfRangeException();
            var columnValue = GetColumn(column);
            columnValue[row] = value;
            switch (column) { {{cases}} }
            """;
        }

        private static string HashCodeBody(int columns) =>
            "unchecked { var hash = 17; " + string.Join(" ", Enumerable.Range(0, columns).Select(column => $"hash = hash * 31 + c{column}.GetHashCode();")) + " return hash; }";

        private static string ToStringBody(int columns, int rows)
        {
            var rowValues = Enumerable.Range(0, rows).Select(row =>
                string.Join(", ", Enumerable.Range(0, columns).Select(column => $"M{row + 1}{column + 1}")));
            var formattedRows = rowValues.Select(row => string.Join(", ", row.Split(", ").Select(value => "{" + value + "}")));
            return "return FormattableString.Invariant($\"[" + string.Join("; ", formattedRows) + "]\");";
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
            if (IsMatrix(typeName))
            {
                return function.Name switch
                {
                    "CreateTranslation" => Helper("delta_createTranslation", "matrix"),
                    "CreateScale" => Helper("delta_createScale", "matrix"),
                    "CreateFromQuaternion" => Helper("delta_createFromQuaternion", "matrix"),
                    "CreateTRS" => Helper("delta_createTRS", "matrix"),
                    "CreateLookTo" => Helper("delta_createLookTo", "matrix"),
                    "CreatePerspectiveFieldOfViewLeftHanded" => Helper("delta_createPerspectiveFieldOfViewLeftHanded", "matrix"),
                    "TransformPoint" => Helper("delta_transformPoint", "matrix"),
                    "TransformDirection" => Helper("delta_transformDirection", "matrix"),
                    _ => new ShaderContract(),
                };
            }

            return new ShaderContract();
        }

        private static ShaderContract Builtin(string name, ShaderCapability capability) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Builtin,
            Capability = capability,
            Stages = ShaderStages.All,
        };

        private static ShaderContract Builtin(string name, string capability) => Builtin(name, ParseCapability(capability));

        private static ShaderContract Helper(string name, string capability) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Helper,
            Capability = ParseCapability(capability),
            Stages = ShaderStages.All,
        };

        private static ShaderCapability ParseCapability(string capability) => capability switch
        {
            "matrix" => ShaderCapability.Matrix,
            _ => throw new InvalidOperationException($"Unsupported shader capability '{capability}'."),
        };

        private static ShaderCapability MatrixCapability(string scalar) => scalar == "double"
            ? ShaderCapability.Float64
            : ShaderCapability.Matrix;

        private static bool IsMatrix(string name) => (name.StartsWith("float", StringComparison.Ordinal)
            || name.StartsWith("double", StringComparison.Ordinal))
            && name.Contains('x', StringComparison.Ordinal);

        private static string MatrixName(string scalar, int columns, int rows) => $"{scalar}{columns}x{rows}";

        private static string GlslName(string scalar, int columns, int rows)
        {
            var prefix = scalar == "double" ? "dmat" : "mat";
            return columns == rows ? $"{prefix}{columns}" : $"{prefix}{columns}x{rows}";
        }

        private static string VectorName(string scalar, int dimension) => $"{scalar}{dimension}";

        private static int ScalarSize(string scalar) => scalar == "double" ? 8 : 4;

        private static string Zero(string scalar) => scalar == "double" ? "0.0" : "0f";

        private static string One(string scalar) => scalar == "double" ? "1.0" : "1f";

        private static string InverseThreshold(string scalar) => scalar == "double" ? "1e-12" : "1e-8f";

        private static bool NeedsMatrixPadding(string scalar, int rows) => rows == 3 && scalar != "double";

        private static string ColumnName(int column) => $"c{column}";

        private static string PaddingName(int column) => $"_padding{column}";

        private static string Component(int row) => row switch
        {
            0 => "x",
            1 => "y",
            2 => "z",
            3 => "w",
            _ => throw new ArgumentOutOfRangeException(nameof(row)),
        };
    }
}
