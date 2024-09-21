using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string API_URL = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
        public string RootFolder { get; set; }
        private string ApiKey;
        private string apiKeyFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apikey.dat");

        public MainWindow()
        {
            InitializeComponent();
            LoadApiKey();
            InitializeLanguageComboBox();
            RootFolder = "C:\\";
            this.DataContext = this;
            UpdateFolderTreeView();
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await PromptTextArea.EnsureCoreWebView2Async(null);

            // ローカルで静的ファイルをホストする場合
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            var htmlPath = Path.Combine(exePath, "Editor\\monaco\\dist\\index.html");

            PromptTextArea.CoreWebView2.SetVirtualHostNameToFolderMapping(
    "appassets",
    new FileInfo(htmlPath).DirectoryName,
    Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
            PromptTextArea.CoreWebView2.Navigate("http://appassets/index.html");
        }


        private void LoadApiKey()
        {
            if (File.Exists(apiKeyFilePath))
            {
                try
                {
                    byte[] encryptedApiKey = File.ReadAllBytes(apiKeyFilePath);
                    byte[] apiKeyBytes = ProtectedData.Unprotect(encryptedApiKey, null, DataProtectionScope.CurrentUser);
                    string apiKey = Encoding.UTF8.GetString(apiKeyBytes);
                    this.ApiKey = apiKey;
                }
                catch (Exception ex)
                {
                    this.ApiKey = null;
                    StatusTextBlock.Text = $"APIキーの読み込みに失敗しました: {ex.Message}";
                }
            }
            else
            {
                this.ApiKey = null;
            }
        }
        private void SaveApiKey(string apiKey)
        {
            byte[] apiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
            byte[] encryptedApiKey = ProtectedData.Protect(apiKeyBytes, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(apiKeyFilePath, encryptedApiKey);
        }

        private void MenuItem_InputApiKey_Click(object sender, RoutedEventArgs e)
        {
            var apiKeyWindow = new ApiKeyInputWindow();
            apiKeyWindow.Owner = this;
            if (apiKeyWindow.ShowDialog() == true)
            {
                var apiKey = apiKeyWindow.ApiKey;
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    SaveApiKey(apiKey);
                    LoadApiKey();
                    StatusTextBlock.Text = "APIキーを保存しました。";
                }
                else
                {
                    StatusTextBlock.Text = "APIキーが空です。";
                }
            }
        }

        private void SelectRootFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "ルートフォルダを選択"
            };

            if (dialog.ShowDialog() == true)
            {
                RootFolder = dialog.FolderName;
                UpdateFolderTreeView();
            }
        }

        private void UpdateFolderTreeView()
        {
            FolderTreeView.Items.Clear();
            var rootItem = new FolderTreeItem(new DirectoryInfo(RootFolder));
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
            string prompt = await GetTextAsync();
            string selectedLanguage = LanguageComboBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("プロンプトを入力してください。");
                return;
            }

            try
            {
                string generatedContent = await GenerateCodeWithGemini(prompt, selectedLanguage, FilePathTextBox.Text);
                ResultTextBox.Text = generatedContent;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}");
            }
        }

        public async void SetText(string text)
        {
            await PromptTextArea.CoreWebView2.ExecuteScriptAsync($"setText(`{text.Replace("`", "\\`")}`)");
        }

        // テキストを取得するメソッド
        public async Task<string> GetTextAsync()
        {
            // JavaScriptの関数を実行
            var resut = await PromptTextArea.CoreWebView2.ExecuteScriptAsync("window.getText()");

            // 非同期で結果を待つ
            return JsonConvert.DeserializeObject<string>(resut);
        }

        private async Task<string> GenerateCodeWithGemini(string prompt, string language, string filePath = null)
        {
            using (var client = new HttpClient())
            {
                string fileContent = null;
                if (!string.IsNullOrEmpty(filePath))
                {
                    if (File.Exists(filePath))
                    {
                        fileContent = File.ReadAllText(filePath);
                    }
                }

                string newPrompt;

                if (!string.IsNullOrEmpty(fileContent))
                {
                    newPrompt = $"Based on the following {language} file content, generate the output. For each code block, include the filename in the code block as shown below:\n\n```langualge:filename.extension\ncode\n```\n\nFile content:\n{fileContent}\n\nAdditional prompt:\n{prompt}";
                }
                else
                {
                    newPrompt = $"Generate {language} code for the following prompt. For each code block, include the filename in the code block as shown below:\n\n```langualge:filename.extension\ncode\n```\n\nPrompt:\n{prompt}";
                }

                var request = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[]
                    {
                        new { text = newPrompt }
                    }
                }
            }
                };

                var json = JsonConvert.SerializeObject(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                client.DefaultRequestHeaders.Add("x-goog-api-key", ApiKey);

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

        private void SaveGeneratedContent(string generatedContent, string outputFolder)
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
                // ファイル名が指定されていない場合はデフォルトの名前を設定
                filenameWithPath = DetermineFilename(language, code);
            }

            // ファイルパスを結合して正規化
            string filePath = Path.GetFullPath(Path.Combine(outputFolder, filenameWithPath));

            // セキュリティ対策：filePath が outputFolder のサブディレクトリか確認
            if (!filePath.StartsWith(Path.GetFullPath(outputFolder), StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("無効なファイルパスが検出されました。");
            }

            // ディレクトリが存在しない場合は作成
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // ファイルに書き込み
            File.WriteAllText(filePath, code);
        }
    }
    else
    {
        // コードブロックがない場合、全体をマークダウンファイルとして保存
        string filePath = Path.Combine(outputFolder, "generated_content.md");
        File.WriteAllText(filePath, generatedContent);
    }

    MessageBox.Show($"ファイルが {outputFolder} に保存されました。");
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
                return fileName + ".xaml";
            }
            return "MainWindow.xaml";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateFolderTreeView();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // ダイアログを表示し、結果を確認
            if (openFileDialog.ShowDialog() == true)
            {
                // 選択されたファイルのパスを取得
                string filePath = openFileDialog.FileName;
                FilePathTextBox.Text = filePath;
            }
        }
    }
    public class FolderTreeItem : TreeViewItem
    {
        public FolderTreeItem(DirectoryInfo info)
        {
            Info = info;
            Header = info.Name;

            Expanded += Folder_Expanded;

            // コンテキストメニュー
            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "エクスプローラーで表示" };
            menuItem.Click += (s, e) => OpenInExplorer();
            contextMenu.Items.Add(menuItem);
            this.ContextMenu = contextMenu;
        }

        public DirectoryInfo Info { get; }

        private void OpenInExplorer()
        {
            Process.Start("explorer.exe", $"\"{Info.FullName}\"");
        }

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            if (this.Items.Count == 1 && this.Items[0] is TreeViewItem dummyItem && dummyItem.Tag == null)
            {
                this.Items.Clear();
                try
                {
                    // サブフォルダの追加
                    foreach (var directory in Info.GetDirectories())
                    {
                        // システムフォルダを除外
                        if ((directory.Attributes & FileAttributes.System) != FileAttributes.System)
                        {
                            var subItem = new FolderTreeItem(directory);
                            subItem.Items.Add(new TreeViewItem());
                            this.Items.Add(subItem);
                        }
                    }

                    // ファイルの追加
                    foreach (var file in Info.GetFiles())
                    {
                        // システムファイルを除外
                        if ((file.Attributes & FileAttributes.System) != FileAttributes.System)
                        {
                            var fileItem = new FileTreeItem(file);
                            this.Items.Add(fileItem);
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // アクセス拒否
                }
            }
        }
    }

    public class FileTreeItem : TreeViewItem
    {
        public FileTreeItem(FileInfo info)
        {
            Info = info;
            Header = info.Name;

            MouseEnter += FileTreeItem_MouseEnter;
            MouseLeave += FileTreeItem_MouseLeave;

            // コンテキストメニュー
            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "エクスプローラーで表示" };
            menuItem.Click += (s, e) => OpenInExplorer();
            contextMenu.Items.Add(menuItem);
            this.ContextMenu = contextMenu;
        }

        public FileInfo Info { get; }

        private void OpenInExplorer()
        {
            Process.Start("explorer.exe", $"/select,\"{Info.FullName}\"");
        }

        private void FileTreeItem_MouseEnter(object sender, MouseEventArgs e)
        {
            try
            {
                using (var stream = new FileStream(Info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        char[] buffer = new char[5000]; // 最大5000文字読み込み
                        int numRead = reader.Read(buffer, 0, buffer.Length);
                        string content = new string(buffer, 0, numRead);
                        this.ToolTip = new ToolTip { Content = content, MaxWidth = 400, MaxHeight = 300 };
                    }
                }
            }
            catch (Exception ex)
            {
                this.ToolTip = new ToolTip { Content = $"ファイルを読み込めません: {ex.Message}" };
            }
        }

        private void FileTreeItem_MouseLeave(object sender, MouseEventArgs e)
        {
            this.ToolTip = null;
        }
    }
}
