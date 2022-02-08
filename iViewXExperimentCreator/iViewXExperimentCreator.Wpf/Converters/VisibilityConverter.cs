using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using iViewXExperimentCreator.Core.Enums;

namespace iViewXExperimentCreator.Wpf.Converters
{
    /// <summary>
    /// Konvertiert die plattformunabhängige Visibility-Enum zu für WPF verständlichen Visibility-Enums.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert plattformunabhängige AgnosticVisibility, welche im Core-Projekt definiert wurde, zu System.Windows.Visbility.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            AgnosticVisibility visibility = (AgnosticVisibility)value;
            switch (visibility)
            {
                case AgnosticVisibility.Hidden:
                    return Visibility.Hidden;
                case AgnosticVisibility.Collapsed:
                    return Visibility.Collapsed;
                default:
                    return Visibility.Visible;
            }
        }

        /// <summary>
        /// Konvertiert System.Windows.Visbility zu plattformunabhängiger AgnosticVisibility, welche im Core-Projekt definiert wurde.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility visibility = (Visibility)value;
            switch (visibility)
            {
                case Visibility.Hidden:
                    return AgnosticVisibility.Hidden;
                case Visibility.Collapsed:
                    return AgnosticVisibility.Collapsed;
                default: 
                    return AgnosticVisibility.Visible;
            }
        }
    }
}
