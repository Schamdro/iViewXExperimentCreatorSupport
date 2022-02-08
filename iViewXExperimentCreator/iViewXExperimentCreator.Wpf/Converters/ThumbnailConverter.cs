using iViewXExperimentCreator.Core;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace iViewXExperimentCreator.Wpf.Converters
{
    /// <summary>
    /// Converter, der Thumbnails aus System.Drawing.Image extrahiert und diese dann durch die ImageConverter-Klasse von konvertiert.
    /// </summary>
    public class ThumbnailConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert Bildpfade oder System.Drawing.Image in Thumbnails und konvertiert diese dann per ImageConverter zu BitmapImages.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string sourcePath)
            {
                try
                {
                    value = Image.FromFile(sourcePath);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Konnte Thumbnail nicht laden.");
                    value = new Bitmap(1, 1);
                }
            }
            if (value is Image image)
            {
                if (image is null) return null;

                value = image.GetThumbnailImage(100, 100, () => false, IntPtr.Zero);
            }

            ImageConverter imgConv = new();
            return imgConv.Convert(value, targetType, parameter, culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
