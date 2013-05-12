using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace Mygod.Skylark.Offline
{
    public partial class YouTube : Page
    {
        protected void GetEmAll()
        {
            string url = RouteData.GetRouteString("Rbase64"), path = Context.GetRelativePath();
            if (string.IsNullOrWhiteSpace(url)) return;
            foreach (var video in Video.GetVideoFromLink(LinkConverter.Decode(Rbase64.Decode(url))))
            {
                Response.Write(string.Format("<h3><a href='{1}'>{0}</a></h3>{2}", video.Title, video.Url, Environment.NewLine));
                foreach (var link in video.FmtStreamMap)
                {
                    Response.Write(string.Format("<div><a href=\"/Offline/{0}/?/{2}\">{1}</a></div>{3}",
                                                 Rbase64.Encode(link.Url), link, path, Environment.NewLine));
                    Response.Flush();
                }
            }
        }

        public static readonly WebClient Client = new WebClient();

        public class Video : VideoBase
        {
            private Video(string id)
            {
                var videoInfo = Client.DownloadString(string.Format(
                    "http://www.youtube.com/get_video_info?video_id={0}&eurl=http://mygodstudio.tk/", this.id = id));
                information = (from info in videoInfo.Split('&')
                               let i = info.IndexOf('=')
                               select new { Key = info.Substring(0, i), Value = info.Substring(i + 1) })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                if (!information.ContainsKey("status") || information["status"] == "fail")
                    throw new Exception("获取视频信息失败！原因：" + Uri.UnescapeDataString(information["reason"]).Replace('+', ' '));
                FmtStreamMap = information["url_encoded_fmt_stream_map"].UrlDecode().Split(',')
                    .SelectMany(s => FmtStream.Create(s, this)).ToList();
                information.Remove("url_encoded_fmt_stream_map");
            }

            private static readonly Regex
                R0 = new Regex("data-video-id=(\"|')([A-Za-z0-9_\\-]{11})\\1", RegexOptions.Compiled),
                R1 = new Regex("(\\?|&)v=([A-Za-z0-9_\\-]{11})", RegexOptions.Compiled),
                R2 = new Regex("youtube(|.googleapis).com/(v|embed)/([A-Za-z0-9_\\-]{11})", RegexOptions.Compiled);
            private static IEnumerable<Video> GetVideoFromContent(ISet<string> exception, string link)
            {
                var match = R0.Match(link);
                while (match.Success)
                {
                    Video result = null;
                    try
                    {
                        var id = match.Groups[2].Value;
                        if (!exception.Contains(id))
                        {
                            exception.Add(id);
                            result = new Video(id);
                        }
                    }
                    catch { }
                    if (result != null) yield return result;
                    match = match.NextMatch();
                }
                match = R1.Match(link);
                while (match.Success)
                {
                    Video result = null;
                    try
                    {
                        var id = match.Groups[2].Value;
                        if (!exception.Contains(id))
                        {
                            exception.Add(id);
                            result = new Video(id);
                        }
                    }
                    catch { }
                    if (result != null) yield return result;
                    match = match.NextMatch();
                }
                match = R2.Match(link);
                while (match.Success)
                {
                    Video result = null;
                    try
                    {
                        var id = match.Groups[3].Value;
                        if (!exception.Contains(id))
                        {
                            exception.Add(id);
                            result = new Video(id);
                        }
                    }
                    catch { }
                    if (result != null) yield return result;
                    match = match.NextMatch();
                }
            }
            public static IEnumerable<Video> GetVideoFromLink(string link)
            {
                var result = new HashSet<string>();
                foreach (var video in GetVideoFromContent(result, link)) yield return video;
                foreach (var video in GetVideoFromContent(result, Client.DownloadString(link))) yield return video;
            }

            private readonly string id;
            private readonly Dictionary<string, string> information;
            public readonly List<FmtStream> FmtStreamMap;

            public override string Title { get { return Uri.UnescapeDataString(information["title"]).Replace('+', ' '); } }
            public override string Author { get { return Uri.UnescapeDataString(information["author"]).Replace('+', ' '); } }
            public string Url { get { return "http://www.youtube.com/watch?v=" + id; } }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return obj.GetType() == GetType() && Equals((Video)obj);
            }
            private bool Equals(Video other)
            {
                return string.Equals(id, other.id);
            }
            public override int GetHashCode()
            {
                return (id != null ? id.GetHashCode() : 0);
            }
            public override string ToString()
            {
                return Title;
            }
        }

        public class FmtStream : VideoLinkBase
        {
            protected FmtStream(Video parent, string url)
            {
                this.parent = parent;
                this.url = url;
            }

            private FmtStream(VideoFormat videoFormat, VideoEncodings videoEncoding, int videoWidth, int videoHeight,
                              double? videoMinBitrate, double? videoMaxBitrate, AudioEncodings audioEncoding, int audioMinChannels,
                              int audioMaxChannels, int audioSamplingRate, int? audioBitrate, string url, Video parent)
                : this(parent, url)
            {
                Format = videoFormat;
                VideoEncoding = videoEncoding;
                MaxVideoWidth = videoWidth;
                MaxVideoHeight = videoHeight;
                VideoMinBitrate = videoMinBitrate;
                VideoMaxBitrate = videoMaxBitrate;
                AudioEncoding = audioEncoding;
                MinChannels = audioMinChannels;
                MaxChannels = audioMaxChannels;
                SamplingRate = audioSamplingRate;
                AudioBitrate = audioBitrate;
            }

            public static IEnumerable<FmtStream> Create(string data, Video parent)
            {
                var dic = (from info in data.Split('&')
                           let i = info.IndexOf('=')
                           select new { Key = info.Substring(0, i), Value = info.Substring(i + 1) })
                    .ToDictionary(pair => pair.Key, pair => pair.Value);
                var result = Create(Convert.ToInt32(dic["itag"]), dic["url"].UrlDecode() + "&signature=" + dic["sig"],
                                    dic["fallback_host"], parent).ToList();
                foreach (var u in result.OfType<UnknownFmtStream>())
                {
                    u.Quality = dic["quality"];
                    u.Type = dic["type"].UrlDecode();
                }
                return result;
            }

            private static IEnumerable<FmtStream> Create(int itag, string url, string fallbackHost, Video parent)
            {
                var fallbackUrl = url.Substring(7);
                fallbackUrl = "http://" + fallbackHost + fallbackUrl.Remove(0, fallbackUrl.IndexOf('/'));
                var urls = string.IsNullOrEmpty(fallbackHost) ? new[] { url } : new[] { url, fallbackUrl };
                switch (itag)
                {
                    case 0:     //OUTDATED, 4 Unknown
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.Undefined, 400, 240, null, null,
                                                                           AudioEncodings.MP3, 1, 1, 22050, null, u, parent);
                        yield break;
                    case 5:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.SorensonH263, 400, 240, 0.25, 0.25,
                                                                           AudioEncodings.MP3, 1, 2, 22050, 64, u, parent);
                        yield break;
                    case 6:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.SorensonH263, 450, 270, 0.8, 0.8,
                                                                           AudioEncodings.MP3, 1, 2, 44100, 64, u, parent);
                        yield break;
                    case 13:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.Format3GP, VideoEncodings.MPEG4Visual, 176, 144, 0.5, 0.5,
                                                                           AudioEncodings.AAC, 1, 1, 22050, 75, u, parent);
                        yield break;
                    case 17:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.Format3GP, VideoEncodings.MPEG4Visual, 176, 144, 2, 2,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 75, u, parent);
                        yield break;
                    case 18:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264Baseline, 640, 360, 0.5, 0.5,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 96, u, parent);
                        yield break;
                    case 22:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264High, 1280, 720, 2.0, 2.9,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 34:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.H264Main, 640, 360, 0.5, 0.5,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 35:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatFLV, VideoEncodings.H264Main, 854, 480, 0.8, 1,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 36:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.Format3GP, VideoEncodings.MPEG4Visual, 400, 240, 0.8, 0.8,
                                                                           AudioEncodings.AAC, 1, 1, 22050, 75, u, parent);
                        yield break;
                    case 37:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264High, 1920, 1080, 3.5, 5,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 38:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H264High, 4096, 3072, 0, 0,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 43:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 640, 360, 0.5, 0.5,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 44:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 854, 480, 1, 1,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 45:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 1280, 720, 2, 2,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 46:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP8, 1920, 1080, 2, 2,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 82:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 640, 360, 0.5, 0.5,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 96, u, parent);
                        yield break;
                    case 83:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 854, 240, 0.5, 0.5,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 84:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 1280, 720, 2, 2.9,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 85:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatMP4, VideoEncodings.H2643D, 1920, 520, 2, 2.9,
                                                                           AudioEncodings.AAC, 2, 2, 44100, 152, u, parent);
                        yield break;
                    case 100:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 640, 360, null, null,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 128, u, parent);
                        yield break;
                    case 101:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 854, 480, null, null,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 102:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 1280, 720, null, null,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    case 103:
                        foreach (var u in urls) yield return new FmtStream(VideoFormat.FormatWebM, VideoEncodings.VP83D, 1920, 540, null, null,
                                                                           AudioEncodings.Vorbis, 2, 2, 44100, 192, u, parent);
                        yield break;
                    default:
                        foreach (var u in urls) yield return new UnknownFmtStream(itag, u, parent);
                        yield break;
                }
            }

            // ReSharper disable MemberCanBePrivate.Global
            public readonly VideoFormat Format = VideoFormat.Undefined;
            public readonly VideoEncodings VideoEncoding = VideoEncodings.Undefined;
            public readonly int MaxVideoWidth;
            public readonly int MaxVideoHeight;
            public readonly double? VideoMinBitrate;
            public readonly double? VideoMaxBitrate;
            public readonly int MinChannels;
            public readonly int MaxChannels;
            public readonly AudioEncodings AudioEncoding = AudioEncodings.Undefined;
            public readonly int SamplingRate;
            public readonly int? AudioBitrate;
            // ReSharper restore MemberCanBePrivate.Global

            private readonly Video parent;
            public override string Properties
            {
                get
                {
                    return string.Format("视频格式：{0}{10}视频编码：{1}{10}视频最大大小：{2}x{3}{10}视频比特率：{4}MBps{10}" +
                                         "音频编码：{5}{10}音频声道数：{6}{10}音频采样速率：{7}{10}音频比特率：{8}KBps{10}" +
                                         "视频下载地址：{9}{10}", VideoFormatToString(), VideoEncodingsToString(), MaxVideoWidth,
                                         MaxVideoHeight, VideoBitrateToString(), AudioEncodingsToString(), ChannelsToString(),
                                         SamplingRate, AudioBitrate, Url, Environment.NewLine);
                }
            }

            public override VideoBase Parent { get { return parent; } }
            public override string Extension { get { return GetVideoFormatExtension(Format); } }

            private readonly string url = "about:blank;";

            public override string Url
            {
                get { return url + "&title=" + this.GetVideoFileName(true).UrlEncode(); }
            }

            public override int CompareTo(VideoLinkBase other)
            {
                if (!(other is FmtStream)) throw new NotSupportedException();
                if (this is UnknownFmtStream) if (other is UnknownFmtStream) return 0; else return -1;
                if (other is UnknownFmtStream) return 1;
                var you = other as FmtStream;
                var mine = MaxVideoWidth * MaxVideoHeight;
                var your = you.MaxVideoWidth * you.MaxVideoHeight;
                if (mine > your) return 1;
                if (mine < your) return -1;
                if (VideoMaxBitrate > you.VideoMaxBitrate) return 1;
                if (VideoMaxBitrate < you.VideoMaxBitrate) return -1;
                if (MinChannels > you.MinChannels) return 1;
                if (MinChannels < you.MinChannels) return -1;
                if (SamplingRate > you.SamplingRate) return 1;
                if (SamplingRate < you.SamplingRate) return -1;
                if (AudioBitrate > you.AudioBitrate) return 1;
                if (AudioBitrate < you.AudioBitrate) return -1;
                if (Format != VideoFormat.FormatWebM && you.Format == VideoFormat.FormatWebM) return 1; // 尽量不使用webm因为兼容性不好
                return 0;
            }

            public override string ToString()
            {
                return VideoFormatToString() + " " + MaxVideoWidth + "x" + MaxVideoHeight + " " + ChannelsToString() + " " + SamplingRate + "Hz" +
                       (AudioEncoding == AudioEncodings.Undefined ? String.Empty : (" " + AudioEncodingsToString()));
            }

            // ReSharper disable MemberCanBePrivate.Global

            public string VideoFormatToString()
            {
                return YouTube.VideoFormatToString(Format);
            }

            public string VideoEncodingsToString()
            {
                return YouTube.VideoEncodingsToString(VideoEncoding);
            }

            public string AudioEncodingsToString()
            {
                return YouTube.AudioEncodingsToString(AudioEncoding);
            }

            public string ChannelsToString()
            {
                return YouTube.ChannelsToString(MinChannels, MaxChannels);
            }

            public string VideoBitrateToString()
            {
                if (VideoMinBitrate == null || VideoMaxBitrate == null) return null;
                return Math.Abs(VideoMinBitrate.Value - VideoMaxBitrate.Value) < 1e-4
                    ? VideoMaxBitrate.Value.ToString(CultureInfo.InvariantCulture) : (VideoMinBitrate + "-" + VideoMaxBitrate);
            }

            // ReSharper restore MemberCanBePrivate.Global
        }

        private class UnknownFmtStream : FmtStream
        {
            public UnknownFmtStream(int code, string url, Video parent)
                : base(parent, url)
            {
                videoTypeCode = code;
            }

            private readonly int videoTypeCode;
            public string Quality, Type;

            public override string ToString()
            {
                return string.Format("未知的FMT #{0} 类型：{1} 质量：{2} 请联系Mygod解决此问题", videoTypeCode, Type, Quality);
            }
        }

        public enum VideoFormat
        {
            Undefined,
            FormatFLV,
            FormatMP4,
            Format3GP,
            FormatWebM
        }

        private static string VideoFormatToString(VideoFormat format)
        {
            switch (format)
            {
                case VideoFormat.Format3GP:
                    return "3GP";
                case VideoFormat.FormatFLV:
                    return "FLV";
                case VideoFormat.FormatMP4:
                    return "MP4";
                case VideoFormat.FormatWebM:
                    return "WebM";
                default:
                    return "未知格式";
            }
        }

        private static string GetVideoFormatExtension(VideoFormat format)
        {
            switch (format)
            {
                case VideoFormat.Format3GP:
                    return ".3gp";
                case VideoFormat.FormatFLV:
                    return ".flv";
                case VideoFormat.FormatMP4:
                    return ".mp4";
                case VideoFormat.FormatWebM:
                    return ".webm";
                default:
                    return string.Empty;
            }
        }

        public enum VideoEncodings
        {
            Undefined,
            SorensonH263,
            H264Main,
            H264Baseline,
            H264High,
            H2643D,
            VP8,
            VP83D,
            MPEG4Visual
        }

        private static string VideoEncodingsToString(VideoEncodings encoding)
        {
            switch (encoding)
            {
                case VideoEncodings.SorensonH263:
                    return "Sorenson H.263";
                case VideoEncodings.H264Main:
                    return "MPEG-4 AVC (H.264) Main";
                case VideoEncodings.H264Baseline:
                    return "MPEG-4 AVC (H.264) Baseline";
                case VideoEncodings.H264High:
                    return "MPEG-4 AVC (H.264) High";
                case VideoEncodings.H2643D:
                    return "MPEG-4 AVC (H.264) 3D";
                case VideoEncodings.VP8:
                    return "VP8";
                case VideoEncodings.VP83D:
                    return "VP8 3D";
                case VideoEncodings.MPEG4Visual:
                    return "MPEG-4 Visual";
                default:
                    return "未知视频解码";
            }
        }

        public enum AudioEncodings
        {
            Undefined,
            AAC,
            MP3,
            Vorbis
        }

        private static string AudioEncodingsToString(AudioEncodings encoding)
        {
            switch (encoding)
            {
                case AudioEncodings.AAC:
                    return "AAC";
                case AudioEncodings.MP3:
                    return "MP3";
                case AudioEncodings.Vorbis:
                    return "Vorbis";
                default:
                    return "未知音频编码";
            }
        }

        private static string ChannelsToString(int minChannels, int maxChannels)
        {
            if (minChannels != maxChannels && minChannels != 1 && maxChannels != 2) return string.Format("{0}至{1}声道", minChannels, maxChannels);
            switch (minChannels)
            {
                case 1:
                    return maxChannels == 1 ? "单声道" : "单声道或双声道";
                case 2:
                    return "双声道";
                case 6:
                    return "5.1声道";
                case 8:
                    return "7.1声道";
                default:
                    return minChannels + "声道";
            }
        }
    }

    public abstract class VideoBase
    {
        public abstract string Title { get; }
        public abstract string Author { get; }
    }

    public abstract class VideoLinkBase : IComparable<VideoLinkBase>
    {
        public abstract string Url { get; }
        public abstract string Extension { get; }
        public abstract string Properties { get; }
        public abstract VideoBase Parent { get; }
        public abstract int CompareTo(VideoLinkBase other);
    }
}