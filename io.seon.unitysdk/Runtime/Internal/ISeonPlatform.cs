using System;

namespace Seon {
    /// <summary>
    /// Internal platform abstraction interface.
    /// Each platform (iOS, Android, Editor) implements this to provide
    /// the native SEON SDK functionality.
    /// </summary>
    public interface ISeonPlatform {
        void SetSessionId(string sessionId);
        void SetLoggingEnabled(bool enabled);
        void SetDnsTimeout(int timeoutMs);
        void SetGeolocationConfig(SeonGeolocationConfig config);
        void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError);
        void StartBehaviourMonitoring();
        void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError);
    }
}
