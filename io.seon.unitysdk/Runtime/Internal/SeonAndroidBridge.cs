using System;
using UnityEngine;

namespace Seon {
    /// <summary>
    /// Android platform bridge using Unity's AndroidJavaObject JNI wrapper.
    /// Calls into the SEON Android SDK (io.seon.androidsdk) resolved by EDM4U.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Seon object is built lazily on first use via <see cref="EnsureSeon"/>.
    /// Call <see cref="SetSessionId"/> before <see cref="GetFingerprint"/>.
    /// </para>
    /// <para>
    /// The native SDK uses an ExecutorService for async work. Callbacks arrive
    /// on worker threads and are marshalled to Unity's main thread via
    /// <see cref="SeonMainThreadDispatcher"/>.
    /// </para>
    /// </remarks>
    internal class SeonAndroidBridge : ISeonPlatform {
#if UNITY_ANDROID || UNITY_EDITOR

        private AndroidJavaObject _seon;
        private string _sessionId;
        private int _dnsTimeoutMs = 3000;
        private bool _behaviourMonitoringActive;

        private AndroidJavaObject Seon {
            get {
                if (_seon == null) {
                    if (string.IsNullOrEmpty(_sessionId)) {
                        throw new InvalidOperationException(
                            "Call SeonSDK.SetSessionId() before using any other SDK method.");
                    }
                    _seon = BuildSeon();
                }
                return _seon;
            }
        }

        // ── ISeonPlatform implementation ────────────────────────────────

        public void SetSessionId(string sessionId) {
            _sessionId = sessionId;
            // Seon interface has setSessionId() — no rebuild needed
            if (_seon != null) {
                _seon.Call("setSessionId", sessionId);
            }
        }

        public void SetLoggingEnabled(bool enabled) {
            Seon.Call("setLoggingEnabled", enabled);
        }

        public void SetDnsTimeout(int timeoutMs) {
            _dnsTimeoutMs = timeoutMs;

            // SeonImpl has setDnsTimeout() setter — can be changed after build
            if (_seon != null) {
                _seon.Call("setDnsTimeout", timeoutMs);
            }
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config) {
            using (var configBuilder = new AndroidJavaObject("io.seon.androidsdk.dto.SeonGeolocationConfigBuilder")) {
                // Fluent builder returns 'this' but Unity wraps it in a new
                // AndroidJavaObject each time — must dispose to avoid JNI leaks.
                using (configBuilder.Call<AndroidJavaObject>("withPrefetchEnabled", config.PrefetchEnabled)) {
                }
                using (configBuilder.Call<AndroidJavaObject>(
                    "withGeolocationServiceTimeoutMs", config.ServiceTimeoutMs)) {
                }
                using (configBuilder.Call<AndroidJavaObject>(
                    "withMaxGeoLocationCacheAgeSec", config.MaxCacheAgeSec)) {
                }

                using (var geoConfig = configBuilder.Call<AndroidJavaObject>("build")) {
                    Seon.Call("setGeolocationEnabled", config.Enabled);
                    Seon.Call("setGeoLocationConfig", geoConfig);
                }
            }
        }

        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError) {
            try {
                var callback = new SeonCallbackProxy(onSuccess, onError);
                Seon.Call("getFingerprintBase64", callback);
            }
            catch (AndroidJavaException ex) {
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onError(new SeonError {
                        Code = -1,
                        Message = "Android SDK error: " + ex.Message
                    }));
            }
        }

        public void StartBehaviourMonitoring() {
            try {
                Seon.Call("startBehaviourMonitoring");
                _behaviourMonitoringActive = true;
            }
            catch (AndroidJavaException ex) {
                throw new SeonException(new SeonError {
                    Code = -1,
                    Message = "Failed to start behaviour monitoring: " + ex.Message
                });
            }
        }

        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError) {
            if (!_behaviourMonitoringActive) {
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onError(new SeonError {
                        Code = -1,
                        Message = "StartBehaviourMonitoring was not called before StopBehaviourMonitoring."
                    }));
                return;
            }

            try {
                var callback = new SeonCallbackProxy(onSuccess, onError);
                Seon.Call("stopBehaviourMonitoring", callback);
                _behaviourMonitoringActive = false;
            }
            catch (AndroidJavaException ex) {
                _behaviourMonitoringActive = false;
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onError(new SeonError {
                        Code = -1,
                        Message = "Failed to stop behaviour monitoring: " + ex.Message
                    }));
            }
        }

        // ── Lazy initialization ─────────────────────────────────────────

        private AndroidJavaObject BuildSeon() {
            SeonMainThreadDispatcher.EnsureInitialized();

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext")) {
                using (var builder = new AndroidJavaObject("io.seon.androidsdk.service.SeonBuilder")) {
                    // Fluent builder returns 'this' but Unity wraps it in a new
                    // AndroidJavaObject each time — must dispose to avoid JNI leaks.
                    using (builder.Call<AndroidJavaObject>("withContext", context)) {
                    }
                    using (builder.Call<AndroidJavaObject>("withSessionId", _sessionId)) {
                    }

                    if (_dnsTimeoutMs != 3000) {
                        using (builder.Call<AndroidJavaObject>("withDnsTimeout", _dnsTimeoutMs)) {
                        }
                    }

                    return builder.Call<AndroidJavaObject>("build");
                }
            }
        }

        // ── Android callback proxy ──────────────────────────────────────

        /// <summary>
        /// AndroidJavaProxy implementing SeonCallbackWithGeo.
        /// <para>
        /// SeonCallback is a @FunctionalInterface with a single method:
        ///   void onComplete(String response)
        /// SeonCallbackWithGeo extends it with:
        ///   void onCompleteWithGeoFailure(String response, int geoStatusCode)
        /// </para>
        /// <para>
        /// Errors are NOT delivered via callback — they are thrown as
        /// SeonException from the SDK method calls (caught as AndroidJavaException).
        /// </para>
        /// </summary>
        private class SeonCallbackProxy : AndroidJavaProxy {
            private readonly Action<string> _onSuccess;
            private readonly Action<SeonError> _onError;

            public SeonCallbackProxy(Action<string> onSuccess, Action<SeonError> onError)
                : base("io.seon.androidsdk.service.SeonCallbackWithGeo") {
                _onSuccess = onSuccess;
                _onError = onError;
            }

            void onComplete(string response) {
                var callback = _onSuccess;
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback?.Invoke(response));
            }

            void onCompleteWithGeoFailure(string response, int geoStatusCode) {
                var errorCallback = _onError;
                SeonMainThreadDispatcher.Instance.Enqueue(() => {
                    errorCallback?.Invoke(new SeonError {
                        Code = -1,
                        Message = response,
                        GeolocationStatusCode = geoStatusCode
                    });
                });
            }
        }

#else
        // Stubs for non-Android platforms
        public void SetSessionId(string sessionId) { throw new PlatformNotSupportedException(); }
        public void SetLoggingEnabled(bool enabled) { throw new PlatformNotSupportedException(); }
        public void SetDnsTimeout(int timeoutMs) { throw new PlatformNotSupportedException(); }
        public void SetGeolocationConfig(SeonGeolocationConfig config) { throw new PlatformNotSupportedException(); }
        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError) { throw new PlatformNotSupportedException(); }
        public void StartBehaviourMonitoring() { throw new PlatformNotSupportedException(); }
        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError) { throw new PlatformNotSupportedException(); }
#endif
    }
}
