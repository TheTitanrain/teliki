using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Teliki.App;
using Teliki.Core;

namespace Teliki.Tests
{
    [TestClass]
    public class SignageControllerTests
    {
        [TestMethod]
        public void PauseAdvanceForVideo_StopsAdvanceTimer()
        {
            var controller = CreateController();
            controller.PauseAdvanceForVideo();
            Assert.IsTrue(controller.AdvanceTimer.StopCalls >= 1);
        }

        [TestMethod]
        public void OnVideoCompleted_WithPausedVideo_CallsAdvancePlayback()
        {
            var controller = CreateController();
            controller.PauseAdvanceForVideo();

            controller.OnVideoCompleted();

            Assert.AreEqual(1, controller.Runtime.AdvanceCalls);
        }

        [TestMethod]
        public void OnVideoCompleted_WhenCalledTwice_AdvancesOnlyOnce()
        {
            var controller = CreateController();
            controller.PauseAdvanceForVideo();

            controller.OnVideoCompleted();
            controller.OnVideoCompleted();

            Assert.AreEqual(1, controller.Runtime.AdvanceCalls);
        }

        [TestMethod]
        public void OnAdvanceTick_WhenVideoPlaying_IsIgnored()
        {
            var controller = CreateController();
            controller.PauseAdvanceForVideo();

            controller.OnAdvanceTick();

            Assert.AreEqual(0, controller.Runtime.AdvanceCalls);
        }

        [TestMethod]
        public void SetNextInterval_WithDuration_UpdatesTimerInterval()
        {
            var controller = CreateController();
            controller.SetNextInterval(TimeSpan.FromSeconds(30));
            Assert.AreEqual(30000, controller.AdvanceTimer.Interval);
            Assert.IsTrue(controller.AdvanceTimer.StartCalls >= 1);
        }

        [TestMethod]
        public void SetNextInterval_WithNullDuration_UsesConfigInterval()
        {
            var controller = CreateController();
            controller.SetNextInterval(null);
            Assert.AreEqual(15000, controller.AdvanceTimer.Interval);
        }

        [TestMethod]
        public void OnScanCompleted_FirstScanWithContent_AdvancesImmediately()
        {
            var controller = CreateController();
            var manifest = new PlaylistManifest(new[] { new CachedMediaItem("a", "a.jpg", MediaKind.Image) });

            controller.CompleteScan(0, manifest);

            Assert.AreEqual(1, controller.Runtime.AdvanceCalls);
        }

        [TestMethod]
        public void OnScanCompleted_SubsequentScanWithSameContent_DoesNotAdvance()
        {
            var controller = CreateController();
            var manifest = new PlaylistManifest(new[] { new CachedMediaItem("a", "a.jpg", MediaKind.Image) });

            controller.CompleteScan(0, manifest);
            controller.CompleteScan(0, manifest);

            Assert.AreEqual(1, controller.Runtime.AdvanceCalls);
        }

        [TestMethod]
        public void OnScanCompleted_ContentAppearsAfterEmpty_AdvancesImmediately()
        {
            var controller = CreateController();

            controller.CompleteScan(0, PlaylistManifest.Empty);
            var manifest = new PlaylistManifest(new[] { new CachedMediaItem("a", "a.jpg", MediaKind.Image) });
            controller.CompleteScan(0, manifest);

            Assert.AreEqual(1, controller.Runtime.AdvanceCalls);
        }

        [TestMethod]
        public void ApplyConfig_UpdatesExistingTimerIntervals()
        {
            var controller = CreateController();
            var newConfig = CreateConfig("updated");

            controller.ApplyConfig(newConfig);

            Assert.AreEqual(15000, controller.AdvanceTimer.Interval);
            Assert.AreEqual(7000, controller.ScanTimer.Interval);
            Assert.AreEqual(1000, controller.WatchdogTimer.Interval);
            Assert.AreEqual(1, controller.AdvanceTimer.StopCalls);
            Assert.AreEqual(1, controller.AdvanceTimer.StartCalls);
        }

