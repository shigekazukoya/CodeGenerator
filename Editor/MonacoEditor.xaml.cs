using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CodeGenerator.Editor
{
    /// <summary>
    /// MonacoEditor.xaml の相互作用ロジック
    /// </summary>
    public partial class MonacoEditor : System.Windows.Controls.UserControl
    {
        public MonacoEditor()
        {
            InitializeComponent();
            InitializeAsync();

        }
        async void InitializeAsync()
        {
            await Editor.EnsureCoreWebView2Async(null);
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            var htmlPath = System.IO.Path.Combine(exePath, "Editor\\monaco\\dist\\index.html");

            Editor.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "appassets",
                new FileInfo(htmlPath).DirectoryName,
                Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
            Editor.CoreWebView2.Navigate("http://appassets/index.html");
        }

        public async Task<string> GetTextAsync()
        {
            var result = await Editor.CoreWebView2.ExecuteScriptAsync("window.getText()");
            return JsonConvert.DeserializeObject<string>(result);
        }

        public async Task SetTextAsync(string text)
        {
            await Editor.CoreWebView2.ExecuteScriptAsync($"setText(`{text.Replace("`", "\\`")}`)");
        }
    }
}
