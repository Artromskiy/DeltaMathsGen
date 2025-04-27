using GLSHGenerator.Members;
using System.Collections.Generic;

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

            foreach (var item in ExtendedFunctions())
                yield return item;


            yield return new Indexer(BaseType)
            {
                ParameterString = "int index",
                Getter = new string[]
                {
                    $"if ((uint)index >= Count)",
                    $"    throw new ArgumentOutOfRangeException(nameof(index));",
                    $"return Unsafe.Add(ref x, index);"
                },
                Setter = new string[]
                {
                    $"if ((uint)index >= Count)",
                    "    throw new ArgumentOutOfRangeException(nameof(index));",
                    "Unsafe.Add(ref x, index) = value;"
                },
                Comment = "Gets/Sets a specific indexed component (a bit slower than direct access)."
            };
        }

    }
}
