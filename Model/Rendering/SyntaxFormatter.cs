using System.Linq;

namespace KibiHex.MathsGen.Model.Rendering
{
    internal static class SyntaxFormatter
    {
        public static string Parameters(ParameterSpec[] parameters) =>
            string.Join(", ", parameters.Select(parameter =>
                $"{parameter.Modifier} {parameter.Type} {parameter.Name}".Trim()));

        public static string Modifiers(Modifiers modifiers)
        {
            var result = modifiers.HasFlag(global::KibiHex.MathsGen.Model.Modifiers.Public) ? "public" : "";
            if (modifiers.HasFlag(global::KibiHex.MathsGen.Model.Modifiers.Static)) result += " static";
            if (modifiers.HasFlag(global::KibiHex.MathsGen.Model.Modifiers.Partial)) result += " partial";
            if (modifiers.HasFlag(global::KibiHex.MathsGen.Model.Modifiers.Readonly)) result += " readonly";
            if (modifiers.HasFlag(global::KibiHex.MathsGen.Model.Modifiers.Override)) result += " override";
            return result.Trim();
        }
    }
}

