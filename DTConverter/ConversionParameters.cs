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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DTConverter
{
    public enum VideoEncoders { HAP, HAP_Alpha, HAP_Q, H264, Still_PNG, Still_JPG, PNG_Sequence, JPG_Sequence, Copy }
    public enum AudioEncoders { WAV_16, WAV_24, WAV_32, Copy }
    public enum AudioChannels { Mono, Stereo, ch_5_1 }
    //public enum AudioEncoders { pcm_s16le }
    public enum ConversionStatus { None, CreatingPreviewIn, CreatingPreviewOut, Converting, Success, Failed };

    /// <summary>
    /// Contains all parameters for each conversion, a VideoInfo and an instance of FFmpegWrapper
    /// </summary>
    public class ConversionParameters : INotifyPropertyChanged
    {
        public ConversionParameters()
        {
            ResetDefaultValues();
        }

        public ConversionParameters(string sourcePath)
        {
            this.SourcePath = sourcePath;
            ResetDefaultValues();
        }

        public void ResetDefaultValues()
        {
            // Changing all public properties (not _variables) Bindings will be notified
            VideoResolutionParams = new VideoResolution();
            VideoResolutionParams.PropertyChanged += VideoResolutionParams_PropertyChanged;
            _StartTime = new TimeDuration();
            _EndTime = new TimeDuration();
            _DurationTime = new TimeDuration();
            _PreviewTime = new TimeDuration();
            CropParams = new Crop();
            CropParams.PropertyChanged += CropParams_PropertyChanged;
            PaddingParams = new Padding();
            PaddingParams.PropertyChanged += CropParams_PropertyChanged;
            SliceParams = new Slicer();
            SliceParams.PropertyChanged += SliceParams_PropertyChanged;

            PreviewResolution = new VideoResolution
            {
                Horizontal = 640,
                Vertical = 360
            };

            VideoConversionStatus = ConversionStatus.None;
            AudioConversionStatus = ConversionStatus.None;

            IsConversionEnabled = true;
            IsVideoEnabled = true;
            IsAudioEnabled = true;
            VideoEncoder = VideoEncoders.HAP;
            AudioEncoder = AudioEncoders.WAV_16;
            IsAudioRateEnabled = false;
            AudioRate = 44100;
            IsChannelsEnabled = false;
            Channels = AudioChannels.Stereo;
            SplitChannels = false;
            
            IsVideoBitrateEnabled = false;
            VideoBitrate = 0;
            IsOutFramerateEnabled = false;
            OutFramerate = 0;
            IsRotationEnabled = false;
            Rotation = 0;
            RotateMetadataOnly = false;

            IsChkOriginalChecked = true;

            RefreshProperties();
        }

        public void RefreshProperties()
        {
            PropertyInfo[] innerPInfos;
            foreach (PropertyInfo pInfo in this.GetType().GetProperties())
            {
                OnPropertyChanged(pInfo.Name);
                innerPInfos = pInfo.GetType().GetProperties();
                if (innerPInfos != null)
                {
                    foreach (PropertyInfo innerPInfo in innerPInfos)
                    {
                        OnPropertyChanged(innerPInfo.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Copies all parameters from input object, except for SourcePath
        /// </summary>
        /// <param name="copyFrom"></param>
        public void PasteParameters(ConversionParameters copyFrom)
        {
            // Every new addition or change MUST be reported here ! ! !

            // SourcePath needs to remain original path
            // SourceInfo should not be copied
            // FFWrapper should not be copied
            // Isvalid should not be copied, user must not copy a valid item into a non-valid item
            // ThumbnailPathIn should not be copied

            StartTimeSeconds = copyFrom.StartTimeSeconds;
            DurationTimeSeconds = copyFrom.DurationTimeSeconds;
            EndTimeSeconds = copyFrom.DurationTimeSeconds;
            PreviewTimeSeconds = copyFrom.PreviewTimeSeconds;

            PreviewResolution = copyFrom.PreviewResolution;

            IsConversionEnabled = copyFrom.IsConversionEnabled;
            IsVideoEnabled = copyFrom.IsVideoEnabled;
            IsAudioEnabled = copyFrom.IsAudioEnabled;
            VideoEncoder = copyFrom.VideoEncoder;
            AudioEncoder = copyFrom.AudioEncoder;
            IsAudioRateEnabled = copyFrom.IsAudioRateEnabled;
            AudioRate = copyFrom.AudioRate;
            IsChannelsEnabled = copyFrom.IsChannelsEnabled;
            Channels = copyFrom.Channels;
            SplitChannels = copyFrom.SplitChannels;
            
            VideoResolutionParams = copyFrom.VideoResolutionParams;

            IsVideoBitrateEnabled = copyFrom.IsVideoBitrateEnabled;
            VideoBitrate = copyFrom.VideoBitrate;
            IsOutFramerateEnabled = copyFrom.IsOutFramerateEnabled;
            OutFramerate = copyFrom.OutFramerate;
            IsRotationEnabled = copyFrom.IsRotationEnabled;
            Rotation = copyFrom.Rotation;
            RotateMetadataOnly = copyFrom.RotateMetadataOnly;
            CropParams = copyFrom.CropParams;
            PaddingParams = copyFrom.PaddingParams;
            SliceParams = copyFrom.SliceParams;

            IsChkOriginalChecked = copyFrom.IsChkOriginalChecked;

            RefreshProperties();
        }

        private void SliceParams_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("ShowColPreviewOut");
        }

        private void CropParams_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("ShowColPreviewOut");
            OnPropertyChanged("DestinationVideoPath");
            OnPropertyChanged("VideoFinalResolutionHorizontal");
            OnPropertyChanged("VideoFinalResolutionVertical");
        }

        private void VideoResolutionParams_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged("DestinationVideoPath");
            OnPropertyChanged("VideoFinalResolutionHorizontal");
            OnPropertyChanged("VideoFinalResolutionVertical");
            OnPropertyChanged("ShowColPreviewOut");
        }

        public string SourcePath { get; }

        /// <summary>
        /// Destination Path will be generated on property read, it will be based on values of this ConversionParameters
        /// </summary>
        public string DestinationVideoPath
        {
            get
            {
                if (SourcePath != null)
                {
                    string outPath = Path.GetDirectoryName(SourcePath);
                    outPath = Path.Combine(outPath, VideoEncoder.ToString());
                    outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(SourcePath));
                    if (VideoResolutionParams.IsEnabled || CropParams.IsEnabled || PaddingParams.IsEnabled)
                    {
                        outPath += $"_{VideoFinalResolutionHorizontal}x{VideoFinalResolutionVertical}";
                    }
                    if (IsOutFramerateEnabled)
                    {
                        outPath += $"_{OutFramerate.ToString(CultureInfo.InvariantCulture)}";
                    }
                    if (IsVideoBitrateEnabled)
                    {
                        outPath += $"_{VideoBitrate}";
                    }
                    if ((VideoEncoder == VideoEncoders.JPG_Sequence) || (VideoEncoder == VideoEncoders.PNG_Sequence))
                    {
                        outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(SourcePath));
                        int nDigits = TimeDuration.GetFrames(_DurationTime.Seconds, OutFramerate).ToString().Length;
                        outPath += $"-%0{nDigits}d";
                    }

                    string extension;
                    if ((VideoEncoder == VideoEncoders.HAP) || (VideoEncoder == VideoEncoders.HAP_Alpha) || (VideoEncoder == VideoEncoders.HAP_Q))
                    {
                        extension = ".mov";
                    }
                    else if (VideoEncoder == VideoEncoders.H264)
                    {
                        extension = ".mp4";
                    }
                    else if ((VideoEncoder == VideoEncoders.Still_JPG) || (VideoEncoder == VideoEncoders.JPG_Sequence))
                    {
                        extension = ".jpg";
                    }
                    else if ((VideoEncoder == VideoEncoders.Still_PNG) || (VideoEncoder == VideoEncoders.PNG_Sequence))
                    {
                        extension = ".png";
                    }
                    else // VideoEncoder == VideoEncoders.Copy
                    {
                        extension = Path.GetExtension(SourcePath);
                    }

                    if (File.Exists(outPath + extension))
                    {
                        outPath += "_" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00");
                    }
                    outPath += extension;

                    return outPath;
                }
                return "noname.mov";
            }
        }

        public string DestinationAudioPath
        {
            get
            {
                if (SourcePath != null)
                {
                    string outPath = Path.GetDirectoryName(SourcePath);
                    outPath = Path.Combine(outPath, AudioEncoder.ToString());
                    outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(SourcePath));
                    if (IsAudioRateEnabled)
                    {
                        outPath += $"_{AudioRate}";
                    }
                    if (IsChannelsEnabled)
                    {
                        outPath += $"_{Channels}";
                    }
                    
                    string extension;
                    if ((AudioEncoder == AudioEncoders.WAV_16) || (AudioEncoder == AudioEncoders.WAV_24) || (AudioEncoder == AudioEncoders.WAV_32))
                    {
                        extension = ".wav";
                    }
                    else // AudioEncoder == AudioEncoders.Copy
                    {
                        extension = Path.GetExtension(SourcePath);
                    }

                    if (File.Exists(outPath + extension))
                    {
                        outPath += "_" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00");
                    }
                    outPath += extension;

                    return outPath;
                }
                return "noname.wav";
            }
        }

        private VideoInfo _SourceInfo;
        /// <summary>
        /// After calling ProbeVideoInfo(), it contains all information read from source file
        /// </summary>
        public VideoInfo SourceInfo
        {
            get => _SourceInfo;
            private set
            {
                _SourceInfo = value;
                OnPropertyChanged("SourceInfo");
            }
        }
        
        /// <summary>
        /// Determines if it is a valid video or audio file by checking if duration is null
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (SourceInfo != null)
                {
                    if (SourceInfo.Duration != null)
                    {
                        return SourceInfo.Duration.Seconds > 0;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// StartTime's Framerate is related to source framerate
        /// </summary>
        private TimeDuration _StartTime;
        public int StartTimeFrames => _StartTime != null ? _StartTime.Frames : 0;
        public double StartTimeSeconds
        {
            get => Math.Round(_StartTime != null? _StartTime.Seconds : 0, 6);
            set
            {
                if (_StartTime != null)
                {
                    TimeDuration newStart = new TimeDuration() { Seconds = value, Framerate = _StartTime.Framerate };
                    if (value >= 0 && newStart < _EndTime)
                    {
                        _StartTime = newStart;
                        _DurationTime = _EndTime - _StartTime;
                        _DurationTime.Framerate = OutFramerate;
                    }
                }
                OnPropertyChanged("StartTimeSeconds");
                OnPropertyChanged("StartTimeHMS");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
            }
        }
        public string StartTimeHMS
        {
            get
            {
                return _StartTime != null ? _StartTime.HMS : "";
            }
            set
            {
                if (_StartTime != null)
                {
                    TimeDuration newStart = new TimeDuration() { HMS = value, Framerate = _StartTime.Framerate };
                    if (newStart.Seconds >= 0 && newStart < _EndTime)
                    {
                        _StartTime.HMS = value;
                        _DurationTime = _EndTime - _StartTime;
                        _DurationTime.Framerate = OutFramerate;
                    }
                }
                OnPropertyChanged("StartTimeSeconds");
                OnPropertyChanged("StartTimeHMS");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
            }
        }

        /// <summary>
        /// EndTime's Framerate is related to source framerate
        /// </summary>
        private TimeDuration _EndTime;
        public int EndTimeFrames => _EndTime != null ? _EndTime.Frames : 0;
        public double EndTimeSeconds
        {
            get => Math.Round(_EndTime != null ? _EndTime.Seconds : 0, 6);
            set
            {
                if (_EndTime != null && SourceInfo != null && SourceInfo.Duration != null)
                {
                    TimeDuration newEnd = new TimeDuration() { Seconds = value, Framerate = _EndTime.Framerate };
                    if (value >= 0 && newEnd > _StartTime && newEnd < SourceInfo.Duration)
                    {
                        _EndTime = newEnd;
                        _DurationTime = _EndTime - _StartTime;
                        _DurationTime.Framerate = OutFramerate;
                    }
                }
                OnPropertyChanged("EndTimeSeconds");
                OnPropertyChanged("EndTimeHMS");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
            }
        }
        public string EndTimeHMS
        {
            get
            {
                return _EndTime != null ? _EndTime.HMS : "";
            }
            set
            {
                if (_EndTime != null && SourceInfo != null && SourceInfo.Duration != null)
                {
                    TimeDuration newEnd = new TimeDuration() { HMS = value, Framerate = _EndTime.Framerate };
                    if (newEnd.Seconds > 0 && newEnd > _StartTime && newEnd < SourceInfo.Duration)
                    {
                        _EndTime = newEnd;
                        _DurationTime = _EndTime - _StartTime;
                        _DurationTime.Framerate = OutFramerate;
                    }
                }
                OnPropertyChanged("EndTimeHMS");
                OnPropertyChanged("EndTimeSeconds");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
            }
        }

        /// <summary>
        /// DurationTime's Framerate is related to output framerate
        /// </summary>
        private TimeDuration _DurationTime;
        public int DurationTimeFrames => _DurationTime != null ? _DurationTime.Frames: 0;
        public double DurationTimeSeconds
        {
            get => Math.Round(_DurationTime != null ? _DurationTime.Seconds : 0, 6);
            set
            {
                if (_DurationTime != null)
                {
                    TimeDuration newDuration = new TimeDuration() { Seconds = value, Framerate = _DurationTime.Framerate };
                    if (value > 0 && newDuration + _StartTime <= _EndTime)
                    {
                        _DurationTime = newDuration;
                        _EndTime = _DurationTime + _StartTime;
                    }
                }
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
                OnPropertyChanged("EndTimeSeconds");
                OnPropertyChanged("EndTimeHMS");
            }
        }
        public string DurationTimeHMS
        {
            get
            {
                return _DurationTime != null ? _DurationTime.HMS : "";
            }
            set
            {
                if (_DurationTime != null)
                {
                    TimeDuration newDuration = new TimeDuration() { HMS = value, Framerate = _DurationTime.Framerate };
                    if (newDuration.Seconds > 0 && newDuration + _StartTime <= _EndTime)
                    {
                        _DurationTime = newDuration;
                        _EndTime = _DurationTime + _StartTime;
                    }
                }
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeFramesEquivalent");
                OnPropertyChanged("EndTimeHMS");
                OnPropertyChanged("EndTimeSeconds");
            }
        }
        public int DurationTimeFramesEquivalent
        {
            get
            {
                if ( _DurationTime != null && _StartTime != null && _StartTime.Framerate != 0)
                {
                    return Convert.ToInt32(_DurationTime.Frames * _DurationTime.Framerate / _StartTime.Framerate);
                }
                return 0;
            }
        }

        private TimeDuration _PreviewTime;
        public int PreviewTimeFrames => _PreviewTime != null ? _PreviewTime.Frames : 0;
        public double PreviewTimeSeconds
        {
            get => Math.Round(_PreviewTime != null ? _PreviewTime.Seconds : 0, 6);
            set
            {
                if (_PreviewTime != null)
                {
                    _PreviewTime.Seconds = value;
                }
                OnPropertyChanged("PreviewTimeSeconds");
                OnPropertyChanged("PreviewTimeHMS");
            }
        }
        public string PreviewTimeHMS
        {
            get => _PreviewTime != null ? _PreviewTime.HMS : "";
            set
            {
                if (_PreviewTime != null)
                {
                    _PreviewTime.HMS = value;
                }
                OnPropertyChanged("PreviewTimeHMS");
                OnPropertyChanged("PreviewTimeSeconds");
            }
        }

        public GridLength ShowColPreviewOut
        {
            get
            {
                GridLength gr;
                if (CropParams.IsEnabled || PaddingParams.IsEnabled || SliceParams.IsEnabled ||
                    VideoResolutionParams.IsEnabled ||
                    _Rotation != 0)
                {
                    gr = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    gr = new GridLength(0, GridUnitType.Star);
                }
                
                OnPropertyChanged("IsChkOriginalVisible");
                return gr;
            }
        }

        public Visibility IsChkOriginalVisible
        {
            get
            {
                if (CropParams.IsEnabled || PaddingParams.IsEnabled || SliceParams.IsEnabled ||
                    VideoResolutionParams.IsEnabled ||
                    _Rotation != 0)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }

        private bool _IsChkOriginalChecked;
        public bool IsChkOriginalChecked
        {
            get => _IsChkOriginalChecked;
            set
            {
                _IsChkOriginalChecked = value;
                OnPropertyChanged("IsChkOriginalChecked");
            }
        }
        
        public VideoResolution PreviewResolution { get; set; }
        
        public string ThumbnailPathIn { get; set; }
        
        public string ThumbnailPathOut => ThumbnailPathIn + "_out";

        private bool _IsConversionEnabled;
        public bool IsConversionEnabled
        {
            get => _IsConversionEnabled;
            set
            {
                _IsConversionEnabled = value;
                OnPropertyChanged("IsConversionEnabled");
            }
        }

        private bool _IsVideoEnabled;
        public bool IsVideoEnabled
        {
            get => _SourceInfo != null ? _IsVideoEnabled && _SourceInfo.HasVideo : false;
            
            set
            {
                _IsVideoEnabled = value;
                OnPropertyChanged("IsVideoEnabled");
            }
        }

        private bool _IsAudioEnabled;
        public bool IsAudioEnabled
        {
            get => _SourceInfo != null ? _IsAudioEnabled && _SourceInfo.HasAudio : false;
            set
            {
                _IsAudioEnabled = value;
                OnPropertyChanged("IsAudioEnabled");
            }
        }

        private VideoEncoders _VideoEncoder;
        public VideoEncoders VideoEncoder
        {
            get => _VideoEncoder;
            set
            {
                _VideoEncoder = value;
                switch (value)
                {
                    case VideoEncoders.HAP:
                        VideoResolutionParams.Multiple = 4;
                        break;
                    case VideoEncoders.HAP_Alpha:
                        VideoResolutionParams.Multiple = 4;
                        break;
                    case VideoEncoders.HAP_Q:
                        VideoResolutionParams.Multiple = 4;
                        break;
                }

                OnPropertyChanged("VideoEncoder");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("EndTimeHMS");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("IsVideoEncoderCopy");
                OnPropertyChanged("IsVideoEncoderNotCopy");
                OnPropertyChanged("IsVideoEncoderH264");
                OnPropertyChanged("IsVideoEncoderNotHAP");
                OnPropertyChanged("IsVideoEncoderNotStillImage");
            }
        }
        public bool IsVideoEncoderCopy => VideoEncoder == VideoEncoders.Copy;
        public bool IsVideoEncoderNotCopy => VideoEncoder != VideoEncoders.Copy;
        public bool IsVideoEncoderH264 => VideoEncoder == VideoEncoders.H264;
        public bool IsVideoEncoderNotHAP => !VideoEncoder.ToString().ToLower().Contains("hap");
        public bool IsVideoEncoderNotStillImage => !VideoEncoder.ToString().ToLower().Contains("still");

        public VideoResolution VideoResolutionParams { get; set; }

        public int VideoFinalResolutionHorizontal
        {
            get
            {
                int hRes;
                if (VideoResolutionParams.IsEnabled && VideoResolutionParams.Horizontal > 0)
                {
                    hRes = VideoResolutionParams.Horizontal;
                }
                else
                {
                    if (SourceInfo != null)
                    {
                        hRes = SourceInfo.HorizontalResolution;
                    }
                    else
                    {
                        return 0;
                    }

                    if (CropParams.IsEnabled)
                    {
                        hRes -= CropParams.Left + CropParams.Right;
                    }
                    if (PaddingParams.IsEnabled)
                    {
                        hRes += PaddingParams.Left + PaddingParams.Right;
                    }
                }

                

                return hRes;
            }
        }
        public int VideoFinalResolutionVertical
        {
            get
            {
                int vRes;
                if (VideoResolutionParams.IsEnabled && VideoResolutionParams.Vertical > 0)
                {
                    vRes = VideoResolutionParams.Vertical;
                }
                else
                {
                    if (SourceInfo != null)
                    {
                        vRes = SourceInfo.VerticalResolution;
                    }
                    else
                    {
                        return 0;
                    }

                    if (CropParams.IsEnabled)
                    {
                        vRes -= CropParams.Top + CropParams.Bottom;
                    }
                    if (PaddingParams.IsEnabled)
                    {
                        vRes += PaddingParams.Top + PaddingParams.Bottom;
                    }
                }

                return vRes;
            }
        }
        
        private bool _IsVideoBitrateEnabled;
        public bool IsVideoBitrateEnabled
        {
            get => _IsVideoBitrateEnabled;
            set
            {
                _IsVideoBitrateEnabled = value;
                OnPropertyChanged("IsVideoBitrateEnabled");
                OnPropertyChanged("VideoBitrate");
                OnPropertyChanged("DestinationVideoPath");
            }
        }
        private int _VideoBitrate;
        /// Video Bitrate in Kb/s
        public int VideoBitrate
        {
            get
            {
                if (_IsVideoBitrateEnabled && _VideoBitrate > 0)
                {
                    return _VideoBitrate;
                }
                else
                {
                    if (SourceInfo != null)
                    {
                        return SourceInfo.VideoBitrate;
                    }
                }
                return 0;
            }
            set
            {
                _VideoBitrate = value;
                OnPropertyChanged("VideoBitrate");
                OnPropertyChanged("DestinationVideoPath");
            }
        }

        private bool _IsOutFramerateEnabled;
        public bool IsOutFramerateEnabled
        {
            get => _IsOutFramerateEnabled;
            set
            {
                _IsOutFramerateEnabled = value;
                _DurationTime.Framerate = value ? OutFramerate :
                    SourceInfo != null ? SourceInfo.Framerate : 0;

                OnPropertyChanged("IsOutFramerateEnabled");
                OnPropertyChanged("OutFrameRate");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
            }
        }
        private double _OutFramerate;
        public double OutFramerate
        {
            get
            {
                if (_IsOutFramerateEnabled && _OutFramerate > 0)
                {
                    return Math.Round(_OutFramerate, 2);
                }
                else
                {
                    if (SourceInfo != null)
                    {
                        return Math.Round(SourceInfo.Framerate, 2);
                    }
                }
                return 0;
            }
            set
            {
                _OutFramerate = value;
                _DurationTime.Framerate = value;
                
                OnPropertyChanged("OutFrameRate");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
                OnPropertyChanged("DurationTimeFramesEquivalent");
            }
        }

        private bool _IsRotationEnabled;
        public bool IsRotationEnabled
        {
            get => _IsRotationEnabled;
            set
            {
                _IsRotationEnabled = value;
                OnPropertyChanged("IsRotationEnabled");
            }
        }
        private int _Rotation;
        public int Rotation
        {
            get => _Rotation;
            set
            {
                _Rotation = value;
                OnPropertyChanged("Rotation");
                OnPropertyChanged("ShowColPreviewOut");
            }
        }

        private bool _RotateMetadataOnly;
        public bool RotateMetadataOnly
        {
            get => _RotateMetadataOnly;
            set
            {
                _RotateMetadataOnly = value;
                OnPropertyChanged("RotateMetadataOnly");
            }
        }

        public Crop CropParams { get; set; }
        public Padding PaddingParams { get; set; }
        public Slicer SliceParams { get; set; }

        private ConversionStatus _VideoConversionStatus;
        public ConversionStatus VideoConversionStatus
        {
            get => _VideoConversionStatus;
            set
            {
                _VideoConversionStatus = value;
                OnPropertyChanged("VideoConversionStatus");
            }
        }

        private AudioEncoders _AudioEncoder;
        public AudioEncoders AudioEncoder
        {
            get => _AudioEncoder;
            set
            {
                _AudioEncoder = value;
                OnPropertyChanged("AudioEncoder");
                OnPropertyChanged("DestinationAudioPath");
            }
        }

        private ConversionStatus _AudioConversionStatus;
        public ConversionStatus AudioConversionStatus
        {
            get => _AudioConversionStatus;
            set
            {
                _AudioConversionStatus = value;
                OnPropertyChanged("AudioConversionStatus");
            }
        }

        private bool _IsAudioRateEnabled;
        public bool IsAudioRateEnabled
        {
            get => _IsAudioRateEnabled;
            set
            {
                _IsAudioRateEnabled = value;
                OnPropertyChanged("IsAudioRateEnabled");
            }
        }

        private int _AudioRate;
        public int AudioRate
        {
            get => _AudioRate;
            set
            {
                _AudioRate = value;
                OnPropertyChanged("AudioRate");
            }
        }

        private bool _IsChannelsEnabled;
        public bool IsChannelsEnabled
        {
            get => _IsChannelsEnabled;
            set
            {
                _IsChannelsEnabled = value;
                OnPropertyChanged("IsChannelsEnabled");
            }
        }

        private AudioChannels _Channels;
        public AudioChannels Channels
        {
            get => _Channels;
            set
            {
                _Channels = value;
                OnPropertyChanged("Channels");
            }
        }

        private bool _SplitChannels;
        public bool SplitChannels
        {
            get => _SplitChannels;
            set
            {
                _SplitChannels = value;
                OnPropertyChanged("SplitChannels");
            }
        }

        /// <summary>
        /// Probes all video informations.
        /// This method takes some seconds to execute so it should be run in a separate thread or Task
        /// </summary>
        public void ProbeVideoInfo()
        {
            if (SourcePath != null)
            {
                SourceInfo = FFmpegWrapper.ProbeVideoInfo(SourcePath, 1000);
                OnPropertyChanged("IsValid");
                OnPropertyChanged("IsAudioEnabled");
                OnPropertyChanged("IsVideoEnabled");

                if (SourceInfo != null && IsValid)
                {
                    VideoResolutionParams.Horizontal = SourceInfo.HorizontalResolution;
                    VideoResolutionParams.Vertical = SourceInfo.VerticalResolution;
                    DurationTimeSeconds = SourceInfo.Duration.Seconds;
                    OutFramerate = SourceInfo.Framerate;
                    VideoBitrate = SourceInfo.VideoBitrate;
                    _StartTime.Framerate = SourceInfo.Framerate;
                    _PreviewTime.Framerate = SourceInfo.Framerate;
                    _EndTime = SourceInfo.Duration;
                    //_EndTime.Framerate = SourceInfo.FrameRate;
                }
                else
                {
                    IsConversionEnabled = false;
                }
            }
        }

        private Process ProcessPreviewIn;
        /// <summary>
        /// Creates a still image (JPG) in Thumbnail directory, usable as preview of source file (no Crop, no Padding, no filters, etc...)
        /// This method takes few seconds to execute so it should be run in a separate thread or Task
        /// </summary>
        public void CreateImagePreviewIn(DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived)
        {
            if (VideoConversionStatus != ConversionStatus.CreatingPreviewIn)
            {
                VideoConversionStatus = ConversionStatus.CreatingPreviewIn;
                try
                {
                    ProcessPreviewIn = FFmpegWrapper.ConvertVideo(SourcePath, ThumbnailPathIn, _PreviewTime, new TimeDuration() { Frames = 1 }, VideoEncoders.Still_JPG,
                        PreviewResolution, 0, 0, 0, 0, false, null, null, null);
                    ProcessPreviewIn.StartInfo.Arguments += " -y";
                    if (ProcessPreviewIn.Start())
                    {
                        ProcessPreviewIn.BeginOutputReadLine();
                        ProcessPreviewIn.BeginErrorReadLine();
                        ProcessPreviewIn.OutputDataReceived += outputDataReceived;
                        ProcessPreviewIn.ErrorDataReceived += errorDataReceived;
                        if (!ProcessPreviewIn.HasExited)
                        {
                            ProcessPreviewIn.WaitForExit();
                        }
                        if (ProcessPreviewIn.ExitCode != 0)
                        {
                            throw new Exception($"Creating input preview image failed with erro {ProcessPreviewIn.ExitCode}");
                        }
                    }
                    VideoConversionStatus = ConversionStatus.None;
                }
                catch (Exception E)
                {
                    VideoConversionStatus = ConversionStatus.Failed;
                    throw E;
                }
            }
        }

        public void KillProcessPreviewIn()
        {
            try
            {
                if (ProcessPreviewIn != null && !ProcessPreviewIn.HasExited)
                {
                    ProcessPreviewIn.Kill();
                    VideoConversionStatus = ConversionStatus.None;
                }
            }
            catch (Exception E) { }
        }

        Process ProcessPreviewOut;
        /// <summary>
        /// Creates one or more still images (JPG) in Thumbnail directory, usable as previews of output file (with Crop, Padding, etc...)
        /// This method takes few seconds to execute so it should be run in a separate thread or Task
        /// </summary>
        public void CreateImagePreviewOut(DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived)
        {
            if (VideoConversionStatus != ConversionStatus.CreatingPreviewOut)
            {
                VideoConversionStatus = ConversionStatus.CreatingPreviewOut;
                try
                {
                    ProcessPreviewOut = FFmpegWrapper.ConvertVideo(SourcePath, ThumbnailPathOut, _PreviewTime, new TimeDuration() { Frames = 1 }, VideoEncoders.Still_JPG,
                        VideoResolutionParams, 0, 0, 0, IsRotationEnabled ? Rotation : 0, false, CropParams, PaddingParams, SliceParams);
                    ProcessPreviewOut.OutputDataReceived += outputDataReceived;
                    ProcessPreviewOut.ErrorDataReceived += errorDataReceived;
                    ProcessPreviewOut.StartInfo.Arguments += " -y";
                    if (ProcessPreviewOut.Start())
                    {
                        ProcessPreviewOut.BeginOutputReadLine();
                        ProcessPreviewOut.BeginErrorReadLine();
                        if (!ProcessPreviewOut.HasExited)
                        {
                            ProcessPreviewOut.WaitForExit();
                        }
                        if (ProcessPreviewOut.ExitCode != 0)
                        {
                            throw new Exception($"Creating output preview image failed with error {ProcessPreviewOut.ExitCode}");
                        }
                    }
                    VideoConversionStatus = ConversionStatus.None;
                }
                catch (Exception E)
                {
                    VideoConversionStatus = ConversionStatus.Failed;
                    throw E;
                }
            }
        }

        public void KillProcessPreviewOut()
        {
            try
            {
                if (ProcessPreviewOut != null && !ProcessPreviewOut.HasExited)
                {
                    ProcessPreviewOut.Kill();
                    VideoConversionStatus = ConversionStatus.None;
                }
            }
            catch (Exception E) { }
        }
        
        private Process VideoConversionProcess;
        /// <summary>
        /// Converts video stream, but not the audio.
        /// This method takes several seconds to execute so it should be run it in a separate thread or Task
        /// </summary>
        public void ConvertVideo(DataReceivedEventHandler outputReceived, DataReceivedEventHandler errorReceived)
        {
            if (VideoConversionStatus != ConversionStatus.Converting)
            {
                VideoConversionStatus = ConversionStatus.Converting;
                try
                {
                    if (IsValid && IsConversionEnabled && IsVideoEnabled)
                    {
                        VideoConversionProcess = FFmpegWrapper.ConvertVideo(SourcePath, DestinationVideoPath, _StartTime, _DurationTime, VideoEncoder,
                            VideoResolutionParams,
                            IsVideoBitrateEnabled? VideoBitrate : 0,
                            _SourceInfo != null? _SourceInfo.Framerate : 0,
                            IsOutFramerateEnabled? OutFramerate : 0,
                            IsRotationEnabled? Rotation : 0, RotateMetadataOnly, CropParams, PaddingParams, SliceParams);
                        VideoConversionProcess.OutputDataReceived += outputReceived;
                        VideoConversionProcess.ErrorDataReceived += errorReceived;
                        if (VideoConversionProcess.Start())
                        {
                            VideoConversionProcess.BeginOutputReadLine();
                            VideoConversionProcess.BeginErrorReadLine();
                            if (!VideoConversionProcess.HasExited)
                            {
                                VideoConversionProcess.WaitForExit();
                            }
                            if (VideoConversionProcess.ExitCode != 0)
                            {
                                throw new Exception($"Conversion failed with error {VideoConversionProcess.ExitCode}");
                            }
                        }
                    }
                    VideoConversionStatus = ConversionStatus.Success;
                }
                catch (Exception E)
                {
                    VideoConversionStatus = ConversionStatus.Failed;
                    throw E;
                }
            }
        }

        /// <summary>
        /// Istantly kill this conversion
        /// </summary>
        public void KillVideoConversion()
        {
            try
            {
                if (VideoConversionProcess != null && !VideoConversionProcess.HasExited)
                {
                    VideoConversionProcess.Kill();
                    VideoConversionStatus = ConversionStatus.None;
                }
            }
            catch (Exception E) { }
        }

        private Process AudioConversionProcess;
        /// <summary>
        /// Converts audio stream, but not the video.
        /// This method takes several seconds to execute so it should be run it in a separate thread or Task
        /// </summary>
        public void ConvertAudio(DataReceivedEventHandler outputReceived, DataReceivedEventHandler errorReceived)
        {
            if (VideoConversionStatus != ConversionStatus.Converting)
            {
                VideoConversionStatus = ConversionStatus.Converting;
                try
                {
                    if (IsValid && IsConversionEnabled && IsAudioEnabled)
                    {
                        AudioConversionProcess = FFmpegWrapper.ConvertAudio(SourcePath, DestinationAudioPath, _StartTime, _DurationTime, AudioEncoder,
                            IsAudioRateEnabled ? AudioRate : 0,
                            IsChannelsEnabled, SourceInfo.AudioChannels, Channels, SplitChannels,
                            _SourceInfo != null ? _SourceInfo.Framerate : 0,
                            OutFramerate);
                        AudioConversionProcess.OutputDataReceived += outputReceived;
                        AudioConversionProcess.ErrorDataReceived += errorReceived;
                        if (AudioConversionProcess.Start())
                        {
                            AudioConversionProcess.BeginOutputReadLine();
                            AudioConversionProcess.BeginErrorReadLine();
                            if (!AudioConversionProcess.HasExited)
                            {
                                AudioConversionProcess.WaitForExit();
                            }
                            if (AudioConversionProcess.ExitCode != 0)
                            {
                                throw new Exception($"Conversion failed with error {AudioConversionProcess.ExitCode}");
                            }
                        }
                    }
                    VideoConversionStatus = ConversionStatus.Success;
                }
                catch (Exception E)
                {
                    VideoConversionStatus = ConversionStatus.Failed;
                    throw E;
                }
            }
        }

        /// <summary>
        /// Istantly kill this conversion
        /// </summary>
        public void KillAudioConversion()
        {
            try
            {
                if (AudioConversionProcess != null && !AudioConversionProcess.HasExited)
                {
                    AudioConversionProcess.Kill();
                    VideoConversionStatus = ConversionStatus.None;
                }
            }
            catch (Exception E) { }
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
