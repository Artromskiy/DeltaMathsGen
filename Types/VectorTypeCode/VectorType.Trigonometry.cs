using GLSHGenerator.Members;
using System.Collections.Generic;

namespace GLSHGenerator.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// Refers to GLSL 450 specs.
        /// 8 Built-in Functions.
        /// 8.1 Angle and Trigonometry Functions.
        /// https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.50.pdf
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> TrigonometryFunctions()
        {
            if (BaseType != BuiltinType.TypeFloat)
                yield break;

            yield return new ComponentWiseStaticFunction(Fields, this, "Radians", this, "v", $"Maths.Radians({{0}})") {GlslName = "radians" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Degrees", this, "v", $"Maths.Degrees({{0}})") {GlslName = "degrees" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Sin", this, "v", $"Maths.Sin({{0}})") {GlslName = "sin" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Cos", this, "v", $"Maths.Cos({{0}})") {GlslName = "cos" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Tan", this, "v", $"Maths.Tan({{0}})") {GlslName = "tan" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Asin", this, "v", $"Maths.Asin({{0}})") {GlslName = "asin" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Acos", this, "v", $"Maths.Acos({{0}})") {GlslName = "acos" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Atan", this, "y", this, "x", $"Maths.Atan({{0}} / {{1}})") {GlslName = "atan" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Atan", this, "v", $"Maths.Atan({{0}})") {GlslName = "atan" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Sinh", this, "v", $"Maths.Sinh({{0}})") {GlslName = "sinh" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Cosh", this, "v", $"Maths.Cosh({{0}})") {GlslName = "cosh" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Tanh", this, "v", $"Maths.Tanh({{0}})") {GlslName = "tanh" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Asinh", this, "v", $"Maths.Asinh({{0}})") {GlslName = "asinh" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Acosh", this, "v", $"Maths.Acosh({{0}})") {GlslName = "acosh" };
            yield return new ComponentWiseStaticFunction(Fields, this, "Atanh", this, "v", $"Maths.Atanh({{0}})") { GlslName = "atanh" };
        }
    }
}
