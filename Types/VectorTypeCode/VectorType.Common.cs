using DVG.GLSH.Generator.Members;
using System.Collections.Generic;
using System.Linq;

namespace DVG.GLSH.Generator.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// Refers to GLSL 450 specs.
        /// 8 Built-in Functions.
        /// 8.3 Common Functions.
        /// https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.50.pdf
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> CommonFunctions()
        {
            var boolVec = new VectorType(BuiltinType.TypeBool, Length);
            var intVec = new VectorType(BuiltinType.TypeInt, Length);
            var uintVec = new VectorType(BuiltinType.TypeUint, Length);
            var floatVec = new VectorType(BuiltinType.TypeFloat, Length);
            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeDouble)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Abs", this, "v", $"Maths.Abs({{0}})") { GlslName = "abs" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Sign", this, "v", $"Maths.Sign({{0}})") { GlslName = "sign" };
            }
            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Floor", this, "v", $"Maths.Floor({{0}})") { GlslName = "floor" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Truncate", this, "v", $"Maths.Truncate({{0}})") { GlslName = "trunc" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Round", this, "v", $"Maths.Round({{0}})") { GlslName = "round" };
                yield return new ComponentWiseStaticFunction(Fields, this, "RoundEven", this, "v", $"Maths.RoundEven({{0}})") { GlslName = "roundEven" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Ceiling", this, "v", $"Maths.Ceiling({{0}})") { GlslName = "ceil" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Fract", this, "v", $"{{0}} - Maths.Floor({{0}})") { GlslName = "fract" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Mod", this, "lhs", this, "rhs", $"{{0}} - {{1}} * Maths.Floor({{0}} / {{1}})") { CanScalar1 = true, GlslName = "mod" };
                //TODO Add Modf
                yield return new ComponentWiseStaticFunction(Fields, this, "Lerp", this, "edge0", this, "edge1", this, "v", $"Maths.Lerp({{0}}, {{1}}, {{2}})") { CanScalar2 = true, GlslName = "mix" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Step", this, "edge", this, "x", $"{{1}} < {{0}} ? 0 : 1") { CanScalar0 = true, GlslName = "step" };
                yield return new ComponentWiseStaticFunction(Fields, this, "SmoothStep", this, "edge0", this, "edge1", this, "x", $"Maths.SmoothStep({{0}}, {{1}}, {{2}})") { CanScalar2 = true, GlslName = "smoothstep" };
                yield return new ComponentWiseStaticFunction(Fields, boolVec, "IsNaN", this, "v", $"{BaseTypeName}.IsNaN({{0}})") { GlslName = "isnan" };
                yield return new ComponentWiseStaticFunction(Fields, boolVec, "IsInfinity", this, "v", $"{BaseTypeName}.IsInfinity({{0}})") { GlslName = "isinf" }; ;
                yield return new ComponentWiseStaticFunction(Fields, this, "Fma", this, "a", this, "b", this, "c", $"Maths.Fma({{0}}, {{1}}, {{2}})") { GlslName = "fma" };
            }
            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeUint)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Min", this, "lhs", this, "rhs", $"Maths.Min({{0}}, {{1}})") { CanScalar1 = true, GlslName = "min" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Max", this, "lhs", this, "rhs", $"Maths.Max({{0}}, {{1}})") { CanScalar1 = true, GlslName = "max" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Clamp", this, "v", this, "min", this, "max", $"Maths.Clamp({{0}}, {{1}}, {{2}})") { GlslName = "clamp" };
                yield return new Function(this, "Clamp")
                {
                    GlslName = "clamp",
                    Static = true,
                    Parameters = new string[] { $"{Name} v", $"{BaseType.Name} min", $"{BaseType.Name} max" },
                    Code = new string[] { $"{Construct(this, Fields.Select(f => $"Maths.Clamp(v.{f}, min, max)"))}" },
                    Comment = $"Returns a {Name} from component-wise application of Clamp (Maths.Clamp(v, min, max)).",
                };
            }

            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeUint || BaseType == BuiltinType.TypeBool)
            {
                // weird boolean mix
                yield return new ComponentWiseStaticFunction(Fields, this, "Mix", this, "x", this, "y", boolVec, "a", $"{{2}} ? {{1}} : {{0}}") { GlslName = "mix" };
            }
            if (BaseType == BuiltinType.TypeFloat)
            {
                yield return new ComponentWiseStaticFunction(Fields, intVec, "FloatBitsToInt", this, "v", $"Unsafe.As<{BaseTypeName}, {intVec.BaseTypeName}>(ref {{0}})") { GlslName = "floatBitsToInt" };
                yield return new ComponentWiseStaticFunction(Fields, uintVec, "FloatBitsToUInt", this, "v", $"Unsafe.As<{BaseTypeName}, {uintVec.BaseTypeName}>(ref {{0}})") { GlslName = "floatBitsToUint" };
            }
            if (BaseType == BuiltinType.TypeInt)
            {
                yield return new ComponentWiseStaticFunction(Fields, floatVec, "IntBitsToFloat", this, "v", $"Unsafe.As<{BaseTypeName}, {floatVec.BaseTypeName}>(ref {{0}})") { GlslName = "intBitsToFloat" };
            }
            if (BaseType == BuiltinType.TypeUint)
            {
                yield return new ComponentWiseStaticFunction(Fields, floatVec, "UIntBitsToFloat", this, "v", $"Unsafe.As<{BaseTypeName}, {floatVec.BaseTypeName}>(ref {{0}})") { GlslName = "uintBitsToFloat" };
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
