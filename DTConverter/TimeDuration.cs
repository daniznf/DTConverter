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
    public enum DurationTypes { Seconds, MilliSeconds, MicroSeconds, Frames, HMS }
    public class TimeDuration: INotifyPropertyChanged
    {
        public TimeDuration()
        {
            Seconds = 0;
        }

        // HMS:      [-][HH:]MM:SS[.m...]
        // s,ms,us:  [-]S+[.m...][s|ms|us]
        private int H, M, S, d;
        
        /// <summary>
        /// Specifies if the duration is in frames or in a time format.
        /// This value is automatically set when setting time or frames number.
        /// </summary>
        public DurationTypes DurationType { get; private set; }

        private int _Frames;
        /// <summary>
        /// Sets the number of frames.
        /// Gets the number of frames if DurationType is Frames, otherwise gets 0
        /// </summary>
        public int Frames {
            get
            {
                if (DurationType == DurationTypes.Frames)
                {
                    return _Frames;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                _Frames = value;
                DurationType = DurationTypes.Frames;
                OnPropertyChanged("Frames");
            }
        }

        /// <summary>
        /// Calculates total number of seconds based on current Frames value and fps
        /// </summary>
        /// <param name="fps"></param>
        /// <returns></returns>
        public double GetSeconds(double fps)
        {
            if (fps > 0)
            {
                return _Frames / fps;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculates total number of frames based on current Seconds value and fps
        /// </summary>
        /// <param name="fps"></param>
        /// <returns></returns>
        public int GetFrames(double fps)
        {
            if (fps > 0)
            {
                return Convert.ToInt32(Seconds * fps);
            }
            else
            {
                return 0;
            }
        }

        

        /// <summary>
        /// Sets the total number of seconds.
        /// Gets the number of seconds if DurationType is not Frames, otherwise gets 0.
        /// </summary>
        public double Seconds 
        {
            get
            {
                if (!(DurationType == DurationTypes.Frames))
                {
                    return S + (M * 60) + (H * 3600) + (1.0 * d / 1000);
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                // 39036.123s
                double seconds = 1.0 * value;
                double minutes = 0;
                double hours = 0;
                
                if (seconds > 60)
                {
                    minutes = seconds / 60;
                    seconds -= 60 * Math.Truncate(minutes);
                    if (minutes > 60)
                    {
                        hours = minutes / 60;
                        minutes -= 60 * Math.Truncate(hours);
                    }
                }

                H = Convert.ToInt32(Math.Truncate(hours));
                M = Convert.ToInt32(Math.Truncate(minutes));
                S = Convert.ToInt32(Math.Truncate(seconds));
                d = Convert.ToInt32((seconds-Math.Truncate(seconds)) * 1000);

                DurationType = DurationTypes.Seconds;

                OnPropertyChanged("Seconds");
                OnPropertyChanged("MilliSeconds");
                OnPropertyChanged("MicroSeconds");
                OnPropertyChanged("HMS");
            }
        }

        public double Hours
        {
            get => Seconds / 60 / 60;
            set => Seconds = value * 60 * 60;
        }

        public double Minutes
        {
            get => Seconds / 60;
            set => Seconds = value * 60;
        }

        public double MilliSeconds
        {
            get => Seconds * 1000;
            set
            {
                Seconds = 1.0 * value / 1000;
                DurationType = DurationTypes.MilliSeconds;
            }
        }

        public double MicroSeconds
        {
            get => Seconds * 1000 * 1000;
            set
            {
                Seconds = 1.0 * value / 1000 / 1000;
                DurationType = DurationTypes.MicroSeconds;
            }
        }

        /// <summary>
        /// Gets a string of current Hours, Minutes, Seconds and decimals 
        /// Sets time from given string
        /// </summary>
        public string HMS
        {
            get
            {
                string outTime = "";

                if (H > 0)
                {
                    outTime = H.ToString() + ":";
                }
                outTime += M.ToString() + ":";
                
                outTime += S.ToString();
                
                if (d > 0)
                {
                    outTime += "." + d.ToString();
                }
                return outTime;
            }
            set
            {
                try
                {
                    //10:50:36.123
                    string[] splitted;
                    splitted = value.Split('.');
                    if (splitted.Length > 1)
                    {
                        string strms = splitted[1];
                        if (strms.Length == 1)
                        {
                            d = int.Parse(strms) * 100;
                        }
                        else if (strms.Length == 2)
                        {
                            d = int.Parse(strms) * 10;
                        }
                        else
                        {
                            d = int.Parse(strms);
                        }
                    }

                    splitted = splitted[0].Split(':');
                    if (splitted.Length == 3)
                    {
                        H = int.Parse(splitted[0]);
                        M = int.Parse(splitted[1]);
                        S = int.Parse(splitted[2]);
                    }
                    else if (splitted.Length == 2)
                    {
                        M = int.Parse(splitted[0]);
                        S = int.Parse(splitted[1]);
                    }
                    else if (splitted.Length == 1)
                    {
                        S = int.Parse(splitted[0]);
                    }

                    DurationType = DurationTypes.HMS;

                    OnPropertyChanged("Seconds");
                    OnPropertyChanged("MilliSeconds");
                    OnPropertyChanged("MicroSeconds");
                    OnPropertyChanged("HMS");
                }
                catch (Exception E)
                {
                    H = M = S = d = 0;
                }
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
