using System;
using System.Collections.Generic;
using KibiHex.MathsGen.CodeModel;
using KibiHex.MathsGen.Model;

namespace KibiHex.MathsGen.Model.Rendering
{
    public sealed class ScalarMathsRenderer
    {
        public string Render(IEnumerable<ScalarMathMethod> methods)
        {
            var writer = new CodeWriter();
            writer.Line("#pragma warning disable IDE1006");
            writer.Line("#nullable enable");
            writer.Line();
            writer.Block("namespace KibiHex", () =>
            {
                writer.Block("public static partial class maths", () =>
                {
                    foreach (var method in methods)
                    {
                        if (method.Parameters.Contains('<')) continue;
                        writer.Line($"public static {method.ReturnType} {Lowercase(method.Name)}({method.Parameters}) => Maths.{method.Name}({method.Arguments});");
                    }
                });
            });
            return writer.ToString();
        }

        public string Render(params ScalarMathMethod[] methods) => Render((IEnumerable<ScalarMathMethod>)methods);

        private static string Lowercase(string name) => string.IsNullOrEmpty(name) ? name : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
