using KibiHex.MathsGen.Types;
using KibiHex.MathsGen.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Members
{
    internal class Indexer : Member
    {
        /// <summary>
        /// Property type
        /// </summary>
        public AbstractType Type { get; set; }

        /// <summary>
        /// True if override property
        /// </summary>
        public bool Override { get; set; }

        /// <summary>
        /// Getter code
        /// </summary>
        public IReadOnlyList<string> Getter { get; set; }
        /// <summary>
        /// Setter code
        /// </summary>
        public IReadOnlyList<string> Setter { get; set; }

        /// <summary>
        /// Single-Line getter
        /// </summary>
        public string GetterLine { set { Getter = new[] { value }; } }
        /// <summary>
        /// Single-Line setter
        /// </summary>
        public string SetterLine { set { Setter = new[] { value }; } }

        /// <summary>
        /// Initial value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Indexer parameters
        /// </summary>
        public IReadOnlyList<string> Parameters { get; set; } = Array.Empty<string>();
        public string ParameterString { set { Parameters = new[] { value }; } }

        public override string MemberPrefix => base.MemberPrefix + (Override ? " override" : "");

        public Indexer(AbstractType type)
        {
            Type = type;
        }

        public override void Render(CodeWriter writer)
        {
            base.Render(writer);
            var getter = Getter ?? throw new NotSupportedException();

            if (Setter == null && getter.Count == 1)
            {
                writer.Line($"{MemberPrefix} {Type.Name} this[{Parameters.CommaSeparated()}] => {getter[0]};");
                return;
            }

            writer.Line($"{MemberPrefix} {Type.Name} this[{Parameters.CommaSeparated()}]");
            writer.Line("{");
            writer.Indent(() =>
            {
                writer.Line("get");
                writer.Line("{");
                writer.Indent(() =>
                {
                    foreach (var line in getter)
                        writer.Line(line);
                });
                writer.Line("}");

                if (Setter != null)
                {
                    writer.Line("set");
                    writer.Line("{");
                    writer.Indent(() =>
                    {
                        foreach (var line in Setter)
                            writer.Line(line);
                    });
                    writer.Line("}");
                }
            });
            writer.Line("}");
        }
    }
}

