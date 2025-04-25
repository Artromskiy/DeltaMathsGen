using GLSHGenerator.Members;
using System.Collections.Generic;
using System.Linq;

namespace GLSHGenerator.Types
{
    internal partial class VectorType
    {

        public override IEnumerable<Member> GenerateMembers()
        {
            // fields
            foreach (var f in Fields)
                yield return new Field(f, BaseType) { Comment = $"{f}-component" };

            foreach (var item in Constructors())
                yield return item;

            foreach (var item in Operators())
                yield return item;

            foreach (var item in CastFunctions())
                yield return item;

            foreach (var item in SwizzleProperties())
                yield return item;

            foreach (var item in TrigonometryFunctions())
                yield return item;

            foreach (var item in GeometryFunctions())
                yield return item;

            foreach (var item in ExponentialFunctions())
                yield return item;

            foreach (var item in RelationalFunctions())
                yield return item;

            foreach (var item in CommonFunctions())
                yield return item;

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
                DefaultValue = Components.ToString(),
                Comment = $"Returns the number of components ({Components})."
            };



            yield return new Indexer(BaseType)
            {
                ParameterString = "int index",
                Getter = [$"if ((uint)index >= Count)",
                          "    throw new ArgumentOutOfRangeException(nameof(index));",
                          "return Unsafe.Add(ref x, index);"],
                Setter = [$"if ((uint)index >= Count)",
                          "    throw new ArgumentOutOfRangeException(nameof(index));",
                          "Unsafe.Add(ref x, index) = value;"],
                Comment = "Gets/Sets a specific indexed component (a bit slower than direct access)."
            };
        }

    }
}
