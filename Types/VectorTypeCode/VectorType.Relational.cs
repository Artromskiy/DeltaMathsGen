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
        /// 8.7 Vector Relational Functions
        /// https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.50.pdf
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> RelationalFunctions()
        {
            var boolVType = new VectorType(BuiltinType.TypeBool, Length);

            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeUint)
            {
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "LesserThan", this, "lhs", this, "rhs", "{0} < {1}") { GlslName = "lessThan" };
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "LesserThanEqual", this, "lhs", this, "rhs", "{0} <= {1}") { GlslName = "lessThanEqual" };
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "GreaterThan", this, "lhs", this, "rhs", "{0} > {1}") { GlslName = "greaterThan" };
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "GreaterThanEqual", this, "lhs", this, "rhs", "{0} >= {1}") { GlslName = "greaterThanEqual" };
            }

            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble || BaseType == BuiltinType.TypeInt || BaseType == BuiltinType.TypeUint || BaseType == BuiltinType.TypeBool)
            {
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "Equal", this, "lhs", this, "rhs", BaseType.EqualFormat) { GlslName = "equal" };
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "NotEqual", this, "lhs", this, "rhs", BaseType.NotEqualFormat) { GlslName = "notEqual" };
            }

            if (BaseType == BuiltinType.TypeBool)
            {
                yield return new Function(BuiltinType.TypeBool, "Any")
                {
                    GlslName = "any",
                    Static = true,
                    ParameterString = $"{Name} v",
                    Code = new string[] { $"{string.Join("||", Fields.Select(s => $"v.{s}"))}" }
                };
                yield return new Function(BuiltinType.TypeBool, "All")
                {
                    GlslName = "all",
                    Static = true,
                    ParameterString = $"{Name} v",
                    Code = new string[] { $"{string.Join("&&", Fields.Select(s => $"v.{s}"))}" }
                };
                yield return new ComponentWiseStaticFunction(Fields, boolVType, "Not", this, "v", "!{0}") { GlslName = "not" };
            }
        }
    }
}
