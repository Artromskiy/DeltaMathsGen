namespace Delta.MathsGen.Model
{
    internal static class DeclarationHelpers
    {
        public static TypeRef Type(string name) => TypeRef.Named(name);
        public static ParameterSpec Param(string name, TypeRef type, ParameterModifier modifier = ParameterModifier.None) => new()
        {
            Name = name,
            Type = type,
            Modifier = modifier,
        };
    }
}
