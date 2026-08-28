namespace Delta.MathsGen.Model
{
    internal sealed class IndexerSpec : MemberSpec
    {
        public required TypeRef Type { get; init; }
        public required ParameterSpec Parameter { get; init; }
        public required string Getter { get; init; }
        public required string Setter { get; init; }
    }
}
