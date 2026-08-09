using KibiHex.MathsGen.CodeModel;
using KibiHex.MathsGen.Members;
using KibiHex.MathsGen.Model;
using System;
using System.Linq;

namespace KibiHex.MathsGen.Types
{
    internal abstract partial class AbstractType
    {
        public string RenderedCSharpFile => RenderFile(writer =>
        {
            var baseClasses = BaseClasses.ToArray();
            writer.Line("#pragma warning disable IDE1006");
            writer.Line("#nullable enable");
            writer.Line("using System;");
            writer.Line("using System.Runtime.InteropServices;");
            writer.Line("using System.Runtime.CompilerServices;");
            writer.Line("using System.Runtime.Serialization;");
            writer.Line("using System.Diagnostics;");
            writer.Line();
            writer.Line();
            writer.Line("namespace " + Namespace);
            writer.Line("{");
            writer.Indent(() =>
            {
                foreach (var line in TypeComment.AsComment())
                    writer.Line(line);
                foreach (var attribute in Attributes)
                    writer.Line($"[{attribute}]");

                writer.Line("public partial struct " + Name +
                    (baseClasses.Length == 0 ? "" : " : " + baseClasses.CommaSeparated()));
                writer.Line("{");
                writer.Indent(() =>
                {
                    RenderSection(writer, "Fields", fields);
                    RenderSection(writer, "Constructors", constructors);
                    RenderSection(writer, "Implicit Operators", implicitOperators);
                    RenderSection(writer, "Explicit Operators", explicitOperators);
                    RenderSection(writer, "Indexer", indexer);
                    RenderSection(writer, "Properties", properties.Where(property => property.Part != TypePart.Swizzles).ToArray());
                    RenderSection(writer, "Static Properties", staticProperties);
                    RenderSection(writer, "Operators", operators);
                    RenderSection(writer, "Functions", functions);
                    RenderSection(writer, "Static Functions", staticFunctions);
                    RenderSection(writer, "Component-Wise Static Functions", componentWiseStaticFunctions);
                    RenderSection(writer, "Component-Wise Operator Overloads", componentWiseOp);
                });
                writer.Line("}");
            });
            writer.Line("}");
        });

        public string RenderedSwizzlesFile => RenderFile(writer =>
        {
            var swizzles = properties.Where(property => property.Part == TypePart.Swizzles).ToArray();
            writer.Line("#pragma warning disable IDE1006");
            writer.Line("#nullable enable");
            writer.Line("using System;");
            writer.Line("using System.Runtime.InteropServices;");
            writer.Line("using System.Runtime.CompilerServices;");
            writer.Line("using System.Runtime.Serialization;");
            writer.Line("using System.Diagnostics;");
            writer.Line();
            writer.Line();
            writer.Line("namespace " + Namespace);
            writer.Line("{");
            writer.Indent(() =>
            {
                writer.Line("public partial struct " + Name);
                writer.Line("{");
                writer.Indent(() => RenderSection(writer, "Swizzles", swizzles));
                writer.Line("}");
            });
            writer.Line("}");
        });

        private static string RenderFile(Action<CodeWriter> render)
        {
            var writer = new CodeWriter();
            render(writer);
            return writer.ToString();
        }

        private static void RenderSection(CodeWriter writer, string name, Member[] members)
        {
            if (members.Length == 0)
                return;

            writer.Line();
            writer.Line($"#region {name}");
            foreach (var member in members)
                member.Render(writer);
            writer.Line();
            writer.Line("#endregion");
            writer.Line();
        }
    }
}

