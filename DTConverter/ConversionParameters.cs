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
    public enum VideoEncoders { HAP, HAP_Alpha, HAP_Q, H264, Still_PNG, Still_JPG, PNG_Sequence, JPG_Sequence, Copy, None }
    public enum AudioEncoders { WAV_16, WAV_24, WAV_32, Copy, None }
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

        /// <summary>
        /// Call OnPropertyChanged of all properties contained in this
        /// </summary>
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
            // Every new addition or change MUST be reported here !
            // Reference (non-value) types must be copied with Clone()

            // SourcePath should not be copied, it needs to remain original path
            // SourceInfo should not be copied, source never changes
            // Isvalid should not be copied, user must not copy a valid item into a non-valid item
            // ThumbnailPathIn should not be copied
            // PreviewResolution should not be copied

            StartTime = copyFrom.StartTime.Clone();
            EndTime = copyFrom.EndTime.Clone();
            PreviewTime = copyFrom.PreviewTime.Clone();

            IsConversionEnabled = IsValid && copyFrom.IsConversionEnabled;
            VideoEncoder = copyFrom.VideoEncoder;
            AudioEncoder = copyFrom.AudioEncoder;
            IsAudioRateEnabled = copyFrom.IsAudioRateEnabled;
            AudioRate = copyFrom.AudioRate;
            IsChannelsEnabled = copyFrom.IsChannelsEnabled;
            Channels = copyFrom.Channels;
            SplitChannels = copyFrom.SplitChannels;

            VideoResolutionParams = copyFrom.VideoResolutionParams.Clone();

            IsVideoBitrateEnabled = copyFrom.IsVideoBitrateEnabled;
            VideoBitrate = copyFrom.VideoBitrate;
            IsOutFramerateEnabled = copyFrom.IsOutFramerateEnabled;
            OutFramerate = copyFrom.OutFramerate;
            IsRotationEnabled = copyFrom.IsRotationEnabled;
            Rotation = copyFrom.Rotation;
            RotateMetadataOnly = copyFrom.RotateMetadataOnly;
            CropParams = copyFrom.CropParams.Clone();
            PaddingParams = copyFrom.PaddingParams.Clone();
            SliceParams = copyFrom.SliceParams.Clone();

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

        /// <summary>
        /// SourcePath is set once in the constructor, no edits are allowed
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Destination Path will be generated on property get, it will be based on values of this ConversionParameters
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
                        int nDigits = TimeDuration.GetFrames(DurationTime.Seconds, OutFramerate).ToString().Length;
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
                return SourceInfo != null ?
                    SourceInfo.Duration > 0 ? true : false :
                false;
            }
        }

        /// <summary>
        /// StartTime's Framerate is related to source framerate
        /// </summary>
        private TimeDuration _StartTime;
        public TimeDuration StartTime
        {
            get => _StartTime;
            set
            {
                if (value >= 0 && SourceInfo != null && value < SourceInfo.Duration)
                {
                    _StartTime = value;
                    _StartTime.Framerate = SourceInfo.Framerate;
                    DurationTime = EndTime - StartTime;
                    _StartTime.PropertyChanged += StartTime_PropertyChanged;
                }
                OnPropertyChanged("StartTime");
            }

        }

        /// <summary>
        /// This is used only for bindings
        /// </summary>
        public string StartTimeHMS
        {
            get => _StartTime.HMS;
            set
            {
                TimeDuration newStart = new TimeDuration() { HMS = value, Framerate = _StartTime.Framerate };
                if (newStart < EndTime)
                {
                    StartTime = newStart;
                }
            }
        }

        private void StartTime_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DurationTime = EndTime - StartTime;
            OnPropertyChanged("StartTimeHMS");
        }

        /// <summary>
        /// EndTime's Framerate is related to source framerate
        /// </summary>
        private TimeDuration _EndTime;
        public TimeDuration EndTime
        {
            get => _EndTime;
            set
            {
                if (value > 0 && SourceInfo != null && value <= SourceInfo.Duration)
                {
                    _EndTime = value;
                    _EndTime.Framerate = SourceInfo.Framerate;
                    DurationTime = EndTime - StartTime;
                    _EndTime.PropertyChanged += EndTime_PropertyChanged;
                }
                OnPropertyChanged("EndTime");
            }
        }

        /// <summary>
        /// This is used only for bindings
        /// </summary>
        public string EndTimeHMS
        {
            get => _EndTime.HMS;
            set
            {
                TimeDuration newEnd = new TimeDuration() { HMS = value, Framerate = _EndTime.Framerate };
                if (newEnd > StartTime)
                {
                    EndTime = newEnd;
                }
            }
        }
        
        private void EndTime_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            DurationTime = EndTime - StartTime;
            OnPropertyChanged("EndTimeHMS");
        }

        /// <summary>
        /// DurationTime's Framerate is related to output framerate
        /// </summary>
        private TimeDuration _DurationTime;
        public TimeDuration DurationTime
        {
            get => _DurationTime;
            set
            {
                if (value > 0 && StartTime + value <= EndTime)
                {
                    _DurationTime = value;
                    _DurationTime.Framerate = OutFramerate;
                }
                OnPropertyChanged("DurationTime");
                OnPropertyChanged("DurationTimeEquivalent");
                
            }
        }

        /// <summary>
        /// Returns a TimeDuration with Frames multipliead by the ratio between output and input framerate
        /// </summary>
        public TimeDuration DurationTimeEquivalent
        {
            get
            {
                if (_DurationTime != null)
                {
                    return _DurationTime.DurationType == DurationTypes.Frames ?
                        _DurationTime * (_DurationTime.Framerate / StartTime.Framerate) :
                        _DurationTime;
                }
                return null;
            }
        }

        private TimeDuration _PreviewTime;
        /// <summary>
        /// This is used only for bindings
        /// </summary>
        public TimeDuration PreviewTime
        {
            get => _PreviewTime;
            set
            {
                if (value < EndTime)
                {
                    _PreviewTime = value;
                    _PreviewTime.Framerate = SourceInfo != null ? SourceInfo.Framerate : 0;
                }
                OnPropertyChanged("PreviewTime");
            }
        }

        /// <summary>
        /// Gets a GridLenght of 1* if conditions for opening ColPreviewOut are met, otherwise 0*
        /// </summary>
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

        /// <summary>
        /// Gets a Visibility of Visible if conditions for showing IsChkOriginalVisible are met, otherwise Collapsed
        /// </summary>
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

        public bool IsVideoEnabled => _SourceInfo != null ? VideoEncoder != VideoEncoders.None && _SourceInfo.HasVideo : false;

        public bool IsAudioEnabled => _SourceInfo != null ? AudioEncoder != AudioEncoders.None && _SourceInfo.HasAudio : false;

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
                OnPropertyChanged("DurationTimeEquivalent");
                OnPropertyChanged("EndTimeHMS");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("IsVideoEnabled");
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

        /// <summary>
        /// Gets horizontal video resolution considering Croppings and Paddings
        /// </summary>
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

        /// <summary>
        /// Gets vertical video resolution considering Croppings and Paddings
        /// </summary>
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
        /// <summary>
        ///  Video Bitrate in Kb/s
        /// </summary>
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
                DurationTime.Framerate = value ? OutFramerate :
                    SourceInfo != null ? SourceInfo.Framerate : 0;

                OnPropertyChanged("IsOutFramerateEnabled");
                OnPropertyChanged("OutFrameRate");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("DurationTime");
                OnPropertyChanged("DurationTimeEquivalent");
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
                DurationTime.Framerate = value;
                
                OnPropertyChanged("OutFrameRate");
                OnPropertyChanged("DestinationVideoPath");
                OnPropertyChanged("DurationTime");
                OnPropertyChanged("DurationTimeEquivalent");
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
                OnPropertyChanged("IsAudioEnabled");
                OnPropertyChanged("DestinationAudioPath");
                OnPropertyChanged("IsAudioEncoderNotCopy");
            }
        }
        public bool IsAudioEncoderNotCopy => AudioEncoder != AudioEncoders.Copy;

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
        /// This method takes some seconds to execute so it should be run in a separate Thread or Task
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
                    OutFramerate = SourceInfo.Framerate;
                    VideoBitrate = SourceInfo.VideoBitrate;
                    
                    StartTime = new TimeDuration() { Seconds = 0 };
                    EndTime = new TimeDuration() { Seconds = SourceInfo.Duration.Seconds };
                    DurationTime = new TimeDuration() { Seconds = SourceInfo.Duration.Seconds };
                    PreviewTime = new TimeDuration { Seconds = SourceInfo.Duration.Seconds / 2 };
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
        /// This method takes few seconds to execute so it should be run in a separate Thread or Task
        /// </summary>
        public void CreateImagePreviewIn(DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived)
        {
            if (VideoConversionStatus != ConversionStatus.CreatingPreviewIn)
            {
                VideoConversionStatus = ConversionStatus.CreatingPreviewIn;
                try
                {
                    ProcessPreviewIn = FFmpegWrapper.ConvertVideoAudio(SourcePath, ThumbnailPathIn, PreviewTime, new TimeDuration() { Frames = 1 }, VideoEncoders.Still_JPG,
                        PreviewResolution, 0, 0, 0, false, null, null, null,
                        null, null, AudioEncoders.None, 0, false, AudioChannels.Mono, AudioChannels.Mono, false);
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
                            throw new Exception($"Creating input preview image failed with error {ProcessPreviewIn.ExitCode}");
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

        /// <summary>
        /// Instantly kills PreviewIn creation
        /// </summary>
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
        /// This method takes few seconds to execute so it should be run in a separate Thread or Task
        /// </summary>
        public void CreateImagePreviewOut(DataReceivedEventHandler outputDataReceived, DataReceivedEventHandler errorDataReceived)
        {
            if (VideoConversionStatus != ConversionStatus.CreatingPreviewOut)
            {
                VideoConversionStatus = ConversionStatus.CreatingPreviewOut;
                try
                {
                    if (SliceParams != null && SliceParams.IsEnabled &&
                    (SliceParams.HorizontalNumber > 1 || SliceParams.VerticalNumber > 1))
                    {
                        for (int r = 1; r <= SliceParams.VerticalNumber; r++)
                        {
                            for (int c = 1; c <= SliceParams.HorizontalNumber; c++)
                            {
                                if (File.Exists(Slicer.GetSliceName(ThumbnailPathOut, r, c)))
                                {
                                    File.Delete(Slicer.GetSliceName(ThumbnailPathOut, r, c));
                                }
                            }
                        }
                    }
                    if (File.Exists(ThumbnailPathOut))
                    {
                        File.Delete(ThumbnailPathOut);
                    }
                    ProcessPreviewOut = FFmpegWrapper.ConvertVideoAudio(SourcePath, ThumbnailPathOut, PreviewTime, new TimeDuration() { Frames = 1 }, VideoEncoders.Still_JPG,
                        VideoResolutionParams, 0, 0, IsRotationEnabled ? Rotation : 0, false, CropParams, PaddingParams, SliceParams,
                        null, null, AudioEncoders.None, 0, false, AudioChannels.Mono, AudioChannels.Mono, false);
                    ProcessPreviewOut.OutputDataReceived += outputDataReceived;
                    ProcessPreviewOut.ErrorDataReceived += errorDataReceived;
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

        /// <summary>
        /// Instantly kills PreviewOut creation
        /// </summary>
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
        /// Converts video and audio stream. 
        /// If DestinationVideoPath == DestinationAudioPath the resulting video will contain audio, otherwise video and audio will be in separate files.
        /// This method takes several seconds to execute so it should be run it in a separate Thread or Task
        /// </summary>
        public void ConvertVideoAudio(DataReceivedEventHandler outputReceived, DataReceivedEventHandler errorReceived)
        {
            if (VideoConversionStatus != ConversionStatus.Converting)
            {
                VideoConversionStatus = ConversionStatus.Converting;
                try
                {
                    if (IsValid && IsConversionEnabled)
                    {
                        VideoConversionProcess = FFmpegWrapper.ConvertVideoAudio(SourcePath, DestinationVideoPath, StartTime, DurationTimeEquivalent, VideoEncoder,
                            VideoResolutionParams,
                            IsVideoBitrateEnabled? VideoBitrate : 0,
                            IsOutFramerateEnabled? OutFramerate : 0,
                            IsRotationEnabled? Rotation : 0, RotateMetadataOnly, CropParams, PaddingParams, SliceParams,
                            SourcePath, (true)? DestinationVideoPath : DestinationAudioPath, AudioEncoder,
                            IsAudioRateEnabled ? AudioRate : 0, 
                            IsChannelsEnabled, SourceInfo != null? SourceInfo.AudioChannels : AudioChannels.Stereo, Channels, SplitChannels);
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
        /// Instantly kills this video conversion
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
