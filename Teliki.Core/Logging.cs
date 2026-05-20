using System;
using System.IO;

namespace Teliki.Core
{
    public interface ILogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message, Exception exception);
    }

    public sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new NullLogger();

        private NullLogger()
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Error(string message, Exception exception)
        {
        }
    }

    public sealed class FileLogger : ILogger
    {
        private readonly string _path;
        private readonly object _sync = new object();

        public FileLogger(string path)
        {
            _path = path;
        }

        public void Info(string message)
        {
            Write("INFO", message, null);
        }

        public void Warn(string message)
        {
            Write("WARN", message, null);
        }

        public void Error(string message, Exception exception)
        {
            Write("ERROR", message, exception);
        }

        private void Write(string level, string message, Exception exception)
        {
            lock (_sync)
            {
                var directory = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RotateIfNeeded();
                var line = string.Format(
                    "{0:O} [{1}] {2}{3}",
                    DateTime.UtcNow,
                    level,
                    message,
                    exception == null ? string.Empty : " " + exception);
                File.AppendAllText(_path, line + Environment.NewLine);
            }
        }

        private void RotateIfNeeded()
        {
            if (!File.Exists(_path))
            {
                return;
            }

            var info = new FileInfo(_path);
            if (info.Length < 1024 * 1024)
            {
                return;
            }

            var rotated = _path + ".1";
            if (File.Exists(rotated))
            {
                File.Delete(rotated);
            }

            File.Move(_path, rotated);
        }
    }
}
