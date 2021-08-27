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

namespace DTConverter
{
    public class VideoInfo
    {
        public string SourcePath { get; set; }
        public TimeDuration Duration { get; set; }

        public bool HasVideo { get; set; }
        public string VideoCodec { get; set; }
        public string ChromaSubsampling { get; set; }
        public int HorizontalResolution { get; set; }
        public int VerticalResolution { get; set; }
        public string VideoResolution => HorizontalResolution.ToString() + "x" + VerticalResolution.ToString();

        public double AspectRatio => VerticalResolution != 0 ? 1.0 * HorizontalResolution / VerticalResolution : -1;
        /// <summary>
        /// Bitrate in kb/s
        /// </summary>
        public int VideoBitrate { get; set; }
        /// <summary>
        /// Framerate in fps
        /// </summary>
        public float FrameRate { get; set; }

        public bool HasAudio { get; set; }
        public string AudioCodec { get; set; }

        /// <summary>
        /// Sampling rate in Hz
        /// </summary>
        public int AudioSamplingRate { get; set; }
        public string AudioChannels { get; set; }

        /// <summary>
        /// Bitrate in kb/s
        /// </summary>
        public int AudioBitrate { get; set; }
    }
}
