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
    internal enum ApiSurface
    {
        None = 0,
        Type = 1,
        Maths = 2,
        ShaderMaths = 4,
        Extension = 8,

        Static = Maths | ShaderMaths,
        Vector = Type | ShaderMaths,
        All = Type | Maths | ShaderMaths | Extension,
    }

    internal sealed class ParameterSpec
    {
        public string Name { get; init; }
        public TypeRef Type { get; init; }
        public string Modifier { get; init; }
    }

    internal abstract class MemberSpec
    {
        public string Name { get; init; }
        public TypePart Part { get; init; } = TypePart.Core;
        public Modifiers Modifiers { get; init; } = Modifiers.Public;
        public string[] Attributes { get; init; } = Array.Empty<string>();
        public string Summary { get; init; }
    }

    internal sealed class FieldSpec : MemberSpec
    {
        public TypeRef Type { get; init; }
    }

    internal sealed class ConstructorSpec : MemberSpec
    {
        public ParameterSpec[] Parameters { get; init; } = Array.Empty<ParameterSpec>();
        public string Body { get; init; } = "";
    }

    internal class FunctionSpec : MemberSpec
    {
        public TypeRef ReturnType { get; init; }
        public ParameterSpec[] Parameters { get; init; } = Array.Empty<ParameterSpec>();
        public string Body { get; init; } = "";
        public string Expression { get; init; }
        public ApiSurface Api { get; init; } = ApiSurface.Type;
        public string ExtensionReceiver { get; init; }

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
        public TypeRef Type { get; init; }
        public string Expression { get; init; }
        public string Getter { get; init; }
        public string Setter { get; init; }
    }

    internal sealed class OperatorSpec : FunctionSpec
    {
        public string Operator { get; init; }
    }

    internal sealed class TypeSpec
    {
        public string Namespace { get; init; } = "KibiHex";
        public string Name { get; init; }
        public string Comment { get; init; }
        public string Kind { get; init; } = "struct";
        public Modifiers Modifiers { get; init; } = Modifiers.Public;
        public string[] Attributes { get; init; } = Array.Empty<string>();
        public string[] Interfaces { get; init; } = Array.Empty<string>();
        public MemberSpec[] Members { get; init; } = Array.Empty<MemberSpec>();
    }

    internal sealed record TypePart(string Name, string FileSuffix)
    {
        public static readonly TypePart Core = new("Core", "");
        public static readonly TypePart Operators = new("Operators", ".operators");
        public static readonly TypePart Swizzles = new("Swizzles", ".swizzles");
        public static readonly TypePart Geometry = new("Geometry", ".geometry");
    }
}
