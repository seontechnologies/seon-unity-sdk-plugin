using System;

namespace Seon {
    /// <summary>
    /// Internal platform abstraction interface.
    /// Each platform (iOS, Android, Editor) implements this to provide
    /// the native SEON SDK functionality.
    /// </summary>
    internal interface ISeonPlatform {
        void SetSessionId(string sessionId);
        string GetSessionId();
        void SetLoggingEnabled(bool enabled);
        void SetDnsTimeout(int timeoutMs);
        void SetGeolocationConfig(SeonGeolocationConfig config);
        void GetFingerprint(Action<SeonFingerprintResult> onFinished);
        void StartBehaviourMonitoring();
        void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished);
    }
}
