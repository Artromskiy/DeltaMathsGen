using KibiHex.MathsGen.CodeModel;

namespace KibiHex.MathsGen.Model.Rendering
{
    internal sealed class TypeRenderer
    {
        private readonly MemberRenderer members = new();

        public void Render(CodeWriter writer, TypeSpec type, TypePart part)
        {
            if (!string.IsNullOrWhiteSpace(type.Comment))
                writer.Line($"/// <summary>{type.Comment}</summary>");
            foreach (var attribute in type.Attributes)
                writer.Line($"[{attribute}]");

            var interfaces = type.Interfaces.Length == 0 ? "" : " : " + string.Join(", ", type.Interfaces);
            writer.Block($"{SyntaxFormatter.Modifiers(type.Modifiers)} {type.Kind} {type.Name}{interfaces}".Trim(), () =>
            {
                foreach (var member in type.Members)
                    if (member.Part == part)
                        members.Render(writer, member, type.Name);
            });
        }
    }
}

