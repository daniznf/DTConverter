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

        /// Tries to read version by launching a new process with FFmpegPath if not null, defaulting to ffmpeg.exe and FFprobePath and ffprobe.exe.
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

        /// Tries to read version by launching a new process with FFprobePath if not null, defaulting to ffprobe.exe.
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
                                videoInfo.FrameRate = double.Parse(framerate, CultureInfo.InvariantCulture);
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
            int videoBitrate,
            double inFramerate,
            double outFramerate,
            int rotation, bool rotateMetadataOnly,
            Crop crop,
            Padding padding,
            Slicer slices)
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
            List<string> metadatas = new List<string>();
            List<string> vOptions = new List<string>();

            string strArgsIn;
            string strvFilters;
            string strArgsOut;

            string strSlices;

            // Input
            vArgsIn.Add("-hide_banner");

            // FFmpeg does not accept frames as input start
            if (start.DurationType == DurationTypes.Frames)
            {
                start.Seconds = TimeDuration.GetSeconds(start.Frames, inFramerate);
            }
            if (start.Seconds > 0)
            {
                vArgsIn.Add($"-ss {Math.Round(start.Seconds, 2).ToString(CultureInfo.InvariantCulture)}s");
            }

            // skip Audio, Subtitles, Data streams
            vArgsIn.Add($"-an -sn -dn");

            // Input file
            vArgsIn.Add($"-i \"{sourcePath}\"");

            strArgsIn = vArgsIn.Aggregate("", AggregateWithSpace);

            // Output
            string vEncoder = null;
            string vFormat = null;

            switch (videoEncoder)
            {
                case VideoEncoders.HAP:
                    vEncoder = "hap";
                    break;
                case VideoEncoders.HAP_Alpha:
                    vEncoder = "hap -format hap_alpha";
                    break;
                case VideoEncoders.HAP_Q:
                    vEncoder = "hap -format hap_q";
                    break;
                case VideoEncoders.H264:
                    vEncoder = "libx264";
                    vOptions.Add("-preset medium");
                    vOptions.Add("-tune fastdecode");
                    //vOptions.Add("-profile baseline";
                    break;
                case VideoEncoders.Still_JPG:
                    vFormat = "image2";
                    break;
                case VideoEncoders.Still_PNG:
                    vFormat = "image2";
                    break;
                case VideoEncoders.JPG_Sequence:
                    vFormat = "image2";
                    break;
                case VideoEncoders.PNG_Sequence:
                    vFormat = "image2";
                    break;
                case VideoEncoders.Copy:
                    vEncoder = "copy";
                    break;
                default:
                    break;
            }

            if (vEncoder != null)
            {
                vOptions.Add("-bf 0");
                if (outFramerate > 0)
                {
                    vOptions.Add($"-g {Math.Round(outFramerate, 2)}");
                }

                vArgsOut.Add($"-c:v {vEncoder}");
                vArgsOut.Add(vOptions.Aggregate("", AggregateWithSpace));
            }

            if (vFormat != null)
            {
                vArgsOut.Add($"-f {vFormat}");
            }

            // force CBR
            if (videoBitrate > 0)
            {
                vArgsOut.Add($"-b:v {videoBitrate}k -minrate {videoBitrate}k -maxrate {videoBitrate}k");
            }

            if (outFramerate > 0)
            {
                vArgsOut.Add($"-r {outFramerate.ToString(CultureInfo.InvariantCulture)}");
            }

            if (videoEncoder == VideoEncoders.Still_JPG || videoEncoder == VideoEncoders.Still_PNG)
            {
                duration = new TimeDuration() { Frames = 1 };
            }

            if (duration.DurationType != DurationTypes.Frames && duration.Seconds > 0)
            {
                vArgsOut.Add($"-t {duration.Seconds.ToString(CultureInfo.InvariantCulture)}s");
            }

            if (duration.DurationType == DurationTypes.Frames)
            {
                vArgsOut.Add($"-frames:v {duration.Frames}");
            }

            // Filters
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
                // -s option uses scale and keep input aspect ratio
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

            metadatas.Add("-metadata comment=\"Encoded with DT Converter\"");
            vArgsOut.Add(metadatas.Aggregate("", AggregateWithSpace));

            if (slices != null && slices.IsEnabled && (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1))
            {
                // Add a split filter without the out connection, there will be many connectors!
                // This split is added here to know the previous out connector like [cropped] or [padded]
                vFilters.Add($"split={slices.HorizontalNumber * slices.VerticalNumber}");
            }


            strArgsOut = vArgsOut.Aggregate("", AggregateWithSpace);

            string ffArguments;

            // vSlices will contain each crop for each slice, like
            // [split_ric1] crop=w:h:x:y: [out_r1c1]; [split_r1c2] crop=w:h:x:y: [out_r1c2]; [split_r2c1] crop=w:h:x:y: [out_r2c1]; [split_r2c2] crop=w:h:x:y: [out_r2c2]
            if (slices != null && slices.IsEnabled && (slices.HorizontalNumber > 1 || slices.VerticalNumber > 1))
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

                        x = $"{w}*{c - 1}-({ slices.HorizontalOverlap}*{c - 1})";

                        vSlices.Add($"[split_{sliceConnector}] crop={w}:{h}:{x}:{y},setsar=1/1 [cropped_{sliceConnector}]");

                        // Round final slice resolution to Multiple
                        vSlices.Add($"[cropped_{sliceConnector}] scale=0:-{videoResolution.Multiple} [scaledh_{sliceConnector}]");
                        vSlices.Add($"[scaledh_{sliceConnector}] scale=-{videoResolution.Multiple}:0 [out_{sliceConnector}]");
                    }
                }

                // last filter should be the split filter
                strvFilters = vFilters.Aggregate("", AggregateFilters);
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
                        vMaps.Add($"-map \"[out_r{r}c{c}]\" {strArgsOut} \"{destinationPathMapped}\" -y");
                    }
                }

                string strvMaps;
                strvMaps = vMaps.Aggregate("", AggregateWithSpace);

                ffArguments = $"{strArgsIn} -filter_complex \"{strvFilters}\" {strvMaps}";
            }
            else
            {
                // Round final resolution to Multiple
                vFilters.Add($"scale=0:-{videoResolution.Multiple} [scaledh]");
                vFilters.Add($"scale=-{videoResolution.Multiple}:0");

                strvFilters = vFilters.Aggregate("", AggregateFilters);
                ffArguments = $"{strArgsIn} -filter:v \"{strvFilters}\" {strArgsOut} \"{destinationPath}\"";
            }

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
        /// Aggregates filters using [out connector] of Prev filter as [input connector] of Next filter
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

        public static Process ConvertAudio(string sourcePath, string destinationPath,
            TimeDuration start, TimeDuration duration,
            AudioEncoders audioEncoder,
            int audioRate,
            bool isAudioChannelsEnabled, AudioChannels inChannels, AudioChannels outChannels, bool splitChannels,
            double videoInputFramerate)
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

            List<string> aArgsIn = new List<string>();
            List<string> aFilters = new List<string>();
            List<string> aArgsOut = new List<string>();
            List<string> metadatas = new List<string>();

            string strArgsIn;

            // Input
            aArgsIn.Add("-hide_banner");

            // FFmpeg does not accept frames as input start
            if (start.DurationType == DurationTypes.Frames)
            {
                start.Seconds =  TimeDuration.GetSeconds(start.Frames, videoInputFramerate);
            }
            if (start.Seconds > 0)
            {
                aArgsIn.Add($"-ss {Math.Round(start.Seconds, 2).ToString(CultureInfo.InvariantCulture)}s");
            }

            // skip Video, Subtitles, Data streams
            aArgsIn.Add($"-vn -sn -dn");

            // Input file
            aArgsIn.Add($"-i \"{sourcePath}\"");


            if (duration.DurationType == DurationTypes.Frames && duration.Frames > 0)
            {
                duration.Seconds = TimeDuration.GetSeconds(duration.Frames, videoInputFramerate);
                //aArgsOut.Add($"-frames:a {duration.Frames.ToString(CultureInfo.InvariantCulture)}");
            }
            if (duration.Seconds > 0)
            {
                aArgsOut.Add($"-t {duration.Seconds.ToString(CultureInfo.InvariantCulture)}s");
            }

            strArgsIn = aArgsIn.Aggregate("", AggregateWithSpace);

            // Output
            metadatas.Add("-metadata comment=\"Encoded with DT Converter\"");
            aArgsOut.Add(metadatas.Aggregate("", AggregateWithSpace));

            string aEncoder = null;
            
            // WAV_16bit, WAV_24bit, WAV_32bit
            switch (audioEncoder)
            {
                case AudioEncoders.WAV_16:
                    aEncoder = "pcm_s16le";
                    break;
                case AudioEncoders.WAV_24:
                    aEncoder = "pcm_s24le";
                    break;
                case AudioEncoders.WAV_32:
                    aEncoder = "pcm_s32le";
                    break;
                case AudioEncoders.Copy:
                    aEncoder = "copy";
                    break;
            }

            if (aEncoder != null)
            {
                aArgsOut.Add($"-c:a {aEncoder}");
            }
            else
            {
                throw new Exception("Audio Encoder cannot be null");
            }

            if (audioRate > 0)
            {
                aArgsOut.Add($"-ar {audioRate}");
            }

            string strArgsOut;
                        
            List<string> aMaps = new List<string>();
            
            if (isAudioChannelsEnabled)
            {
                strArgsOut = aArgsOut.Aggregate("", AggregateWithSpace);
                switch (outChannels)
                {
                    case AudioChannels.Mono:
                        if (splitChannels)
                        {
                            // take L
                            aFilters.Add($"channelsplit=1 [L]");
                            aMaps.Add($"-map \"[L]\" {strArgsOut} \"{destinationPathCh(destinationPath, "L")}\" -y");
                        }
                        else
                        {
                            // Any source will be downmixed to mono
                            aArgsOut.Add($"-ac 1");
                        }
                        break;
                    case AudioChannels.Stereo:
                        if (splitChannels)
                        {
                            // source should be stereo, otherwise only L and R will be considered
                            aFilters.Add($"channelsplit=channel_layout=stereo [L][R]");
                            aMaps.Add($"-map \"[L]\" {strArgsOut} \"{destinationPathCh(destinationPath, "L")}\"");
                            aMaps.Add($"-map \"[R]\" {strArgsOut} \"{destinationPathCh(destinationPath, "R")}\"");
                        }
                        else
                        {
                            // if source is mono: output will be dual mono
                            // if source is 5.1: output will be downmixed to stereo
                            aArgsOut.Add($"-ac 2");
                        }
                        break;
                    case AudioChannels.ch_5_1:
                        string[] channels51 = { "FL", "FR", "FC", "LFE", "SL", "SR" };
                        if (splitChannels)
                        {
                            // source should be 5.1, otherwise error will occur
                            if (inChannels == AudioChannels.ch_5_1)
                            {
                                aFilters.Add($"channelsplit=channel_layout=5.1 {channels51.Aggregate("", AggregateWithSquareBrackets)}");
                                foreach (string ch in channels51)
                                {
                                    aMaps.Add($"-map \"[{ch}]\" {strArgsOut} \"{destinationPathCh(destinationPath, ch)}\"");
                                }
                            }
                            else
                            {
                                throw new Exception($"Cannot split a {inChannels} source into 6 channels");
                            }
                        }
                        else
                        {
                            if (inChannels == AudioChannels.Mono)
                            {
                                aFilters.Add($"asplit=6 {channels51.Aggregate("", AggregateWithSquareBrackets)}");
                                aFilters.Add($"join=inputs=6:channel_layout=5.1");
                            }
                            else if (inChannels == AudioChannels.Stereo)
                            {
                                aFilters.Add($"channelsplit=channel_layout=stereo [L][R]");
                                aFilters.Add($"join=inputs=2:channel_layout=5.1:map=0.0-FL|1.0-FR|0.0-FC|0.0-BL|1.0-BR|1.0-LFE");
                            }
                            else if (inChannels == AudioChannels.ch_5_1)
                            {
                                // 5.1 source will be encoded without mappings
                            }
                        }
                        break;
                }
            }

            string straFilters;
            

            string straMaps;
            string ffArguments;

            strArgsOut = aArgsOut.Aggregate("", AggregateWithSpace);

            if (aFilters.Count > 0)
            {
                straFilters = aFilters.Aggregate("", AggregateFilters);

                if (splitChannels)
                {
                    straMaps = aMaps.Aggregate("", AggregateWithSpace);
                    ffArguments = $"{strArgsIn} -filter_complex \"{straFilters}\" {straMaps}";
                }
                else
                {
                    ffArguments = $"{strArgsIn} -filter_complex \"{straFilters}\" {strArgsOut} \"{destinationPath}\"";
                }
            }
            else
            {
                ffArguments = $"{strArgsIn} {strArgsOut} \"{destinationPath}\"";
            }
        
            // Process
            Process FFmpegProcess = new Process();
            FFmpegProcess.StartInfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            FFmpegProcess.StartInfo.FileName = FFmpegPath;
            FFmpegProcess.StartInfo.Arguments = ffArguments;
            return FFmpegProcess;
        }

        /// <summary>
        /// Generates the name for given path, adding channel standard way.
        /// </summary>
        public static string destinationPathCh(string originalName, string channel)
        {
            return Path.Combine(Path.GetDirectoryName(originalName), Path.GetFileNameWithoutExtension(originalName) + $"_{channel}" + Path.GetExtension(originalName));
        }
    }
}
