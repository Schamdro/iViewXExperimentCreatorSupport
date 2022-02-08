using iViewXExperimentCreator.Core;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace iViewXExperimentCreator.Wpf.Converters
{
    /// <summary>
    /// Converter zwischen der plattformunabhängigen System.Drawing.Image und dem windowsspezifischen System.Windows.Media.Imaging.BitmapImage.
    /// </summary>
    public class ImageConverter : IValueConverter
    {
        /// <summary>
        /// Konvertiert Image-Daten in BitmapImage-/ImageSource-Daten. 
        /// 
        /// Dies ist notwendig, um Plattformunabhängigkeit des Core-Projektes sicherzustellen, da BitmapImage ein Windows-
        /// Datentyp ist und direktes Binding auf Image nicht funktioniert.
        /// 
        /// Dies passiert über einen Memory-Stream und nicht über einen Cache-Ordner auf der Festplatte, da es unerklärte 
        /// Caching-Probleme mit dem direkten Speichern gab (nichtreferenzierte Images konnten trotz Image.Dispose() nicht 
        /// sofort gelöscht werden, hat einige ms gedauert bis sie frei waren - manchmal ging Dispose() auch einfach gar nicht). 
        /// 
        /// Direktes Binding auf die Image-Pfade der Snapshots ist deshalb nicht möglich und sollte auch nicht mehr
        /// implementiert werden, da die momentane Lösung ebenfalls gut funktioniert. Falls die Image-Daten auf der Festplatte
        /// gebraucht werden, können die Snapshots in SnapshotModel bei der Erstellung des Bitmaps gespeichert werden.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage bmi = new();

            if (value is string sourcePath)
            {
                value = Image.FromFile(sourcePath);
            }
            if (value is Image image)
            {
                if (image is null) return null;

                using MemoryStream ms = new();
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                bmi.BeginInit();
                bmi.StreamSource = ms;
                bmi.CacheOption = BitmapCacheOption.OnLoad;
                bmi.EndInit();
                return bmi;
            }
            return bmi;
        }

        /// <summary>
        /// Diese Funktion konvertiert nur ImageSources in Strings zurück.
        /// 
        /// Normalerweise sollte lediglich BitmapFrameDecode hier ankommen.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is ImageSource imgsrc)
            {
                return imgsrc;
            }
            Logger.Error(new ArgumentException(), $"ImageConverter.ConvertBack kann keinen Typ {value.GetType()} behandeln.");
            return null;
        }
    }
}
