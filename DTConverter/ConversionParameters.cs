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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace DTConverter
{
    public enum VideoEncoders { HAP, HAP_Alpha, HAP_Q, H264, Still_PNG, Still_JPG, PNG_Sequence, JPG_Sequence, Copy }
    public enum AudioEncoders { pcm_s16le }
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
            StartTime = new TimeDuration();
            DurationTime = new TimeDuration();
            PreviewTime = new TimeDuration();
            CropParams = new Crop();
            PaddingParams = new Padding();
            SliceParams = new Slicer();

            PreviewResolution = new VideoResolution
            {
                Horizontal = 640,
                Vertical = 360
            };

            IsValid = false;
            VideoConversionStatus = ConversionStatus.None;
            AudioConversionStatus = ConversionStatus.None;

            IsConversionEnabled = true;
            IsVideoEnabled = true;
            IsAudioEnabled = true;
            VideoEncoder = VideoEncoders.HAP;
            AudioEncoder = AudioEncoders.pcm_s16le;

            VideoBitrate = 0;
            OutFrameRate = 0;
            Rotation = 0;
            RotateMetadataOnly = false;

            IsCropEnabled = false;
            IsPaddingEnabled = false;
            IsSliceEnabled = false;
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
            // Isvalid should not be copied, we don't want user to copy a valid item into a non-valid item
            // ThumbnailPath should not be copied

            StartTime = copyFrom.StartTime;
            DurationTime = copyFrom.DurationTime;
            PreviewTime = copyFrom.PreviewTime;

            PreviewResolution = copyFrom.PreviewResolution;

            IsConversionEnabled = copyFrom.IsConversionEnabled;
            IsVideoEnabled = copyFrom.IsVideoEnabled;
            IsAudioEnabled = copyFrom.IsAudioEnabled;
            VideoEncoder = copyFrom.VideoEncoder;
            AudioEncoder = copyFrom.AudioEncoder;

            VideoResolutionParams.Horizontal = copyFrom.VideoResolutionParams.Horizontal;
            VideoResolutionParams.Vertical = copyFrom.VideoResolutionParams.Vertical;
            VideoBitrate = copyFrom.VideoBitrate;
            OutFrameRate = copyFrom.OutFrameRate;
            Rotation = copyFrom.Rotation;
            RotateMetadataOnly = copyFrom.RotateMetadataOnly;

            IsCropEnabled = copyFrom.IsCropEnabled;
            CropParams = copyFrom.CropParams;

            IsPaddingEnabled = copyFrom.IsPaddingEnabled;
            PaddingParams = copyFrom.PaddingParams;

            IsSliceEnabled = copyFrom.IsSliceEnabled;
            SliceParams = copyFrom.SliceParams;
        }

        // TODO: implement -pix_fmt

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
                    if ((VideoResolutionParams.Horizontal != 0) && (VideoResolutionParams.Vertical != 0))
                    {
                        outPath += $"_{VideoResolutionParams.Horizontal}x{VideoResolutionParams.Vertical}";
                    }
                    if (OutFrameRate != 0)
                    {
                        outPath += $"_{OutFrameRate}";
                    }
                    if (VideoBitrate != 0)
                    {
                        outPath += $"_{VideoBitrate}";
                    }
                    if ((VideoEncoder == VideoEncoders.JPG_Sequence) || (VideoEncoder == VideoEncoders.PNG_Sequence))
                    {
                        outPath = Path.Combine(outPath, Path.GetFileNameWithoutExtension(SourcePath));
                        int nDigits = DurationTime.GetFrames(OutFrameRate).ToString().Length;
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

                    // ChangeExtension even if there is not actually any extension works as espected
                    if (File.Exists(Path.ChangeExtension(outPath, extension)))
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
                // TODO: implement DestinationAudioPath
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
            set
            {
                _SourceInfo = value;
                OnPropertyChanged("SourceInfo");
            }
        }

        private bool _IsValid;
        /// <summary>
        /// Determines if it is a valid video or audio file by checking if duration is null
        /// </summary>
        public bool IsValid
        {
            get => _IsValid;
            private set
            {
                _IsValid = value;
                OnPropertyChanged("IsValid");
            }
        }

        private TimeDuration _StartTime;
        /// Start time must be in seconds
        public TimeDuration StartTime
        {
            get => _StartTime;
            set
            {
                _StartTime = value;
                OnPropertyChanged("StartTime");
            }
        }

        private TimeDuration _DurationTime;
        public TimeDuration DurationTime
        {
            get => _DurationTime;
            set
            {
                _DurationTime = value;
                OnPropertyChanged("DurationTime");
            }
        }

        private TimeDuration _PreviewTime;
        public TimeDuration PreviewTime
        {
            get => _PreviewTime;
            set
            {
                _PreviewTime = value;
                OnPropertyChanged("PreviewTime");
            }
        }

        private VideoResolution _PreviewResolution;
        public VideoResolution PreviewResolution
        {
            get => _PreviewResolution;
            set
            {
                _PreviewResolution = value;
                OnPropertyChanged("PreviewResolution");
            }
        }

        private string _ThumbnailPathIn;
        public string ThumbnailPathIn
        {
            get => _ThumbnailPathIn;
            set
            {
                _ThumbnailPathIn = value;
                OnPropertyChanged("ThumbnailPath");
                OnPropertyChanged("ThumbnailOutPath");
            }
        }

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
            get => _IsVideoEnabled;
            set
            {
                _IsVideoEnabled = value;
                OnPropertyChanged("IsVideoEnabled");
            }
        }

        private bool _IsAudioEnabled;
        public bool IsAudioEnabled
        {
            get => _IsAudioEnabled;
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
                        VideoResolutionParams.Multiple = 8;
                        break;
                    case VideoEncoders.HAP_Alpha:
                        VideoResolutionParams.Multiple = 8;
                        break;
                    case VideoEncoders.HAP_Q:
                        VideoResolutionParams.Multiple = 8;
                        break;
                    case VideoEncoders.H264:
                        VideoResolutionParams.Multiple = 8;
                        break;
                }

                OnPropertyChanged("VideoEncoder");
                OnPropertyChanged("IsVideoEncoderCopy");
                OnPropertyChanged("IsVideoEncoderH264");
                OnPropertyChanged("IsVideoEncoderNotCopy");
                OnPropertyChanged("IsVideoEncoderNotHAP");
            }
        }
        public bool IsVideoEncoderCopy => VideoEncoder == VideoEncoders.Copy;
        public bool IsVideoEncoderH264 => VideoEncoder == VideoEncoders.H264;
        public bool IsVideoEncoderNotCopy => VideoEncoder != VideoEncoders.Copy;
        public bool IsVideoEncoderNotHAP => !VideoEncoder.ToString().ToLower().Contains("hap");

        private AudioEncoders _AudioEncoder;
        public AudioEncoders AudioEncoder
        {
            get => _AudioEncoder;
            set
            {
                _AudioEncoder = value;
                OnPropertyChanged("AudioEncoder");
            }
        }

        private VideoResolution _VideoResolutionParams;
        public VideoResolution VideoResolutionParams
        {
            get => _VideoResolutionParams;
            set
            {
                _VideoResolutionParams = value;
                OnPropertyChanged("VideoResolutionParams");
            }
        }

        private int _VideoBitrate;
        // Video Bitrate in Kb/s
        public int VideoBitrate
        {
            get => _VideoBitrate;
            set
            {
                _VideoBitrate = value;
                OnPropertyChanged("VideoBitrate");
            }
        }

        private double _OutFrameRate;
        public double OutFrameRate
        {
            get => _OutFrameRate;
            set
            {
                _OutFrameRate = value;
                OnPropertyChanged("OutFrameRate");
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

        private bool _IsCropEnabled;
        public bool IsCropEnabled
        {
            get => _IsCropEnabled;
            set
            {
                _IsCropEnabled = value;
                OnPropertyChanged("IsCropEnabled");
            }
        }

        private Crop _CropParams;
        public Crop CropParams
        {
            get => _CropParams;
            set
            {
                _CropParams = value;
                OnPropertyChanged("CropParams");
            }
        }

        private bool _IsPaddingEnabled;
        public bool IsPaddingEnabled
        {
            get => _IsPaddingEnabled;
            set
            {
                _IsPaddingEnabled = value;
                OnPropertyChanged("IsPaddingEnabled");
            }
        }

        private Padding _PaddingParams;
        public Padding PaddingParams
        {
            get => _PaddingParams;
            set
            {
                _PaddingParams = value;
                OnPropertyChanged("PaddingParams");
            }
        }

        private bool _IsSliceEnabled;
        public bool IsSliceEnabled
        {
            get => _IsSliceEnabled;
            set
            {
                _IsSliceEnabled = value;
                OnPropertyChanged("IsSliceEnabled");
            }
        }

        private Slicer _SliceParams;
        public Slicer SliceParams
        {
            get => _SliceParams;
            set
            {
                _SliceParams = value;
                OnPropertyChanged("SliceParams");
            }
        }

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

        /// <summary>
        /// Generates the name for given path, adding r and c in a standard way.
        /// Every method that tries to access an r x c image should call this method.
        /// </summary>
        public string getSliceName(string originalName, int r, int c)
        {
            return Slicer.GetSliceName(originalName, r, c);
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
            }

            if (SourceInfo != null && SourceInfo.Duration != null)
            {
                IsValid = true;
            }
            else
            {
                IsConversionEnabled = false;
                IsValid = false;
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
                    ProcessPreviewIn = FFmpegWrapper.ConvertVideo(SourcePath, ThumbnailPathIn, _PreviewTime, new TimeDuration() { Frames = 1 }, VideoEncoders.Still_JPG, PreviewResolution,
                        0, 0, 0, false, false, null, false, null, false, null, SourceInfo);
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
                            if (ProcessPreviewIn.ExitCode != 0)
                            {
                                throw new Exception($"Error creating preview image ({ProcessPreviewIn.ExitCode})");
                            }
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
                    ProcessPreviewOut = FFmpegWrapper.ConvertVideo(SourcePath, ThumbnailPathOut, _PreviewTime, new TimeDuration() { Frames = 1 }, VideoEncoders.Still_JPG, VideoResolutionParams,
                        0, 0, Rotation, false, IsCropEnabled, CropParams, IsPaddingEnabled, PaddingParams, IsSliceEnabled, SliceParams, SourceInfo);
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
                            if (ProcessPreviewOut.ExitCode != 0)
                            {
                                throw new Exception($"Error creating out preview image ({ProcessPreviewOut.ExitCode})");
                            }
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
                        VideoConversionProcess = FFmpegWrapper.ConvertVideo(SourcePath, DestinationVideoPath,
                            StartTime, DurationTime,
                            VideoEncoder,
                            VideoResolutionParams,
                             VideoBitrate, OutFrameRate,
                             Rotation, RotateMetadataOnly,
                             IsCropEnabled, CropParams,
                             IsPaddingEnabled, PaddingParams,
                             IsSliceEnabled, SliceParams,
                             SourceInfo);
                        VideoConversionProcess.OutputDataReceived += outputReceived;
                        VideoConversionProcess.ErrorDataReceived += errorReceived;
                        if (VideoConversionProcess.Start())
                        {
                            VideoConversionProcess.BeginOutputReadLine();
                            VideoConversionProcess.BeginErrorReadLine();
                            if (!VideoConversionProcess.HasExited)
                            {
                                VideoConversionProcess.WaitForExit();
                                if (VideoConversionProcess.ExitCode != 0)
                                {
                                    throw new Exception($"Conversion Failed ({VideoConversionProcess.ExitCode})");
                                }
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
        public void KillConversion()
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