using DVG.GLSH.Generator.Members;
using System.Collections.Generic;
using System.Linq;

namespace DVG.GLSH.Generator.Types
{
    internal partial class VectorType
    {

        /// <summary>
        /// Refers to GLSL 450 specs.
        /// 8 Built-in Functions
        /// 8.5 Geometric Functions
        /// https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.50.pdf
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> GeometryFunctions()
        {
            if (BaseType != BuiltinType.TypeFloat && BaseType != BuiltinType.TypeDouble)
                yield break;

            yield return new Function(BaseType, "Length")
            {
                GlslName = "length",
                Static = true,
                Parameters = this.TypedArgs("v"),
                CodeString = $"Maths.Sqrt({string.Join(" + ", Fields.Select(f => $"v.{f} * v.{f}"))})",
                Comment = "Returns the euclidean length of this vector."
            };
            yield return new Function(BaseType, "Distance")
            {
                GlslName = "distance",
                Static = true,
                Parameters = this.LhsRhs(),
                CodeString = $"{Name}.Length(lhs - rhs)",
                Comment = "Returns the euclidean distance between the two vectors."
            };
            yield return new Function(BaseType, "Dot")
            {
                GlslName = "dot",
                Static = true,
                Parameters = this.LhsRhs(),
                CodeString = string.Join(" + ", Fields.Select(f => $"lhs.{f} * rhs.{f}")),
                Comment = "Returns the inner product (dot product, scalar product) of the two vectors."
            };
            if (Length == 3)
                yield return new Function(this, "Cross")
                {
                    GlslName = "cross",
                    Static = true,
                    Parameters = this.LhsRhs(),
                    CodeString = Construct(this, "lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x"),
                    Comment = "Returns the outer product (cross product, vector product) of the two vectors."
                };
            yield return new Function(this, "Normalize")
            {
                GlslName = "normalize",
                Static = true,
                Parameters = this.TypedArgs("v"),
                CodeString = $"v / {Name}.Length(v)",
                Comment = "Returns a copy of this vector with length one (undefined if this has zero length)."
            };
            yield return new Function(this, "FaceForward")
            {
                GlslName = "faceforward",
                Static = true,
                Parameters = this.TypedArgs("N", "I", "Nref"),
                CodeString = $"{Name}.Dot(Nref, I) < 0 ? N : -N",
                Comment = "Returns a vector pointing in the same direction as another (faceforward orients a vector to point away from a surface as defined by its normal. If dot(Nref, I) is negative faceforward returns N, otherwise it returns -N)."
            };
            yield return new Function(this, "Reflect")
            {
                GlslName = "reflect",
                Static = true,
                Parameters = this.TypedArgs("I", "N"),
                CodeString = $"I - 2 * {Name}.Dot(N, I) * N",
                Comment = "Calculate the reflection direction for an incident vector (N should be normalized in order to achieve the desired result)."
            };
            yield return new Function(this, "Refract")
            {
                GlslName = "refract",
                Static = true,
                Parameters = this.TypedArgs("I", "N").SConcat(BaseTypeName + " eta"),
                Code = new[]
                {
                    $"var dNI = {Name}.Dot(N, I);",
                    "var k = 1 - eta * eta * (1 - dNI * dNI);",
                    $"if (k < 0) return {Construct(this, BaseTypeCast + "0")};",
                    $"return eta * I - (eta * dNI + Maths.Sqrt(k)) * N;",
                },
                Comment = "Calculate the refraction direction for an incident vector (The input parameters I and N should be normalized in order to achieve the desired result)."
            };
        }
    }
}
