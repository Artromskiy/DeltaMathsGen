using KibiHex.MathsGen.Members;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// 8 Built-in Functions.
        /// 8.3 Common Functions.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> CommonFunctions()
        {
            var boolVec = new VectorType(BuiltinType.TypeBool, Length);
            var intVec = new VectorType(BuiltinType.TypeInt, Length);
            var uintVec = new VectorType(BuiltinType.TypeUint, Length);
            var floatVec = new VectorType(BuiltinType.TypeFloat, Length);
            var fixVec = new VectorType(BuiltinType.TypeFix, Length);
            if (BaseType == BuiltinType.TypeFloat ||
                BaseType == BuiltinType.TypeInt ||
                BaseType == BuiltinType.TypeDouble ||
                BaseType == BuiltinType.TypeFix)
            {
            }
            if (BaseType == BuiltinType.TypeFloat ||
                BaseType == BuiltinType.TypeDouble ||
                BaseType == BuiltinType.TypeFix)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Abs", this, "v", "Maths.Abs({0})");
                yield return new ComponentWiseStaticFunction(Fields, this, "Sign", this, "v", "Maths.Sign({0})");
                yield return new ComponentWiseStaticFunction(Fields, this, "Lerp", this, "edge0", this, "edge1", this, "v", "Maths.Lerp({0}, {1}, {2})")
                {
                    CanScalar2 = true,
                };
                yield return new ComponentWiseStaticFunction(Fields, this, "Min", this, "lhs", this, "rhs", "Maths.Min({0}, {1})")
                {
                    CanScalar1 = true,
                };
                yield return new ComponentWiseStaticFunction(Fields, this, "Max", this, "lhs", this, "rhs", "Maths.Max({0}, {1})")
                {
                    CanScalar1 = true,
                };
                if (BaseType != BuiltinType.TypeFix)
                {
                }
                //TODO Add Modf

                if (BaseType != BuiltinType.TypeFix)
                {
                }
            }
            if (BaseType == BuiltinType.TypeFloat ||
                BaseType == BuiltinType.TypeDouble ||
                BaseType == BuiltinType.TypeInt ||
                BaseType == BuiltinType.TypeUint ||
                BaseType == BuiltinType.TypeFix)
            {
                yield return new Function(this, "Clamp")
                {
                    Static = true,
                    Parameters = new string[] { $"{Name} v", $"{BaseType.Name} min", $"{BaseType.Name} max" },
                    Code = new string[] { $"{Construct(this, Fields.Select(f => $"Maths.Clamp(v.{f}, min, max)"))}" },
                    Comment = $"Returns a {Name} from component-wise application of Clamp (Maths.Clamp(v, min, max)).",
                };
            }

            if (BaseType == BuiltinType.TypeFloat ||
                BaseType == BuiltinType.TypeDouble ||
                BaseType == BuiltinType.TypeInt ||
                BaseType == BuiltinType.TypeUint ||
                BaseType == BuiltinType.TypeBool ||
                BaseType == BuiltinType.TypeFix)
            {
                // weird boolean mix
            }
            if (BaseType == BuiltinType.TypeFloat)
            {
            }
            if (BaseType == BuiltinType.TypeInt)
            {
            }
            if (BaseType == BuiltinType.TypeUint)
            {
            }
            // TODO
            // frexp
            // ldexp




            // frexp
            // exp = MathF.ILogB(x);
            // x = significand * 2 ^ exp;
            // significand = x / (2 ^ exp)

            // ldexp  x = MathF.ScaleB(significand, exp)
        }
    }
}

