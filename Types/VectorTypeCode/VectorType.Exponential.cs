using KibiHex.MathsGen.Members;
using System.Collections.Generic;

namespace KibiHex.MathsGen.Types
{
    internal partial class VectorType
    {
        private IEnumerable<Member> ExponentialFunctions()
        {
            if (BaseType == BuiltinType.TypeFloat)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Pow", this, "lhs", this, "rhs", $"Maths.Pow({{0}}, {{1}})");
                yield return new ComponentWiseStaticFunction(Fields, this, "Exp", this, "v", $"Maths.Exp({{0}})");
                yield return new ComponentWiseStaticFunction(Fields, this, "Log", this, "v", $"Maths.Log({{0}})");
                yield return new ComponentWiseStaticFunction(Fields, this, "Exp2", this, "v", $"Maths.Exp2({{0}})");
                yield return new ComponentWiseStaticFunction(Fields, this, "Log2", this, "v", $"Maths.Log2({{0}})");
            }
            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Sqrt", this, "v", $"Maths.Sqrt({{0}})");
                yield return new ComponentWiseStaticFunction(Fields, this, "InverseSqrt", this, "v", $"Maths.InverseSqrt({{0}})");
            }
        }
    }
}

