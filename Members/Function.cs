using KibiHex.MathsGen.Types;
using KibiHex.MathsGen.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Members
{
    internal class Function : Member
    {
        /// <summary>
        /// Return types
        /// </summary>
        public AbstractType ReturnType { get; set; }

        /// <summary>
        /// Parameters
        /// </summary>
        public IReadOnlyList<string> Parameters { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Parameters as a string
        /// </summary>
        public string ParameterString { set { Parameters = new[] { value }; } }

        /// <summary>
        /// Lines of code
        /// </summary>
        public IReadOnlyList<string> Code { get; set; }

        /// <summary>
        /// Code as a string
        /// </summary>
        public string CodeString { set { Code = new[] { value }; } }

        /// <summary>
        /// True if override property
        /// </summary>
        public bool Override { get; set; }
        public bool Readonly { get; set; }
        public virtual string ReturnName => ReturnType.Name;
        public virtual string FunctionName => Name;
        public override string MemberPrefix => base.MemberPrefix + (Override ? " override" : "") + (Readonly ? " readonly" : "");

        public Function(AbstractType returnType, string name)
        {
            ReturnType = returnType;
            Name = name;
        }

        public List<Member> LegacyMembers()
        {
            var result = new List<Member>();
            if (Visibility != "public")
                return result;
            if (this is ExplicitOperator)
                return result;
            if (this is ImplicitOperator)
                return result;
            if (this is Operator)
                return result;

            if (Static)
            {
                var paras = Parameters.ParasRecovered().ToArray();
                if (paras.Length == 0)
                    throw new NotSupportedException();

                var ptype = paras[0].Split(' ')[0];
                if (ptype == OriginalType.Name)
                {
                    result.Add(new Function(ReturnType, Name)
                    {
                        Static = true,
                        Parameters = Parameters,
                        Comment = Comment,
                        CodeString = $"{OriginalType.Name}.{Name}({Parameters.ArgNames().CommaSeparated()})"
                    });
                }

                return result; // nothing for static props
            }

            var varname = OriginalType is VectorType ? "v" : "m";

            result.Add(new Function(ReturnType, Name)
            {
                Static = true,
                Comment = Comment,
                Parameters = OriginalType.TypedArgs(varname).Concat(Parameters).ToArray(),
                CodeString = $"{varname}.{Name}({Parameters.ArgNames().CommaSeparated()})"
            });
            return result;
        }

        public override void Render(CodeWriter writer)
        {
            base.Render(writer);
            var code = Code ?? Array.Empty<string>();

            if (code.Count == 1)
            {
                writer.Line($"{MemberPrefix} {ReturnName} {FunctionName}({Parameters.CommaSeparated()}) => {code[0]};".Trim());
                return;
            }

            writer.Line($"{MemberPrefix} {ReturnName} {FunctionName}({Parameters.CommaSeparated()})".Trim());
            writer.Line("{");
            writer.Indent(() =>
            {
                foreach (var line in code)
                    writer.Line(line);
            });
            writer.Line("}");
        }
    }

}

