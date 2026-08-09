namespace KibiHex.MathsGen.Model
{
    internal sealed class IndexerSpec : MemberSpec
    {
        public TypeRef Type { get; init; }
        public ParameterSpec Parameter { get; init; }
        public string Getter { get; init; }
        public string Setter { get; init; }
    }
}
