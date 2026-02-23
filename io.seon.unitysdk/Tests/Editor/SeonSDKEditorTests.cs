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

        // ── SetSessionId ────────────────────────────────────────────

        [Test]
        public void SetSessionId_WithValidId_SetsSessionId()
        {
            SeonSDK.SetSessionId("test-session-123");
            Assert.AreEqual("test-session-123", _mockPlatform.LastSessionId);
        }

        [Test]
        public void SetSessionId_WithNullId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SeonSDK.SetSessionId(null));
        }

        [Test]
        public void SetSessionId_WithEmptyId_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => SeonSDK.SetSessionId(""));
        }

        [Test]
        public void SetSessionId_CalledTwice_UpdatesSessionId()
        {
            SeonSDK.SetSessionId("original");
            SeonSDK.SetSessionId("updated");
            Assert.AreEqual("updated", _mockPlatform.LastSessionId);
        }

        // ── SetLoggingEnabled ───────────────────────────────────────

        [Test]
        public void SetLoggingEnabled_PropagatesValue()
        {
            SeonSDK.SetLoggingEnabled(true);
            Assert.IsTrue(_mockPlatform.LoggingEnabled);
        }

        // ── SetDnsTimeout ───────────────────────────────────────────

        [Test]
        public void SetDnsTimeout_PropagatesValue()
        {
            SeonSDK.SetDnsTimeout(5000);
            Assert.AreEqual(5000, _mockPlatform.DnsTimeout);
        }

        // ── SetGeolocationConfig ────────────────────────────────────

        [Test]
        public void SetGeolocationConfig_NullConfig_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() => SeonSDK.SetGeolocationConfig(null));
        }

        [Test]
        public void SetGeolocationConfig_ValidConfig_PropagatesValues()
        {
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
        public void GetFingerprint_NullCallbacks_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                SeonSDK.GetFingerprint(null, _ => { }));
            Assert.Throws<ArgumentNullException>(() =>
                SeonSDK.GetFingerprint(_ => { }, null));
        }

        [Test]
        public void GetFingerprint_InvokesSuccessCallback()
        {
            string result = null;
            SeonSDK.GetFingerprint(fp => result = fp, _ => { });
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        // ── Behaviour Monitoring ────────────────────────────────────

        [Test]
        public void StopBehaviourMonitoring_WithoutStart_InvokesErrorCallback()
        {
            SeonError error = null;
            SeonSDK.StopBehaviourMonitoring(_ => { }, e => error = e);
            Assert.IsNotNull(error);
        }

        [Test]
        public void BehaviourMonitoring_StartThenStop_ReturnsFingerprint()
        {
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
        }
    }
}
