using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string API_KEY = "";
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
        private string rootFolder;

        public MainWindow()
        {
            InitializeComponent();
            InitializeLanguageComboBox();
        }

        private void SelectRootFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "ルートフォルダを選択"
            };

            if (dialog.ShowDialog() == true)
            {
                rootFolder = dialog.FolderName;
                UpdateFolderTreeView();
            }
        }

        private void UpdateFolderTreeView()
        {
            FolderTreeView.Items.Clear();
            var rootItem = new FolderTreeItem(new DirectoryInfo(rootFolder));
            rootItem.Items.Add(DummyTreeViewItem());
            rootItem.IsSelected = true;
            FolderTreeView.Items.Add(rootItem);
            rootItem.Expanded += Folder_Expanded;
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            var item = (FolderTreeItem)sender;
            if (item.Items.Count == 1 && item.Items[0] is TreeViewItem dummyItem && dummyItem.Tag == null)
            {
                item.Items.Clear();
                try
                {
                    foreach (var directory in item.Info.GetDirectories())
                    {
                        var subItem = new FolderTreeItem(directory);
                        subItem.Items.Add(DummyTreeViewItem());
                        subItem.Expanded += Folder_Expanded;
                        item.Items.Add(subItem);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // アクセス権限がない場合は、このフォルダの内容を表示しない
                }
            }
        }

        private TreeViewItem DummyTreeViewItem()
        {
            return new TreeViewItem();
        }
        private void InitializeLanguageComboBox()
        {
            var languages = new List<string>
            {
                "TypeScript (React)",
                "TypeScript (Next.js)",
                "JavaScript",
                "bash",
                "fish",
                "bat",
                "C#",
                "C#+WPF", 
                "ASP.NET Core", 
                "Rust", 
                "Python",
                "Json"
            };

            LanguageComboBox.ItemsSource = languages;
            LanguageComboBox.SelectedIndex = 0;
        }


        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptTextArea.Text;
            string selectedLanguage = LanguageComboBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("プロンプトを入力してください。");
                return;
            }

            try
            {
                string generatedContent = await GenerateCodeWithGemini(prompt, selectedLanguage);
                ResultTextBox.Text = generatedContent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}");
            }
        }


private async Task<string> GenerateCodeWithGemini(string prompt, string language)
        {
            using (var client = new HttpClient())
            {
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"Generate {language} code for the following prompt: {prompt}" }
                            }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("x-goog-api-key", API_KEY);

                var response = await client.PostAsync($"{API_URL}", content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed: {responseString}");
                }

                var responseObject = JObject.Parse(responseString);
                return responseObject["candidates"][0]["content"]["parts"][0]["text"].ToString();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FolderTreeView.SelectedItem as FolderTreeItem;
            if (selectedItem == null)
            {
                MessageBox.Show("保存先フォルダを選択してください。");
                return;
            }

            string outputFolder = selectedItem.Info.FullName;
            string generatedContent = ResultTextBox.Text;

            if (string.IsNullOrWhiteSpace(generatedContent))
            {
                MessageBox.Show("保存するコンテンツがありません。");
                return;
            }

            try
            {
                SaveGeneratedContent(generatedContent, outputFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存中にエラーが発生しました: {ex.Message}");
            }
        }

        private void SaveGeneratedContent(string generatedContent, string outputFolder)
    {
        var codeBlocks = Regex.Matches(generatedContent, @"```(\w+)\r?\n([\s\S]*?)\r?\n```");
        
        if (codeBlocks.Count > 0)
        {
            foreach (Match codeBlock in codeBlocks)
            {
                string language = codeBlock.Groups[1].Value.ToLower();
                string code = codeBlock.Groups[2].Value.Trim();

                string filename = DetermineFilename(language, code);
                string filePath = System.IO.Path.Combine(outputFolder, filename);

                File.WriteAllText(filePath, code);
            }
        }
        else
        {
            // コードブロックがない場合、全体をマークダウンファイルとして保存
            string filePath = System.IO.Path.Combine(outputFolder, "generated_content.md");
            File.WriteAllText(filePath, generatedContent);
        }

        MessageBox.Show($"ファイルが {outputFolder} に保存されました。");
    }

 private string DetermineFilename(string language, string code)
        {
            switch (language)
            {
                case "Python":
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
                    var classMatch = Regex.Match(code, @"class\s+([^\s]+)\s*:\s*([^\s{]+)");
                    if (classMatch.Success)
                    {
                        string className = classMatch.Groups[1].Value;
                        string baseClass = classMatch.Groups[2].Value;

                        // クラスがWindowまたはUserControlを継承している場合はxaml.csとして扱う
                        if (baseClass == "Window" || baseClass == "UserControl")
                        {
                            return className + ".xaml.cs";
                        }
                        else
                        {
                            return className + ".cs";
                        }
                    }

                    //if (csClassMatch.Success)
                    //    return $"{csClassMatch.Groups[1].Value}.cs";
                    //else if (csNamespaceMatch.Success)
                    //    return $"{csNamespaceMatch.Groups[1].Value}.cs";
                    //else
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
        static string GetXamlClassName(string xamlCode)
        {
            var match = Regex.Match(xamlCode, @"x:Class=""([^""]+)""");
            if (match.Success)
            {
                string fullClassName = match.Groups[1].Value;
                string[] parts = fullClassName.Split('.');
                var fileName = parts.Length > 0 ? parts[^1] : fullClassName;  // クラス名部分のみ取得
                return fileName+".xaml";
            }
            return "MainWindow.xaml";
        }

    }
    public class FolderTreeItem : TreeViewItem
    {
        public FolderTreeItem(DirectoryInfo info)
        {
            Info = info;
            Header = info.Name;
        }

        public DirectoryInfo Info { get; }
    }
}
