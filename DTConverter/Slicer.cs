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

using System.ComponentModel;
using System.IO;

namespace DTConverter
{
    public class Slicer: INotifyPropertyChanged
    {
        public Slicer()
        {
            _HorizontalNumber = 1;
            _VerticalNumber = 1;
        }

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

        private int _HorizontalNumber;
        public int HorizontalNumber
        {
            get => _HorizontalNumber;
            set
            {
                _HorizontalNumber = value;
                OnPropertyChanged("HorizontalNumber");
            }
        }

        private int _VerticalNumber;
        public int VerticalNumber
        {
            get => _VerticalNumber;
            set
            {
                _VerticalNumber = value;
                OnPropertyChanged("VerticalNumber");
            }
        }

        private int _HorizontalOverlap;
        public int HorizontalOverlap
        {
            get => _HorizontalOverlap;
            set
            {
                _HorizontalOverlap = value;
                OnPropertyChanged("HorizontalOverlap");
            }
        }

        private int _VerticalOverlap;
        public int VerticalOverlap
        {
            get => _VerticalOverlap;
            set
            {
                _VerticalOverlap = value;
                OnPropertyChanged("VerticalOverlap");
            }
        }
        
        public int GetSliceWidth(int horizontalResolution)
        {
            return horizontalResolution / HorizontalNumber + (HorizontalOverlap / 2);
        }

        public int GetSliceHeight(int verticalResolution)
        {
            return verticalResolution / VerticalNumber + (VerticalOverlap / 2);
        }

        /// <summary>
        /// Generates the name for given path, adding r and c in a standard way.
        /// Every method that tries to access an r x c image should call this method.
        /// </summary>
        public static string GetSliceName(string originalName, int r, int c)
        {
            return Path.Combine(Path.GetDirectoryName(originalName), Path.GetFileNameWithoutExtension(originalName)) + $"_r{r}c{c}" + Path.GetExtension(originalName);
        }

        /// <summary>
        /// Returns a new clone of this object
        /// </summary>
        public Slicer Clone()
        {
            return new Slicer()
            {
                IsEnabled = this.IsEnabled,
                HorizontalNumber = this.HorizontalNumber,
                VerticalNumber = this.VerticalNumber,
                HorizontalOverlap = this.HorizontalOverlap,
                VerticalOverlap = this.VerticalOverlap,
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
