using System;
using System.Text;

namespace Delta.MathsGen.CodeModel
{
    internal sealed class CodeWriter
    {
        private readonly StringBuilder builder = new();
        private int indent;

        public void Line(string text = "")
        {
            if (text.Length > 0)
                builder.Append(' ', indent * 4);

            builder.AppendLine(text);
        }

        public void Indent(Action body)
        {
            indent++;
            body();
            indent--;
        }

        public void Block(string header, Action body)
        {
            Line(header);
            Line("{");
            Indent(body);
            Line("}");
        }

        public override string ToString() => builder.ToString();
    }
}

