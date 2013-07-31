using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using Kudu.Contracts.Tracing;
using Kudu.Core.Infrastructure;
using Newtonsoft.Json;

namespace Kudu.Core.Tracing
{
    public class Analytics : IAnalytics
    {
        private const string DefaultFileNameFormat = "analytics{0}.log";
        private const int MaxFileSize = 1024 * 1024; // 1 MB
        private const int MaxTotalFilesSize = MaxFileSize * 10;

        private static readonly string AnalyticsLogSearchPattern = DefaultFileNameFormat.FormatInvariant("*");
        private static readonly Regex AnalyticsLogFileIndexRegex = new Regex("\\d+");

        private readonly IFileSystem _fileSystem;
        private readonly ITracer _tracer;
        private readonly string _directoryPath;
        private string _currentFileName;
        private string _currentPath;
        private int _currentFileIndex = 0;

        public Analytics(IFileSystem fileSystem, ITracer tracer, string directoryPath)
        {
            _fileSystem = fileSystem;
            _tracer = tracer;
            _directoryPath = directoryPath;

            UpdateCurrentPath(ListLogFiles());
        }

        public void ProjectDeployed(string projectType, string result, long deploymentDurationInMilliseconds)
        {
            var o = new KuduAnalyticsEvent()
            {
                EventType = "ProjectDeployed",
                ProjectType = projectType,
                Result = result,
                Duration = deploymentDurationInMilliseconds
            };

            Log(o);
        }

        private void Log(KuduAnalyticsEvent kuduAnalyticsEvent)
        {
            try
            {
                HandleCleanup();
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }

            try
            {
                string message = JsonConvert.SerializeObject(kuduAnalyticsEvent);
                _tracer.Trace("Analytics", kuduAnalyticsEvent.ToDictionary());

                OperationManager.Attempt(() =>
                {
                    using (var streamWriter = new StreamWriter(_fileSystem.File.Open(_currentPath, FileMode.Append, FileAccess.Write, FileShare.Read)))
                    {
                        streamWriter.WriteLine(message);
                    }
                });
            }
            catch (Exception ex)
            {
                _tracer.TraceError(ex);
            }
        }

        private void HandleCleanup()
        {
            FileInfoBase[] analyticLogFiles = ListLogFiles();

            if (analyticLogFiles.Sum(file => file.Length) > MaxTotalFilesSize)
            {
                analyticLogFiles.OrderBy(file => file.LastWriteTimeUtc).First().Delete();
            }

            FileInfoBase currentFileInfo = analyticLogFiles.First/*OrDefault*/(file => String.Equals(file.Name, _currentFileName));
            if (currentFileInfo != null && currentFileInfo.Length > MaxFileSize)
            {
                UpdateCurrentPath(analyticLogFiles, increaseIndex: true);
            }
        }

        private void UpdateCurrentPath(FileInfoBase[] analyticLogFiles, bool increaseIndex = false)
        {
            UpdateCurrentFileName(analyticLogFiles, increaseIndex);
            _currentPath = Path.Combine(_directoryPath, _currentFileName);
        }

        private void UpdateCurrentFileName(FileInfoBase[] analyticLogFiles, bool increaseIndex)
        {
            if (analyticLogFiles.Length > 0)
            {
                FileInfoBase latestLogFile = analyticLogFiles.OrderBy(file => file.Name).Last();

                Match fileIndexMatch = AnalyticsLogFileIndexRegex.Match(latestLogFile.Name);
                int index;
                if (int.TryParse(fileIndexMatch.Value, out index))
                {
                    _currentFileIndex = index;
                }
            }

            if (increaseIndex)
            {
                _currentFileIndex++;
            }

            string filePostfix = _currentFileIndex == 0 ? String.Empty : "_" + _currentFileIndex;
            _currentFileName = DefaultFileNameFormat.FormatInvariant(filePostfix);
        }

        private FileInfoBase[] ListLogFiles()
        {
            try
            {
                return _fileSystem.DirectoryInfo.FromDirectoryName(_directoryPath).GetFiles(AnalyticsLogSearchPattern);
            }
            catch
            {
                return new FileInfoBase[0];
            }
        }

        private class KuduAnalyticsEvent
        {
            public string EventType { get; set; }
            public string ProjectType { get; set; }
            public string Result { get; set; }
            public long? Duration { get; set; }

            public IDictionary<string, string> ToDictionary()
            {
                Dictionary<string, string> result = new Dictionary<string, string>();

                result["EventType"] = EventType;
                result["ProjectType"] = ProjectType;
                result["Result"] = Result;
                result["Duration"] = String.Empty + Duration;

                return result;
            }
        }
    }
}
