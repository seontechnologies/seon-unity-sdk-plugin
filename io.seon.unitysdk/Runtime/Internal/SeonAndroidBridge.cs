using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seon {
    internal class SeonAndroidBridge : ISeonPlatform {
#if UNITY_ANDROID || UNITY_EDITOR

        private AndroidJavaObject _seon;
        private string _sessionId;
        private int _dnsTimeoutMs = 3000;
        private bool _behaviourMonitoringActive;

        private readonly object _callbackLock = new object();
        private readonly HashSet<SeonCallbackProxy> _activeCallbacks = new HashSet<SeonCallbackProxy>();

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

        public void SetSessionId(string sessionId) {
            _sessionId = sessionId;
            if (_seon != null) {
                _seon.Call("setSessionId", sessionId);
            }
        }

        public string GetSessionId() {
            if (_seon != null) {
                return _seon.Call<string>("getSessionId");
            }
            return _sessionId;
        }

        public void SetLoggingEnabled(bool enabled) {
            // Native SDK takes java.lang.Boolean (boxed), not primitive boolean.
            // Unity marshals C# bool as primitive boolean (JNI signature 'Z'),
            // so we must box it explicitly.
            using (var boxedEnabled = new AndroidJavaObject("java.lang.Boolean", enabled)) {
                Seon.Call("setLoggingEnabled", boxedEnabled);
            }
        }

        public void SetDnsTimeout(int timeoutMs) {
            _dnsTimeoutMs = timeoutMs;

            if (_seon != null) {
                _seon.Call("setDnsTimeout", timeoutMs);
            }
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config) {
            using (var configBuilder = new AndroidJavaObject("io.seon.androidsdk.dto.SeonGeolocationConfigBuilder")) {
                // Fluent builder calls return new wrappers; dispose them to avoid JNI local ref leaks.
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

        public void GetFingerprint(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                throw new ArgumentNullException(nameof(onFinished));
            }

            SeonCallbackProxy callback = null;
            try {
                callback = new SeonCallbackProxy(onFinished, UnregisterCallback);
                RegisterCallback(callback);
                Seon.Call("getFingerprintBase64", callback);
            }
            catch (AndroidJavaException ex) {
                if (callback != null) {
                    UnregisterCallback(callback);
                }

                var error = new SeonError {
                    Code = -1,
                    Message = "Android SDK error: " + ex.Message
                };
                SeonMainThreadDispatcher.Instance.Enqueue(() => onFinished(new SeonFingerprintResult(null, error)));
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

        public void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!_behaviourMonitoringActive) {
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onFinished(new SeonFingerprintResult(null, new SeonError {
                        Code = -1,
                        Message = "StartBehaviourMonitoring was not called before StopBehaviourMonitoring."
                    })));
                return;
            }

            SeonCallbackProxy callback = null;
            try {
                callback = new SeonCallbackProxy(onFinished, UnregisterCallback);
                RegisterCallback(callback);
                Seon.Call("stopBehaviourMonitoring", callback);
                _behaviourMonitoringActive = false;
            }
            catch (AndroidJavaException ex) {
                if (callback != null) {
                    UnregisterCallback(callback);
                }

                _behaviourMonitoringActive = false;
                var error = new SeonError {
                    Code = -1,
                    Message = "Failed to stop behaviour monitoring: " + ex.Message
                };
                SeonMainThreadDispatcher.Instance.Enqueue(() => onFinished(new SeonFingerprintResult(null, error)));
            }
        }

        private AndroidJavaObject BuildSeon() {
            SeonMainThreadDispatcher.EnsureInitialized();

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext")) {
                using (var builder = new AndroidJavaObject("io.seon.androidsdk.service.SeonBuilder")) {
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

        private void RegisterCallback(SeonCallbackProxy callback) {
            lock (_callbackLock) {
                _activeCallbacks.Add(callback);
            }
        }

        private void UnregisterCallback(SeonCallbackProxy callback) {
            lock (_callbackLock) {
                _activeCallbacks.Remove(callback);
            }
        }

        private class SeonCallbackProxy : AndroidJavaProxy {
            private readonly Action<SeonFingerprintResult> _onFinished;
            private readonly Action<SeonCallbackProxy> _onCompleted;

            public SeonCallbackProxy(
                Action<SeonFingerprintResult> onFinished,
                Action<SeonCallbackProxy> onCompleted)
                : base("io.seon.androidsdk.service.SeonCallbackWithGeo") {
                _onFinished = onFinished;
                _onCompleted = onCompleted;
            }

            private void Complete(SeonFingerprintResult result) {
                _onCompleted?.Invoke(this);

                var callback = _onFinished;
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback?.Invoke(result));
            }

            void onComplete(string response) {
                Complete(new SeonFingerprintResult(response));
            }

            void onCompleteWithGeoFailure(string response, int geoStatusCode) {
                var warning = new SeonError {
                    Code = geoStatusCode,
                    Message = "Geolocation collection failed, but fingerprint generation succeeded.",
                    GeolocationStatusCode = geoStatusCode
                };
                Complete(new SeonFingerprintResult(response, warning));
            }
        }

#else
        public void SetSessionId(string sessionId) { throw new PlatformNotSupportedException(); }
        public string GetSessionId() { throw new PlatformNotSupportedException(); }
        public void SetLoggingEnabled(bool enabled) { throw new PlatformNotSupportedException(); }
        public void SetDnsTimeout(int timeoutMs) { throw new PlatformNotSupportedException(); }
        public void SetGeolocationConfig(SeonGeolocationConfig config) { throw new PlatformNotSupportedException(); }
        public void GetFingerprint(Action<SeonFingerprintResult> onFinished) { throw new PlatformNotSupportedException(); }
        public void StartBehaviourMonitoring() { throw new PlatformNotSupportedException(); }
        public void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished) { throw new PlatformNotSupportedException(); }
#endif
    }
}
