using DVG.GLSH.Generator.Members;
using System.Collections.Generic;
using System.Linq;

namespace DVG.GLSH.Generator.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// Does not refers to GLSL
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> ExtendedFunctions()
        {
            yield return new Function(BuiltinType.TypeInt, "GetHashCode")
            {
                Override = true,
                Readonly = true,
                CodeString = $"HashCode.Combine({string.Join(", ", Fields)})",
                Comment = "Returns HashCode"
            };

            yield return new Function(new AnyType("string"), "ToString")
            {
                Override = true,
                Readonly = true,
                CodeString = $"{string.Join(" + \", \" + ", Fields)}",
                Comment = "Returns a string representation of this vector."
            };

            yield return new Function(BuiltinType.TypeBool, "Equals")
            {
                Readonly = true,
                ParameterString = $"{Name} other",
                CodeString = "other == this",
            };

            yield return new Function(BuiltinType.TypeBool, "Equals")
            {
                Override = true,
                Readonly = true,
                ParameterString = $"object? obj",
                CodeString = $"obj is {Name} other && Equals(other)",
            };

            yield return new Field("Count", BuiltinType.TypeInt)
            {
                Constant = true,
                DefaultValue = Length.ToString(),
                Comment = $"Returns the number of components ({Length})."
            };

            yield return new Field("zero", this)
            {
                Static = true,
                Readonly = true,
                DefaultValue = Construct(this, Fields.Select(f => BaseType.ZeroValue)),
                Comment = $"Returns new vector with every component set to default."
            };

            if (BaseType == BuiltinType.TypeFloat || BaseType == BuiltinType.TypeDouble)
            {
                yield return new Function(BaseType, "SqrLength")
                {
                    Static = true,
                    Parameters = this.TypedArgs("v"),
                    CodeString = $"{string.Join(" + ", Fields.Select(f => $"v.{f} * v.{f}"))}",
                    Comment = "Returns the square length of this vector."
                };

                yield return new Function(BaseType, "SqrDistance")
                {
                    Static = true,
                    Parameters = this.LhsRhs(),
                    CodeString = $"{Name}.SqrLength(lhs - rhs)",
                    Comment = "Returns the square distance between the two vectors."
                };

                yield return new ComponentWiseStaticFunction(Fields, this, "InvLerp", this, "edge0", this, "edge1", this, "v", $"Maths.InvLerp({{0}}, {{1}}, {{2}})")
                {
                    CanScalar2 = true
                };


                yield return new Function(this, "SmoothDamp")
                {
                    Static = true,
                    Parameters = new string[] { $"{Name} source", $"{Name} target", $"ref {Name} velocity", $"{BaseType.Name} smoothTime", $"{BaseType.Name} deltaTime" },
                    CodeString = $"{Construct(this, Fields.Select(f => $"Maths.SmoothDamp(source.{f}, target.{f}, ref velocity.{f}, smoothTime, deltaTime)"))}",
                    DisableGlmGen = true
                };
                yield return new Function(this, "ClampLength")
                {
                    Static = true,
                    Parameters = new string[] { $"{Name} value, {BaseType.Name} maxLength" },
                    Code = new string[]
                    {
                        $"var sqrLength = SqrLength(value);",
                        $"if (sqrLength > maxLength * maxLength)",
                        $"{{",
                        $"    {BaseType.Name} ratio = maxLength / Maths.Sqrt(sqrLength);",
                        $"    return {Construct(this, Fields.Select(f=>$"value.{f} * ratio"))};",
                        $"}}",
                        $"return value;"
                    },
                    Comment = "Returns this vector with length clamped to maxLength."
                };

                yield return new Function(this, "MoveTowards")
                {
                    Static = true,
                    Parameters = new string[] { $"{Name} current, {Name} target, {BaseType.Name} maxDelta" },
                    CodeString = $"{Construct(this, Fields.Select(f => $"Maths.MoveTowards(current.{f}, target.{f}, maxDelta)"))}",
                    Comment = "Moves vector towards target."
                };
            }

            //TODO vector clamp length
            //TODO add span enumerator
            //TODO add smoothDamp
        }
    }
}
