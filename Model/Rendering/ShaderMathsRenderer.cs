using System;
using System.Collections.Generic;
using System.Linq;
using KibiHex.MathsGen.CodeModel;

namespace KibiHex.MathsGen.Model.Rendering
{
    internal sealed class ShaderMathsRenderer
    {
        public bool CanRender(IEnumerable<TypeSpec> types) => ShaderMathsFunctions(types).Any();

        public string Render(IEnumerable<TypeSpec> types)
        {
            var functions = ShaderMathsFunctions(types).ToArray();
            if (functions.Length == 0)
                throw new InvalidOperationException("No shader maths functions found.");

            var writer = new CodeWriter();
            writer.Line("#pragma warning disable IDE1006");
            writer.Line("#nullable enable");
            writer.Line("using System.Runtime.CompilerServices;");
            writer.Line();
            writer.Block("namespace KibiHex", () =>
            {
                writer.Block("public static partial class maths", () =>
                {
                    foreach (var item in functions)
                    {
                        writer.Line();
                        RenderFunction(writer, item.Type, item.Function);
                    }
                });
            });
            return writer.ToString();
        }

        private static void RenderFunction(CodeWriter writer, TypeSpec type, FunctionSpec function)
        {
            var signature = $"public static {function.ReturnType} {function.MathsName}({SyntaxFormatter.Parameters(function.Parameters)})";
            writer.Line("[MethodImpl(MethodImplOptions.AggressiveInlining)]");

            if (function.Api.HasFlag(ApiSurface.Type))
            {
                var arguments = string.Join(", ", function.Parameters.Select(parameter =>
                    string.IsNullOrWhiteSpace(parameter.Modifier) ? parameter.Name : parameter.Modifier + " " + parameter.Name));
                writer.Line($"{signature} => {type.Name}.{function.Name}({arguments});");
            }
            else if (!string.IsNullOrWhiteSpace(function.Expression))
            {
                writer.Line($"{signature} => {function.Expression};");
            }
            else
            {
                BodyRenderer.Block(writer, signature, function.Body);
            }
        }

        private static IEnumerable<(TypeSpec Type, FunctionSpec Function)> ShaderMathsFunctions(IEnumerable<TypeSpec> types) =>
            types.SelectMany(type => type.Members
                .OfType<FunctionSpec>()
                .Where(function => function.Api.HasFlag(ApiSurface.ShaderMaths))
                .Select(function => (type, function)));
    }
}
