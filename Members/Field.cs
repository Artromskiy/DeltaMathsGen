using DVG.GLSH.Generator.Types;
using System.Collections.Generic;

namespace DVG.GLSH.Generator.Members
{
    internal class Field : Member
    {
        /// <summary>
        /// Member type
        /// </summary>
        public AbstractType Type { get; set; }

        /// <summary>
        /// True iff field is readonly
        /// </summary>
        public bool Readonly { get; set; }

        /// <summary>
        /// True iff field is constant
        /// </summary>
        public bool Constant { get; set; }

        public override string MemberPrefix => base.MemberPrefix + (Constant ? " const" : Readonly ? " readonly" : "");

        /// <summary>
        /// Value that will be used as initializer
        /// </summary>
        public string DefaultValue { get; set; }

        private string GetDefaultValue => string.IsNullOrEmpty(DefaultValue) ? string.Empty : " = " + DefaultValue;

        public override IEnumerable<string> Lines
        {
            get
            {
                foreach (var line in base.Lines)
                    yield return line;

                yield return $"{MemberPrefix} {Type.Name} {Name}{GetDefaultValue};";
            }
        }

        public Field(string name, AbstractType type)
        {
            Name = name;
            Type = type;
            //Attributes = new[] { "DataMember" };
        }
    }
}
