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
            OutFrameRate = 0;
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

            _StartTime.Seconds = copyFrom.StartTimeSeconds;
            _DurationTime.Seconds = copyFrom.DurationTimeSeconds;
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
            OutFrameRate = copyFrom.OutFrameRate;
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
                        outPath += $"_{OutFrameRate.ToString(CultureInfo.InvariantCulture)}";
                    }
                    if (IsVideoBitrateEnabled)
                    {
                        outPath += $"_{VideoBitrate}";
                    }
                    if ((VideoEncoder == VideoEncoders.JPG_Sequence) || (VideoEncoder == VideoEncoders.PNG_Sequence))
                    {
                        outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(SourcePath));
                        int nDigits = TimeDuration.GetFrames(_DurationTime.Seconds, OutFrameRate).ToString().Length;
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

        /// Start time must be in seconds
        private TimeDuration _StartTime;
        public double StartTimeSeconds
        {
            get
            {
                if (_StartTime.DurationType == DurationTypes.Frames)
                {
                    return _SourceInfo != null ? TimeDuration.GetSeconds(_StartTime.Frames, _SourceInfo.FrameRate) : 0;
                }
                else
                {
                    return Math.Round(_StartTime.Seconds, 6);
                }
            }
            set
            {
                if (value <= EndTimeSeconds)
                {
                    DurationTimeSeconds = EndTimeSeconds - value;
                    _StartTime.Seconds = value;
                }

                OnPropertyChanged("StartTimeSeconds");
                OnPropertyChanged("StartTimeHMS");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
            }
        }
        public string StartTimeHMS
        {
            get => _StartTime.HMS;
            set
            {
                if (value.Contains("f") && !value.Contains(":") && !value.Contains("."))
                {
                    int tryFrames;
                    if (int.TryParse(value.Remove(value.IndexOf("f")), out tryFrames))
                    {
                        if (tryFrames < EndTimeFrames)
                        {
                            StartTimeFrames = tryFrames;
                        }
                    }
                }
                else if (!value.Contains("f"))
                {
                    string sValue = value.Contains("s") ? value.Remove(value.IndexOf("s")) : value;
                    StartTimeSeconds = new TimeDuration() { HMS = sValue }.Seconds;
                }

                OnPropertyChanged("StartTimeSeconds");
                OnPropertyChanged("StartTimeHMS");
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
            }
        }
        public int StartTimeFrames
        {
            get
            {
                if (_StartTime.DurationType == DurationTypes.Frames)
                {
                    return _StartTime.Frames;
                }
                else
                {
                    return _SourceInfo != null ? TimeDuration.GetFrames(_StartTime.Seconds, _SourceInfo.FrameRate) : 0;
                }
            }
            set
            {
                if (value <= EndTimeFrames)
                {
                    DurationTimeFramesAdjusted = EndTimeFrames - value;
                    _StartTime.Frames = value;
                }
            }
        }


        private TimeDuration _DurationTime;
        public double DurationTimeSeconds
        {
            get
            {
                if (_DurationTime.DurationType == DurationTypes.Frames)
                {
                    return _SourceInfo != null ? TimeDuration.GetSeconds(_DurationTime.Frames, _SourceInfo.FrameRate) : 0;
                }
                else
                {
                    return Math.Round(_DurationTime.Seconds, 6);
                }
            }
            set
            {
                if (_SourceInfo != null)
                {
                    double durationLeft = _SourceInfo.Duration.Seconds - StartTimeSeconds;
                    if (value <= durationLeft)
                    {
                        _DurationTime.Seconds = value;
                    }
                    else
                    {
                        _DurationTime.Seconds = durationLeft;
                    }
                }
                OnPropertyChanged("DurationTimeSeconds");
                OnPropertyChanged("DurationTimeHMS");
            }
        }
        public string DurationTimeHMS
        {
            get
            {
                if (_VideoEncoder == VideoEncoders.Still_JPG || _VideoEncoder == VideoEncoders.Still_PNG)
                {
                    return "1 frame";
                }
                else
                {
                    return _DurationTime.HMS;
                }
            }
            set
            {
                if (value.Contains("f") && !value.Contains(":") && !value.Contains("."))
                {
                    int tryFrames;
                    if (int.TryParse(value.Remove(value.IndexOf("f")), out tryFrames))
                    {
                        DurationTimeFramesAdjusted = tryFrames;
                    }
                }
                else
                {
                    string sValue = value.Contains("s") ? value.Remove(value.IndexOf("s")) : value;
                    DurationTimeSeconds = new TimeDuration() { HMS = sValue }.Seconds;
                }
            }
        }
        public int DurationTimeFramesAdjusted
        {
            get
            {
                if (_DurationTime.DurationType == DurationTypes.Frames)
                {
                    return _DurationTime.Frames;
                }
                else
                {
                    return _SourceInfo != null ? TimeDuration.GetFrames(_DurationTime.Seconds, _SourceInfo.FrameRate) : 0;
                }
            }
            set
            {
                if (_SourceInfo != null)
                {
                    int framesLeft;
                    framesLeft = TimeDuration.GetFrames(_SourceInfo.Duration.Seconds, _SourceInfo.FrameRate) - StartTimeFrames;
                    if (value < framesLeft)
                    {
                        if (_IsOutFramerateEnabled && OutFrameRate > 0 && _SourceInfo != null && _SourceInfo.FrameRate != 0)
                        {
                            _DurationTime.Frames = Convert.ToInt32(value * OutFrameRate / _SourceInfo.FrameRate);
                        }
                        else
                        {
                            _DurationTime.Frames = value;
                        }
                    }
                    else
                    {
                        _DurationTime.Frames = framesLeft;
                    }
                    OnPropertyChanged("DurationTimeSeconds");
                    OnPropertyChanged("DurationTimeHMS");
                }
            }
        }

        public double EndTimeSeconds
        {
            get
            {
                return Math.Round(StartTimeSeconds + DurationTimeSeconds, 6);
            }
            set
            {
                if (_SourceInfo != null)
                {
                    if (value >= StartTimeSeconds && value <= (StartTimeSeconds + _SourceInfo.Duration.Seconds))
                    {
                        DurationTimeSeconds = value - StartTimeSeconds;
                    }
                }
                OnPropertyChanged("EndTimeSeconds");
                OnPropertyChanged("EndTimeHMS");
            }
        }
        public string EndTimeHMS
        {
            get
            {
                if (_VideoEncoder == VideoEncoders.Still_JPG || _VideoEncoder == VideoEncoders.Still_PNG)
                {
                    return StartTimeHMS;
                }
                else
                {
                    if (_DurationTime.DurationType == DurationTypes.Frames)
                    {
                        if (_IsOutFramerateEnabled && OutFrameRate > 0 && _SourceInfo != null && _SourceInfo.FrameRate != 0)
                        {
                            return new TimeDuration() { Frames = StartTimeFrames + Convert.ToInt32(DurationTimeFramesAdjusted * _SourceInfo.FrameRate / OutFrameRate) }.HMS;
                        }
                        else
                        {
                            return new TimeDuration() { Frames = StartTimeFrames + DurationTimeFramesAdjusted }.HMS;
                        }
                    }
                    else
                    {
                        return new TimeDuration() { Seconds = StartTimeSeconds + DurationTimeSeconds }.HMS;
                    }
                }
            }
            set
            {
                if (value.Contains("f") && !value.Contains(":") && !value.Contains("."))
                {
                    int tryFrames;
                    if (int.TryParse(value.Remove(value.IndexOf("f")), out tryFrames))
                    {
                        EndTimeFrames = tryFrames;
                    }
                }
                else if (!value.Contains("f"))
                {
                    string sValue = value.Contains("s") ? value.Remove(value.IndexOf("s")) : value;
                    EndTimeSeconds = new TimeDuration() { HMS = sValue }.Seconds;
                }
            }
        }
        public int EndTimeFrames
        {
            get 
            {
                return StartTimeFrames + DurationTimeFramesAdjusted;
            }
            set
            {
                if (SourceInfo != null)
                {
                    if (value >= StartTimeFrames && value <= (StartTimeFrames + TimeDuration.GetFrames(_SourceInfo.Duration.Seconds, _SourceInfo.FrameRate)))
                    {
                        DurationTimeFramesAdjusted = value - StartTimeFrames;
                        OnPropertyChanged("EndTimeSeconds");
                        OnPropertyChanged("EndTimeHMS");
                    }
                }
            }
        }

        private TimeDuration _PreviewTime;
        public double PreviewTimeSeconds
        {
            get => Math.Round(_PreviewTime.Seconds, 6);
            set
            {
                _PreviewTime.Seconds = Math.Round(value, 3);
                OnPropertyChanged("PreviewTimeSeconds");
                OnPropertyChanged("PreviewTimeHMS");
            }
        }
        public string PreviewTimeHMS => _PreviewTime.HMS;
        public int PreviewTimeFrames => _SourceInfo != null ? TimeDuration.GetFrames(_PreviewTime.Seconds, SourceInfo.FrameRate) : 0;

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
                int orFrames = 0;
                if (_DurationTime.DurationType == DurationTypes.Frames)
                {
                    if (_IsOutFramerateEnabled && OutFrameRate > 0 && _SourceInfo != null && _SourceInfo.FrameRate != 0)
                    {
                        orFrames = Convert.ToInt32(DurationTimeFramesAdjusted / OutFrameRate * _SourceInfo.FrameRate);
                    }
                    else
                    {
                        orFrames = DurationTimeFramesAdjusted;
                    }
                }

                _IsOutFramerateEnabled = value;
                
                if (orFrames > 0)
                {
                    DurationTimeFramesAdjusted = orFrames;
                }

                OnPropertyChanged("IsOutFramerateEnabled");
                OnPropertyChanged("OutFrameRate");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("DurationTimeHMS");
            }
        }
        private double _OutFrameRate;
        public double OutFrameRate
        {
            get
            {
                if (_IsOutFramerateEnabled && _OutFrameRate > 0)
                {
                    return Math.Round(_OutFrameRate, 2);
                }
                else
                {
                    if (SourceInfo != null)
                    {
                        return Math.Round(SourceInfo.FrameRate, 2);
                    }
                }
                return 0;
            }
            set
            {
                int orFrames = 0;
                if (_DurationTime.DurationType == DurationTypes.Frames)
                {
                    if (_IsOutFramerateEnabled && _OutFrameRate > 0 && _SourceInfo != null && _SourceInfo.FrameRate != 0)
                    {
                        orFrames = Convert.ToInt32(DurationTimeFramesAdjusted / _OutFrameRate * _SourceInfo.FrameRate);
                    }
                }
                _OutFrameRate = value;
                if (orFrames > 0)
                {
                    DurationTimeFramesAdjusted = orFrames;
                }
                
                OnPropertyChanged("OutFrameRate");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("DurationTimeHMS");
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
            }

            if (SourceInfo != null && SourceInfo.Duration != null)
            {
                VideoResolutionParams.Horizontal = SourceInfo.HorizontalResolution;
                VideoResolutionParams.Vertical = SourceInfo.VerticalResolution;
                DurationTimeSeconds = SourceInfo.Duration.Seconds;
                OutFrameRate = SourceInfo.FrameRate;
                VideoBitrate = SourceInfo.VideoBitrate;
            }
            else
            {
                IsConversionEnabled = false;
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
                            _SourceInfo != null? _SourceInfo.FrameRate : 0,
                            IsOutFramerateEnabled? OutFrameRate : 0,
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
                            _SourceInfo != null ? _SourceInfo.FrameRate : 0,
                            OutFrameRate);
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
