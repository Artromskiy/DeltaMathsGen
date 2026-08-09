using System;
using System.Collections.Generic;
using System.Linq;

namespace KibiHex.MathsGen.Validation
{
    public sealed class ApiTypeComparison
    {
        public ApiTypeComparison(string typeName, IEnumerable<string> missing, IEnumerable<string> extra)
        {
            TypeName = typeName;
            Missing = missing.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Extra = extra.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        }

        public string TypeName { get; }
        public IReadOnlyList<string> Missing { get; }
        public IReadOnlyList<string> Extra { get; }
        public bool IsEqual => Missing.Count == 0 && Extra.Count == 0;
    }

    public sealed class ApiSurfaceComparison
    {
        public ApiSurfaceComparison(IEnumerable<ApiTypeComparison> types)
        {
            Types = types.ToArray();
        }

        public IReadOnlyList<ApiTypeComparison> Types { get; }
        public bool IsEqual => Types.All(x => x.IsEqual);
    }

    public static class ApiSurfaceComparer
    {
        public static ApiSurfaceComparison Compare(ApiSurface expected, ApiSurface actual)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));

            var names = expected.Types.Keys.Union(actual.Types.Keys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal);
            return new ApiSurfaceComparison(names.Select(name =>
            {
                var expectedMembers = Members(expected, name);
                var actualMembers = Members(actual, name);
                return new ApiTypeComparison(name, expectedMembers.Except(actualMembers, StringComparer.Ordinal), actualMembers.Except(expectedMembers, StringComparer.Ordinal));
            }));
        }

        public static ApiSurfaceComparison Compare(string expectedAssemblyPath, string actualAssemblyPath)
        {
            return Compare(ApiSurfaceReader.Read(expectedAssemblyPath), ApiSurfaceReader.Read(actualAssemblyPath));
        }

        private static IEnumerable<string> Members(ApiSurface surface, string name)
        {
            return surface.Types.TryGetValue(name, out var type) ? type.Members : Array.Empty<string>();
        }
    }
}
