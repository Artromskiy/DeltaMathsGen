using DVG.GLSH.Generator.Members;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DVG.GLSH.Generator.Types
{
    internal partial class VectorType
    {
        /// <summary>
        /// Refers to GLSL 450 specs.
        /// 5 Operators and Expressions.
        /// 5.5 Vector and Scalar Components and Length.
        /// https://registry.khronos.org/OpenGL/specs/gl/GLSLangSpec.4.50.pdf
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Member> SwizzleProperties()
        {
            for (int i = 0; i < Length; i++)
            {
                yield return RenameProperty(i, ArgOfRGBA);
                yield return RenameProperty(i, ArgOfSTPQ);
            }
            foreach (var indices in Combinations(4).Concat(Combinations(3)).Concat(Combinations(2)))
            {
                var xyzw = SwizzleProperty(indices, ArgOf);
                if (xyzw != null)
                    yield return xyzw;
                var rgba = SwizzleProperty(indices, ArgOfRGBA);
                if (rgba != null)
                    yield return rgba;
                var stpq = SwizzleProperty(indices, ArgOfSTPQ);
                if (stpq != null)
                    yield return stpq;
            }
        }

        private Property? SwizzleProperty(List<int> combination, Func<int, char> rename)
        {
            var swizzleRename = string.Concat(combination.Select(c => c == -1 ? '_' : rename.Invoke(c)));
            var returnType = new VectorType(BaseType, combination.Count);
            bool noSetter = combination.Contains(-1) || combination.Count > Length || combination.Distinct().Count() != combination.Count;
            bool isGlsl = !combination.Contains(-1);
            if (combination.All(c => c == -1))
                return null;
            return new Property(swizzleRename, returnType)
            {
                GetterLine = $"{Construct(returnType, combination.Select(c => c == -1 ? BaseType.ZeroValue : ArgOf(c).ToString()))}",
                Setter = noSetter ? null : combination.Select((c, i) => $"{ArgOf(c)} = value.{ArgOf(i)};"),
                Comment = "Gets or sets the specified subset of components.",
                GlslName = isGlsl ? "Swizzle" : string.Empty,
                DisableGlmGen = true
            };
        }

        private Property RenameProperty(int index, Func<int, char> rename)
        {
            var prop = ArgOf(index);
            var propRename = rename(index);
            var returnType = BaseType;
            return new Property(propRename.ToString(), returnType)
            {
                GetterLine = $"{prop}",
                SetterLine = $"{prop} = value;",
                Comment = "Gets or sets the specified subset of components.",
                GlslName = "Swizzle",
                DisableGlmGen = true
            };
        }

        private IEnumerable<List<int>> Combinations(int k)
        {
            int[] numbers = Enumerable.Range(-1, Length + 1).ToArray();
            var indices = new int[k];
            var totalCombinations = (int)Math.Pow(numbers.Length, k);
            var combination = new List<int>(k);
            for (int count = 0; count < totalCombinations; count++)
            {
                combination.Clear();
                for (int i = 0; i < k; i++)
                    combination.Add(numbers[indices[i]]);
                yield return combination;

                for (int i = k - 1; i >= 0; i--)
                {
                    indices[i]++;
                    if (indices[i] < numbers.Length)
                        break;

                    indices[i] = 0;
                }
            }
        }
    }
}
