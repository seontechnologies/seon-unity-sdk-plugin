using System;
using UnityEngine;

namespace Seon
{
    /// <summary>
    /// Editor stub implementation for in-editor testing.
    /// Returns deterministic mock fingerprints so integrators can test
    /// their flow without deploying to a device.
    /// </summary>
    internal class SeonEditorStub : ISeonPlatform
    {
        private string _sessionId;
        private bool _loggingEnabled;
        private bool _behaviourMonitoringActive;
        private SeonGeolocationConfig _geoConfig;

        // Deterministic mock fingerprint (base64 of "seon-editor-mock-fingerprint")
        private const string MockFingerprint = "c2Vvbi1lZGl0b3ItbW9jay1maW5nZXJwcmludA==";
        private const string MockBehaviourFingerprint = "c2Vvbi1lZGl0b3ItYmVoYXZpb3VyLWZpbmdlcnByaW50";

        public void Init(string sessionId)
        {
            _sessionId = sessionId;
            Log($"[SeonEditorStub] Init with sessionId: {sessionId}");
        }

        public void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
            Log($"[SeonEditorStub] SetSessionId: {sessionId}");
        }

        public void SetLoggingEnabled(bool enabled)
        {
            _loggingEnabled = enabled;
            Log($"[SeonEditorStub] Logging {(enabled ? "enabled" : "disabled")}");
        }

        public void SetDnsTimeout(int timeoutMs)
        {
            Log($"[SeonEditorStub] SetDnsTimeout: {timeoutMs}ms (Android only, no-op in editor)");
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config)
        {
            _geoConfig = config;
            Log($"[SeonEditorStub] Geolocation config set (enabled={config.Enabled})");
        }

        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError)
        {
            Log("[SeonEditorStub] GetFingerprint → returning mock fingerprint");
            // In editor, invoke immediately on main thread
            onSuccess?.Invoke(MockFingerprint);
        }

        public void StartBehaviourMonitoring()
        {
            if (_behaviourMonitoringActive)
            {
                Log("[SeonEditorStub] Behaviour monitoring already active");
                return;
            }

            _behaviourMonitoringActive = true;
            Log("[SeonEditorStub] Behaviour monitoring started");
        }

        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError)
        {
            if (!_behaviourMonitoringActive)
            {
                onError?.Invoke(new SeonError
                {
                    Code = -1,
                    Message = "StartBehaviourMonitoring was not called before StopBehaviourMonitoring."
                });
                return;
            }

            _behaviourMonitoringActive = false;
            Log("[SeonEditorStub] Behaviour monitoring stopped → returning mock fingerprint");
            onSuccess?.Invoke(MockBehaviourFingerprint);
        }

        public void Cleanup()
        {
            _sessionId = null;
            _behaviourMonitoringActive = false;
            _geoConfig = null;
            Log("[SeonEditorStub] Cleanup");
        }

        private void Log(string message)
        {
            if (_loggingEnabled)
                Debug.Log(message);
        }
    }
}
