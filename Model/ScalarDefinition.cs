using System;
using System.Linq;

namespace KibiHex.MathsGen.Model
{
    [Flags]
    internal enum ScalarCapabilities
    {
        None = 0,
        Boolean = 1 << 0,
        Arithmetic = 1 << 1,
        Signed = 1 << 2,
        Ordered = 1 << 3,
        Remainder = 1 << 4,
        Bitwise = 1 << 5,
        Shift = 1 << 6,
        Real = 1 << 7,
        FloatingPoint = 1 << 8,
        FixedPoint = 1 << 9,
        Rounding = 1 << 10,
        Trigonometry = 1 << 11,
        Hyperbolic = 1 << 12,
        Exponential = 1 << 13,
        UnaryPlus = 1 << 14,
        Increment = 1 << 15,
    }

    internal sealed class ScalarDefinition
    {
        public required string Name { get; init; }
        public required string ZeroLiteral { get; init; }
        public required ScalarCapabilities Capabilities { get; init; }
        public bool ParseAcceptsFormatProvider { get; init; } = true;
        public string NormalizeSafeThreshold { get; init; } = "0";
        public string[] ImplicitTargets { get; init; } = [];
        public string[] ExplicitTargets { get; init; } = [];

        public bool Supports(ScalarCapabilities required) =>
            (Capabilities & required) == required;
    }

    internal static class ScalarTypes
    {
        public static readonly ScalarDefinition[] All =
        [
            new()
            {
                Name = "bool",
                ZeroLiteral = "false",
                Capabilities = ScalarCapabilities.Boolean,
                ParseAcceptsFormatProvider = false,
            },
            new()
            {
                Name = "int",
                ZeroLiteral = "0",
                Capabilities = ScalarCapabilities.Arithmetic | ScalarCapabilities.Signed |
                    ScalarCapabilities.Ordered | ScalarCapabilities.Remainder |
                    ScalarCapabilities.Bitwise | ScalarCapabilities.Shift |
                    ScalarCapabilities.UnaryPlus | ScalarCapabilities.Increment,
                ImplicitTargets = ["float", "double", "fix"],
                ExplicitTargets = ["uint"],
            },
            new()
            {
                Name = "uint",
                ZeroLiteral = "0u",
                Capabilities = ScalarCapabilities.Arithmetic | ScalarCapabilities.Ordered |
                    ScalarCapabilities.Remainder | ScalarCapabilities.Bitwise | ScalarCapabilities.Shift |
                    ScalarCapabilities.UnaryPlus | ScalarCapabilities.Increment,
                ImplicitTargets = ["float", "double"],
                ExplicitTargets = ["int"],
            },
            new()
            {
                Name = "float",
                ZeroLiteral = "0f",
                Capabilities = ScalarCapabilities.Arithmetic | ScalarCapabilities.Signed |
                    ScalarCapabilities.Ordered | ScalarCapabilities.Remainder |
                    ScalarCapabilities.Real | ScalarCapabilities.FloatingPoint |
                    ScalarCapabilities.Rounding | ScalarCapabilities.Trigonometry |
                    ScalarCapabilities.Hyperbolic | ScalarCapabilities.Exponential |
                    ScalarCapabilities.UnaryPlus | ScalarCapabilities.Increment,
                NormalizeSafeThreshold = "1.17549435E-38f",
                ImplicitTargets = ["double"],
                ExplicitTargets = ["int", "uint", "fix"],
            },
            new()
            {
                Name = "double",
                ZeroLiteral = "0.0",
                Capabilities = ScalarCapabilities.Arithmetic | ScalarCapabilities.Signed |
                    ScalarCapabilities.Ordered | ScalarCapabilities.Remainder |
                    ScalarCapabilities.Real | ScalarCapabilities.FloatingPoint |
                    ScalarCapabilities.Rounding | ScalarCapabilities.Trigonometry |
                    ScalarCapabilities.Hyperbolic | ScalarCapabilities.Exponential |
                    ScalarCapabilities.UnaryPlus | ScalarCapabilities.Increment,
                NormalizeSafeThreshold = "2.2250738585072014E-308",
                ExplicitTargets = ["int", "uint", "float", "fix"],
            },
            new()
            {
                Name = "fix",
                ZeroLiteral = "0",
                Capabilities = ScalarCapabilities.Arithmetic | ScalarCapabilities.Signed |
                    ScalarCapabilities.Ordered | ScalarCapabilities.Remainder | ScalarCapabilities.Shift |
                    ScalarCapabilities.Real | ScalarCapabilities.FixedPoint | ScalarCapabilities.Rounding |
                    ScalarCapabilities.Trigonometry | ScalarCapabilities.Increment,
                ExplicitTargets = ["int", "float", "double"],
            },
        ];
    }

    internal sealed class VectorContext
    {
        public required ScalarDefinition Scalar { get; init; }
        public required int Dimension { get; init; }

        public string Name => Scalar.Name + Dimension;
        public string Components => "xyzw"[..Dimension];
        public string[] Fields => Components.Select(component => component.ToString()).ToArray();
        public string BoolVectorName => "bool" + Dimension;
    }
}
