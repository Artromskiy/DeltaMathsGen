using System;

namespace KibiHex.MathsGen.Model
{
    [Flags]
    internal enum Modifiers
    {
        None = 0,
        Public = 1,
        Static = 2,
        Partial = 4,
        Readonly = 8,
        Override = 16,
    }

    [Flags]
    internal enum FunctionTargets
    {
        None = 0,
        Type = 1,
        ShaderMaths = 2,
    }

    internal sealed class ParameterSpec
    {
        public required string Name { get; init; }
        public required TypeRef Type { get; init; }
        public string? Modifier { get; init; }
    }

    internal abstract class MemberSpec
    {
        public string Name { get; init; } = "";
        public TypePart Part { get; init; } = TypePart.Core;
        public Modifiers Modifiers { get; init; } = Modifiers.Public;
        public string[] Attributes { get; init; } = Array.Empty<string>();
        public string? Summary { get; init; }
    }

    internal sealed class FieldSpec : MemberSpec
    {
        public required TypeRef Type { get; init; }
        public string? Initializer { get; init; }
    }

    internal sealed class ConstructorSpec : MemberSpec
    {
        public ParameterSpec[] Parameters { get; init; } = Array.Empty<ParameterSpec>();
        public string Body { get; init; } = "";
    }

    internal class FunctionSpec : MemberSpec
    {
        public required TypeRef ReturnType { get; init; }
        public ParameterSpec[] Parameters { get; init; } = Array.Empty<ParameterSpec>();
        public string Body { get; init; } = "";
        public string? Expression { get; init; }
        public FunctionTargets Targets { get; init; } = FunctionTargets.Type;

        public string MathsName => LowercaseFirst(Name);

        private static string LowercaseFirst(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            return char.ToLowerInvariant(name[0]) + name[1..];
        }
    }

    internal sealed class PropertySpec : MemberSpec
    {
        public required TypeRef Type { get; init; }
        public string? Expression { get; init; }
        public string? Getter { get; init; }
        public string? Setter { get; init; }
    }

    internal sealed class OperatorSpec : FunctionSpec
    {
        public required string Operator { get; init; }
    }

    internal sealed class TypeSpec
    {
        public string Namespace { get; init; } = "KibiHex";
        public required string Name { get; init; }
        public string? Comment { get; init; }
        public string Kind { get; init; } = "struct";
        public Modifiers Modifiers { get; init; } = Modifiers.Public;
        public string[] Attributes { get; init; } = Array.Empty<string>();
        public string[] Interfaces { get; init; } = Array.Empty<string>();
        public MemberSpec[] Members { get; init; } = Array.Empty<MemberSpec>();
    }

    internal enum TypePart
    {
        Core,
        Operators,
        Common,
        Geometry,
        Trigonometry,
        Exponential,
        Relational,
        Swizzles,
    }
}
