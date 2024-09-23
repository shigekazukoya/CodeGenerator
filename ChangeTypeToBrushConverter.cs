using DiffPlex.DiffBuilder.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CodeGenerator
{
public class ChangeTypeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ChangeType type)
            {
                switch (type)
                {
                    case ChangeType.Deleted:
                        return Brushes.LightCoral;
                    case ChangeType.Inserted:
                        return Brushes.LightGreen;
                    case ChangeType.Modified:
                        return Brushes.LightBlue;
                    default:
                        return Brushes.Transparent;
                }
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