        [TestMethod]
        public void SaveDuringRunningScan_MarksOldScanStaleAndTriggersFollowUpScan()
        {
            var controller = CreateController();
            controller.RequestScan();
            var oldGeneration = controller.ScanRunner.LastRequest.Generation;

            controller.ApplyConfig(CreateConfig("new-folder"));
            controller.CompleteScan(oldGeneration, new PlaylistManifest(new[]
            {
                new CachedMediaItem("old", "old.jpg", MediaKind.Image)
            }));

            Assert.AreEqual(2, controller.ScanRunner.Requests.Count);
            Assert.AreEqual("new-folder", controller.ScanRunner.LastRequest.Config.MediaFolder);
            Assert.AreEqual(0, controller.Runtime.AppliedPlaylists.Count);
        }

        [TestMethod]
        public void ModalState_SuppressesWatchdogAndPlaybackEscape()
        {
            var controller = CreateController();

            Assert.IsTrue(controller.TryOpenSettings());
            controller.OnWatchdogTick();
            controller.HandlePlaybackEscape();
            controller.CloseModalUi();

            Assert.AreEqual(1, controller.Runtime.RestoreCalls);
            Assert.AreEqual(0, controller.Runtime.ExitCalls);
        }

        [TestMethod]
        public void TryOpenSettings_AllowsOnlyOneModalInstance()
        {
            var controller = CreateController();

            Assert.IsTrue(controller.TryOpenSettings());
            Assert.IsFalse(controller.TryOpenSettings());
        }

        [TestMethod]
        public void TryOpenAbout_AllowsOnlyOneModalInstance()
        {
            var controller = CreateController();

            Assert.IsTrue(controller.Controller.TryOpenAbout());
            Assert.IsFalse(controller.Controller.TryOpenAbout());
        }

        [TestMethod]
        public void TryOpenAbout_BlockedWhileSettingsOpen()
        {
            var controller = CreateController();

            Assert.IsTrue(controller.TryOpenSettings());
            Assert.IsFalse(controller.Controller.TryOpenAbout());
        }

        [TestMethod]
        public void TryOpenSettings_BlockedWhileAboutOpen()
        {
            var controller = CreateController();

            Assert.IsTrue(controller.Controller.TryOpenAbout());
            Assert.IsFalse(controller.TryOpenSettings());
        }

        [TestMethod]
        public void SaveSettings_WriteFailureLeavesRuntimeStateUnchanged()
        {
            var runtime = new RecordingRuntime();
            var scanRunner = new RecordingScanRunner();
            var controller = new SignageController(
                CreateConfig("media"),
                runtime,
                scanRunner,
                new FakeTimer(),
                new FakeTimer(),
                new FakeTimer(),
                NullLogger.Instance,
                new ConfigFileStore(new FailingFileSystem()),
                "C:\\locked\\appsettings.ini",
                "C:\\locked");

            Assert.ThrowsExactly<InvalidOperationException>(delegate
            {
                controller.SaveSettings(new EditableSettings("new-media", 20, 8, 32, DisplayModeParser.AllScreens, 0));
            });

            Assert.AreEqual("media", controller.CurrentConfig.MediaFolder);
            Assert.AreEqual(0, scanRunner.Requests.Count);
        }

        private static TestControllerHarness CreateController()
        {
            var runtime = new RecordingRuntime();
            var scanRunner = new RecordingScanRunner();
            var controller = new SignageController(
                CreateConfig("media"),
                runtime,
                scanRunner,
                new FakeTimer(),
                new FakeTimer(),
                new FakeTimer(),
                NullLogger.Instance);
            return new TestControllerHarness(controller, runtime, scanRunner);
        }

        private static AppConfig CreateConfig(string mediaFolder)
        {
            return new AppConfig(
                mediaFolder,
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(7),
                TimeSpan.FromSeconds(31),
                "cache",
                1024,
                512,
                DisplayModeParser.AllScreens,
                0);
        }

