using System;
using System.Globalization;
using System.Windows.Data;

namespace iViewXExperimentCreator.Wpf.Converters
{
    /// <summary>
    /// Converter, der den Null-Zustand eines Objekts als Boolean konvertiert.
    /// </summary>
    public class IsNotNullConverter : IValueConverter
    {
        /// <summary>
        /// Gibt bei einem Null-Zustand false und andernfalls true zurück.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        /// <summary>
        /// Nicht implementiert. Nur vorhanden, um IValueConverter zu implementieren.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}