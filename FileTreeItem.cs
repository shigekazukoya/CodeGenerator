using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MessageBox = System.Windows.MessageBox;

namespace CodeGenerator
{
    public class FileTreeItem : TreeViewItem
    {
        public FileTreeItem(FileInfo info)
        {
            Info = info;
            Header = info.Name;

            MouseEnter += FileTreeItem_MouseEnter;
            MouseLeave += FileTreeItem_MouseLeave;

            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "エクスプローラーで表示" };
            menuItem.Click += (s, e) => OpenInExplorer();
            contextMenu.Items.Add(menuItem);

            var menuItem2 = new MenuItem { Header = "VSCodeで開く" };
            menuItem2.Click += (s, e) => OpenInVsCode();
            contextMenu.Items.Add(menuItem2);

            this.ContextMenu = contextMenu;
        }

        public FileInfo Info { get; }

        private void OpenInExplorer()
        {
            Process.Start("explorer.exe", $"/select,\"{Info.FullName}\"");
        }

        private void OpenInVsCode()
        {
            try
            {
                Process.Start("code", $"\"{Info.FullName}\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("エラーが発生しました: " + ex.Message);
            }
        }

        private void FileTreeItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            try
            {
                using (var stream = new FileStream(Info.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        char[] buffer = new char[5000];
                        int numRead = reader.Read(buffer, 0, buffer.Length);
                        string content = new string(buffer, 0, numRead);
                        this.ToolTip = new System.Windows.Controls.ToolTip { Content = content, MaxWidth = 400, MaxHeight = 300 };
                    }
                }
            }
            catch (Exception ex)
            {
                this.ToolTip = new System.Windows.Controls.ToolTip { Content = $"ファイルを読み込めません: {ex.Message}" };
            }
        }

        private void FileTreeItem_MouseLeave(object sender, MouseEventArgs e)
        {
            this.ToolTip = null;
        }
    }
}
