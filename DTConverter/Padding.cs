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

using System.ComponentModel;

namespace DTConverter
{
    public class Padding: INotifyPropertyChanged
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

        private int _Top;
        public int Top
        {
            get => _Top;
            set
            {
                _Top = value;
                OnPropertyChanged("Top");
            }
        }

        private int _Bottom;
        public int Bottom
        {
            get => _Bottom;
            set
            {
                _Bottom = value;
                OnPropertyChanged("Bottom");
            }
        }

        private int _Left;
        public int Left
        {
            get => _Left;
            set
            {
                _Left = value;
                OnPropertyChanged("Left");
            }
        }

        private int _Right;
        public int Right
        {
            get => _Right;
            set
            {
                _Right = value;
                OnPropertyChanged("Right");
            }
        }

        public int X() => _Left;

        public int Y() =>_Top;
        
        /// <summary>
        /// Calculates padded video width
        /// </summary>
        /// <param name="inWidth">Original video width</param>
        /// <returns></returns>
        public int OutWidth(int inWidth)
        {
            return inWidth + Left + Right;
        }

        /// <summary>
        /// Calculates padded video height
        /// </summary>
        /// <param name="inHeight">Original video height</param>
        /// <returns></returns>
        public int OutHeight(int inHeight)
        {
            return inHeight + Top + Bottom;
        }

        /// <summary>
        /// Returns a new clone of this object
        /// </summary>
        public Padding Clone()
        {
            return new Padding()
            {
                IsEnabled = this.IsEnabled,
                Left = this.Left,
                Right = this.Right,
                Top = this.Top,
                Bottom = this.Bottom
            };
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
