using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DTConverter
{
    [ValueConversion(typeof(ConversionStatus), typeof(SolidBrush))]
    public class BackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ConversionStatus statusValue)
            {
                switch (statusValue)
                {
                    case ConversionStatus.CreatingPreviewIn:
                        return new SolidColorBrush(Colors.LightYellow);
                    case ConversionStatus.CreatingPreviewOut:
                        return new SolidColorBrush(Colors.LightYellow);
                    case ConversionStatus.Converting:
                        return new SolidColorBrush(Colors.Orange);
                    case ConversionStatus.Success:
                        return new SolidColorBrush(Colors.LightGreen);
                    case ConversionStatus.Failed:
                        return new SolidColorBrush(Colors.OrangeRed);
                }
            }

            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
