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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DTConverter
{
    /// <summary>
    /// Communicates with ffmpeg, and ffprobe. 
    /// Before calling any other method, it's important to run FindFFPaths
    /// </summary>
    public static class FFmpegWrapper
    {
        public static string FFmpegVersion { get; private set; }
        public static string FFprobeVersion { get; private set; }
        static readonly string[] channels51 = { "FL", "FR", "FC", "LFE", "SL", "SR" };
        static readonly string[] channels51v = { "voutFL", "voutFR", "voutFC", "voutLFE", "voutSL", "voutSR" };


        /// <summary>
        /// Video encoders found by FFmpeg, this collection is retrieved for future use
        /// </summary>
        public static Dictionary<string, string> VideoEncoderDictionary { get; private set; }

        /// <summary>
        /// Audio encoders found by FFmpeg, this collection is retrieved for future use
        /// </summary>
        public static Dictionary<string, string> AudioEncoderDictionary { get; private set; }

        /// <summary>
        /// All paths are retrieved by FindFFPath which decides the best exes to use
        /// </summary>
        public static string FFmpegPath, FFprobePath; // FFplayPath

        /// <summary>
        /// Searches for ffmpeg, ffprobe executables and HAP codec. Path variables will be stored in static FFmpegWrapper class
        /// </summary>
        /// <param name="findCompleted">Bool will be true if everything was found</param>
        public static void FindFFPaths(Action<string, bool> outMessages, Action<bool> findCompleted)
        {
            bool errors = false;
            VideoEncoderDictionary = new Dictionary<string, string>();
            AudioEncoderDictionary = new Dictionary<string, string>();

            outMessages("Checking FFmpeg...", false);

            // local search
            FFmpegWrapper.FFmpegPath = FindExePath("ffmpeg.exe", Directory.GetCurrentDirectory());
            FFmpegWrapper.FFprobePath = FindExePath("ffprobe.exe", Directory.GetCurrentDirectory());
            //FFmpegWrapper.FFplayPath = FindExePath("ffplay.exe", Directory.GetCurrentDirectory());

            Task FFmpegVersionTask = Task.Run(() =>
            {
                try
                {
                    CheckFFmpegVersion();
                }
                catch (Exception E)
                {
                    errors = true;
                    outMessages(E.Message, true);
                }
            });


            Task FFprobeVersionTask = Task.Run(() =>
            {
                try
                {
                    CheckFFprobeVersion();
                }
                catch (Exception E)
                {
                    errors = true;
                    outMessages(E.Message, true);
                }
            });

            outMessages("Checking FFmpeg......", false);

            FFprobeVersionTask.Wait();
            FFmpegVersionTask.Wait();

            outMessages("Checking FFmpeg.........", false);

            if (FFmpegWrapper.FFmpegPath == null)
            {
                errors = true;
                outMessages("Could not find FFmpeg executable!", true);
            }
            else if (FFmpegWrapper.FFprobePath == null)
            {
                errors = true;
                outMessages("Could not find FFprobe executable!", false);
            }
            //else if (FFmpegWrapper.FFplayPath == null)
            //{
            //    outErrors.Invoke("Could not find FFplay executable!");
            //    errors = true;
            //}
            else if (!FFmpegWrapper.VideoEncoderDictionary.ContainsKey("hap"))
            {
                errors = true;
                outMessages("HAP Codec not found! Please install it.", true);
            }

            findCompleted.Invoke(!errors);
        }

        /// <summary>
        /// Checks recursively to find fileName in parentDir
        /// </summary>
        /// <param name="fileName">File to search</param>
        /// <param name="parendDir">Parent in which to search</param>
        /// <returns></returns>
        private static string FindExePath(string fileName, string parendDir)
        {
            string returningPath;

            foreach (string eachFile in Directory.GetFiles(parendDir))
            {
                if (fileName.ToLower() == Path.GetFileName(eachFile).ToLower())
                {
                    return Path.Combine(parendDir, eachFile);
                }
            }
            foreach (string eachDir in Directory.GetDirectories(parendDir))
            {
                returningPath = FindExePath(fileName, eachDir);
                if (returningPath != null)
                {
                    return returningPath;
                }
            }
            return null;
        }

        /// Tries to read version by launching a new process with FFmpegPath if not null, defaulting to ffmpeg.exe.
        /// Video and Audio encoders will be probed by this method.
        private static void CheckFFmpegVersion()
        {
            string line;

            ProcessStartInfo startInfo;
            startInfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

            if (FFmpegWrapper.FFmpegPath != null)
            {
                startInfo.FileName = FFmpegWrapper.FFmpegPath;
            }
            else
            {
                startInfo.FileName = "ffmpeg.exe";
            }
            startInfo.Arguments = "-encoders";
            
            Process ffmpegProc;
            ffmpegProc = Process.Start(startInfo);
            ffmpegProc.WaitForExit(1000);
            FFmpegWrapper.FFmpegPath = startInfo.FileName;

            // Read Video and Audio Encoders
            while (!ffmpegProc.StandardOutput.EndOfStream)
            {
                try
                {
                    // Enumerates encoders, lines will be like:
                    // V..... hap                  Vidvox Hap
                    line = ffmpegProc.StandardOutput.ReadLine().Trim();
                    string[] splitted = line.Split(' ');
                    //we will get a long array with a lot of "" elements
                    if ((splitted[0].Length == 6) && splitted[0].Contains('.'))
                    {
                        string encoderName = "";
                        string encoderDescription = "";
                        int j = 0;

                        for (int i = 0; i < splitted.Length; i++)
                        {
                            if (splitted[i].Length > 0)
                            {
                                if (j == 0)
                                {
                                    // first valid element is the capabilities string
                                    j++;
                                }
                                else if (j == 1)
                                {
                                    // second valid element is encoder name
                                    encoderName = splitted[i];
                                    j++;
                                }
                                else if (j > 1)
                                {
                                    // after this point, it's all description
                                    encoderDescription += splitted[i] + " ";
                                }
                            }
                        }

                        if (splitted[0].Contains('V'))
                        {
                            if (encoderName != "=")
                            {
                                FFmpegWrapper.VideoEncoderDictionary.Add(encoderName, encoderDescription);
                            }
                        }
                        if (splitted[0].Contains('A'))
                        {
                            if (encoderName != "=")
                            {
                                FFmpegWrapper.AudioEncoderDictionary.Add(encoderName, encoderDescription);
                            }
                        }
                    }
                }
                catch { }
            }

            // Version is printed in StandardError 
            while (!ffmpegProc.StandardError.EndOfStream)
            {
                try
                {
                    line = ffmpegProc.StandardError.ReadLine().Trim();
                    if (line.Contains("version") && line.Contains("ffmpeg"))
                    {
                        FFmpegWrapper.FFmpegVersion = line.Replace("version", "").Replace("ffmpeg", "").Trim().Split(' ')[0];
                    }
                }
                catch { }
            }
        }


        /// <summary>
        /// Tries to read version by launching a new process with FFprobePath if not null, defaulting to ffprobe.exe.
        /// </summary>
        private static void CheckFFprobeVersion()
        {
            string line;
            
            ProcessStartInfo startInfo;
            startInfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            if (FFmpegWrapper.FFprobePath != null)
            {
                startInfo.FileName = FFmpegWrapper.FFprobePath;
            }
            else
            {
                startInfo.FileName = "ffprobe.exe";
            }
            startInfo.Arguments = "-version";

            Process ffprobeProc;
            ffprobeProc = Process.Start(startInfo);
            ffprobeProc.WaitForExit(1000);
            FFmpegWrapper.FFprobePath = startInfo.FileName;

            // Version is printed in StandardOutput 
            while (!ffprobeProc.StandardOutput.EndOfStream)
            {
                try
                {
                    line = ffprobeProc.StandardOutput.ReadLine().ToLower();
                    if (line.Contains("version") && line.Contains("ffprobe"))
                    {
                        FFmpegWrapper.FFprobeVersion = line.Replace("version", "").Replace("ffprobe", "").Trim().Split(' ')[0];
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Probes all possible informations from SourcePath. 
        /// This method takes few seconds to execute so it should be run in a separate Thread or Task
        /// </summary>
        /// <param name="SourcePath">Video or Audio file complete path</param>
        /// <param name="timeout">Time in ms to wait for the process to finish</param>
        /// <returns>VideoInfo with all informations read</returns>
        public static VideoInfo ProbeVideoInfo(string SourcePath, int timeout)
        {
            if (FFprobePath == null)
            {
                return null;
            }
            if (timeout <= 0)
            {
                timeout = 1000;
            }

            VideoInfo videoInfo = new VideoInfo();
            
            Process FFprobeProcess = new Process();
            FFprobeProcess.StartInfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            FFprobeProcess.StartInfo.FileName = FFmpegWrapper.FFprobePath;
            FFprobeProcess.StartInfo.Arguments = $"\"{SourcePath}\"";
            FFprobeProcess.Start();

            string lineRead;
            string[] splitted;
            string duration;
            string videoCodec, chromaSubsampling, resolution, videoBitrate, framerate;
            string audioCodec, samplingRate, audioBitrate;

            while (!FFprobeProcess.StandardError.EndOfStream)
            {
                try
                {
                    videoInfo.SourcePath = SourcePath;
                    lineRead = FFprobeProcess.StandardError.ReadLine().ToLower().Trim();
                    splitted = lineRead.Split(',');
                    if (lineRead.StartsWith("duration:"))
                    {
                        duration = splitted[0].Split(' ')[1].Trim();
                        videoInfo.Duration = new TimeDuration()
                        {
                            HMS = duration
                        };
                    }
                    if (lineRead.Contains("video:"))
                    {
                        videoInfo.HasVideo = true;
                        videoCodec = splitted[0].Split(':').Last().Split('(')[0].Trim();
                        videoInfo.VideoCodec = videoCodec;

                        foreach (string sPiece in splitted)
                        {
                            if (sPiece.Contains("420") || sPiece.Contains("422") || sPiece.Contains("444"))
                            {
                                chromaSubsampling = sPiece.Split('(')[0];
                                videoInfo.ChromaSubsampling = chromaSubsampling;
                            }
                            if (sPiece.Contains('x'))
                            {
                                Match m = Regex.Match(sPiece, @"\d+x\d+");
                                if (m.Success)
                                {
                                    resolution = m.Value;
                                    videoInfo.HorizontalResolution = int.Parse(resolution.Split('x')[0]);
                                    videoInfo.VerticalResolution = int.Parse(resolution.Split('x')[1]);
                                }
                            }
                            if (sPiece.Contains("fps"))
                            {
                                framerate = sPiece.Trim().Split(' ')[0].Trim();
                                videoInfo.Framerate = double.Parse(framerate, CultureInfo.InvariantCulture);
                            }
                            if (sPiece.Contains("kb/s"))
                            {
                                videoBitrate = sPiece.Trim().Split(' ')[0].Trim();
                                videoInfo.VideoBitrate = int.Parse(videoBitrate);
                            }
                        }
                    }
                    if (lineRead.Contains("audio:"))
                    {
                        videoInfo.HasAudio = true;
                        foreach (string sPiece in splitted)
                        {
                            if (sPiece.Contains(':'))
                            {
                                audioCodec = sPiece.Split(':').Last().Split('(')[0].Trim();
                                if (FFmpegWrapper.AudioEncoderDictionary.ContainsKey(audioCodec))
                                {
                                    videoInfo.AudioCodec = audioCodec;
                                }
                            }
                            
                            if (sPiece.Contains("hz"))
                            {
                                samplingRate = splitted[1].Trim().Split(' ')[0].Trim();
                                videoInfo.AudioSamplingRate = int.Parse(samplingRate);
                            }
                            if (sPiece.Contains("1 channel") || sPiece.Contains("mono"))
                            {
                                videoInfo.AudioChannels = AudioChannels.Mono;
                            }
                            if (sPiece.Contains("2 channel") || sPiece.Contains("stereo"))
                            {
                                videoInfo.AudioChannels = AudioChannels.Stereo;
                            }
                            if (sPiece.Contains('.'))
                            {
                                Match m = Regex.Match(sPiece, @"\d\.\d");
                                if (m.Success)
                                {
                                    if (m.Value == "5.1")
                                    {
                                        videoInfo.AudioChannels = AudioChannels.ch_5_1;
                                    }
                                }
                            }
                            if (sPiece.Contains("kb/s"))
                            {
                                audioBitrate = sPiece.Trim().Split(' ')[0].Trim();
                                videoInfo.AudioBitrate = int.Parse(audioBitrate);
                            }
                        }
                    }
                    if (videoInfo.VideoCodec == "png" || videoInfo.VideoCodec == "mjpeg")
                    {
                        videoInfo.Duration = new TimeDuration() { Frames = 1 };
                    }
                }
                catch (Exception E)
                { }
            }
            FFprobeProcess.WaitForExit(timeout);
            return videoInfo;
        }

        /// <summary>
        /// Creates the Process for converting videos and images, not audio. The Process returned needs to be started
        /// </summary>
        /// <param name="srcVideoPath">Complete path of source file (C:\dir\file.ext)</param>
        /// <param name="dstVideoPath">Complete path of destination file (C:\dir\file.ext")</param>
        public static Process ConvertVideoAudio(string srcVideoPath, string dstVideoPath,
            TimeDuration start, TimeDuration duration,
            VideoEncoders videoEncoder,
            VideoResolution videoResolution,
            int videoBitrate,
            double outFramerate,
            int rotation, bool rotateMetadataOnly,
            Crop crop, Padding padding, Slicer slices,
            string srcAudioPath, string dstAudioPath,
            AudioEncoders audioEncoder,
            int audioRate,
            bool isAudioChannelsEnabled, AudioChannels inChannels, AudioChannels outChannels, bool splitChannels)
        {
            if (FFmpegPath == null) { return null; }

            // There are 4 main lists:
            // vArgsIn             : common arguments such as start time
            // vFilters/aFilters   : filter_complex for crop, slices, audio split, etc. Will be aggregated by AggregateFilters
            // vArgsOut/aArgsOut   : encoders, parameters for encoders. Will be put at the end of every map
            // vMaps/aMaps         : connect streams to real output files

            // Filters should be added in this way: name=all:the:parameters [out_connector]
            // Aggregate method will aggregate all filters using connectors
            // Video and Audio streams can be split in separate files, or joined together (destinationVideoPath == destinationAudioPath)
            // When video slices are more than 1x1, audio channels must not be split
            // When splitting audio channels, slices must be 1x1

            List<string> argsIn = new List<string>();
            List<string> vFilters = new List<string>();
            List<string> vSlices = new List<string>();
            List<string> aFilters = new List<string>();
            List<string> aSlices = new List<string>();
            List<string> metadatas = new List<string>();
            List<string> vOptions = new List<string>();
            List<string> vArgsOut = new List<string>();
            List<string> aArgsOut = new List<string>();
            List<string> vMaps = new List<string>();
            List<string> aMaps = new List<string>();


            string strArgsIn;
            string strvFilters = "";
            string straFilters = "";
            string strMetadata;
            string strvArgsOut;
            string straArgsOut;
            string strvDuration = "";
            string straDuration = "";
            string strvMapOut = "";
            string straMapOut = "";
            string strvMaps;
            string straMaps;
            string ffArguments;

            if (audioEncoder == AudioEncoders.None && videoEncoder == VideoEncoders.None)
            {
                throw new Exception("At least Audio or Video encoder must be selected");
            }

            if (dstVideoPath == dstAudioPath &&
                (videoEncoder == VideoEncoders.None || audioEncoder == AudioEncoders.None))
            {
                throw new Exception("Audio and Video encoders must not be None when joining video and audio");
            }

            if (slices != null && slices.IsEnabled && videoEncoder != VideoEncoders.Copy && videoEncoder != VideoEncoders.None)
            {
                if (isAudioChannelsEnabled && splitChannels && audioEncoder != AudioEncoders.Copy && audioEncoder != AudioEncoders.None)
                {
                    throw new Exception("Cannot split audio channels when using video slices");
                }

                if (dstVideoPath == dstAudioPath && audioEncoder == AudioEncoders.Copy)
                {
                    throw new Exception("Cannot join video slices and audio Copy");
                }
            }

            #region argsIn
            argsIn.Add("-hide_banner");

            // skip Subtitles, Data streams
            argsIn.Add($"-sn -dn");

            // FFmpeg does not accept frames as input start
            if (start > 0)
            {
                argsIn.Add($"-ss {Math.Round(start.Seconds, 6).ToString(CultureInfo.InvariantCulture)}s");
            }

            if (videoEncoder == VideoEncoders.None)
            {
                argsIn.Add($"-vn");
                argsIn.Add($"-i \"{srcAudioPath}\"");
            }
            else
            {
                argsIn.Add($"-i \"{srcVideoPath}\"");
            }

            if (audioEncoder == AudioEncoders.None)
            {
                argsIn.Add($"-an");
            }
            else
            {
                // a double input is not a problem, it will be ignored
                argsIn.Add($"-i \"{srcAudioPath}\"");
            }

            strArgsIn = argsIn.Aggregate("", AggregateWithSpace);
            #endregion

            #region Video ArgsOut
            if (videoEncoder != VideoEncoders.None)
            {
                string strvEncoder = null;
                switch (videoEncoder)
                {
                    case VideoEncoders.HAP:
                        strvEncoder = "hap";
                        break;
                    case VideoEncoders.HAP_Alpha:
                        strvEncoder = "hap -format hap_alpha";
                        break;
                    case VideoEncoders.HAP_Q:
                        strvEncoder = "hap -format hap_q";
                        break;
                    case VideoEncoders.H264:
                        strvEncoder = "libx264";
                        vOptions.Add("-preset medium");
                        vOptions.Add("-tune fastdecode");
                        //vOptions.Add("-profile baseline";
                        break;
                    case VideoEncoders.Still_JPG:
                        strvEncoder = "mjpeg -f image2";
                        break;
                    case VideoEncoders.Still_PNG:
                        strvEncoder = "png -f image2";
                        break;
                    case VideoEncoders.JPG_Sequence:
                        strvEncoder = "mjpeg -f image2";
                        break;
                    case VideoEncoders.PNG_Sequence:
                        strvEncoder = "png -f image2";
                        break;
                    case VideoEncoders.Copy:
                        strvEncoder = "copy";
                        break;
                    default:
                        break;
                }

                if (strvEncoder != null)
                {
                    vArgsOut.Add($"-c:v {strvEncoder}");

                    if (videoEncoder != VideoEncoders.Copy)
                    {
                        // bframes 0
                        vOptions.Add("-bf 0");
                    }

                    if (outFramerate > 0 && videoEncoder != VideoEncoders.Copy)
                    {
                        // framerate
                        vArgsOut.Add($"-r {outFramerate.ToString(CultureInfo.InvariantCulture)}");
                        
                        // gop size
                        vOptions.Add($"-g {Math.Round(outFramerate, 2)}");
                    }
                    vArgsOut.Add(vOptions.Aggregate("", AggregateWithSpace));

                    // force CBR
                    if (videoBitrate > 0 && videoEncoder != VideoEncoders.Copy)
                    {
                        vArgsOut.Add($"-b:v {videoBitrate}k -minrate {videoBitrate}k -maxrate {videoBitrate}k");
                    }
                }

                if (videoEncoder == VideoEncoders.Still_JPG || videoEncoder == VideoEncoders.Still_PNG)
                {
                    strvDuration = $"-frames:v 1";
                }
                else if (duration > 0)
                {
                    if (duration.DurationType == DurationTypes.Frames)
                    {
                        strvDuration = $"-frames:v {duration.Frames}";
                    }
                    else
                    {
                        strvDuration = $"-t {Math.Round(duration.Seconds, 6).ToString(CultureInfo.InvariantCulture)}s";
                    }
                }
            }
            #endregion

            #region Audio ArgsOut
            if (audioEncoder != AudioEncoders.None)
            {
                string straEncoder = null;
                switch (audioEncoder)
                {
                    case AudioEncoders.WAV_16bit:
                        straEncoder = "pcm_s16le";
                        break;
                    case AudioEncoders.WAV_24bit:
                        straEncoder = "pcm_s24le";
                        break;
                    case AudioEncoders.WAV_32bit:
                        straEncoder = "pcm_s32le";
                        break;
                    case AudioEncoders.Copy:
                        straEncoder = "copy";
                        break;
                }

                if (straEncoder != null)
                {
                    aArgsOut.Add($"-c:a {straEncoder}");
                }

                if (audioRate > 0 && audioEncoder != AudioEncoders.Copy)
                {
                    aArgsOut.Add($"-ar {audioRate}");
                }

                if (duration > 0)
                {
                    // using frames as duration for audio-only files, returns an audio file of different length than the video-only file with same frames
                    straDuration = $"-t {Math.Round(duration.Seconds, 6).ToString(CultureInfo.InvariantCulture)}s";
                }
            }

            
            #endregion

            metadatas.Add("-metadata comment=\"Encoded with DT Converter\"");

            #region Video Filters
            if (videoEncoder != VideoEncoders.None && videoEncoder != VideoEncoders.Copy)
            {
                // Crop
                if (crop != null && crop.IsEnabled)
                {
                    vFilters.Add($"crop=iw-{crop.Left}-{crop.Right}:ih-{crop.Top}-{crop.Bottom}:{crop.X}:{crop.Y} [cropped]");
                }

                // Padding
                if (padding != null && padding.IsEnabled)
                {
                    vFilters.Add($"pad=iw+{padding.Left}+{padding.Right}:ih+{padding.Top}+{padding.Bottom}:{padding.Left}:{padding.Top} [padded]");
                }

                // Scale Resolution
                if (videoResolution != null && videoResolution.IsEnabled)
                {
                    // -s option uses scale and keeps input aspect ratio, so it may happen to have a wrong display aspect ratio in output file
                    vFilters.Add($"scale={videoResolution.Horizontal}:{videoResolution.Vertical},setsar=1/1 [scaled]");
                }

                // Rotation
                string metadataRot = "-metadata:s:v:0 ";
                if (rotateMetadataOnly)
                {
                    if (rotation == 90)
                    {
                        metadatas.Add($"{metadataRot} rotate=-90");
                    }
                    else if (rotation == 180)
                    {
                        metadatas.Add($"{metadataRot} rotate=-180 ");
                    }
                    else if (rotation == 270)
                    {
                        metadatas.Add($"{metadataRot} rotate=-270 ");
                    }
                }
                else
                {
                    if (rotation == 90)
                    {
                        vFilters.Add($"transpose=1 [rotated]");
                    }
                    else if (rotation == 180)
                    {
                        vFilters.Add($"transpose=2, transpose=2 [rotated]");
                    }
                    else if (rotation == 270)
                    {
                        vFilters.Add($"transpose=0 [rotated]");
                    }
                }
            }
            #endregion

            #region Slices
            string straSplitConnectors = "";
            if (videoEncoder != VideoEncoders.None && videoEncoder != VideoEncoders.Copy)
            {
                if (slices != null && slices.IsEnabled &&
                (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1))
                {
                    // vSlices will contain all crop filters for each slice, like:
                    // [split_ric1] crop=w:h:x:y: [cropped_r1c1]; [cropped_r1c1] scale=h:v [scaledh_r1c1]; [scaledh_r1c1] scale=h:v [vout_r1c1]
                    
                    string strvSplitConnectors = "";
                    string sliceConnector;
                    string w, h, x, y;

                    w = $"(iw+{slices.HorizontalOverlap}*{slices.HorizontalNumber - 1})/{slices.HorizontalNumber}";
                    h = $"(ih+{slices.VerticalOverlap}*{slices.VerticalNumber - 1})/{slices.VerticalNumber}";

                    // for r,c add slices in vfilters
                    for (int r = 1; r <= slices.VerticalNumber; r++)
                    {
                        y = $"{h}*{r - 1}-({ slices.VerticalOverlap}*{r - 1})";
                        for (int c = 1; c <= slices.HorizontalNumber; c++)
                        {
                            sliceConnector = $"r{r}c{c}";
                            
                            // split connectors for video
                            strvSplitConnectors += $"[vsplit_{sliceConnector}]";
                            
                            // split connectos for audio
                            straSplitConnectors += $"[asplit_{sliceConnector}]";

                            x = $"{w}*{c - 1}-({ slices.HorizontalOverlap}*{c - 1})";

                            vSlices.Add($"[vsplit_{sliceConnector}] crop={w}:{h}:{x}:{y},setsar=1/1 [cropped_{sliceConnector}]");
                            // Round final slice resolution to Multiple
                            vSlices.Add($"[cropped_{sliceConnector}] scale=0:-{videoResolution.Multiple} [scaledh_{sliceConnector}]");
                            vSlices.Add($"[scaledh_{sliceConnector}] scale=-{videoResolution.Multiple}:0 [vout_{sliceConnector}]");
                        }
                    }
                    
                    // add a split filter with all splitConnectors as outputs
                    vFilters.Add($"split={slices.HorizontalNumber * slices.VerticalNumber} {strvSplitConnectors}");
                }
                else
                {
                    // Round final resolution to Multiple, even if no filters were added, because input file may be of a non-multiple size
                    vFilters.Add($"scale=0:-{videoResolution.Multiple} [scaledh]");
                    vFilters.Add($"scale=-{videoResolution.Multiple}:0 [vout]");
                }
            }
            #endregion

            #region AudioChannels
            if (isAudioChannelsEnabled && audioEncoder != AudioEncoders.None && audioEncoder != AudioEncoders.Copy)
            {
                if (splitChannels)
                {
                    switch (outChannels)
                    {
                        case AudioChannels.Mono:
                            // take L
                            aFilters.Add($"channelsplit=1 [L]");
                            break;
                        case AudioChannels.Stereo:
                            // source should be stereo, otherwise only L and R will be considered
                            aFilters.Add($"channelsplit=channel_layout=stereo [L][R]");
                            if (dstVideoPath == dstAudioPath)
                            {
                                // 2 videos with audio will be output
                                vFilters.Add($"split=2 [voutL][voutR]");
                            }

                            break;
                        case AudioChannels.ch_5_1:
                            // source should be 5.1, otherwise error will occur
                            if (inChannels == AudioChannels.ch_5_1)
                            {
                                aFilters.Add($"channelsplit=channel_layout=5.1 {channels51.Aggregate("", AggregateWithSquareBrackets)}");
                                if (dstVideoPath == dstAudioPath)
                                {
                                    // if video is requested, 6 videos with audio will be output
                                    vFilters.Add($"split=6 {channels51v.Aggregate("", AggregateWithSquareBrackets)}");
                                }
                            }
                            else
                            {
                                throw new Exception($"Cannot split a {inChannels} source into 6 channels");
                            }
                            break;
                    }
                }
                else
                {
                    switch (outChannels)
                    {
                        case AudioChannels.Mono:
                            // Any source will be downmixed to mono
                            aArgsOut.Add($"-ac 1");
                            break;
                        case AudioChannels.Stereo:
                            // if source is mono: output will be dual mono
                            // if source is 5.1: output will be downmixed to stereo
                            aArgsOut.Add($"-ac 2");
                            break;
                        case AudioChannels.ch_5_1:
                            if (inChannels == AudioChannels.Mono)
                            {
                                aFilters.Add($"asplit=6 {channels51.Aggregate("", AggregateWithSquareBrackets)}");
                                aFilters.Add($"join=inputs=6:channel_layout=5.1 [aout]");
                            }
                            else if (inChannels == AudioChannels.Stereo)
                            {
                                aFilters.Add($"channelsplit=channel_layout=stereo [L][R]");
                                aFilters.Add($"join=inputs=2:channel_layout=5.1:map=0.0-FL|1.0-FR|0.0-FC|0.0-BL|1.0-BR|1.0-LFE [aout]");
                            }
                            else if (inChannels == AudioChannels.ch_5_1)
                            {
                                // 5.1 source will be encoded without mappings
                            }
                            break;
                    }
                }
            }
            #endregion

            strMetadata = metadatas.Aggregate("", AggregateWithSpace);
            strvArgsOut = vArgsOut.Aggregate("", AggregateWithSpace);
            straArgsOut = aArgsOut.Aggregate("", AggregateWithSpace);

            #region Maps
            if (slices != null && slices.IsEnabled &&
                (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1) &&
                videoEncoder != VideoEncoders.None && videoEncoder != VideoEncoders.Copy && dstVideoPath != null)
            {
                // Slices
                if (audioEncoder != AudioEncoders.None)
                {
                    if (dstVideoPath == dstAudioPath)
                    {
                        if (videoEncoder != VideoEncoders.Still_JPG && videoEncoder != VideoEncoders.Still_PNG &&
                                    videoEncoder != VideoEncoders.JPG_Sequence && videoEncoder != VideoEncoders.PNG_Sequence &&
                                    audioEncoder != AudioEncoders.Copy)
                        {
                            // add a split filter that will feed audio to all slices
                            aFilters.Add($"asplit={slices.HorizontalNumber * slices.VerticalNumber} {straSplitConnectors}");
                        }
                    }
                    else
                    {
                        if (dstAudioPath != null)
                        {
                            if (audioEncoder == AudioEncoders.Copy)
                            {
                                aMaps.Add($"-map 0:a {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, VideoEncoders.None)}\"");
                            }
                            else
                            {
                                aMaps.Add($"-map \"[aout]\" {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, VideoEncoders.None)}\"");
                            }
                        }
                    }
                }

                for (int r = 1; r <= slices.VerticalNumber; r++)
                {
                    for (int c = 1; c <= slices.HorizontalNumber; c++)
                    {
                        strvMapOut = $"-map \"[vout_r{r}c{c}]\" {strvArgsOut}";

                        if (dstVideoPath == dstAudioPath)
                        {
                            if (audioEncoder != AudioEncoders.None && audioEncoder != AudioEncoders.Copy)
                            {
                                if (videoEncoder != VideoEncoders.Still_JPG && videoEncoder != VideoEncoders.Still_PNG &&
                                    videoEncoder != VideoEncoders.JPG_Sequence && videoEncoder != VideoEncoders.PNG_Sequence)
                                {
                                    straMapOut = $"-map \"[aout_r{r}c{c}]\" {straArgsOut}";
                                }
                            }
                        }

                        // straMapOut may be "" so it's not necessary to check the audioEncoder
                        vMaps.Add($"{strvMapOut} {straMapOut} {strvDuration} {strMetadata} \"{DestinationPath(dstVideoPath, r, c, videoEncoder)}\"");
                    }
                }
            }
            else
            {
                // Not slices
                if (videoEncoder != VideoEncoders.None)
                {
                    strvMapOut = videoEncoder == VideoEncoders.Copy ? $"-map 0:v {strvArgsOut}" : $"-map \"[vout]\" {strvArgsOut}";
                }
                if (audioEncoder != AudioEncoders.None)
                {
                    straMapOut = audioEncoder == AudioEncoders.Copy ? $"-map 0:a {straArgsOut}" : $"-map \"[aout]\" {straArgsOut}";
                }

                if (dstVideoPath == dstAudioPath &&
                    videoEncoder != VideoEncoders.Still_JPG && videoEncoder != VideoEncoders.Still_PNG &&
                    videoEncoder != VideoEncoders.JPG_Sequence && videoEncoder != VideoEncoders.PNG_Sequence)
                {
                    // Join Audio and Video
                    if (isAudioChannelsEnabled && splitChannels && dstAudioPath != null && audioEncoder != AudioEncoders.Copy && audioEncoder != AudioEncoders.None)
                    {
                        switch (outChannels)
                        {
                            // strvMapOut may be "" so it's not necessary to check the videoEncoder
                            case AudioChannels.Mono:
                                aMaps.Add($"{strvMapOut} -map \"[L]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, "L")}\"");
                                break;
                            case AudioChannels.Stereo:
                                // source should be stereo, otherwise only L and R will be considered
                                aMaps.Add($"{strvMapOut.Replace("vout", "voutL")} -map \"[L]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, "L")}\"");
                                aMaps.Add($"{strvMapOut.Replace("vout", "voutR")} -map \"[R]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, "R")}\"");
                                break;
                            case AudioChannels.ch_5_1:
                                string[] channels51 = { "FL", "FR", "FC", "LFE", "SL", "SR" };
                                foreach (string ch in channels51)
                                {
                                    aMaps.Add($"{strvMapOut.Replace("vout", "vout" + ch)} -map \"[{ch}]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, ch)}\"");
                                }
                                break;
                        }
                    }
                    else
                    {
                        vMaps.Add($"{strvMapOut} {straMapOut} {straDuration} {strMetadata} \"{DestinationPath(dstVideoPath, videoEncoder)}\"");
                    }
                }
                else
                {
                    // Not Join Audio and Video
                    if (isAudioChannelsEnabled && splitChannels && dstAudioPath != null && audioEncoder != AudioEncoders.Copy && audioEncoder != AudioEncoders.None)
                    {
                        switch (outChannels)
                        {
                            case AudioChannels.Mono:
                                aMaps.Add($"-map \"[L]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, "L")}\"");
                                break;
                            case AudioChannels.Stereo:
                                // source should be stereo, otherwise only L and R will be considered
                                aMaps.Add($"-map \"[L]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, "L")}\"");
                                aMaps.Add($"-map \"[R]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, "R")}\"");
                                break;
                            case AudioChannels.ch_5_1:
                                string[] channels51 = { "FL", "FR", "FC", "LFE", "SL", "SR" };
                                foreach (string ch in channels51)
                                {
                                    aMaps.Add($"-map \"[{ch}]\" {straArgsOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, ch)}\"");
                                }
                                break;
                        }

                        if (videoEncoder != VideoEncoders.None && dstVideoPath != null)
                        {
                            vMaps.Add($"{strvMapOut} {strvDuration} {strMetadata} \"{DestinationPath(dstVideoPath, videoEncoder)}\"");
                        }
                    }
                    else
                    {
                        if (videoEncoder != VideoEncoders.None && dstVideoPath != null)
                        {
                            vMaps.Add($"{strvMapOut} {strvDuration} {strMetadata} \"{DestinationPath(dstVideoPath, videoEncoder)}\"");
                        }

                        if (audioEncoder != AudioEncoders.None && dstAudioPath != null)
                        {
                            aMaps.Add($"{straMapOut} {straDuration} {strMetadata} \"{DestinationPath(dstAudioPath, VideoEncoders.None)}\"");
                        }
                    }
                }
            }

            strvMaps = vMaps.Aggregate("", AggregateWithSpace);
            straMaps = aMaps.Aggregate("", AggregateWithSpace);
            #endregion

            #region Arguments
            if (videoEncoder != VideoEncoders.None && videoEncoder != VideoEncoders.Copy)
            {
                strvFilters = $"[0:v] {vFilters.Aggregate("", AggregateFilters)}";

                if (vSlices.Count > 0)
                {
                    strvFilters += $"; {vSlices.Aggregate("", AggregateWithSemicolon)}";
                }
            }

            if (audioEncoder != AudioEncoders.None && audioEncoder != AudioEncoders.Copy)
            {
                if (aFilters.Count == 0)
                {
                    aFilters.Add("anull [aout]");
                }

                if (dstVideoPath == dstAudioPath)
                {
                    if (slices != null && slices.IsEnabled &&
                        (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1) &&
                        videoEncoder != VideoEncoders.None && videoEncoder != VideoEncoders.Copy)
                    {
                        for (int r = 1; r <= slices.VerticalNumber; r++)
                        {
                            for (int c = 1; c <= slices.HorizontalNumber; c++)
                            {
                                aSlices.Add($"[asplit_r{r}c{c}] anull [aout_r{r}c{c}]");
                            }
                        }
                    }
                }

                straFilters = $"[0:a] {aFilters.Aggregate("", AggregateFilters)}";
                if (aSlices.Count > 0)
                {
                    straFilters += $"; {aSlices.Aggregate(AggregateWithSemicolon)}";
                }
            }

            ffArguments = $"{strArgsIn}";
            if (strvFilters != "" || straFilters != "")
            {
                ffArguments += $" -filter_complex";
                
                if (strvFilters != "" && straFilters != "")
                {
                    ffArguments += $" \"{strvFilters}; {straFilters}\"";
                }
                else if (strvFilters != "")
                {
                    ffArguments += $" \"{strvFilters}\"";
                } 
                else if (straFilters != "")
                {
                    ffArguments += $" \"{straFilters}\"";
                }
            }

            ffArguments += $" {strvMaps} {straMaps}";
            #endregion

            // Process
            Process FFmpegProcess = new Process();
            FFmpegProcess.StartInfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            FFmpegProcess.StartInfo.FileName = FFmpegPath;
            FFmpegProcess.StartInfo.Arguments = ffArguments;
            return FFmpegProcess;
        }

        /// <summary>
        /// Aggregates elements separating them with a space
        /// </summary>
        /// <param name="Prev"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        private static string AggregateWithSpace(string Prev, string Next)
        {
            if (Prev.Trim() != "")
            {
                return Prev + " " + Next;
            }
            else
            {
                return Next;
            }
        }

        /// <summary>
        /// Aggregates elements separating them with a semicolon
        /// </summary>
        /// <param name="Prev"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        private static string AggregateWithSemicolon(string Prev, string Next)
        {
            if (Prev.Trim() != "")
            {
                return Prev + "; " + Next;
            }
            else
            {
                return Next;
            }
        }

        /// <summary>
        /// Aggregates elements enclosing them in [square][brackets]
        /// </summary>
        private static string AggregateWithSquareBrackets(string Prev, string Next)
        {
            if (Prev.Trim() != "")
            {
                return $"{Prev}[{Next}]";
            }
            else
            {
                return $"[{Next}]";
            }
        }

        /// <summary>
        /// Aggregates filters using out [connectors] of Prev filter as input [connectors] of Next filter
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private static string AggregateFilters(string prev, string next)
        {
            // -filter [ina] aaa [outa];[outa] bbb [outb]; [outb] ccc [outc]
            // -filter_complex [ia1][ia2][ia3] aaa [oa1][oa2][oa3]; [oa1][oa2][oa3] bbb [ob1][ob2][ob3]; [ob1][ob2][ob3] ccc [oc1][oc2][oc3]
            if (prev.Contains('[') && prev.Contains(']'))
            {
                return prev + "; " + prev.Substring(prev.LastIndexOf(' ')) + " " + next;
            }
            else
            {
                return next;
            }
        }

        /// <summary>
        /// Checks if destinationPath exists, adds time (hhmmss) to the end of originalName.
        /// If parent directory does not exists, tries to create it.
        /// </summary>
        public static string DestinationPath(string originalName, VideoEncoders vEncoder)
        {
            string destinationName = Path.GetFileNameWithoutExtension(originalName);
            string originalDir = Path.GetDirectoryName(originalName);

            // If image sequence rename its folder, instead of single file
            if (vEncoder == VideoEncoders.JPG_Sequence || vEncoder == VideoEncoders.PNG_Sequence)
            {
                if (Directory.Exists(originalDir))
                {
                    originalDir += "_" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00");
                }
            }
            else
            {
                if (File.Exists(originalName))
                {
                    destinationName += "_" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00");
                }
            }

            if (!Directory.Exists(originalDir))
            {
                Directory.CreateDirectory(originalDir);
            }

            destinationName += Path.GetExtension(originalName);
            return Path.Combine(originalDir, destinationName);
        }

        /// <summary>
        /// Generates the name for given path, adding channel name in a standard way.
        /// </summary>
        public static string DestinationPath(string originalName, string channel)
        {
            return DestinationPath(Path.Combine(
                Path.GetDirectoryName(originalName),
                Path.GetFileNameWithoutExtension(originalName) + $"_{channel}" + Path.GetExtension(originalName)), VideoEncoders.None);
        }

        /// <summary>
        /// Generates the name for given path, adding slice number in a standard way.
        /// </summary>
        public static string DestinationPath(string originalName, int r, int c, VideoEncoders vEncoder)
        {
            // If image sequence rename its folder, instead of each single image file
            if (vEncoder == VideoEncoders.JPG_Sequence || vEncoder == VideoEncoders.PNG_Sequence)
            {
                string sliceFile = Path.GetFileName(originalName);
                string sliceDir = Slicer.GetSliceName(Path.GetDirectoryName(originalName), r, c); 
                if (Directory.Exists(sliceDir))
                {
                    sliceDir += "_" + DateTime.Now.Hour.ToString("00") + DateTime.Now.Minute.ToString("00") + DateTime.Now.Second.ToString("00");
                }
                return DestinationPath(Path.Combine(sliceDir, sliceFile), vEncoder);
            }
            else
            {
                return DestinationPath(Slicer.GetSliceName(originalName, r, c), vEncoder);
            }
        }
    }
}
