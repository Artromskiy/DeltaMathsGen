using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Delta.MathsGen.Model
{
    /// <summary>Describes a scalar maths method discovered in a source file.</summary>
    /// <param name="ReturnType">The CLR return type text.</param>
    /// <param name="Name">The method name.</param>
    /// <param name="Parameters">The original parameter declaration text.</param>
    /// <param name="Arguments">The generated argument forwarding text.</param>
    public sealed record ScalarMathMethod(string ReturnType, string Name, string Parameters, string Arguments);

    /// <summary>Small, dependency-free scanner for the scalar DeltaMaths source files.</summary>
    /// <summary>Scans scalar maths source files for public static methods.</summary>
    public sealed partial class ScalarDeltaMathsScanner
    {
        private readonly Encoding _encoding;

        /// <summary>Initializes a scanner using UTF-8 source decoding.</summary>
        public ScalarDeltaMathsScanner()
        {
            _encoding = Encoding.UTF8;
        }

        [GeneratedRegex(@"\bpublic\s+static\s+(?<return>[A-Za-z_][\w.<>?]*(?:\s*\[\])?)\s+(?<name>[A-Za-z_]\w*)\s*\((?<parameters>[^()]*)\)", RegexOptions.Compiled)]
        private static partial Regex MethodRegex();

        [GeneratedRegex(@"//[^\r\n]*|/\*[\s\S]*?\*/", RegexOptions.Compiled)]
        private static partial Regex CommentRegex();

        /// <summary>Scans the specified source files.</summary>
        public ScalarMathMethod[] Scan(string[] sourcePaths)
        {
            ArgumentNullException.ThrowIfNull(sourcePaths);
            var result = new List<ScalarMathMethod>();
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in sourcePaths)
            {
                ScanSource(File.ReadAllText(path, _encoding), result, signatures);
            }

            return result.ToArray();
        }

        /// <summary>Scans all scalar maths files in a directory.</summary>
        public ScalarMathMethod[] ScanDirectory(string directory) => Scan(
            Directory.GetFiles(directory, "DeltaMaths*.cs", SearchOption.TopDirectoryOnly));

        private static void ScanSource(string source, List<ScalarMathMethod> result, HashSet<string> signatures)
        {
            source = CommentRegex().Replace(source, "");
            foreach (Match match in MethodRegex().Matches(source))
            {
                var parameters = match.Groups["parameters"].Value.Trim();
                if (parameters.Contains('<', StringComparison.Ordinal) || match.Groups["name"].Value.Contains('<', StringComparison.Ordinal))
                {
                    continue;
                }

                var arguments = string.Join(", ", ParseParameterNames(parameters));
                var method = new ScalarMathMethod(match.Groups["return"].Value, match.Groups["name"].Value, parameters, arguments);
                var key = method.Name + "(" + method.Parameters + ")";
                if (signatures.Add(key))
                {
                    result.Add(method);
                }
            }
        }

        private static string[] ParseParameterNames(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                return [];
            }

            var result = new List<string>();
            foreach (var parameter in parameters.Split(','))
            {
                var tokens = parameter.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0)
                {
                    continue;
                }

                var name = tokens[^1].Split('=')[0].Trim();
                var modifier = Array.Find(tokens, token => token is "ref" or "out" or "in");
                result.Add(modifier == null ? name : modifier + " " + name);
            }
            return result.ToArray();
        }
    }
}
