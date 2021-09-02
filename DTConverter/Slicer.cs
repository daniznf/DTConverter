﻿/*
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
using System.IO;

namespace DTConverter
{
    public class Slicer: INotifyPropertyChanged
    {
        public Slicer()
        {
            _HorizontalNumber = 1;
            _VerticalNumber = 1;
            _HorizontalOverlap = 0;
            _VerticalOverlap = 0;
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

        public static string GetSliceName(string originalName, int r, int c)
        {
            return Path.Combine(Path.GetDirectoryName(originalName), Path.GetFileNameWithoutExtension(originalName)) + $"_r{r}c{c}" + Path.GetExtension(originalName);
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