using System;
using DeltaMathsGen.CodeModel;

namespace DeltaMathsGen.Model.Rendering
{
    internal static class MemberRenderer
    {
        public static void Render(CodeWriter writer, MemberSpec member, string typeName)
        {
            writer.Line();
            foreach (var attribute in member.Attributes)
            {
                writer.Line($"[{attribute}]");
            }

            if (!string.IsNullOrWhiteSpace(member.Summary))
            {
                writer.Line($"/// <summary>{member.Summary}</summary>");
            }

            switch (member)
            {
                case FieldSpec field:
                    var initializer = string.IsNullOrWhiteSpace(field.Initializer) ? "" : " = " + field.Initializer;
                    writer.Line($"{SyntaxFormatter.Modifiers(field.Modifiers)} {field.Type} {field.Name}{initializer};".Trim());
                    break;
                case ConstructorSpec constructor:
                    BodyRenderer.Block(writer,
                        $"{SyntaxFormatter.Modifiers(constructor.Modifiers)} {typeName}({SyntaxFormatter.Parameters(constructor.Parameters)})",
                        constructor.Body);
                    break;
                case OperatorSpec op:
                    var operatorSignature = op.Operator is "implicit" or "explicit"
                        ? $"{SyntaxFormatter.Modifiers(op.Modifiers)} {op.Operator} operator {op.ReturnType}({SyntaxFormatter.Parameters(op.Parameters)})"
                        : $"{SyntaxFormatter.Modifiers(op.Modifiers)} {op.ReturnType} operator {op.Operator}({SyntaxFormatter.Parameters(op.Parameters)})";
                    BodyRenderer.Block(writer, operatorSignature, op.Body);
                    break;
                case FunctionSpec function:
                    RenderFunction(writer, function);
                    break;
                case PropertySpec property:
                    RenderProperty(writer, property);
                    break;
                case IndexerSpec indexer:
                    RenderIndexer(writer, indexer);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported member: {member.GetType().Name}");
            }
        }

        private static void RenderFunction(CodeWriter writer, FunctionSpec function)
        {
            var signature = $"{SyntaxFormatter.Modifiers(function.Modifiers)} {function.ReturnType} {function.Name}({SyntaxFormatter.Parameters(function.Parameters)})";
            if (!string.IsNullOrWhiteSpace(function.Expression))
            {
                writer.Line($"{signature} => {function.Expression};");
            }
            else
            {
                BodyRenderer.Block(writer, signature, function.Body);
            }
        }

        private static void RenderProperty(CodeWriter writer, PropertySpec property)
        {
            var signature = $"{SyntaxFormatter.Modifiers(property.Modifiers)} {property.Type} {property.Name}";
            if (!string.IsNullOrWhiteSpace(property.Expression))
            {
                writer.Line($"{signature} => {property.Expression};");
                return;
            }

            writer.Block(signature, () =>
            {
                if (property.Getter != null)
                {
                    RenderAccessor(writer, "get", property.Getter);
                }

                if (property.Setter != null)
                {
                    RenderAccessor(writer, "set", property.Setter);
                }
            });
        }

        private static void RenderIndexer(CodeWriter writer, IndexerSpec indexer)
        {
            var signature = $"{SyntaxFormatter.Modifiers(indexer.Modifiers)} {indexer.Type} this[{SyntaxFormatter.Parameters(new[] { indexer.Parameter })}]";

            writer.Block(signature, () =>
            {
                if (indexer.Getter != null)
                {
                    RenderAccessor(writer, "get", indexer.Getter);
                }

                if (indexer.Setter != null)
                {
                    RenderAccessor(writer, "set", indexer.Setter);
                }
            });
        }

        private static void RenderAccessor(CodeWriter writer, string name, string body)
        {
            if (!body.Contains('\n', StringComparison.Ordinal))
            {
                writer.Line($"{name} => {body};");
            }
            else
            {
                BodyRenderer.Block(writer, name, body);
            }
        }
    }
}
