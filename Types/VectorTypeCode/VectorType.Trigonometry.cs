using KibiHex.MathsGen.Members;
using System.Collections.Generic;

namespace KibiHex.MathsGen.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// 8 Built-in Functions.
        /// 8.1 Angle and Trigonometry Functions.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> TrigonometryFunctions()
        {
            if (BaseType != BuiltinType.TypeFloat)
                yield break;

        }
    }
}


