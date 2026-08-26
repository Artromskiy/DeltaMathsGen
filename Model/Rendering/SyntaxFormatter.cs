using System.Linq;

namespace DeltaMathsGen.Model.Rendering
{
    internal static class SyntaxFormatter
    {
        public static string Parameters(ParameterSpec[] parameters) =>
            string.Join(", ", parameters.Select(parameter =>
                $"{ShaderMetadata.ModifierToken(parameter.Modifier)} {parameter.Type} {parameter.Name}".Trim()));

        public static string Modifiers(Modifiers modifiers)
        {
            var result = modifiers.HasFlag(global::DeltaMathsGen.Model.Modifiers.Public) ? "public" : "";
            if (modifiers.HasFlag(global::DeltaMathsGen.Model.Modifiers.Static))
            {
                result += " static";
            }

            if (modifiers.HasFlag(global::DeltaMathsGen.Model.Modifiers.Partial))
            {
                result += " partial";
            }

            if (modifiers.HasFlag(global::DeltaMathsGen.Model.Modifiers.Readonly))
            {
                result += " readonly";
            }

            if (modifiers.HasFlag(global::DeltaMathsGen.Model.Modifiers.Override))
            {
                result += " override";
            }

            return result.Trim();
        }
    }
}
