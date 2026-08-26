using System;
using System.Linq;
using DeltaMathsGen.CodeModel;

namespace DeltaMathsGen.Model.Rendering
{
    internal static class BodyRenderer
    {
        private static readonly string[] NewLineSeparators = ["\r\n", "\n"];

        public static void Block(CodeWriter writer, string signature, string body)
        {
            writer.Block(signature.Trim(), () =>
            {
                foreach (var line in Normalize(body).Split(NewLineSeparators, StringSplitOptions.None))
                {
                    writer.Line(line);
                }
            });
        }

        private static string Normalize(string body)
        {
            var lines = body.Trim('\r', '\n').Split(NewLineSeparators, StringSplitOptions.None);
            var indentation = int.MaxValue;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var spaces = line.TakeWhile(char.IsWhiteSpace).Count();
                indentation = Math.Min(indentation, spaces);
            }

            if (indentation == int.MaxValue || indentation == 0)
            {
                return string.Join("\n", lines);
            }

            return string.Join("\n", lines.Select(line =>
                line.Length >= indentation ? line[indentation..] : ""));
        }
    }
}
