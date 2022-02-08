using System;
using System.Globalization;
using System.Windows.Data;
using WindowsColor = System.Windows.Media.Color;
using XPlatformColor = System.Drawing.Color;
using System.Windows.Media;

namespace iViewXExperimentCreator.Wpf.Converters
{
    /// <summary>
    /// Converter zwischen der plattformunabhängigen System.Drawing.Color und dem windowsspezifischen SolidColorBrush.
    /// </summary>
    public class BrushConverter : IValueConverter
    {
        /// <summary>
        /// Konversion von System.Drawing.Color zu System.Windows.Media.SolidColorBrush.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte r = 0, g = 0, b = 0, a = 0;

            if (value is XPlatformColor colorSYS)
            {
                r = colorSYS.R;
                g = colorSYS.G;
                b = colorSYS.B;
                a = colorSYS.A;
            }

            SolidColorBrush brush = new(new WindowsColor { A = a, R = r, G = g, B = b });
            return brush;
        }

        /// <summary>
        /// Konversion von System.Windows.Media.SolidColorBrush zu System.Drawing.Color.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            SolidColorBrush brush = new((WindowsColor)value);
            return XPlatformColor.FromArgb(brush.Color.A, brush.Color.R, brush.Color.G, brush.Color.B);
        }
    }
}