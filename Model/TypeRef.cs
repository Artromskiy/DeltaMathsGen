namespace DVG.MathsGen.Model
{
    internal sealed class TypeRef
    {
        public required string Name { get; init; }

        public override string ToString() => Name;

        public static TypeRef Named(string name) => new() { Name = name };
    }
}
