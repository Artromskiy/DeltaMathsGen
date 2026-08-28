using Delta.MathsGen.CodeModel;

namespace Delta.MathsGen.Model.Rendering
{
    internal static class TypeRenderer
    {
        public static void Render(CodeWriter writer, TypeSpec type, TypePart part)
        {
            if (part == TypePart.Core && !string.IsNullOrWhiteSpace(type.Comment))
            {
                writer.Line($"/// <summary>{type.Comment}</summary>");
            }

            if (part == TypePart.Core)
            {
                foreach (var attribute in type.Attributes)
                {
                    writer.Line($"[{attribute}]");
                }
            }

            var interfaces = part != TypePart.Core || type.Interfaces.Length == 0 ? "" : " : " + string.Join(", ", type.Interfaces);
            writer.Block($"{SyntaxFormatter.Modifiers(type.Modifiers)} {type.Kind} {type.Name}{interfaces}".Trim(), () =>
            {
                foreach (var member in type.Members)
                {
                    if (member.Part == part &&
                        (member is not FunctionSpec function || function.Targets.HasFlag(FunctionTargets.Type)))
                    {
                        MemberRenderer.Render(writer, member, type.Name);
                    }
                }
            });
        }
    }
}
