using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CodeGenerator
{
    public static class Commands
    {
        public static ICommand AddFileCommand { get; set; } = new RoutedCommand(nameof(AddFileCommand), typeof(MainWindow));
    }
}
