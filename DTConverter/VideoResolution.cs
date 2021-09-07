/*
    DT Converter - Dani's Tools Video Converter    
    Copyright (C) 2021 Daniznf

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
using System.ComponentModel;

namespace DTConverter
{
    public class VideoResolution : INotifyPropertyChanged
    {
        private bool _IsEnabled;
        public bool IsEnabled
        {
            get => _IsEnabled;
            set
            {
                _IsEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }

        private int _Horizontal;
        public int Horizontal
        {
            get =>_Horizontal;
            set
            {
                _Horizontal = AdjustMultiple(value, _Multiple);
                OnPropertyChanged("Horizontal");
            }
        }
        private int _Vertical;
        public int Vertical
        {
            get => _Vertical;
            set
            {
                _Vertical = AdjustMultiple(value, _Multiple);
                OnPropertyChanged("Vertical");
            }
        }

        private int _Multiple;
        public int Multiple
        {
            get => _Multiple;
            set
            {
                _Multiple = value;
                OnPropertyChanged("Multiple");
            }
        }

        /// <summary>
        /// Calculates aspect ratio of current resolution (like 1.7777, 1.25...)
        /// </summary>
        /// <returns></returns>
        public double AspectRatio()
        {
            if (Vertical > 0)
            {
                return Horizontal / Vertical;
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Calculates aspect ratio of current resolution in nth (like 16/9, 4/3, ...)
        /// </summary>
        /// <param name="denominator"></param>
        /// <returns></returns>
        public double AspectRatio(int denominator)
        {
            if ((Vertical > 0) && (denominator > 0))
            {
                return Horizontal / Vertical * denominator;
            }
            else
            {
                return 0;
            }

        }

        /// <summary>
        /// Returns the nearest number to res, multiple of m
        /// </summary>
        /// <param name="res"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static int AdjustMultiple(int res, int m)
        {
            if (m != 0)
            {
                if (res % m == 0)
                {
                    return res;
                }
                else
                {
                    return Convert.ToInt32(Math.Round(1.0 * res / m) * m);
                }
            }
            else
            {
                return res;
            }
        }

        // This implements INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string info)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
