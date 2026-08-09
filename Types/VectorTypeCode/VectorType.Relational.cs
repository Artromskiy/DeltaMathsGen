using KibiHex.MathsGen.Members;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// 8 Built-in Functions
        /// 8.7 Vector Relational Functions
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> RelationalFunctions()
        {
            var boolVType = new VectorType(BuiltinType.TypeBool, Length);

            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeUint)
            {
            }

            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeUint || BaseType == BuiltinType.TypeBool)
            {
            }

            if (BaseType == BuiltinType.TypeBool)
            {
                yield return new Function(BuiltinType.TypeBool, "Any")
                {
                    Static = true,
                    ParameterString = $"{Name} v",
                    Code = new string[] { $"{string.Join("||", Fields.Select(s => $"v.{s}"))}" }
                };
                yield return new Function(BuiltinType.TypeBool, "All")
                {
                    Static = true,
                    ParameterString = $"{Name} v",
                    Code = new string[] { $"{string.Join("&&", Fields.Select(s => $"v.{s}"))}" }
                };
            }
        }
    }
}


