using System.Windows;
using System.Windows.Controls;

namespace CodeGenerator
{
    public partial class DiffWindow : Window
    {
        private readonly MainWindow window;

        public string PreviousContent { get; set; }
        public string LatestContent { get; set; }

        public List<CodeVersion>  Before { get; set; } = new List<CodeVersion>();
        public List<CodeVersion> After { get; set; } = new List<CodeVersion>();

        public DiffWindow(MainWindow window)
        {
            InitializeComponent();
            var selectedItem = (CodeVersion)window.VersionComboBox.SelectedItem;
            var index = window.CodeVersions.IndexOf(selectedItem);
            var previous =  (index == 0)? selectedItem : window.CodeVersions[(index - 1)];
            foreach ( var codeVersion in window.CodeVersions)
            {
                Before.Add(codeVersion);
                After.Add(codeVersion);
            }

            this.window = window;
            DataContext = this;

            BeforeCombo.SelectedItem = previous;
            AfterCombo.SelectedItem = selectedItem;
        }

        private void BeforeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Update();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DiffView.NextDiff();
        }

        public void Update()
        {
            if(this.BeforeCombo.SelectedItem is CodeVersion codeversion)
            {
                PreviousContent = codeversion.Content;
            }

            if(this.AfterCombo.SelectedItem is CodeVersion after)
            {
                LatestContent = after.Content;
            }

            DiffView.OldText = PreviousContent;
            DiffView.NewText = LatestContent;
            DiffView.NextDiff();
        }

        private void AfterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Update();
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            foreach (var codeVersion in this.window.CodeVersions)
            {
                if (!Before.Contains(codeVersion))
                {
                    Before.Add(codeVersion);
                }
                
                if (!After.Contains(codeVersion))
                {
                    After.Add(codeVersion);
                }

            }

        }
    }
}
