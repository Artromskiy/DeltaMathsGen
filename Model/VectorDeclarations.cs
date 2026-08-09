using static KibiHex.MathsGen.Model.DeclarationHelpers;

namespace KibiHex.MathsGen.Model
{
    internal static class VectorDeclarations
    {
        public static TypeSpec Float2() => new()
        {
            Name = "float2",
            Comment = "A two-component vector of single-precision floating-point values.",
            Modifiers = Modifiers.Public | Modifiers.Partial,
            Attributes = ["Serializable", "StructLayout(LayoutKind.Sequential)"],
            Interfaces = ["IEquatable<float2>"],
            Members =
            [
                new FieldSpec { Name = "x", Type = Type("float") },
                new FieldSpec { Name = "y", Type = Type("float") },
                new ConstructorSpec
                {
                    Parameters =
                    [
                        Param("x", Type("float")),
                        Param("y", Type("float")),
                    ],
                    Body =
                    """
                    this.x = x;
                    this.y = y;
                    """,
                },
                new FunctionSpec
                {
                    Name = "Dot",
                    ReturnType = Type("float"),
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Parameters =
                    [
                        Param("left", Type("float2")),
                        Param("right", Type("float2")),
                    ],
                    Body =
                    """
                    return left.x * right.x
                         + left.y * right.y;
                    """,
                },
            ],
        };
    }
}

