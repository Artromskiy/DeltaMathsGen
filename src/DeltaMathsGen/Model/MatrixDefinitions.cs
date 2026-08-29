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
            var types = new List<TypeSpec>(9);
            for (var columns = 2; columns <= 4; columns++)
            {
                for (var rows = 2; rows <= 4; rows++)
                {
                    types.Add(CreateMatrix(columns, rows));
                }
            }

            return types.ToArray();
        }

        private static TypeSpec CreateMatrix(int columns, int rows)
        {
            var name = MatrixName(columns, rows);
            var vector = VectorName(rows);
            var members = new List<MemberSpec>();

            for (var column = 0; column < columns; column++)
            {
                members.Add(new FieldSpec
                {
                    Name = ColumnName(column),
                    Type = Type(vector),
                    Summary = $"Column {column}.",
                });
                if (rows == 3)
                {
                    members.Add(new FieldSpec
                    {
                        Name = PaddingName(column),
                        Type = Type("float"),
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
                    Initializer = $"new {name}(1f)",
                    Summary = "The identity matrix.",
                });
            }

            members.Add(new ConstructorSpec
            {
                Parameters = Enumerable.Range(0, columns)
                    .Select(column => Param(ColumnName(column), Type(vector)))
                    .ToArray(),
                Body = AssignColumns(columns, includePadding: rows == 3),
                Summary = "Creates a matrix from its columns in column-major order.",
            });
            members.Add(new ConstructorSpec
            {
                Parameters = [Param("value", Type("float"))],
                Body = ScalarConstructorBody(columns, rows),
                Summary = "Creates a matrix with value on the diagonal and zero elsewhere.",
            });
            members.Add(new ConstructorSpec
            {
                Parameters = RowMajorParameters(columns, rows),
                Body = RowMajorConstructorBody(columns, rows),
                Summary = "Creates a matrix from row-major named components in mathematical order.",
            });
            AddMatrixConversions(members, columns, rows);

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

            AddMatrixAccessors(members, columns, rows);
            AddMatrixProperties(members, columns, rows);
            AddMatrixAlgebra(members, columns, rows);
            if (columns == rows)
            {
                AddSquareAlgebra(members, columns, rows);
            }

            if (columns == 4 && rows == 4)
            {
                AddFloat4x4Geometry(members);
            }

            AddMatrixOperators(members, columns, rows);
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
            var stride = rows == 2 ? 8 : 16;
            return new TypeSpec
            {
                Namespace = "Delta.Maths",
                Name = name,
                Kind = "struct",
                Modifiers = Modifiers.Public | Modifiers.Partial,
                Interfaces = [$"IEquatable<{name}>"],
                ShaderContract = new ShaderContract
                {
                    GlslName = GlslName(columns, rows),
                    Mapping = ShaderMappingKind.Builtin,
                    ColumnMajor = true,
                    Alignment = stride,
                    MatrixStride = stride,
                    MatrixColumns = columns,
                    MatrixRows = rows,
                    ElementGlslType = "float",
                    Size = columns * stride,
                    Capability = ShaderCapability.Std430,
                },
                Comment = $"A column-major {columns}x{rows} matrix represented by {columns} float{rows} columns.",
                Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)", "System.Runtime.Serialization.DataContract"],
                Members = members.ToArray(),
            };
        }

        private static void AddMatrixAccessors(List<MemberSpec> members, int columns, int rows)
        {
            var vector = VectorName(rows);
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
                ReturnType = Type(VectorName(columns)),
                Parameters = [Param("index", Type("int"))],
                Part = TypePart.Core,
                Body = "return index switch { " + string.Join(", ", Enumerable.Range(0, rows).Select(row => $"{row} => new {VectorName(columns)}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"c{column}.{Component(row)}"))})")) + ", _ => throw new ArgumentOutOfRangeException(nameof(index)) };",
                Summary = "Returns a row using zero-based indexing.",
            });
            members.Add(new FunctionSpec
            {
                Name = "GetElement",
                ReturnType = Type("float"),
                Parameters = [Param("column", Type("int")), Param("row", Type("int"))],
                Part = TypePart.Core,
                Body = "return GetColumn(column)[row];",
            });
            members.Add(new FunctionSpec
            {
                Name = "SetElement",
                ReturnType = Type("void"),
                Parameters = [Param("column", Type("int")), Param("row", Type("int")), Param("value", Type("float"))],
                Part = TypePart.Core,
                Body = SetElementBody(columns, rows),
            });
        }

        private static void AddMatrixConversions(List<MemberSpec> members, int columns, int rows)
        {
            for (var sourceColumns = 2; sourceColumns <= 4; sourceColumns++)
            {
                for (var sourceRows = 2; sourceRows <= 4; sourceRows++)
                {
                    var source = MatrixName(sourceColumns, sourceRows);
                    members.Add(new ConstructorSpec
                    {
                        Parameters = [Param("value", Type(source))],
                        Body = MatrixConversionBody(columns, rows, sourceColumns, sourceRows),
                        Summary = $"Creates a {MatrixName(columns, rows)} from a {source} using GLSL matrix conversion rules.",
                    });
                }
            }
        }

        private static void AddMatrixProperties(List<MemberSpec> members, int columns, int rows)
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
                        Type = Type("float"),
                        Part = TypePart.Core,
                        Getter = $"{columnName}.{component}",
                        Setter = $"{columnName}.{component} = value",
                    });
                }
            }
        }

        private static void AddMatrixAlgebra(List<MemberSpec> members, int columns, int rows)
        {
            var name = MatrixName(columns, rows);
            var transposed = MatrixName(rows, columns);
            var vector = VectorName(rows);
            var sourceVector = VectorName(columns);
            members.Add(new FunctionSpec
            {
                Name = "Transpose",
                ReturnType = Type(transposed),
                Parameters = [Param("value", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("transpose", "matrix"),
                Body = "return new " + transposed + "(" + string.Join(", ", Enumerable.Range(0, rows).Select(row => $"new {sourceVector}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"value.c{column}.{Component(row)}"))})")) + ");",
            });
            members.Add(new FunctionSpec
            {
                Name = "MatrixCompMult",
                ReturnType = Type(name),
                Parameters = [Param("left", Type(name)), Param("right", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("matrixCompMult", "matrix"),
                Body = "return new " + name + "(" + string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} * right.c{column}")) + ");",
            });
            members.Add(new FunctionSpec
            {
                Name = "OuterProduct",
                ReturnType = Type(name),
                Parameters = [Param("c", Type(vector)), Param("r", Type(sourceVector))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("outerProduct", "matrix"),
                Body = "return new " + name + "(" + string.Join(", ", Enumerable.Range(0, columns).Select(column => $"c * r.{Component(column)}")) + ");",
            });
        }

        private static void AddSquareAlgebra(List<MemberSpec> members, int size, int rows)
        {
            var name = MatrixName(size, rows);
            members.Add(new FunctionSpec
            {
                Name = "Determinant",
                ReturnType = Type("float"),
                Parameters = [Param("value", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("determinant", "matrix"),
                Body = DeterminantBody(size),
            });
            members.Add(new FunctionSpec
            {
                Name = "TryInverse",
                ReturnType = Type("bool"),
                Parameters = [Param("value", Type(name)), Param("result", Type(name), ParameterModifier.Out)],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = InverseBody(size),
            });
            members.Add(new FunctionSpec
            {
                Name = "Inverse",
                ReturnType = Type(name),
                Parameters = [Param("value", Type(name))],
                Part = TypePart.Common,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                ShaderContract = Builtin("inverse", "matrix"),
                Body = $"return TryInverse(value, out var result) ? result : {name}.identity;",
            });
        }

        private static void AddMatrixOperators(List<MemberSpec> members, int columns, int rows)
        {
            var name = MatrixName(columns, rows);
            var vector = VectorName(rows);
            var sourceVector = VectorName(columns);
            AddOperator(members, "Add", "+", name, [Param("left", Type(name)), Param("right", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} + right.c{column}"))});");
            AddOperator(members, "Subtract", "-", name, [Param("left", Type(name)), Param("right", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} - right.c{column}"))});");
            AddOperator(members, "Negate", "-", name, [Param("value", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"-value.c{column}"))});");
            AddOperator(members, "Multiply", "*", name, [Param("left", Type(name)), Param("right", Type("float"))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} * right"))});");
            AddOperator(members, "Multiply", "*", name, [Param("left", Type("float")), Param("right", Type(name))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left * right.c{column}"))});");
            AddOperator(members, "Divide", "/", name, [Param("left", Type(name)), Param("right", Type("float"))], $"return new {name}({string.Join(", ", Enumerable.Range(0, columns).Select(column => $"left.c{column} / right"))});");
            AddOperator(members, "Multiply", "*", vector, [Param("left", Type(name)), Param("right", Type(sourceVector))], $"return new {vector}({string.Join(", ", Enumerable.Range(0, rows).Select(row => string.Join(" + ", Enumerable.Range(0, columns).Select(column => $"left.c{column}.{Component(row)} * right.{Component(column)}"))))});");
            AddOperator(members, "Multiply", "*", VectorName(columns), [Param("left", Type(VectorName(rows))), Param("right", Type(name))], $"return new {VectorName(columns)}({string.Join(", ", Enumerable.Range(0, columns).Select(column => string.Join(" + ", Enumerable.Range(0, rows).Select(row => $"left.{Component(row)} * right.c{column}.{Component(row)}"))))});");
            AddOperator(members, "Equality", "==", "bool", [Param("left", Type(name)), Param("right", Type(name))], "return " + string.Join(" && ", Enumerable.Range(0, columns).Select(column => $"left.c{column} == right.c{column}")) + ";");
            AddOperator(members, "Inequality", "!=", "bool", [Param("left", Type(name)), Param("right", Type(name))], "return !(left == right);");

            for (var rightColumns = 2; rightColumns <= 4; rightColumns++)
            {
                var right = MatrixName(rightColumns, columns);
                var result = MatrixName(rightColumns, rows);
                AddOperator(
                    members,
                    "Multiply",
                    "*",
                    result,
                    [Param("left", Type(name)), Param("right", Type(right))],
                    $"return new {result}({string.Join(", ", Enumerable.Range(0, rightColumns).Select(column => $"left * right.c{column}"))});");
            }
        }

        private static void AddOperator(List<MemberSpec> members, string name, string symbol, string returnType, ParameterSpec[] parameters, string body)
        {
            members.Add(new OperatorSpec
            {
                Name = name,
                Part = TypePart.Operators,
                Modifiers = Modifiers.Public | Modifiers.Static,
                Operator = symbol,
                ReturnType = Type(returnType),
                Parameters = parameters,
                ShaderContract = Builtin(symbol, "matrix"),
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
                Body = "var xx = rotation.x * rotation.x;\nvar yy = rotation.y * rotation.y;\nvar zz = rotation.z * rotation.z;\nvar xy = rotation.x * rotation.y;\nvar xz = rotation.x * rotation.z;\nvar yz = rotation.y * rotation.z;\nvar wx = rotation.w * rotation.x;\nvar wy = rotation.w * rotation.y;\nvar wz = rotation.w * rotation.z;\nreturn new float4x4(1f - 2f * (yy + zz), 2f * (xy - wz), 2f * (xz + wy), 0f, 2f * (xy + wz), 1f - 2f * (xx + zz), 2f * (yz - wx), 0f, 2f * (xz - wy), 2f * (yz + wx), 1f - 2f * (xx + yy), 0f, 0f, 0f, 0f, 1f);",
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
                Body = "var transformed = matrix * new float4(point, 1f);\nreturn transformed.w == 0f ? transformed.xyz : transformed.xyz / transformed.w;",
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
                Body = "var zaxis = float3.NormalizeSafe(direction);\nvar xaxis = float3.NormalizeSafe(float3.Cross(up, zaxis));\nvar yaxis = float3.Cross(zaxis, xaxis);\nreturn new float4x4(xaxis.x, yaxis.x, zaxis.x, -float3.Dot(xaxis, eye), xaxis.y, yaxis.y, zaxis.y, -float3.Dot(yaxis, eye), xaxis.z, yaxis.z, zaxis.z, -float3.Dot(zaxis, eye), 0f, 0f, 0f, 1f);",
            });
            members.Add(new FunctionSpec
            {
                Name = "CreatePerspectiveFieldOfViewLeftHanded",
                ReturnType = Type("float4x4"),
                Parameters = [Param("fieldOfView", Type("float")), Param("aspectRatio", Type("float")), Param("nearPlaneDistance", Type("float")), Param("farPlaneDistance", Type("float"))],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "var yScale = 1f / DeltaMaths.Tan(fieldOfView * 0.5f);\nvar xScale = yScale / aspectRatio;\nvar range = farPlaneDistance / (farPlaneDistance - nearPlaneDistance);\nreturn new float4x4(xScale, 0f, 0f, 0f, 0f, yScale, 0f, 0f, 0f, 0f, range, -nearPlaneDistance * range, 0f, 0f, 1f, 0f);",
            });
            members.Add(new FunctionSpec
            {
                Name = "Decompose",
                ReturnType = Type("bool"),
                Parameters = [Param("value", Type("float4x4")), Param("scale", Type("float3"), ParameterModifier.Out), Param("rotation", Type("quaternion"), ParameterModifier.Out), Param("translation", Type("float3"), ParameterModifier.Out)],
                Part = TypePart.Geometry,
                Targets = FunctionTargets.Type | FunctionTargets.ShaderDeltaMaths,
                Body = "translation = new float3(value.M14, value.M24, value.M34);\nvar x = new float3(value.M11, value.M21, value.M31);\nvar y = new float3(value.M12, value.M22, value.M32);\nvar z = new float3(value.M13, value.M23, value.M33);\nscale = new float3(float3.Length(x), float3.Length(y), float3.Length(z));\nif (scale.x <= 1e-20f || scale.y <= 1e-20f || scale.z <= 1e-20f) { rotation = quaternion.identity; return false; }\nx /= scale.x;\ny /= scale.y;\nz /= scale.z;\nif (float3.Dot(float3.Cross(x, y), z) < 0f) { scale.x = -scale.x; x = -x; }\nvar rotationMatrix = new float4x4(x.x, y.x, z.x, 0f, x.y, y.y, z.y, 0f, x.z, y.z, z.z, 0f, 0f, 0f, 0f, 1f);\nrotation = quaternion.NormalizeSafe(quaternion.CreateFromRotationMatrix(rotationMatrix));\nreturn true;",
            });
        }

        private static string DeterminantBody(int size) => size switch
        {
            2 => "return value.M11 * value.M22 - value.M12 * value.M21;",
            3 => "return value.M11 * (value.M22 * value.M33 - value.M23 * value.M32) - value.M12 * (value.M21 * value.M33 - value.M23 * value.M31) + value.M13 * (value.M21 * value.M32 - value.M22 * value.M31);",
            4 => "static float minor(float a11, float a12, float a13, float a21, float a22, float a23, float a31, float a32, float a33) => a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);\nreturn value.M11 * minor(value.M22, value.M23, value.M24, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44) - value.M12 * minor(value.M21, value.M23, value.M24, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44) + value.M13 * minor(value.M21, value.M22, value.M24, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44) - value.M14 * minor(value.M21, value.M22, value.M23, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43);",
            _ => throw new ArgumentOutOfRangeException(nameof(size)),
        };

        private static string InverseBody(int size)
        {
            var name = MatrixName(size, size);
            return size switch
            {
                2 => $"var determinant = Determinant(value);\nif (DeltaMaths.Abs(determinant) <= 1e-8f) {{ result = default; return false; }}\nvar inverse = 1f / determinant;\nresult = new {name}(value.M22 * inverse, -value.M12 * inverse, -value.M21 * inverse, value.M11 * inverse);\nreturn true;",
                3 => $"var c11 = value.M22 * value.M33 - value.M23 * value.M32;\nvar c12 = value.M13 * value.M32 - value.M12 * value.M33;\nvar c13 = value.M12 * value.M23 - value.M13 * value.M22;\nvar c21 = value.M23 * value.M31 - value.M21 * value.M33;\nvar c22 = value.M11 * value.M33 - value.M13 * value.M31;\nvar c23 = value.M13 * value.M21 - value.M11 * value.M23;\nvar c31 = value.M21 * value.M32 - value.M22 * value.M31;\nvar c32 = value.M12 * value.M31 - value.M11 * value.M32;\nvar c33 = value.M11 * value.M22 - value.M12 * value.M21;\nvar determinant = value.M11 * c11 + value.M12 * c12 + value.M13 * c13;\nif (DeltaMaths.Abs(determinant) <= 1e-8f) {{ result = default; return false; }}\nvar inverse = 1f / determinant;\nresult = new {name}(c11 * inverse, c21 * inverse, c31 * inverse, c12 * inverse, c22 * inverse, c32 * inverse, c13 * inverse, c23 * inverse, c33 * inverse);\nreturn true;",
                4 => "var determinant = Determinant(value);\nif (DeltaMaths.Abs(determinant) <= 1e-8f) { result = default; return false; }\nvar inverse = 1f / determinant;\nstatic float minor(float a11, float a12, float a13, float a21, float a22, float a23, float a31, float a32, float a33) => a11 * (a22 * a33 - a23 * a32) - a12 * (a21 * a33 - a23 * a31) + a13 * (a21 * a32 - a22 * a31);\nresult = new float4x4(\n    minor(value.M22, value.M23, value.M24, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44) * inverse,\n    -minor(value.M12, value.M13, value.M14, value.M32, value.M33, value.M34, value.M42, value.M43, value.M44) * inverse,\n    minor(value.M12, value.M13, value.M14, value.M22, value.M23, value.M24, value.M42, value.M43, value.M44) * inverse,\n    -minor(value.M12, value.M13, value.M14, value.M22, value.M23, value.M24, value.M32, value.M33, value.M34) * inverse,\n    -minor(value.M21, value.M23, value.M24, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44) * inverse,\n    minor(value.M11, value.M13, value.M14, value.M31, value.M33, value.M34, value.M41, value.M43, value.M44) * inverse,\n    -minor(value.M11, value.M13, value.M14, value.M21, value.M23, value.M24, value.M41, value.M43, value.M44) * inverse,\n    minor(value.M11, value.M13, value.M14, value.M21, value.M23, value.M24, value.M31, value.M33, value.M34) * inverse,\n    minor(value.M21, value.M22, value.M24, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44) * inverse,\n    -minor(value.M11, value.M12, value.M14, value.M31, value.M32, value.M34, value.M41, value.M42, value.M44) * inverse,\n    minor(value.M11, value.M12, value.M14, value.M21, value.M22, value.M24, value.M41, value.M42, value.M44) * inverse,\n    -minor(value.M11, value.M12, value.M14, value.M21, value.M22, value.M24, value.M31, value.M32, value.M34) * inverse,\n    -minor(value.M21, value.M22, value.M23, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43) * inverse,\n    minor(value.M11, value.M12, value.M13, value.M31, value.M32, value.M33, value.M41, value.M42, value.M43) * inverse,\n    -minor(value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M41, value.M42, value.M43) * inverse,\n    minor(value.M11, value.M12, value.M13, value.M21, value.M22, value.M23, value.M31, value.M32, value.M33) * inverse);\nreturn true;",
                _ => throw new ArgumentOutOfRangeException(nameof(size)),
            };
        }

        private static string AssignColumns(int columns, bool includePadding)
        {
            var lines = new List<string>(columns * (includePadding ? 2 : 1));
            for (var column = 0; column < columns; column++)
            {
                lines.Add($"this.c{column} = c{column};");
                if (includePadding)
                {
                    lines.Add($"this._padding{column} = 0f;");
                }
            }

            return string.Join("\n", lines.Where(line => includePadding || !line.Contains("padding", StringComparison.Ordinal)));
        }

        private static string ScalarConstructorBody(int columns, int rows)
        {
            var lines = new List<string>(columns + 1);
            for (var column = 0; column < columns; column++)
            {
                var values = Enumerable.Range(0, rows).Select(row => row == column ? "value" : "0f");
                lines.Add($"c{column} = new {VectorName(rows)}({string.Join(", ", values)});");
                if (rows == 3)
                {
                    lines.Add($"_padding{column} = 0f;");
                }
            }

            return string.Join("\n", lines);
        }

        private static string MatrixConversionBody(int columns, int rows, int sourceColumns, int sourceRows)
        {
            var lines = new List<string>(columns + 1);
            for (var column = 0; column < columns; column++)
            {
                var values = Enumerable.Range(0, rows).Select(row =>
                    row < sourceRows && column < sourceColumns
                        ? $"value.c{column}.{Component(row)}"
                        : row == column ? "1f" : "0f");
                lines.Add($"c{column} = new {VectorName(rows)}({string.Join(", ", values)});");
                if (rows == 3)
                {
                    lines.Add($"_padding{column} = 0f;");
                }
            }

            return string.Join("\n", lines);
        }

        private static ParameterSpec[] RowMajorParameters(int columns, int rows) =>
            Enumerable.Range(0, rows)
                .SelectMany(row => Enumerable.Range(0, columns).Select(column => Param($"m{row + 1}{column + 1}", Type("float"))))
                .ToArray();

        private static string RowMajorConstructorBody(int columns, int rows)
        {
            var lines = new List<string>(columns + 1);
            for (var column = 0; column < columns; column++)
            {
                var values = Enumerable.Range(0, rows).Select(row => $"m{row + 1}{column + 1}");
                lines.Add($"c{column} = new {VectorName(rows)}({string.Join(", ", values)});");
                if (rows == 3)
                {
                    lines.Add($"_padding{column} = 0f;");
                }
            }

            return string.Join("\n", lines);
        }

        private static string SetElementBody(int columns, int rows) =>
            $"if ((uint)column >= {columns}u || (uint)row >= {rows}u) throw new ArgumentOutOfRangeException();\nvar columnValue = GetColumn(column);\ncolumnValue[row] = value;\nswitch (column) {{ " + string.Join(" ", Enumerable.Range(0, columns).Select(column => $"case {column}: c{column} = columnValue; break;")) + " }";

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

        private static ShaderContract Builtin(string name, string capability) => new()
        {
            GlslName = name,
            Mapping = ShaderMappingKind.Builtin,
            Capability = ParseCapability(capability),
            Stages = ShaderStages.All,
        };

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

        private static bool IsMatrix(string name) => name.StartsWith("float", StringComparison.Ordinal)
            && name.Contains('x', StringComparison.Ordinal);

        private static string MatrixName(int columns, int rows) => $"float{columns}x{rows}";

        private static string GlslName(int columns, int rows) => columns == rows ? $"mat{columns}" : $"mat{columns}x{rows}";

        private static string VectorName(int dimension) => $"float{dimension}";

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
