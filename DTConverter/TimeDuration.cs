﻿/*
    DT Converter - Daniele's Tools Video Converter    
    Copyright (C) 2024 Daniznf

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
    public class TimeDuration : INotifyPropertyChanged
    {
        public TimeDuration()
        { }

        // HMS:      [-][HH:]MM:SS[.m...]
        // s,ms,us:  [-]S+[.m...][s|ms|us]
        private int H, M, S, ms, us;

        /// <summary>
        /// Specifies if the duration is in frames or in a time format.
        /// This value is automatically set when setting time or frames number.
        /// </summary>
        public DurationTypes DurationType { get; private set; }

        private double _Framerate;
        /// <summary>
        /// This is used when converting Seconds to Frames and viceversa
        /// </summary>
        public double Framerate
        {
            get =>  _Framerate;
            set
            {
                _Framerate = value;
                OnPropertyChanged("Framerate");
            }
        }

        private int _Frames;
        /// <summary>
        /// Gets / sets the number of frames if DurationType is Frames.
        /// Otherwise, if Framerate is set, gets the converted seconds based on frames.
        /// </summary>
        public int Frames
        {
            get => DurationType == DurationTypes.Frames ? _Frames : GetFrames(Seconds, Framerate) ;
            set
            {
                _Frames = value;
                DurationType = DurationTypes.Frames;
                OnPropertyChanged("Frames");
            }
        }

        /// <summary>
        /// Calculates total number of seconds based on passed parameters
        /// </summary>
        /// <param name="frames">Total number of frames</param>
        /// <param name="fps">Framerate of passed frames</param>
        /// <returns>Total number of seconds</returns>
        public static double GetSeconds(int frames, double fps)
        {
            if (fps > 0)
            {
                return frames / fps;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Calculates total number of frames based on passed parameters
        /// </summary>
        /// <param name="seconds">Total number of seconds</param>
        /// <param name="fps">Framerate of passed seconds</param>
        /// <returns>Total number of frames</returns>
        public static int GetFrames(double seconds, double fps)
        {
            if (fps > 0)
            {
                return Convert.ToInt32(seconds * fps);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets / sets the total number of seconds if DurationType is not Frames. It may be more than 60.
        /// Otherwise, if Framerate is set, gets the converted seconds based on frames.
        /// </summary>
        public double Seconds
        {
            get
            {
                return DurationType != DurationTypes.Frames ?
                    S + (M * 60) + (H * 3600) + (ms / 1000.0) + (us / 1000.0 / 1000) :
                    GetSeconds(Frames, Framerate);
            }
            set
            {
                // 39036.123s
                double seconds = 1.0 * value;
                double minutes = 0;
                double hours = 0;
                double msDouble = 0;
                
                if (seconds >= 60)
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

                msDouble = (seconds - Math.Truncate(seconds)) * 1000;
                // 10.51 - 10 = 0.50999999999999979
                msDouble = Math.Round(msDouble, 3);
                ms = Convert.ToInt32(Math.Truncate(msDouble));
                us = Convert.ToInt32((msDouble - Math.Truncate(msDouble)) * 1000);

                DurationType = DurationTypes.Seconds;

                OnPropertyChanged("Seconds");
                OnPropertyChanged("MilliSeconds");
                OnPropertyChanged("MicroSeconds");
                OnPropertyChanged("HMS");
            }
        }

        /// <summary>
        /// Gets/sets total number of Hours, may be more than 24
        /// </summary>
        public double Hours
        {
            get => Seconds / 60 / 60;
            set => Seconds = value * 60 * 60;
        }

        /// <summary>
        /// Gets/sets total number of Minutes, may be more than 60
        /// </summary>
        public double Minutes
        {
            get => Seconds / 60;
            set => Seconds = value * 60;
        }

        /// <summary>
        /// Gets/sets total number of MilliSeconds, may be more than 1000
        /// </summary>
        public double MilliSeconds
        {
            get => Seconds * 1000;
            set
            {
                Seconds = value / 1000.0;
                DurationType = DurationTypes.MilliSeconds;
            }
        }

        /// <summary>
        /// Gets/sets total number of MicroSeconds, may be more than 1000000
        /// </summary>
        public double MicroSeconds
        {
            get => Seconds * 1000 * 1000;
            set
            {
                Seconds = value / 1000.0 / 1000.0;
                DurationType = DurationTypes.MicroSeconds;
            }
        }

        /// <summary>
        /// Gets a string of current Hours:Minutes:Seconds.MilliSecondsMicroSeconds, or frames.
        /// Sets time from given H:M:S.dddddd string, or frames string ending in f, or seconds string ending in s
        /// </summary>
        public string HMS
        {
            get
            {
                string outTime = "";

                if (DurationType == DurationTypes.Frames)
                {
                    outTime = Frames.ToString() + "f";
                }
                else
                {
                    if (H > 0)
                    {
                        outTime = H.ToString("00") + ":";
                    }
                    outTime += M.ToString("00") + ":";

                    outTime += S.ToString("00");

                    if (ms > 0)
                    {
                        outTime += "." + ms.ToString("000");
                    }

                    if (us > 0)
                    {
                        outTime += us.ToString("000");
                    }
                }
                return outTime;
            }
            set
            {
                try
                {
                    if (value.Contains("f"))
                    {
                        int tryFrames;
                        if (int.TryParse(value.Remove(value.IndexOf("f")), out tryFrames))
                        {
                            Frames = tryFrames;
                        }
                    }
                    else if (value.Contains("s"))
                    {
                        int tryFrames;
                        if (int.TryParse(value.Remove(value.IndexOf("s")), out tryFrames))
                        {
                            Seconds = tryFrames;
                        }
                    }
                    else
                    {
                        //10:50:36.123
                        string[] splitted;
                        splitted = value.Split('.');
                        if (splitted.Length > 1)
                        {
                            string strms = splitted[1];
                            switch (strms.Length)
                            {
                                case 1:
                                    strms += "00000";
                                    break;
                                case 2:
                                    strms += "0000";
                                    break;
                                case 3:
                                    strms += "000";
                                    break;
                                case 4:
                                    strms += "00";
                                    break;
                                case 5:
                                    strms += "0";
                                    break;
                                case 6:
                                    break;
                            }
                            ms = Convert.ToInt32(Math.Truncate(Convert.ToInt32(strms) / 1000.0));
                            us = Convert.ToInt32(Math.Truncate(Convert.ToInt32(strms) % 1000.0));
                        }
                        else
                        {
                            ms = 0;
                            us = 0;
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
                            H = 0;
                            M = int.Parse(splitted[0]);
                            S = int.Parse(splitted[1]);
                        }
                        else if (splitted.Length == 1)
                        {
                            H = 0;
                            M = 0;
                            S = int.Parse(splitted[0]);
                        }

                        DurationType = DurationTypes.HMS;

                        OnPropertyChanged("Seconds");
                        OnPropertyChanged("MilliSeconds");
                        OnPropertyChanged("MicroSeconds");
                        OnPropertyChanged("HMS");
                    }
                }
                catch (Exception E)
                {
                    H = M = S = ms = us = 0;
                }
            }
        }

        #region Operator overloads
        /// <summary>
        /// Sums frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator +(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? B : A;
            }
            
            return A.DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = A.Frames + B.Frames, Framerate = A.Framerate } :
                new TimeDuration() { Seconds = A.Seconds + B.Seconds, Framerate = A.Framerate };
        }

        /// <summary>
        /// Subtracts frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator -(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? B : A;
            }

            return A.DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = A.Frames - B.Frames, Framerate = A.Framerate } :
                new TimeDuration() { Seconds = A.Seconds - B.Seconds, Framerate = A.Framerate };
        }

        /// <summary>
        /// Multiplies frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator *(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? B : A;
            }

            return A.DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = A.Frames * B.Frames, Framerate = A.Framerate } :
                new TimeDuration() { Seconds = A.Seconds * B.Seconds, Framerate = A.Framerate };
        }

        /// <summary>
        /// Divides frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator /(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? B : A;
            }

            if (A.DurationType == DurationTypes.Frames)
            {
                return B.Frames == 0 ?
                    new TimeDuration() { Frames = 0 } :
                    new TimeDuration() { Frames = A.Frames / B.Frames, Framerate = A.Framerate };
            }
            else
            {
                return B.Seconds == 0 ?
                    new TimeDuration() { Seconds = 0 } :
                    new TimeDuration() { Seconds = A.Seconds / B.Seconds, Framerate = A.Framerate };
            }
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is greater than B</returns>
        public static bool operator >(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? false : true;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames > B.Frames :
                A.Seconds > B.Seconds;
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is less than than B</returns>
        public static bool operator <(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ?
                    B is null ? false : true :
                    false;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames < B.Frames :
                A.Seconds < B.Seconds;
        }

        /// <summary>
        /// Detects equality between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is equal to B</returns>
        public static bool operator ==(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null && B is null;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames == B.Frames :
                A.Seconds == B.Seconds;
        }

        /// <summary>
        /// Detects disequality between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is not equal to B</returns>
        public static bool operator !=(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return (A is null && !(B is null)) ||
                    (!(A is null) && (B is null));
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames != B.Frames :
                A.Seconds != B.Seconds;
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is greater than or equal to B</returns>
        public static bool operator >=(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? 
                    B is null? true : false :
                    true;
            }

            return A > B || A == B;
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is less than or equal to B</returns>
        public static bool operator <=(TimeDuration A, TimeDuration B)
        {
            if (A is null || B is null)
            {
                return A is null ? true : false;
            }

            return A < B || A == B;
        }

        /// <summary>
        /// Sums frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator +(TimeDuration A, double B)
        {
            if (A is null)
            {
                return null;
            }
            
            return A.DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = Convert.ToInt32(A.Frames + B), Framerate = A.Framerate } :
                new TimeDuration() { Seconds = A.Seconds + B, Framerate = A.Framerate };
        }

        /// <summary>
        /// Subtracts frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator -(TimeDuration A, double B)
        {
            if (A is null)
            {
                return null;
            }

            return A.DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = Convert.ToInt32(A.Frames - B), Framerate = A.Framerate } :
                new TimeDuration() { Seconds = A.Seconds - B, Framerate = A.Framerate };
        }

        /// <summary>
        /// Multiplies frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator *(TimeDuration A, double B)
        {
            if (A is null)
            {
                return null;
            }

            return A.DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = Convert.ToInt32(A.Frames * B), Framerate = A.Framerate } :
                new TimeDuration() { Seconds = A.Seconds * B, Framerate = A.Framerate };
        }

        /// <summary>
        /// Divides frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>TimeDuration with Framerate based on A's Framerate.</returns>
        public static TimeDuration operator /(TimeDuration A, double B)
        {
            if (A is null)
            {
                return null;
            }

            if (A.DurationType == DurationTypes.Frames)
            {
                return B == 0 ?
                    new TimeDuration() { Frames = 0 } :
                    new TimeDuration() { Frames = Convert.ToInt32(A.Frames / B), Framerate = A.Framerate };
            }
            else
            {
                return B == 0 ?
                    new TimeDuration() { Seconds = 0 } :
                    new TimeDuration() { Seconds = A.Seconds / B, Framerate = A.Framerate };
            }
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is greater than B</returns>
        public static bool operator >(TimeDuration A, double B)
        {
            if (A is null)
            {
                return false;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames > B :
                A.Seconds > B;
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is less than than B</returns>
        public static bool operator <(TimeDuration A, double B)
        {
            if (A is null)
            {
                return true;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames < B:
                A.Seconds < B;
        }

        /// <summary>
        /// Detects equality between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is equal to B</returns>
        public static bool operator ==(TimeDuration A, double B)
        {
            if (A is null)
            {
                return false;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames == B:
                A.Seconds == B;
        }

        /// <summary>
        /// Detects disequality between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is not equal to B</returns>
        public static bool operator !=(TimeDuration A, double B)
        {
            if (A is null)
            {
                return true;
            }

            return A.DurationType == DurationTypes.Frames ?
                A.Frames != B:
                A.Seconds != B;
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is greater than or equal to B</returns>
        public static bool operator >=(TimeDuration A, double B)
        {
            if (A is null)
            {
                return false;
            }

            return A > B || A == B;
        }

        /// <summary>
        /// Detects greater between A and B frames or seconds depending on A's DurationType.
        /// </summary>
        /// <returns>true if A is less than or equal to B</returns>
        public static bool operator <=(TimeDuration A, double B)
        {
            if (A is null)
            {
                return true;
            }

            return A < B || A == B;
        }
        #endregion

        /// <summary>
        /// Returns a new clone of this object
        /// </summary>
        public TimeDuration Clone()
        {
            return DurationType == DurationTypes.Frames ?
                new TimeDuration() { Frames = this.Frames, Framerate = this.Framerate } :
                new TimeDuration() { Seconds = this.Seconds, Framerate = this.Framerate };
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