        private sealed class TestControllerHarness
        {
            public TestControllerHarness(SignageController controller, RecordingRuntime runtime, RecordingScanRunner scanRunner)
            {
                Controller = controller;
                Runtime = runtime;
                ScanRunner = scanRunner;
            }

            public SignageController Controller { get; private set; }
            public RecordingRuntime Runtime { get; private set; }
            public RecordingScanRunner ScanRunner { get; private set; }
            public FakeTimer AdvanceTimer { get { return (FakeTimer)Controller.AdvanceTimer; } }
            public FakeTimer ScanTimer { get { return (FakeTimer)Controller.ScanTimer; } }
            public FakeTimer WatchdogTimer { get { return (FakeTimer)Controller.WatchdogTimer; } }

            public void ApplyConfig(AppConfig config) { Controller.ApplyConfig(config); }
            public void RequestScan() { Controller.RequestScan(); }
            public void CompleteScan(long generation, PlaylistManifest manifest) { Controller.OnScanCompleted(new ScanCompletion(generation, manifest)); }
            public bool TryOpenSettings() { return Controller.TryOpenSettings(); }
            public void OnWatchdogTick() { Controller.OnWatchdogTick(); }
            public void HandlePlaybackEscape() { Controller.HandlePlaybackEscape(); }
            public void CloseModalUi() { Controller.CloseModalUi(); }
            public void SetNextInterval(TimeSpan? duration) { Controller.SetNextInterval(duration); }
            public void PauseAdvanceForVideo() { Controller.PauseAdvanceForVideo(); }
            public void OnVideoCompleted() { Controller.OnVideoCompleted(); }
            public void OnAdvanceTick() { Controller.OnAdvanceTick(); }
        }

        private sealed class FakeTimer : IAppTimer
        {
            public int Interval { get; set; }
            public int StartCalls { get; private set; }
            public int StopCalls { get; private set; }

            public void Start() { StartCalls++; }
            public void Stop() { StopCalls++; }
        }

        private sealed class RecordingRuntime : ISignageRuntime
        {
            public readonly List<PlaylistManifest> AppliedPlaylists = new List<PlaylistManifest>();
            public int RestoreCalls;
            public int ExitCalls;
            public int AdvanceCalls;

            public void AdvancePlayback()
            {
                AdvanceCalls++;
            }

            public void ApplyPlaylist(PlaylistManifest manifest)
            {
                AppliedPlaylists.Add(manifest);
            }

            public void ExitApplication()
            {
                ExitCalls++;
            }

            public void RestoreFullscreen()
            {
                RestoreCalls++;
            }
        }

        private sealed class RecordingScanRunner : IScanRunner
        {
            public readonly List<ScanRequest> Requests = new List<ScanRequest>();
            public ScanRequest LastRequest { get { return Requests[Requests.Count - 1]; } }
            public event Action<ScanCompletion> ScanCompleted
            {
                add { }
                remove { }
            }

            public void Start(ScanRequest request)
            {
                Requests.Add(request);
            }
        }

        private sealed class FailingFileSystem : IFileSystem
        {
            public Stream CreateFile(string path)
            {
                throw new InvalidOperationException("File is locked.");
            }

            public void CreateDirectory(string path)
            {
            }

            public void DeleteFile(string path)
            {
            }

            public bool DirectoryExists(string path)
            {
                return true;
            }

            public System.Collections.Generic.IEnumerable<string> EnumerateFiles(string path)
            {
                return new string[0];
            }

            public bool FileExists(string path)
            {
                return true;
            }

            public long GetAvailableFreeSpace(string path)
            {
                return 0;
            }

            public FileMetadata GetFileMetadata(string path)
            {
                return new FileMetadata(0, DateTime.UtcNow);
            }

            public void MoveFile(string sourcePath, string destinationPath)
            {
            }

            public Stream OpenRead(string path)
            {
                return new MemoryStream();
            }

            public void ReplaceFile(string sourcePath, string destinationPath)
            {
            }
        }
    }
}
