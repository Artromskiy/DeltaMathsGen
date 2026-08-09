namespace KibiHex.MathsGen.Model
{
    internal sealed class TypeRef
    {
        public string Name { get; init; }
        public bool IsSelf { get; init; }

        public override string ToString() => IsSelf ? "__SELF__" : Name;

        public static TypeRef Named(string name) => new() { Name = name };
        public static TypeRef Self => new() { IsSelf = true };
    }
}

