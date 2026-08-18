using System;
using System.Collections.Generic;
using System.Linq;

namespace DVG.MathsGen.Model.Rendering
{
    internal sealed record TypeFile(string Name, TypePart Part, string Source);

    internal sealed class TypeFileLayout
    {
        private static readonly TypePart[] Parts = Enum.GetValues<TypePart>();

        private readonly CSharpRenderer renderer;

        public TypeFileLayout(CSharpRenderer? renderer = null)
        {
            this.renderer = renderer ?? new CSharpRenderer();
        }

        public IReadOnlyList<TypeFile> Render(TypeSpec type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (string.IsNullOrWhiteSpace(type.Name))
                throw new ArgumentException("A type name is required to create its file layout.", nameof(type));

            var parts = Parts.Where(part => type.Members.Any(member => member.Part == part)).ToArray();
            var files = new TypeFile[parts.Length];
            for (var index = 0; index < parts.Length; index++)
            {
                var part = parts[index];
                files[index] = new TypeFile(
                    type.Name + Suffix(part) + ".cs",
                    part,
                    renderer.Render(type, part));
            }

            return files;
        }

        private static string Suffix(TypePart part) => part switch
        {
            TypePart.Core => "",
            TypePart.Operators => ".operators",
            TypePart.Common => ".common",
            TypePart.Geometry => ".geometry",
            TypePart.Trigonometry => ".trigonometry",
            TypePart.Exponential => ".exponential",
            TypePart.Relational => ".relational",
            TypePart.Swizzles => ".swizzles",
            _ => throw new ArgumentOutOfRangeException(nameof(part), part, null),
        };
    }
}
