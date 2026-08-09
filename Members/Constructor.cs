using KibiHex.MathsGen.Types;
using KibiHex.MathsGen.CodeModel;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Members
{
    internal class Constructor : Member
    {
        /// <summary>
        /// Constructor type
        /// </summary>
        public AbstractType Type { get; set; }

        /// <summary>
        /// ctor parameters
        /// </summary>
        public IReadOnlyList<string> Parameters { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Single parameter
        /// </summary>
        public string ParameterString { set { Parameters = new[] { value }; } }

        /// <summary>
        /// Constructor chain
        /// </summary>
        public string ConstructorChain { get; set; }

        /// <summary>
        /// Fields to initialize
        /// </summary>
        public IReadOnlyList<string> Fields { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Initializer expressions
        /// </summary>
        public IReadOnlyList<string> Initializers { get; set; } = System.Array.Empty<string>();

        public IReadOnlyList<string> Code { get; set; }

        public override void Render(CodeWriter writer)
        {
            base.Render(writer);
            writer.Line($"{MemberPrefix} {Type.Name}({Parameters.CommaSeparated()})");
            if (!string.IsNullOrEmpty(ConstructorChain))
                writer.Line((": " + ConstructorChain).Indent());
            writer.Line("{");
            writer.Indent(() =>
            {
                if (Code != null)
                    foreach (var code in Code)
                        writer.Line(code);
                if (string.IsNullOrEmpty(ConstructorChain) && Code == null)
                {
                    var it = Initializers.GetEnumerator();
                    foreach (var c in Fields)
                        writer.Line($"this.{c} = {(it.MoveNext() ? it.Current : Type.ZeroValue)};");
                }
            });
            writer.Line("}");
        }

        public Constructor(AbstractType type, IEnumerable<string> fields)
        {
            Fields = fields.ToArray();
            Type = type;
        }
    }
}

