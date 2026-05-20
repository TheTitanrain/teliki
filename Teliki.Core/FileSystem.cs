using System;
using System.Collections.Generic;
using System.IO;

namespace Teliki.Core
{
    public interface IFileSystem
    {
        bool DirectoryExists(string path);
        void CreateDirectory(string path);
        IEnumerable<string> EnumerateFiles(string path);
        bool FileExists(string path);
        FileMetadata GetFileMetadata(string path);
        Stream OpenRead(string path);
        Stream CreateFile(string path);
        void MoveFile(string sourcePath, string destinationPath);
        void ReplaceFile(string sourcePath, string destinationPath);
        void DeleteFile(string path);
        long GetAvailableFreeSpace(string path);
    }

    public sealed class PhysicalFileSystem : IFileSystem
    {
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public IEnumerable<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public FileMetadata GetFileMetadata(string path)
        {
            var info = new FileInfo(path);
            return new FileMetadata(info.Length, info.LastWriteTimeUtc);
        }

        public Stream OpenRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public Stream CreateFile(string path)
        {
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        }

        public void MoveFile(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            File.Move(sourcePath, destinationPath);
        }

        public void ReplaceFile(string sourcePath, string destinationPath)
        {
            if (File.Exists(destinationPath))
            {
                var backupPath = destinationPath + ".bak";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Replace(sourcePath, destinationPath, backupPath, true);
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                return;
            }

            File.Move(sourcePath, destinationPath);
        }

        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public long GetAvailableFreeSpace(string path)
        {
            var root = Path.GetPathRoot(Path.GetFullPath(path));
            return new DriveInfo(root).AvailableFreeSpace;
        }
    }

    public struct FileMetadata
    {
        public long Length { get; private set; }
        public DateTime LastWriteUtc { get; private set; }

        public FileMetadata(long length, DateTime lastWriteUtc)
        {
            Length = length;
            LastWriteUtc = lastWriteUtc;
        }
    }
}
