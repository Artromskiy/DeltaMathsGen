using GLSHGenerator.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GLSHGenerator.Members
{
    internal class Property : Member
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
        public IEnumerable<string> Getter { get; set; }
        /// <summary>
        /// Setter code
        /// </summary>
        public IEnumerable<string>? Setter { get; set; }

        /// <summary>
        /// Single-Line getter
        /// </summary>
        public string GetterLine { set => Getter = new[] { value }; }
        /// <summary>
        /// Single-Line setter
        /// </summary>
        public string SetterLine { set => Setter = new[] { value }; }

        /// <summary>
        /// Initial value
        /// </summary>
        public string Value { get; set; }

        public override string MemberPrefix => base.MemberPrefix + (Override ? " override" : "");

        public Property(string name, AbstractType type)
        {
            Name = name;
            Type = type;
        }

        public override IEnumerable<Member> GlshMembers()
        {
            if (DisableGlmGen)
                yield break;
            if (Static)
                yield break; // nothing for static props
            if (Setter != null)
                yield break; // nothing for stuff with setters

            var varname = OriginalType is VectorType ? "v" : "m";
            yield return new Function(Type, Name)
            {
                Static = true,
                Comment = Comment,
                ParameterString = $"{OriginalType.Name} {varname}",
                CodeString = $"{varname}.{Name}"
            };
        }

        public override IEnumerable<string> Lines
        {
            get
            {
                foreach (var line in base.Lines)
                    yield return line;

                if (!string.IsNullOrEmpty(Value))
                {
                    yield return $"{MemberPrefix} readonly {Type.Name} {Name} {{ get; }} = {Value};";
                    yield break;
                }

                var getter = Getter?.ToArray();
                var setter = Setter?.ToArray();

                if (getter == null)
                    throw new NotSupportedException();

                if (getter.Length == 1 && setter == null)
                {
                    yield return $"{MemberPrefix} readonly {Type.Name} {Name} => {getter[0]};";
                    yield break;
                }
                yield return $"{MemberPrefix} {Type.Name} {Name}";
                yield return "{";
                if (getter.Length == 1)
                {
                    yield return $"get => {getter[0]};".Indent();
                }
                else
                {
                    yield return "get".Indent();
                    yield return "{".Indent();
                    foreach (var line in getter)
                        yield return line.Indent(2);
                    yield return "}".Indent();
                }
                if (setter != null)
                {
                    yield return "set".Indent();
                    yield return "{".Indent();
                    foreach (var line in setter)
                        yield return line.Indent(2);
                    yield return "}".Indent();
                    yield return "}";
                }
            }
        }
    }
}
