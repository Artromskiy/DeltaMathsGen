using DVG.GLSH.Generator.Members;
using System.Collections.Generic;

namespace DVG.GLSH.Generator.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// Refers to GLSL 450 specs.
        /// 8 Built-in Functions
        /// 8.2 Exponential Functions
        /// https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.50.pdf
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> ExponentialFunctions()
        {
            if (BaseType == BuiltinType.TypeFloat)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Pow", this, "lhs", this, "rhs", $"Maths.Pow({{0}}, {{1}})") { GlslName = "pow" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Exp", this, "v", $"Maths.Exp({{0}})") { GlslName = "exp" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Log", this, "v", $"Maths.Log({{0}})") { GlslName = "log" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Exp2", this, "v", $"Maths.Exp2({{0}})") { GlslName = "exp2" };
                yield return new ComponentWiseStaticFunction(Fields, this, "Log2", this, "v", $"Maths.Log2({{0}})") { GlslName = "log2" };
            }
            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble)
            {
                yield return new ComponentWiseStaticFunction(Fields, this, "Sqrt", this, "v", $"Maths.Sqrt({{0}})") { GlslName = "sqrt" };
                yield return new ComponentWiseStaticFunction(Fields, this, "InverseSqrt", this, "v", $"Maths.InverseSqrt({{0}})") { GlslName = "inversesqrt" };
            }
        }
    }
}
