using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace KibiHex.MathsGen.Model
{
    public sealed record ScalarMathMethod(string ReturnType, string Name, string Parameters, string Arguments);

    /// <summary>Small, dependency-free scanner for the scalar Maths source files.</summary>
    public sealed class ScalarMathsScanner
    {
        private static readonly Regex Method = new(
            @"\bpublic\s+static\s+(?<return>[A-Za-z_][\w.<>?]*(?:\s*\[\])?)\s+(?<name>[A-Za-z_]\w*)\s*\((?<parameters>[^()]*)\)",
            RegexOptions.Compiled);

        public ScalarMathMethod[] Scan(string[] sourcePaths)
        {
            var result = new List<ScalarMathMethod>();
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in sourcePaths)
                ScanSource(File.ReadAllText(path), result, signatures);
            return result.ToArray();
        }

        public ScalarMathMethod[] ScanDirectory(string directory) => Scan(
            Directory.GetFiles(directory, "Maths*.cs", SearchOption.TopDirectoryOnly));

        private static void ScanSource(string source, List<ScalarMathMethod> result, HashSet<string> signatures)
        {
            source = Regex.Replace(source, @"//[^\r\n]*|/\*[\s\S]*?\*/", "");
            foreach (Match match in Method.Matches(source))
            {
                var parameters = match.Groups["parameters"].Value.Trim();
                if (parameters.Contains('<') || match.Groups["name"].Value.Contains('<'))
                    continue;

                var arguments = string.Join(", ", ParseParameterNames(parameters));
                var method = new ScalarMathMethod(match.Groups["return"].Value, match.Groups["name"].Value, parameters, arguments);
                var key = method.Name + "(" + method.Parameters + ")";
                if (signatures.Add(key))
                    result.Add(method);
            }
        }

        private static string[] ParseParameterNames(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters)) return [];
            var result = new List<string>();
            foreach (var parameter in parameters.Split(','))
            {
                var tokens = parameter.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;
                var name = tokens[^1].Split('=')[0].Trim();
                var modifier = Array.Find(tokens, token => token is "ref" or "out" or "in");
                result.Add(modifier == null ? name : modifier + " " + name);
            }
            return result.ToArray();
        }
    }
}
