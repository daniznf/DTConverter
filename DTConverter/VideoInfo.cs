/*
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

using System.ComponentModel;

namespace DTConverter
{
    public class VideoInfo : INotifyPropertyChanged
    {
        private string _SourcePath;
        public string SourcePath
        {
            get => _SourcePath;
            set
            {
                _SourcePath = value;
                OnPropertyChanged("SourcePath");
            }
        }

        private TimeDuration _Duration;
        public TimeDuration Duration
        {
            get => _Duration;
            set
            {
                _Duration = value;
                OnPropertyChanged("Duration");
            }
        }

        private bool _HasVideo;
        public bool HasVideo
        { 
            get => _HasVideo;
            set
            {
                _HasVideo = value;
                OnPropertyChanged("HasVideo");
            }
        }

        private string _VideoCodec;
        public string VideoCodec
        { 
            get => _VideoCodec;
            set
            {
                _VideoCodec = value;
                OnPropertyChanged("VideoCodec");
            }
        }

        private string _ChromaSubsampling;
        public string ChromaSubsampling
        { 
            get => _ChromaSubsampling;
            set
            {
                _ChromaSubsampling = value;
                OnPropertyChanged("ChromaSubsampling");
            }
        }

        private int _HorizontalResolution;
        public int HorizontalResolution
        { 
            get => _HorizontalResolution;
            set
            {
                _HorizontalResolution = value;
                OnPropertyChanged("HorizontalResolution");
            }
        }
        private int _VerticalResolution;
        public int VerticalResolution
        { 
            get => _VerticalResolution;
            set
            {
                _VerticalResolution = value;
                OnPropertyChanged("VerticalResolution");
            }
        }

        public string VideoResolution => HorizontalResolution.ToString() + "x" + VerticalResolution.ToString();

        public double AspectRatio => VerticalResolution != 0 ? 1.0 * HorizontalResolution / VerticalResolution : -1;

        private int _VideoBitrate;
        /// <summary>
        /// Bitrate in Kb/s
        /// </summary>
        public int VideoBitrate
        { 
            get => _VideoBitrate;
            set
            {
                _VideoBitrate = value;
                OnPropertyChanged("VideoBitrate");
            }
        }

        /// <summary>
        /// Framerate in fps
        /// </summary>
        public double Framerate
        {
            get => Duration != null ? Duration.Framerate : 0;
            set
            {
                Duration.Framerate = value;
                OnPropertyChanged("Framerate");
            }
        }
        
        private bool _HasAudio;
        public bool HasAudio
        { 
            get => _HasAudio;
            set
            {
                _HasAudio = value;
                OnPropertyChanged("HasAudio");
            }
        }

        private string _AudioCodec;
        public string AudioCodec
        { 
            get => _AudioCodec;
            set
            {
                _AudioCodec = value;
                OnPropertyChanged("AudioCodec");
            }
        }

        private int _AudioSamplingRate;
        /// <summary>
        /// Sampling rate in Hz
        /// </summary>
        public int AudioSamplingRate
        {
            get => _AudioSamplingRate;
            set
            {
                _AudioSamplingRate = value;
                OnPropertyChanged("AudioSamplingRate");
            }
        }

        private AudioChannels _AudioChannels;
        public AudioChannels AudioChannels
        {
            get => _AudioChannels;
            set
            {
                _AudioChannels = value;
                OnPropertyChanged("AudioChannels");
            }
        }

        private int _AudioBitrate;
        /// <summary>
        /// Bitrate in Kb/s
        /// </summary>
        public int AudioBitrate
        { 
            get => _AudioBitrate;
            set
            {
                _AudioBitrate = value;
                OnPropertyChanged("AudioBitrate");
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
