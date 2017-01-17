﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Mygod.Xml.Linq;

namespace Mygod.Skylark
{
    public static class TaskType
    {
        public const string NoTask = "ready",
                            OfflineDownloadTask = "offline-download",
                            CompressTask = "compress",
                            DecompressTask = "decompress",
                            FtpUploadTask = "ftp-upload",
                            ConvertTask = "convert",
                            CrossAppCopyTask = "cross-app-copy",
                            UploadTask = "upload",
                            BatchMergeVATask = "batch-merge-va";
    }
    public enum TaskStatus
    {
        Terminated, Error, Starting, Working, Done
    }
    public abstract partial class CloudTask
    {
        protected CloudTask(string filePath)
        {
            if (!String.IsNullOrEmpty(FilePath = filePath) && File.Exists(filePath))
                TaskXml = XHelper.Load(filePath).Root;
        }
        protected CloudTask(string filePath, string root)
        {
            FilePath = filePath;
            TaskXml = new XElement(root);
        }

        protected static readonly Regex
            AccountRemover = new Regex(@"^ftp:\/\/[^\/]*?:[^\/]*?@", RegexOptions.Compiled);

        protected readonly XElement TaskXml;
        protected readonly string FilePath;

        public int PID
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValueWithDefault<int>("pid"); }
            set { TaskXml.SetAttributeValue("pid", value); }
        }
        public string ErrorMessage
        {
            get { return TaskXml == null ? "任务数据缺失！" : TaskXml.GetAttributeValue("message"); }
            set { TaskXml.SetAttributeValue("message", value); }
        }

        public abstract string Type { get; }

        public abstract DateTime? StartTime { get; set; }   // leave it for derived classes
        public DateTime? EndTime
        {
            get
            {
                if (TaskXml == null) return null;
                var endTime = TaskXml.GetAttributeValueWithDefault<long>("endTime", -1);
                return endTime < 0 ? null
                    : (DateTime?)new DateTime(TaskXml.GetAttributeValue<long>("endTime"), DateTimeKind.Utc);
            }
            set
            {
                if (value.HasValue) TaskXml.SetAttributeValue("endTime", value.Value.Ticks);
                else TaskXml.SetAttributeValue("endTime", null);
            }
        }

        public virtual long? FileLength
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue<long?>("size"); }
            set
            {
                if (value.HasValue) TaskXml.SetAttributeValue("size", value.Value);
                else TaskXml.SetAttributeValue("size", null);
            }
        }
        public virtual long ProcessedFileLength
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValueWithDefault<long>("sizeProcessed"); }
            set { TaskXml.SetAttributeValue("sizeProcessed", value); }
        }

        public virtual double? Percentage { get { return 100.0 * ProcessedFileLength / FileLength; } }

        public void Save()
        {
            lock (TaskXml)
            {
                TaskXml.Save(FilePath);
            }
        }

        public abstract void Finish();

        public static void KillProcessTree(int pid)
        {
            if (pid == 0) return;
            try
            {
                foreach (var mbo in
                    new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid).Get())
                    KillProcessTree(int.Parse(mbo["ProcessID"].ToString()));
            }
            catch (UnauthorizedAccessException)
            {
                Process.Start(new ProcessStartInfo("taskkill", "/F /T /PID " + pid));
            }
            try
            {
                Process.GetProcessById(pid).Kill();
            }
            catch { }
        }
    }
    public interface IRemoteTask
    {
        string Url { get; }
    }
    public interface ISingleSource
    {
        string Source { get; }
    }
    public interface IMultipleSources
    {
        string CurrentSource { get; }
        long ProcessedSourceCount { get; }
        IEnumerable<string> Sources { get; }
    }

    public abstract partial class GenerateFileTask : CloudTask
    {
        protected GenerateFileTask(string relativePath) : base(FileHelper.GetDataFilePath(relativePath))
        {
            RelativePath = relativePath;
        }
        protected GenerateFileTask(string relativePath, string state)
            : base(FileHelper.GetDataFilePath(relativePath), "file")
        {
            State = state;
            Mime = Helper.GetMimeType(RelativePath = relativePath);
            StartTime = DateTime.UtcNow;
            File.WriteAllText(FileHelper.GetFilePath(relativePath), string.Empty);  // temp
        }

        public override sealed string Type { get { return State; } }

        public override sealed DateTime? StartTime
        {
            get
            {
                return TaskXml == null
                    ? null : (DateTime?)new DateTime(TaskXml.GetAttributeValue<long>("startTime"), DateTimeKind.Utc);
            }
            set
            {
                if (value.HasValue) TaskXml.SetAttributeValue("startTime", value.Value.Ticks);
                else TaskXml.SetAttributeValue("startTime", null);
            }
        }

        public string State
        {
            get { return TaskXml.GetAttributeValue("state"); }
            set { TaskXml.SetAttributeValue("state", value); }
        }
        public string Mime
        {
            get { return TaskXml.GetAttributeValue("mime"); }
            set {  TaskXml.SetAttributeValue("mime", value); }
        }
        public string RelativePath { get; private set; }

        public override void Finish()
        {
            State = TaskType.NoTask;
            Save();
        }
    }
    public abstract class OneToOneFileTask : GenerateFileTask, ISingleSource
    {
        protected OneToOneFileTask(string relativePath)
            : base(relativePath)
        {
        }
        protected OneToOneFileTask(string source, string target, string state)
            : base(target, state)
        {
            Source = source;
        }

        public string Source
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("source"); }
            set { TaskXml.SetAttributeValue("source", value); }
        }
    }
    public abstract partial class MultipleToOneFileTask : GenerateFileTask, IMultipleSources
    {
        protected MultipleToOneFileTask(string relativePath) : base(relativePath)
        {
        }
        protected MultipleToOneFileTask(IEnumerable<string> sources, string relativePath, string state)
            : base(relativePath, state)
        {
            Sources = sources;
        }

        public string CurrentSource
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("currentFile"); }
            set { TaskXml.SetAttributeValue("currentFile", value); }
        }

        public long ProcessedSourceCount
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValueWithDefault<long>("sourceProcessed"); }
            set { TaskXml.SetAttributeValue("sourceProcessed", value); }
        }

        public IEnumerable<string> Sources
        {
            get { return TaskXml.ElementsCaseInsensitive("source").Select(file => file.Value); }
            set
            {
                TaskXml.ElementsCaseInsensitive("source").Remove();
                foreach (var file in value) TaskXml.Add(new XElement("source", file));
            }
        }
    }

    public sealed partial class OfflineDownloadTask : GenerateFileTask, IRemoteTask
    {
        public OfflineDownloadTask(string relativePath) : base(relativePath)
        {
        }

        public string Url
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("url"); }
            set { TaskXml.SetAttributeValue("url", AccountRemover.Replace(value, "ftp://")); }
        }

        public override long ProcessedFileLength
        {
            get
            {
                if (base.ProcessedFileLength > 0) return base.ProcessedFileLength;
                var file = new FileInfo(FileHelper.GetFilePath(RelativePath));
                return file.Exists ? file.Length : 0;
            }
            set { throw new NotSupportedException(); }
        }
    }
    public sealed partial class CompressTask : MultipleToOneFileTask
    {
        public CompressTask(string archiveFilePath) : base(archiveFilePath)
        {
        }
        public CompressTask(string archiveFilePath, IEnumerable<string> files, string baseFolder = null,
                            string compressionLevel = null) : base(files, archiveFilePath, TaskType.CompressTask)
        {
            TaskXml.SetAttributeValue("compressionLevel", compressionLevel ?? "Ultra");
            BaseFolder = baseFolder ?? string.Empty;
        }

        public string BaseFolder
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("baseFolder"); }
            set { TaskXml.SetAttributeValue("baseFolder", value); }
        }
    }
    public sealed partial class ConvertTask : OneToOneFileTask
    {
        public ConvertTask(string relativePath) : base(relativePath)
        {
        }
        public ConvertTask(string source, string target, TimeSpan duration, string audioPath = null,
                           string arguments = null) : base(source, target, TaskType.ConvertTask)
        {
            Duration = duration;
            AudioPath = audioPath;
            Arguments = arguments;
        }

        public TimeSpan ProcessedDuration
        {
            get { return new TimeSpan(TaskXml.GetAttributeValueWithDefault<long>("durationProcessed")); }
            set { TaskXml.SetAttributeValue("durationProcessed", value.Ticks); }
        }
        public TimeSpan Duration
        {
            get { return new TimeSpan(TaskXml.GetAttributeValue<long>("duration")); }
            set { TaskXml.SetAttributeValue("duration", value.Ticks); }
        }
        public string AudioPath
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("audioPath"); }
            set { TaskXml.SetAttributeValue("audioPath", value); }
        }
        public string Arguments
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("arguments"); }
            set { TaskXml.SetAttributeValue("arguments", value); }
        }

        public override double? Percentage { get { return 100.0 * ProcessedDuration.Ticks / Duration.Ticks; } }

        private static readonly Regex DurationParser = new Regex("Duration: (.*?),", RegexOptions.Compiled);
        public static ConvertTask Create(string source, string target, string size = null, string vcodec = null,
                                         string acodec = null, string scodec = null, string audioPath = null,
                                         string startPoint = null, string endPoint = null)
        {
            var arguments = string.Empty;
            if (!string.IsNullOrWhiteSpace(size)) arguments += " -s " + size;
            if (!string.IsNullOrWhiteSpace(vcodec)) arguments += " -vcodec " + vcodec;
            if (!string.IsNullOrWhiteSpace(acodec)) arguments += " -acodec " + acodec;
            if (!string.IsNullOrWhiteSpace(scodec)) arguments += " -scodec " + scodec;
            TimeSpan duration;
            TimeSpan.TryParse(DurationParser.Match(FFmpeg.Analyze(FileHelper.GetFilePath(source))).Groups[1].Value,
                              out duration);    // silent fail with TimeSpan.Zero
            TimeSpan start = FFmpeg.Parse(startPoint), end = FFmpeg.Parse(endPoint, duration);
            if (start <= TimeSpan.Zero) start = TimeSpan.Zero;
            else arguments += " -ss " + startPoint;
            if (end >= duration) end = duration;
            else arguments += " -to " + endPoint;
            return new ConvertTask(source, target, end - start, audioPath, arguments);
        }
    }

    public abstract partial class GeneralTask : CloudTask
    {
        protected GeneralTask(string id) : base(FileHelper.GetDataPath(id + ".task"))
        {
            ID = id;
        }
        protected GeneralTask(string type, bool create)
            : base(FileHelper.GetDataPath(DateTime.UtcNow.Shorten() + ".task"), type)
        {
            if (!create)
                throw new NotSupportedException("You should call .ctor(id) if you are NOT going to create something!");
            ID = Path.GetFileNameWithoutExtension(FilePath);
        }

        public string ID { get; private set; }
        public override sealed DateTime? StartTime
        {
            get { return Helper.Deshorten(ID); }
            set { throw new NotSupportedException(); }
        }

        public override sealed string Type { get { return TaskXml.Name.LocalName; } }

        public override void Finish()
        {
            EndTime = DateTime.UtcNow;
            Save();
        }
    }
    public abstract partial class MultipleFilesTask : GeneralTask
    {
        protected MultipleFilesTask(string id) : base(id)
        {
        }
        protected MultipleFilesTask(string type, string target) : base(type, true)
        {
            Target = target;
        }

        public virtual string CurrentFile
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("currentFile"); }
            set { TaskXml.SetAttributeValue("currentFile", value); }
        }

        public virtual long? FileCount
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue<long?>("fileCount"); }
            set
            {
                if (value.HasValue) TaskXml.SetAttributeValue("fileCount", value.Value);
                else TaskXml.SetAttributeValue("fileCount", null);
            }
        }
        public virtual long ProcessedFileCount
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValueWithDefault<long>("fileProcessed"); }
            set { TaskXml.SetAttributeValue("fileProcessed", value); }
        }

        public string Target
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("target"); }
            set { TaskXml.SetAttributeValue("target", value); }
        }
    }
    public abstract partial class OneToMultipleFilesTask : MultipleFilesTask, ISingleSource
    {
        protected OneToMultipleFilesTask(string id) : base(id)
        {
        }
        protected OneToMultipleFilesTask(string type, string source, string target) : base(type, target)
        {
            Source = source;
        }

        public string Source
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("source"); }
            set { TaskXml.SetAttributeValue("source", value); }
        }
    }
    public abstract class MultipleToMultipleFilesTask : MultipleFilesTask, IMultipleSources
    {
        protected MultipleToMultipleFilesTask(string id) : base(id)
        {
        }
        protected MultipleToMultipleFilesTask(string type, IEnumerable<string> sources, string target)
            : base(type, target)
        {
            Sources = sources;
        }

        public string CurrentSource
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("currentFile"); }
            set { TaskXml.SetAttributeValue("currentFile", value); }
        }

        public long ProcessedSourceCount
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValueWithDefault<long>("sourceProcessed"); }
            set { TaskXml.SetAttributeValue("sourceProcessed", value); }
        }

        public IEnumerable<string> Sources
        {
            get { return TaskXml.ElementsCaseInsensitive("source").Select(file => file.Value); }
            set
            {
                TaskXml.ElementsCaseInsensitive("source").Remove();
                foreach (var file in value) TaskXml.Add(new XElement("source", file));
            }
        }
    }

    public sealed partial class FtpUploadTask : GeneralTask, IRemoteTask, IMultipleSources
    {
        public FtpUploadTask(string id) : base(id)
        {
        }
        public FtpUploadTask(string baseFolder, IEnumerable<string> sources, string url)
            : base(TaskType.FtpUploadTask, true)
        {
            BaseFolder = baseFolder ?? string.Empty;
            Sources = sources;
            Url = url;
        }

        public string Url
        {
            get { return TaskXml == null ? null : AccountRemover.Replace(TaskXml.GetAttributeValue("url"), "ftp://"); }
            set { TaskXml.SetAttributeValue("url", value); }
        }
        internal string UrlFull { get { return TaskXml == null ? null : TaskXml.GetAttributeValue("url"); } }

        public string BaseFolder
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("baseFolder"); }
            set { TaskXml.SetAttributeValue("baseFolder", value); }
        }

        public string CurrentSource
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("currentFile"); }
            set { TaskXml.SetAttributeValue("currentFile", value); }
        }

        public long? SourceCount
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue<long?>("sourceCount"); }
            set
            {
                if (value.HasValue) TaskXml.SetAttributeValue("sourceCount", value.Value);
                else TaskXml.SetAttributeValue("sourceCount", null);
            }
        }
        public long ProcessedSourceCount
        {
            get { return TaskXml == null ? 0 : TaskXml.GetAttributeValueWithDefault<long>("sourceProcessed"); }
            set { TaskXml.SetAttributeValue("sourceProcessed", value); }
        }

        public IEnumerable<string> Sources
        {
            get { return TaskXml.ElementsCaseInsensitive("source").Select(file => file.Value); }
            set
            {
                TaskXml.ElementsCaseInsensitive("source").Remove();
                foreach (var file in value) TaskXml.Add(new XElement("source", file));
            }
        }
    }
    public sealed partial class CrossAppCopyTask : MultipleFilesTask
    {
        public CrossAppCopyTask(string id) : base(id)
        {
        }
        public CrossAppCopyTask(string domain, string source, string target, string password = null)
            : base(TaskType.CrossAppCopyTask, target)
        {
            Domain = domain;
            Source = source;
            Password = password;
        }

        public string Domain
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("domain"); }
            set { TaskXml.SetAttributeValue("domain", value); }
        }
        public string Source
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("source"); }
            set { TaskXml.SetAttributeValue("source", value); }
        }
        public string Password
        {
            get { return TaskXml == null ? null : TaskXml.GetAttributeValue("password"); }
            set { TaskXml.SetAttributeValue("password", value); }
        }

        public override long? FileCount { get { return null; } set { throw new NotSupportedException(); } }
        public override long? FileLength { get { return null; } set { throw new NotSupportedException(); } }
    }
    public sealed partial class DecompressTask : OneToMultipleFilesTask
    {
        public DecompressTask(string id) : base(id)
        {
        }
        public DecompressTask(string source, string target) : base(TaskType.DecompressTask, source, target)
        {
        }
    }
}