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
            SeonSDK.SetSessionId("editor-test-session");
        }

        // ── SetSessionId ────────────────────────────────────────────

        [Test]
        public void SetSessionId_WithValidId_SetsSessionId()
        {
            SeonSDK.SetSessionId("test-session-123");
            Assert.AreEqual("test-session-123", _mockPlatform.LastSessionId);
        }

        [Test]
        public void SetSessionId_WithNullId_DoesNotThrow()
        {
            // SDK should log error and not propagate to host app
            SeonSDK.SetSessionId("valid-session");
            SeonSDK.SetSessionId(null);
            // Previous valid session should remain
            Assert.AreEqual("valid-session", _mockPlatform.LastSessionId);
        }

        [Test]
        public void SetSessionId_WithEmptyId_DoesNotThrow()
        {
            SeonSDK.SetSessionId("valid-session");
            SeonSDK.SetSessionId("");
            Assert.AreEqual("valid-session", _mockPlatform.LastSessionId);
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
        public void SetGeolocationConfig_NullConfig_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SeonSDK.SetGeolocationConfig(null));
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
        public void GetFingerprint_NullCallback_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SeonSDK.GetFingerprint(null));
        }

        [Test]
        public void GetFingerprint_ResultCallback_InvokesSuccessResult()
        {
            SeonFingerprintResult result = null;
            SeonSDK.GetFingerprint(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotEmpty(result.Fingerprint);
        }

        // ── Behaviour Monitoring ────────────────────────────────────

        [Test]
        public void BehaviourMonitoring_StartThenStop_ReturnsSuccessResult()
        {
            var startError = SeonSDK.StartBehaviourMonitoring();
            Assert.IsNull(startError, "StartBehaviourMonitoring should return null on success.");

            SeonFingerprintResult result = null;
            SeonSDK.StopBehaviourMonitoring(r => result = r);
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotEmpty(result.Fingerprint);
        }

        [Test]
        public void StopBehaviourMonitoring_ResultCallback_WithoutStart_ReturnsFailure()
        {
            SeonFingerprintResult result = null;
            SeonSDK.StopBehaviourMonitoring(r => result = r);

            Assert.IsNotNull(result);
            Assert.IsFalse(result.IsSuccess);
            Assert.IsNotNull(result.Error);
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

        // ── Partial Success (Geo Warning) ──────────────────────────

        [Test]
        public void GetFingerprint_PartialSuccess_IsSuccessWithGeoWarning()
        {
            _mockPlatform.SimulateGeoWarning = true;
            SeonFingerprintResult capturedResult = null;

            SeonSDK.GetFingerprint(result => capturedResult = result);

            Assert.IsNotNull(capturedResult, "Callback was not invoked");
            Assert.IsTrue(capturedResult.IsSuccess, "Partial success should still be IsSuccess=true");
            Assert.IsNotNull(capturedResult.Fingerprint, "Fingerprint should be present on partial success");
            Assert.IsNotNull(capturedResult.Error, "Error should be present on partial success");
            Assert.IsTrue(capturedResult.Error.GeolocationStatusCode.HasValue,
                "GeolocationStatusCode should be set on partial success");
        }

        // ── Mock Platform ───────────────────────────────────────────

        private class MockPlatform : ISeonPlatform
        {
            public string LastSessionId;
            public bool LoggingEnabled;
            public int DnsTimeout;
            public SeonGeolocationConfig GeoConfig;
            public bool SimulateGeoWarning;
            private bool _behaviourActive;

            private const string MockFp = "bW9jay1mcA==";
            private const string MockBehaviourFp = "bW9jay1iZWhhdmlvdXItZnA=";

            public void SetSessionId(string sessionId) => LastSessionId = sessionId;
            public string GetSessionId() => LastSessionId;
            public void SetLoggingEnabled(bool enabled) => LoggingEnabled = enabled;
            public void SetDnsTimeout(int timeoutMs) => DnsTimeout = timeoutMs;
            public void SetGeolocationConfig(SeonGeolocationConfig config) => GeoConfig = config;

            public void GetFingerprint(Action<SeonFingerprintResult> onFinished)
            {
                if (SimulateGeoWarning)
                {
                    var geoWarning = new SeonError
                    {
                        Code = 2,
                        Message = "Geolocation collection failed.",
                        GeolocationStatusCode = 2
                    };
                    onFinished(new SeonFingerprintResult(MockFp, geoWarning));
                }
                else
                {
                    onFinished(new SeonFingerprintResult(MockFp));
                }
            }

            public void StartBehaviourMonitoring()
            {
                _behaviourActive = true;
            }

            public void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished)
            {
                if (!_behaviourActive)
                {
                    onFinished(new SeonFingerprintResult(null, new SeonError { Code = -1, Message = "Not started" }));
                    return;
                }
                _behaviourActive = false;
                onFinished(new SeonFingerprintResult(MockBehaviourFp));
            }
        }
    }
}
