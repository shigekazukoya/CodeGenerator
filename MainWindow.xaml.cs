using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CodeGenerator
{
    public partial class MainWindow : Window
    {
        public string RootFolder { get; set; }
        private string ApiKey;
        private string apiKeyFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "apikey.dat");
        private ApiService apiService;
        private CodeFileSaver codeFileSaver;

        public MainWindow()
        {
            InitializeComponent();
            LoadApiKey();
            apiService = new ApiService(ApiKey);
            codeFileSaver = new CodeFileSaver();
            InitializeLanguageComboBox();
            RootFolder = "C:\\";
            this.DataContext = this;
            UpdateFolderTreeView();
            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await PromptTextArea.EnsureCoreWebView2Async(null);
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
            rootItem.Items.Add(new TreeViewItem());
            rootItem.IsSelected = true;
            FolderTreeView.Items.Add(rootItem);
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
                StatusTextBlock.Text = "プロンプトを入力してください。";
                return;
            }

            try
            {
                string fileContent = null;
                if (!string.IsNullOrEmpty(FilePathTextBox.Text))
                {
                    if (File.Exists(FilePathTextBox.Text))
                    {
                        fileContent = File.ReadAllText(FilePathTextBox.Text);
                    }
                }

                string generatedContent = await apiService.GenerateCodeWithGemini(prompt, selectedLanguage, fileContent);
                ResultTextBox.Text = generatedContent;
                StatusTextBlock.Text = $"コード生成完了";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"エラーが発生しました: {ex.Message}";
            }
        }

        public async Task<string> GetTextAsync()
        {
            var result = await PromptTextArea.CoreWebView2.ExecuteScriptAsync("window.getText()");
            return JsonConvert.DeserializeObject<string>(result);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = FolderTreeView.SelectedItem as FolderTreeItem;
            if (selectedItem == null)
            {
                StatusTextBlock.Text = "保存先フォルダを選択してください。";
                return;
            }

            string outputFolder = selectedItem.Info.FullName;
            string generatedContent = ResultTextBox.Text;

            if (string.IsNullOrWhiteSpace(generatedContent))
            {
                StatusTextBlock.Text = "保存するコンテンツがありません。";
                return;
            }

            try
            {
                codeFileSaver.SaveGeneratedContent(generatedContent, outputFolder);
                StatusTextBlock.Text = $"ファイルが保存されました。";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"保存中にエラーが発生しました: {ex.Message}";
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UpdateFolderTreeView();
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                FilePathTextBox.Text = filePath;
            }
        }
    }
}
