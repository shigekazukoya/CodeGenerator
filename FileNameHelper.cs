using System.Text.RegularExpressions;

namespace CodeGenerator
{
    public static class FileNameHelper
    {
        public static string DetermineFilename(string language, string code)
        {
            switch (language)
            {
                case "python":
                    var pyClassMatch = Regex.Match(code, @"class\s+(\w+)");
                    var pyFuncMatch = Regex.Match(code, @"def\s+(\w+)");
                    if (pyClassMatch.Success)
                        return $"{pyClassMatch.Groups[1].Value}.py";
                    else if (pyFuncMatch.Success)
                        return $"{pyFuncMatch.Groups[1].Value}.py";
                    else
                        return "main.py";
                case "xaml":
                    return GetXamlClassName(code);

                case "csharp":
                case "c#":
                    var csClassMatch = Regex.Match(code, @"class\s+(\w+)");
                    if (csClassMatch.Success)
                        return $"{csClassMatch.Groups[1].Value}.cs";
                    else
                        return "Program.cs";
                case "bat":
                    return "Program.bat";
                case "fish":
                    return "Program.fish";
                case "bash":
                    return "Program.bash";
                case "json":
                    return "Sample.json";
                case "rust":
                    var rsModMatch = Regex.Match(code, @"mod\s+(\w+)");
                    var rsFnMatch = Regex.Match(code, @"fn\s+(\w+)");
                    if (rsModMatch.Success)
                        return $"{rsModMatch.Groups[1].Value}.rs";
                    else if (rsFnMatch.Success)
                        return $"{rsFnMatch.Groups[1].Value}.rs";
                    else
                        return "main.rs";
                case "typescript":
                    var tsComponentMatch = Regex.Match(code, @"(?:function|const)\s+(\w+)");
                    if (tsComponentMatch.Success)
                        return $"{tsComponentMatch.Groups[1].Value}.tsx";
                    else
                        return "component.tsx";
                case "javascript":
                    var jsComponentMatch = Regex.Match(code, @"(?:function|const)\s+(\w+)");
                    if (jsComponentMatch.Success)
                        return $"{jsComponentMatch.Groups[1].Value}.js";
                    else
                        return "script.js";
                default:
                    return $"code.{language.ToLower()}";
            }
        }

        private static string GetXamlClassName(string xamlCode)
        {
            var match = Regex.Match(xamlCode, @"x:Class=""([^""]+)""");
            if (match.Success)
            {
                string fullClassName = match.Groups[1].Value;
                string[] parts = fullClassName.Split('.');
                var fileName = parts.Length > 0 ? parts[^1] : fullClassName;
                return fileName + ".xaml";
            }
            return "MainWindow.xaml";
        }
    }
}
