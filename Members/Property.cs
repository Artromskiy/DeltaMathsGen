using KibiHex.MathsGen.CodeModel;
using KibiHex.MathsGen.Types;
using System;
using System.Collections.Generic;

namespace KibiHex.MathsGen.Members
{
    internal class Property : Member
    {
        public AbstractType Type { get; set; }
        public bool Override { get; set; }
        public IReadOnlyList<string> Getter { get; set; }
        public IReadOnlyList<string> Setter { get; set; }
        public string GetterLine { set => Getter = new[] { value }; }
        public string SetterLine { set => Setter = new[] { value }; }
        public string Value { get; set; }
        public override string MemberPrefix => base.MemberPrefix + (Override ? " override" : "");

        public Property(string name, AbstractType type)
        {
            Name = name;
            Type = type;
        }

        public override void Render(CodeWriter writer)
        {
            base.Render(writer);
            if (!string.IsNullOrEmpty(Value))
            {
                writer.Line($"{MemberPrefix} readonly {Type.Name} {Name} {{ get; }} = {Value};");
                return;
            }

            var getter = Getter ?? throw new NotSupportedException();
            writer.Line($"{MemberPrefix} {Type.Name} {Name}");
            writer.Line("{");
            writer.Indent(() =>
            {
                if (getter.Count == 1)
                    writer.Line($"get => {getter[0]};");
                else
                {
                    writer.Line("get");
                    writer.Line("{");
                    writer.Indent(() => { foreach (var line in getter) writer.Line(line); });
                    writer.Line("}");
                }

                if (Setter != null)
                {
                    writer.Line("set");
                    writer.Line("{");
                    writer.Indent(() => { foreach (var line in Setter) writer.Line(line); });
                    writer.Line("}");
                }
            });
            writer.Line("}");
        }
    }
}

