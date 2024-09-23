using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeGenerator
{
    public class CodeFileSaver
    {
        public void SaveGeneratedContent(string generatedContent, string outputFolder)
        {
            var codeBlocks = Regex.Matches(generatedContent, @"```([\w\+\#\.]+)?(?:\:([^\r\n]+))?\r?\n([\s\S]*?)\r?\n```");

            if (codeBlocks.Count > 0)
            {
                foreach (Match codeBlock in codeBlocks)
                {
                    string language = codeBlock.Groups[1].Value?.ToLower();
                    string filenameWithPath = codeBlock.Groups[2].Value;
                    string code = codeBlock.Groups[3].Value.Trim();

                    if (string.IsNullOrEmpty(filenameWithPath))
                    {
                        filenameWithPath = FileNameHelper.DetermineFilename(language, code);
                    }

                    string filePath = Path.GetFullPath(Path.Combine(outputFolder, filenameWithPath));

                    if (!filePath.StartsWith(Path.GetFullPath(outputFolder), StringComparison.OrdinalIgnoreCase))
                    {
                        throw new UnauthorizedAccessException("無効なファイルパスが検出されました。");
                    }

                    string directoryPath = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    File.WriteAllText(filePath, code);
                }
            }
            else
            {
                string filePath = Path.Combine(outputFolder, "generated_content.md");
                File.WriteAllText(filePath, generatedContent);
            }
        }
    }
}
