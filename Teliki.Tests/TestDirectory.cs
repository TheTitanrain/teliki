using System;
using System.IO;

namespace Teliki.Tests
{
    internal sealed class TestDirectory
    {
        public string Path { get; private set; }

        private TestDirectory(string path)
        {
            Path = path;
        }

        public static TestDirectory Create()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "TelikiTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TestDirectory(path);
        }
    }
}
