using KibiHex.MathsGen.Types;
using KibiHex.MathsGen.CodeModel;
using KibiHex.MathsGen.Model;
using System;
using System.Collections.Generic;

namespace KibiHex.MathsGen.Members
{
    internal abstract class Member
    {
        public TypePart Part { get; set; } = TypePart.Core;
        /// <summary>
        /// Original type ref
        /// </summary>
        public AbstractType OriginalType { get; set; }

        /// <summary>
        /// Name of the member
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Comment of the member
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Visibility modifier
        /// </summary>
        public string Visibility { get; set; } = "public";

        /// <summary>
        /// True iff member is static
        /// </summary>
        public bool Static { get; set; }

        /// <summary>
        /// True if member is extension
        /// </summary>
        public bool Extension { get; set; }

        /// <summary>
        /// Attributes of this member
        /// </summary>
        public string[] Attributes = new string[] { };

        /// <summary>
        /// Renders this member into the current source writer.
        /// </summary>
        public virtual void Render(CodeWriter writer)
        {
            foreach (var line in Comment.AsComment())
                writer.Line(line);
            foreach (var attribute in Attributes)
                writer.Line($"[{attribute}]");
        }

        // Compatibility adapter for the current type renderer. New code should call Render directly.
        public IReadOnlyList<string> Lines
        {
            get
            {
                var writer = new CodeWriter();
                Render(writer);
                var text = writer.ToString().TrimEnd('\r', '\n');
                return text.Length == 0
                    ? Array.Empty<string>()
                    : text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            }
        }

        /// <summary>
        /// Prefix for members (visibility, static)
        /// </summary>
        public virtual string MemberPrefix => Visibility + (Static ? " static" : "");

    }
}

