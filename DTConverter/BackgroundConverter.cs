/*
    DT Converter - Daniele's Tools Video Converter    
    Copyright (C) 2022 Daniznf

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
    
    https://github.com/daniznf/DTConverter
 */

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
