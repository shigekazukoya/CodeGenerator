using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;

namespace CodeGenerator
{
    public class FolderTreeItem : TreeViewItem
    {
        public FolderTreeItem(DirectoryInfo info)
        {
            Info = info;
            Header = info.Name;

            Expanded += Folder_Expanded;

            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem { Header = "エクスプローラーで表示" };
            menuItem.Click += (s, e) => OpenInExplorer();
            contextMenu.Items.Add(menuItem);

            var menuItem2 = new MenuItem { Header = "VSCodeで開く" };
            menuItem2.Click += (s, e) => OpenInVsCode();
            contextMenu.Items.Add(menuItem2);

            this.ContextMenu = contextMenu;
        }

        public DirectoryInfo Info { get; }

        private void OpenInExplorer()
        {
            Process.Start("explorer.exe", $"\"{Info.FullName}\"");
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

        private void Folder_Expanded(object sender, RoutedEventArgs e)
        {
            if (this.Items.Count == 1 && this.Items[0] is TreeViewItem)
            {
                this.Items.Clear();
                try
                {
                    foreach (var directory in Info.GetDirectories())
                    {
                        if ((directory.Attributes & FileAttributes.System) != FileAttributes.System)
                        {
                            var subItem = new FolderTreeItem(directory);
                            subItem.Items.Add(new TreeViewItem());
                            this.Items.Add(subItem);
                        }
                    }

                    foreach (var file in Info.GetFiles())
                    {
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
}
