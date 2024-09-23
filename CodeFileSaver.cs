using DiffPlex.DiffBuilder.Model;
using DiffPlex.DiffBuilder;
using DiffPlex;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

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

                    SaveFile(filePath, code);
                }
            }
            else
            {
                string filePath = Path.Combine(outputFolder, "generated_content.md");
                SaveFile(filePath, generatedContent);
            }
        }

        private void SaveFile(string filePath, string newContent)
        {
            if (File.Exists(filePath))
            {
                string existingContent = File.ReadAllText(filePath);

                var diffBuilder = new InlineDiffBuilder(new Differ());
                var diffResult = diffBuilder.BuildDiffModel(existingContent, newContent);

                // Check if there are differences
                if (diffResult.Lines.Any(line => line.Type != ChangeType.Unchanged))
                {
                    // Show diff and ask for merge
                    var diffWindow = new FileMager(existingContent, newContent, filePath);
                    bool? result = diffWindow.ShowDialog();

                    if (result == true)
                    {
                        // User merged changes
                        string mergedContent = diffWindow.MergedText;
                        File.WriteAllText(filePath, mergedContent);
                    }
                    else
                    {
                        // User canceled
                    }
                }
                else
                {
                    // No differences, overwrite
                    File.WriteAllText(filePath, newContent);
                }
            }
            else
            {
                File.WriteAllText(filePath, newContent);
            }
        }
    }
}
