using System.IO;
using System.Text;

namespace Teliki.Core
{
    public sealed class ConfigFileStore
    {
        private readonly IFileSystem _fileSystem;

        public ConfigFileStore(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public ConfigDocument Load(string path)
        {
            if (!_fileSystem.FileExists(path))
            {
                return ConfigDocument.Parse(string.Empty);
            }

            using (var stream = _fileSystem.OpenRead(path))
            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                return ConfigDocument.Parse(reader.ReadToEnd());
            }
        }

        public void Save(string path, ConfigDocument document)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            using (var stream = _fileSystem.CreateFile(tempPath))
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.Write(document.ToString());
            }

            _fileSystem.ReplaceFile(tempPath, path);
        }
    }
}
