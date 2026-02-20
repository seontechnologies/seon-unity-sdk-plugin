using System;
using NUnit.Framework;

namespace Seon.Tests.Editor
{
    /// <summary>
    /// EditMode unit tests for SEON SDK.
    /// These run in the Unity Editor without a device build.
    /// </summary>
    [TestFixture]
    public class SeonSDKEditorTests
    {
        private MockPlatform _mockPlatform;

        [SetUp]
        public void SetUp()
        {
            _mockPlatform = new MockPlatform();
            SeonSDK.SetPlatform(_mockPlatform);
        }

        [TearDown]
        public void TearDown()
        {
            SeonSDK.Cleanup();
        }

        // ── Init ────────────────────────────────────────────────────

        [Test]
        public void Init_WithValidSessionId_SetsSessionId()
        {
            SeonSDK.Init("test-session-123");
            Assert.AreEqual("test-session-123", _mockPlatform.LastSessionId);
        }

        [Test]
        public void Init_WithNullSessionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SeonSDK.Init(null));
        }

        [Test]
        public void Init_WithEmptySessionId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SeonSDK.Init(""));
        }

        // ── SetSessionId ────────────────────────────────────────────

        [Test]
        public void SetSessionId_BeforeInit_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => SeonSDK.SetSessionId("new-id"));
        }

        [Test]
        public void SetSessionId_AfterInit_UpdatesSessionId()
        {
            SeonSDK.Init("original");
            SeonSDK.SetSessionId("updated");
            Assert.AreEqual("updated", _mockPlatform.LastSessionId);
        }

        // ── SetLoggingEnabled ───────────────────────────────────────

        [Test]
        public void SetLoggingEnabled_BeforeInit_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => SeonSDK.SetLoggingEnabled(true));
        }

        [Test]
        public void SetLoggingEnabled_AfterInit_PropagatesValue()
        {
            SeonSDK.Init("session");
            SeonSDK.SetLoggingEnabled(true);
            Assert.IsTrue(_mockPlatform.LoggingEnabled);
        }

        // ── SetDnsTimeout ───────────────────────────────────────────

        [Test]
        public void SetDnsTimeout_AfterInit_PropagatesValue()
        {
            SeonSDK.Init("session");
            SeonSDK.SetDnsTimeout(5000);
            Assert.AreEqual(5000, _mockPlatform.DnsTimeout);
        }

        // ── SetGeolocationConfig ────────────────────────────────────

        [Test]
        public void SetGeolocationConfig_NullConfig_ThrowsArgumentNull()
        {
            SeonSDK.Init("session");
            Assert.Throws<ArgumentNullException>(() => SeonSDK.SetGeolocationConfig(null));
        }

        [Test]
        public void SetGeolocationConfig_ValidConfig_PropagatesValues()
        {
            SeonSDK.Init("session");
            var config = new SeonGeolocationConfig
            {
                Enabled = true,
                PrefetchEnabled = true,
                MaxCacheAgeSec = 120,
                ServiceTimeoutMs = 5000
            };

            SeonSDK.SetGeolocationConfig(config);

            Assert.IsNotNull(_mockPlatform.GeoConfig);
            Assert.IsTrue(_mockPlatform.GeoConfig.Enabled);
            Assert.AreEqual(5000, _mockPlatform.GeoConfig.ServiceTimeoutMs);
        }

        // ── GetFingerprint ──────────────────────────────────────────

        [Test]
        public void GetFingerprint_BeforeInit_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SeonSDK.GetFingerprint(_ => { }, _ => { }));
        }

        [Test]
        public void GetFingerprint_NullCallbacks_ThrowsArgumentNull()
        {
            SeonSDK.Init("session");
            Assert.Throws<ArgumentNullException>(() =>
                SeonSDK.GetFingerprint(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() =>
                SeonSDK.GetFingerprint(_ => { }, null));
        }

        [Test]
        public void GetFingerprint_AfterInit_InvokesCallback()
        {
            SeonSDK.Init("session");
            string result = null;
            SeonSDK.GetFingerprint(fp => result = fp, _ => { });
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        // ── Behaviour Monitoring ────────────────────────────────────

        [Test]
        public void StartBehaviourMonitoring_BeforeInit_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                SeonSDK.StartBehaviourMonitoring());
        }

        [Test]
        public void StopBehaviourMonitoring_WithoutStart_InvokesErrorCallback()
        {
            SeonSDK.Init("session");
            SeonError error = null;
            SeonSDK.StopBehaviourMonitoring(_ => { }, e => error = e);
            Assert.IsNotNull(error);
        }

        [Test]
        public void BehaviourMonitoring_StartThenStop_ReturnsFingerprint()
        {
            SeonSDK.Init("session");
            SeonSDK.StartBehaviourMonitoring();
            string result = null;
            SeonSDK.StopBehaviourMonitoring(fp => result = fp, _ => { });
            Assert.IsNotNull(result);
        }

        // ── Geolocation Config Defaults ─────────────────────────────

        [Test]
        public void GeolocationConfig_Defaults_MatchSpec()
        {
            var config = new SeonGeolocationConfig();
            Assert.IsFalse(config.Enabled);
            Assert.IsFalse(config.PrefetchEnabled);
            Assert.AreEqual(600, config.MaxCacheAgeSec);
            Assert.AreEqual(3000, config.ServiceTimeoutMs);
        }

        // ── SeonError ───────────────────────────────────────────────

        [Test]
        public void SeonError_ToString_ContainsCodeAndMessage()
        {
            var error = new SeonError { Code = 42, Message = "test error" };
            var str = error.ToString();
            Assert.IsTrue(str.Contains("42"));
            Assert.IsTrue(str.Contains("test error"));
        }

        [Test]
        public void SeonError_WithGeoStatus_IncludesInToString()
        {
            var error = new SeonError { Code = 1, Message = "geo fail", GeolocationStatusCode = 3 };
            Assert.IsTrue(error.ToString().Contains("GeoStatus=3"));
        }

        // ── Cleanup ─────────────────────────────────────────────────

        [Test]
        public void Cleanup_ResetInitialized_RequiresReInit()
        {
            SeonSDK.Init("session");
            SeonSDK.Cleanup();
            Assert.Throws<InvalidOperationException>(() =>
                SeonSDK.GetFingerprint(_ => { }, _ => { }));
        }

        // ── Mock Platform ───────────────────────────────────────────

        private class MockPlatform : ISeonPlatform
        {
            public string LastSessionId;
            public bool LoggingEnabled;
            public int DnsTimeout;
            public SeonGeolocationConfig GeoConfig;
            private bool _behaviourActive;

            private const string MockFp = "bW9jay1mcA==";
            private const string MockBehaviourFp = "bW9jay1iZWhhdmlvdXItZnA=";

            public void Init(string sessionId) => LastSessionId = sessionId;
            public void SetSessionId(string sessionId) => LastSessionId = sessionId;
            public void SetLoggingEnabled(bool enabled) => LoggingEnabled = enabled;
            public void SetDnsTimeout(int timeoutMs) => DnsTimeout = timeoutMs;
            public void SetGeolocationConfig(SeonGeolocationConfig config) => GeoConfig = config;

            public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError)
                => onSuccess(MockFp);

            public void StartBehaviourMonitoring() => _behaviourActive = true;

            public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError)
            {
                if (!_behaviourActive)
                {
                    onError(new SeonError { Code = -1, Message = "Not started" });
                    return;
                }
                _behaviourActive = false;
                onSuccess(MockBehaviourFp);
            }

            public void Cleanup()
            {
                LastSessionId = null;
                LoggingEnabled = false;
                _behaviourActive = false;
                GeoConfig = null;
            }
        }
    }
}
