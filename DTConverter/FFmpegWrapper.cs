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
        public static string FFversion { get; private set; }

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
        public static void FindFFPaths(Action<string> outMessages, Action<string> outErrors, Action<bool> findCompleted)
        {
            bool errors = false;
            VideoEncoderDictionary = new Dictionary<string, string>();
            AudioEncoderDictionary = new Dictionary<string, string>();

            outMessages("Checking FFmpeg...");
            Task findFFmpegTask = Task.Run(() => FFmpegWrapper.FFmpegPath = FindExePath("ffmpeg.exe", outMessages, outErrors));
            Task findFFprobeTask = Task.Run(() => FFmpegWrapper.FFprobePath = FindExePath("ffprobe.exe", outMessages, outErrors));
            //Task findFFplayTask = Task.Run(() => FFmpegWrapper.FFplayPath = FindExePath("ffplay.exe", outMessages, outErrors));

            outMessages("Checking FFmpeg......");
            findFFmpegTask.Wait();
            findFFprobeTask.Wait();
            //findFFplayTask.Wait();          

            outMessages("Checking FFmpeg.........");
            if (FFmpegWrapper.FFmpegPath == null)
            {
                outErrors.Invoke("Could not find FFmpeg executable!");
                errors = true;
            }
            else if (FFmpegWrapper.FFprobePath == null)
            {
                outMessages.Invoke("Could not find FFprobe executable!");
                errors = true;
            }
            //else if (FFmpegWrapper.FFplayPath == null)
            //{
            //    outErrors.Invoke("Could not find FFplay executable!");
            //    errors = true;
            //}
            else if (!FFmpegWrapper.VideoEncoderDictionary.ContainsKey("hap"))
            {
                outErrors("HAP Codec not found! Please install it.");
                errors = true;
            }

            findCompleted.Invoke(!errors);
        }

        /// <summary>
        /// Tries to launch a new process using passed fileName. If a local executable is found, it will be preferred.
        /// If exe is ffmpeg.exe, Video and Audio encoders will be probed
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="outMessages"></param>
        /// <param name="outErrors"></param>
        /// <returns></returns>
        private static string FindExePath(string fileName, Action<string> outMessages, Action<string> outErrors)
        {
            // if exe is correctly installed on system, we can just call it
            string returningFileName = fileName;

            // if a local executable is present, we use this one
            try
            {
                foreach (string eachFile in Directory.GetFiles(Directory.GetCurrentDirectory()))
                {
                    if (fileName == eachFile.ToLower())
                    {
                        returningFileName = eachFile;
                    }
                }
            }
            catch (Exception E) 
            {
                outErrors(E.Message);
            }

            Process fProc;
            ProcessStartInfo startInfo;
            startInfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };

            startInfo.FileName = returningFileName;
            if (fileName.ToLower().Contains("ffmpeg"))
            {
                startInfo.Arguments = "-encoders";
            }
            else
            {
                startInfo.Arguments = "-version";
            }
            fProc = Process.Start(startInfo);
            fProc.WaitForExit(2000);

            string line;
            if (fileName.ToLower().Contains("ffmpeg"))
            {
                while (!fProc.StandardOutput.EndOfStream)
                {
                    try
                    {
                        // Enumerates encoders, lines will be like:
                        // V..... hap                  Vidvox Hap
                        line = fProc.StandardOutput.ReadLine().Trim();
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

                    catch (Exception E)
                    {
                        outErrors(E.Message);
                    }
                }
            }

            while (!fProc.StandardError.EndOfStream)
            {
                try
                {
                    line = fProc.StandardError.ReadLine();
                    if (line.ToLower().Contains("version"))
                    {
                        FFmpegWrapper.FFversion = line.Replace("version", "").Replace("ffmpeg", "").Trim();
                    }
                }
                catch (Exception E)
                {
                    outErrors(E.Message);
                }
            }

            return returningFileName;
        }
        
        /// <summary>
        /// Probes all possible informations from SourcePath. 
        /// This method takes few seconds to execute so it should be run in a separate Process or Task
        /// </summary>
        /// <param name="SourcePath"></param>
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
            string audioCodec, samplingRate, audioChannels, audioBitrate;

            while (!FFprobeProcess.StandardError.EndOfStream)
            {
                try
                {
                    videoInfo.SourcePath = SourcePath;
                    lineRead = FFprobeProcess.StandardError.ReadLine().Trim();
                    splitted = lineRead.Split(',');
                    if (lineRead.StartsWith("Duration:"))
                    {
                        duration = splitted[0].Split(' ')[1].Trim();
                        videoInfo.Duration = new TimeDuration()
                        {
                            HMS = duration
                        };
                    }
                    if (lineRead.Contains("Video:"))
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
                            if (sPiece.ToLower().Contains('x'))
                            {
                                Match m = Regex.Match(sPiece, @"\d+x\d+");
                                if (m.Success)
                                {
                                    resolution = m.Value;
                                    videoInfo.HorizontalResolution = int.Parse(resolution.Split('x')[0]);
                                    videoInfo.VerticalResolution = int.Parse(resolution.Split('x')[1]);
                                }
                            }
                            if (sPiece.ToLower().Contains("fps"))
                            {
                                framerate = sPiece.Trim().Split(' ')[0].Trim();
                                videoInfo.FrameRate = float.Parse(framerate.Replace('.', ','));
                            }
                            if (sPiece.ToLower().Contains("kb/s"))
                            {
                                videoBitrate = sPiece.Trim().Split(' ')[0].Trim();
                                videoInfo.VideoBitrate = int.Parse(videoBitrate);
                            }
                        }
                    }
                    if (lineRead.Contains("Audio:"))
                    {
                        videoInfo.HasAudio = true;
                        foreach (string sPiece in splitted)
                        {
                            audioCodec = sPiece.Split(':').Last().Split('(')[0].Trim();
                            if (sPiece.ToLower().Contains("hz"))
                            {
                                samplingRate = splitted[1].Trim().Split(' ')[0].Trim();
                                videoInfo.AudioSamplingRate = int.Parse(samplingRate);
                            }
                            if (sPiece.ToLower().Contains("mono"))
                            {
                                audioChannels = "mono";
                                videoInfo.AudioCodec = audioCodec;
                            }
                            if (sPiece.ToLower().Contains("stereo"))
                            {
                                audioChannels = "stereo";
                                videoInfo.AudioCodec = audioCodec;
                            }
                            if (sPiece.Contains('.'))
                            {
                                Match m = Regex.Match(sPiece, @"\d\.\d");
                                if (m.Success)
                                {
                                    audioChannels = m.Value;
                                    videoInfo.AudioCodec = audioCodec;
                                }
                            }
                            if (sPiece.ToLower().Contains("kb/s"))
                            {
                                audioBitrate = sPiece.Trim().Split(' ')[0].Trim();
                                videoInfo.AudioBitrate = int.Parse(audioBitrate);
                            }
                        }
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
        /// <param name="sourcePath">Complete path of source file (C:\dir\file.ext)</param>
        /// <param name="destinationPath">Complete path of destination file (C:\dir\file.ext")</param>
        public static Process ConvertVideo(string sourcePath, string destinationPath,
            TimeDuration start, TimeDuration duration,
            VideoEncoders videoEncoder,
            VideoResolution videoResolution,
            int videoBitrate, double outFramerate,
            int rotation, bool rotateMetadataOnly,
            bool isCropEnabled, Crop crop,
            bool isPaddingEnabled, Padding padding,
            bool isSliceEnabled, Slicer slices,
            VideoInfo videoInfo)
        {
            if (FFmpegPath == null)
            {
                return null;
            }

            string destinationDir = Path.GetDirectoryName(destinationPath);
            if (!Directory.Exists(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // There are 3 lists: vArgsIn, vFilters, and ffArgsOut
            // vArgsIn contains arguments to be given prior to input file
            // vFilters contains filters, that will be aggregated using connectors like
            // [connectorA] filter [connectorB]; [connectorB] filter [connectorC]; [connectorC] filter [connectorD]; ...
            // if IsSlicesEnabled, last filter of vFilters will be a split like
            // [connectorN] split [split_r1c1][split_r1c2][split_r2c1][split_r2c2]...
            // ffArgsOut wil be put at the end of ffArguments, or in each vMaps if IsSliceEnabled

            List<string> vArgsIn = new List<string>();
            List<string> vFilters = new List<string>();
            List<string> vArgsOut = new List<string>();

            string strArgsIn = "";
            string strvFilters = "";
            string strSlices = "";

            // Input
            vArgsIn.Add("-hide_banner");

            // FFmpeg does not accept frames as input start
            if (start.DurationType == DurationTypes.Frames)
            {
                start.Seconds = start.GetSeconds(outFramerate);
            }
            if (start.Seconds > 0)
            {
                vArgsIn.Add($"-ss {start.Seconds.ToString().Replace(',', '.')}s");
            }

            // skip Audio, Subtitles, Data streams
            vArgsIn.Add($"-an -sn -dn");

            // Input file
            vArgsIn.Add($"-i \"{sourcePath}\"");
            
            if (duration.Seconds > 0)
            {
                if ((duration.DurationType == DurationTypes.Seconds) ||
                    (duration.DurationType == DurationTypes.Milliseconds) ||
                    (duration.DurationType == DurationTypes.Microseconds))
                {
                    vArgsOut.Add($"-t {duration.Seconds.ToString().Replace(',', '.')}s");
                }
            }

            strArgsIn = vArgsIn.Aggregate("", AggregateWithSpace);

            // Output
            string vEncoder = null;
            string vFormat = null;

            switch (videoEncoder.ToString().ToLower())
            {
                case "hap":
                    vEncoder = "hap";
                    break;
                case "hap_alpha":
                    vEncoder = "hap -format hap_alpha";
                    break;
                case "hap_q":
                    vEncoder = "hap -format hap_q";
                    break;
                case "h264":
                    vEncoder = "libx264";
                    // TODO: elaborate h264 conversion
                    break;
                case "still_jpg":
                    vFormat = "image2";
                    break;
                case "still_ong":
                    vFormat = "image2";
                    break;
                case "jpg_sequence":
                    vFormat = "image2";
                    break;
                case "png_sequence":
                    vFormat = "image2";
                    break;
                case "copy":
                    vEncoder = "copy";
                    break;
                default:
                    break;
            }

            if (vEncoder != null)
            {
                vArgsOut.Add($"-c:v {vEncoder}");
            }

            if (vFormat != null)
            {
                vArgsOut.Add($"-f {vFormat}");
            }

            if ((videoResolution.Horizontal > 0) && (videoResolution.Vertical > 0))
            {
                //if (!isSliceEnabled)
                {
                    vArgsOut.Add($"-s {videoResolution.Horizontal}x{videoResolution.Vertical}");
                }
            }

            // force CBR
            if (videoBitrate != 0)
            {
                vArgsOut.Add($"-b {videoBitrate} -minrate {videoBitrate} -maxrate {videoBitrate}");
            }

            if (outFramerate != 0)
            {
                vArgsOut.Add($"-r {outFramerate}");
            }

            if (duration.DurationType == DurationTypes.Frames)
            {
                vArgsOut.Add($"-frames:v {duration.Frames}");
            }

            // Filters

            // Crop
            if (isCropEnabled)
            {
                vFilters.Add($"crop=iw-{crop.Left}-{crop.Right}:ih-{crop.Top}-{crop.Bottom}:{crop.Left}:{crop.Top} [cropped]");
            }

            // Padding
            if (isPaddingEnabled)
            {
                vFilters.Add($"pad=iw+{padding.Left}+{padding.Right}:ih+{padding.Top}+{padding.Bottom}:{padding.Left}:{padding.Top} [padded]");
            }

            // Rotation
            string metadata = "";
            if (rotateMetadataOnly)
            {
                if (rotation == 90)
                {
                    metadata += "rotate=-90";
                }
                else if (rotation == 180)
                {
                    metadata += "rotate=-180";
                }
                else if (rotation == 270)
                {
                    metadata += "rotate=-270";
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

            if (metadata != "")
            {
                vArgsOut.Add($"-metadata:s:v:0 {metadata}");
            }

            string strArgsOut;
            strArgsOut = vArgsOut.Aggregate("", AggregateWithSpace);

            if (isSliceEnabled && (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1))
            {
                // Add a split filter without the out connection, there will be many connectors!
                // This split is added here to know the previous out connector like [cropped] or [padded]
                vFilters.Add($"split={slices.HorizontalNumber * slices.VerticalNumber}");
            }

            string ffArguments;

            if (vFilters.Count > 0)
            {
                // There will be no more vFilters, so aggregate them here
                strvFilters = vFilters.Aggregate("", AggregateFilters);

                // vSlices will contain each crop for each slice, like
                // [split_ric1] crop=w:h:x:y: [out_r1c1]; [split_r1c2] crop=w:h:x:y: [out_r1c2]; [split_r2c1] crop=w:h:x:y: [out_r2c1]; [split_r2c2] crop=w:h:x:y: [out_r2c2]
                if (isSliceEnabled && (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1))
                {
                    List<string> vSlices = new List<string>();

                    string strSplitConnectors = "";
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
                            strSplitConnectors += $"[split_{sliceConnector}]";

                            x = $"{w}*{c-1}-({ slices.HorizontalOverlap}*{c-1})";

                            vSlices.Add($"[split_{sliceConnector}] crop={w}:{h}:{x}:{y} [out_{sliceConnector}]");
                        }
                    }

                    // complete strvFilters with many outputs of the split filter
                    strvFilters += " " + strSplitConnectors + ";";
                    strSlices = vSlices.Aggregate("", AggregateWithSemicolon);

                    strvFilters += " " + strSlices;

                    List<string> vMaps = new List<string>();
                    string destinationPathMapped;

                    for (int r = 1; r <= slices.VerticalNumber; r++)
                    {
                        for (int c = 1; c <= slices.HorizontalNumber; c++)
                        {
                            destinationPathMapped = Slicer.GetSliceName(destinationPath, r, c);
                            vMaps.Add($"-map \"[out_r{r}c{c}]\" {strArgsOut} \"{destinationPathMapped}\"");
                        }
                    }

                    string strvMaps;
                    strvMaps = vMaps.Aggregate("", AggregateWithSpace);

                    ffArguments = $"{strArgsIn} -filter_complex \"{strvFilters}\" {strvMaps}";
                }
                else
                {
                    ffArguments = $"{strArgsIn} -filter:v \"{strvFilters}\" {strArgsOut} \"{destinationPath}\"";
                }
            }
            else
            {
                ffArguments = $"{strArgsIn} {strArgsOut} \"{destinationPath}\"";
            }

            ffArguments += " -y";

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
        /// Aggregates filters using [out connector] of Prev filter as [input connector] of Next filter
        /// </summary>
        /// <param name="Prev"></param>
        /// <param name="Next"></param>
        /// <returns></returns>
        private static string AggregateFilters(string Prev, string Next)
        {
            if (Prev.Contains('[') && Prev.Contains(']'))
            {
                return Prev + "; " + Prev.Substring(Prev.LastIndexOf('[')) + " " + Next;
            }
            else
            {
                return Next;
            }
        }

        public static void ConvertAudio()
        {
            // TODO: implement ConvertAudio
        }
    }
}
