using System;
using UnityEngine;

namespace Seon {
    /// <summary>
    /// Editor stub for in-editor testing.
    /// Returns deterministic mock fingerprints so integrators can test
    /// their flow without deploying to a device.
    /// </summary>
    internal class SeonEditorStub : ISeonPlatform {
        private string _sessionId;
        private bool _loggingEnabled;
        private bool _behaviourMonitoringActive;
        private SeonGeolocationConfig _geoConfig;

        // Deterministic mock fingerprint (base64 of "seon-editor-mock-fingerprint")
        private const string MockFingerprint = "unity-editor;stub-session-id;c3R1Yi11bml0eS1pdgMDAw==;c2Vvbi1lZGl0b3ItbW9jay1maW5nZXJwcmludA==";
        private const string MockBehaviourFingerprint = "unity-editor;stub-session-id;c3R1Yi11bml0eS1pdgMDAw==;c2Vvbi1lZGl0b3ItYmVoYXZpb3VyLWZpbmdlcnByaW50";

        public void SetSessionId(string sessionId) {
            _sessionId = sessionId;
            Log("[SeonEditorStub] SetSessionId: " + sessionId);
        }

        public string GetSessionId() {
            return _sessionId;
        }

        public void SetLoggingEnabled(bool enabled) {
            _loggingEnabled = enabled;
            Log("[SeonEditorStub] Logging " + (enabled ? "enabled" : "disabled"));
        }

        public void SetDnsTimeout(int timeoutMs) {
            Log("[SeonEditorStub] SetDnsTimeout: " + timeoutMs + "ms (Android only, no-op in editor)");
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config) {
            _geoConfig = config;
            Log("[SeonEditorStub] Geolocation config set (enabled=" + config.Enabled + ")");
        }

        public void GetFingerprint(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                throw new ArgumentNullException(nameof(onFinished));
            }

            Log("[SeonEditorStub] GetFingerprint → returning mock fingerprint");
            onFinished(new SeonFingerprintResult(MockFingerprint));
        }

        public void StartBehaviourMonitoring() {
            if (_behaviourMonitoringActive) {
                Log("[SeonEditorStub] Behaviour monitoring already active");
                return;
            }

            _behaviourMonitoringActive = true;
            Log("[SeonEditorStub] Behaviour monitoring started");
        }

        public void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!_behaviourMonitoringActive) {
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "StartBehaviourMonitoring was not called before StopBehaviourMonitoring."
                }));
                return;
            }

            _behaviourMonitoringActive = false;
            Log("[SeonEditorStub] Behaviour monitoring stopped → returning mock fingerprint");
            onFinished(new SeonFingerprintResult(MockBehaviourFingerprint));
        }

        private void Log(string message) {
            if (_loggingEnabled) {
                Debug.Log(message);
            }
        }
    }
}
