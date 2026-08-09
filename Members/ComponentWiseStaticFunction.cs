using KibiHex.MathsGen.CodeModel;
using KibiHex.MathsGen.Types;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Members
{
    internal class ComponentWiseStaticFunction : Member
    {
        public AbstractType ReturnType { get; set; }
        public bool CanScalar0 { get; set; }
        public bool CanScalar1 { get; set; }
        public bool CanScalar2 { get; set; }
        public string CompString { get; set; }
        public AbstractType[] ParameterTypes { get; set; }
        public string[] ParameterNames { get; set; }
        public string AdditionalComment { get; set; }
        public IReadOnlyList<string> Fields { get; set; }

        public ComponentWiseStaticFunction(IEnumerable<string> fields, AbstractType returnType, string name, AbstractType para0, string paraName0, string compString)
            : this(fields, returnType, name, new[] { para0 }, new[] { paraName0 }, compString) { }

        public ComponentWiseStaticFunction(IEnumerable<string> fields, AbstractType returnType, string name, AbstractType para0, string paraName0, AbstractType para1, string paraName1, string compString)
            : this(fields, returnType, name, new[] { para0, para1 }, new[] { paraName0, paraName1 }, compString) { }

        public ComponentWiseStaticFunction(IEnumerable<string> fields, AbstractType returnType, string name, AbstractType para0, string paraName0, AbstractType para1, string paraName1, AbstractType para2, string paraName2, string compString)
            : this(fields, returnType, name, new[] { para0, para1, para2 }, new[] { paraName0, paraName1, paraName2 }, compString) { }

        private ComponentWiseStaticFunction(IEnumerable<string> fields, AbstractType returnType, string name, AbstractType[] parameterTypes, string[] parameterNames, string compString)
        {
            Fields = fields.ToArray(); ReturnType = returnType; Name = name; Static = true;
            ParameterTypes = parameterTypes; ParameterNames = parameterNames; CompString = compString;
            Comment = "DUMMY";
        }

        private IEnumerable<string> Variants(int index)
        {
            if (index >= ParameterNames.Length) { yield return ""; yield break; }
            foreach (var tail in Variants(index + 1)) yield return "0" + tail;
            if ((index == 0 && CanScalar0) || (index == 1 && CanScalar1) || (index == 2 && CanScalar2))
                foreach (var tail in Variants(index + 1)) yield return "1" + tail;
        }

        public override void Render(CodeWriter writer)
        {
            foreach (var variant in Variants(0))
            {
                var scalar = variant.Select((flag, i) => flag == '1').ToArray();
                var types = ParameterTypes.Select((type, i) => scalar[i] ? type.BaseType ?? type : type).ToArray();
                var args = ParameterNames.Select((name, i) => scalar[i] ? name : $"{name}.{Fields.First()}").ToArray();
                var expression = string.Format(CompString, args.Cast<object>().ToArray());
                if (!scalar.All(x => x))
                    expression = Fields.Select(field => string.Format(CompString, ParameterNames.Select((name, i) => scalar[i] ? name : $"{name}.{field}").Cast<object>().ToArray())).CommaSeparated();
                base.Render(writer);
                writer.Line($"{MemberPrefix} {ReturnType.Name} {Name}({ParameterNames.Select((name, i) => types[i].Name + " " + name).CommaSeparated()}) => new {ReturnType.Name}({expression});".Trim());
            }
        }
    }
}

