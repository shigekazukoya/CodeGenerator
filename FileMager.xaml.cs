using ICSharpCode.AvalonEdit.Highlighting;
using System.IO;
using System.Windows;

namespace CodeGenerator
{
    /// <summary>
    /// FileMager.xaml の相互作用ロジック
    /// </summary>
    public partial class FileMager : Window
    {
        public string MergedText { get; set; }

        public FileMager(string before, string after, string filePath)
        {
            InitializeComponent();
            this.FilePath.Text = filePath;

            DiffView.OldText = before;
            DiffView.NewText = after;
            var extensiton = Path.GetExtension(filePath);
            ResultTextEditor.Text = after;
            //ResultTextEditor.SetLanguageAsync(extensiton);
            //ResultTextEditor.SetTextAsync(after);
        }


        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // マージ処理を実装
            //MergedText =  await ResultTextEditor.GetTextAsync();
            MergedText = ResultTextEditor.Text;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DiffView.NextDiff();
        }
    }
}
