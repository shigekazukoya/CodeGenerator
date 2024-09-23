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
            ResultTextEditor.Text = after;
            var extensiton = Path.GetExtension(filePath);
            ResultTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition(extensiton);
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // マージ処理を実装
            MergedText = ResultTextEditor.Text;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
