using System;
using System.Runtime.InteropServices;
using AOT;

namespace Seon {
    /// <summary>
    /// iOS platform bridge using DllImport for native calls from SeonUnityBridge.mm.
    /// </summary>
    internal class SeoniOSBridge : ISeonPlatform {
#if UNITY_IOS

        [DllImport("__Internal")]
        private static extern void _SeonSetSessionId(string sessionId);

        [DllImport("__Internal")]
        private static extern IntPtr _SeonGetSessionId();

        [DllImport("__Internal")]
        private static extern void _SeonFreeString(IntPtr str);

        [DllImport("__Internal")]
        private static extern void _SeonSetLoggingEnabled(bool enabled);

        [DllImport("__Internal")]
        private static extern void _SeonSetGeolocationConfig(
            bool enabled,
            bool prefetchEnabled,
            int maxCacheAgeSec,
            int serviceTimeoutMs);

        private delegate void FingerprintSuccessCallback(string fingerprint);
        private delegate void FingerprintErrorCallback(int code, string message);
        private delegate void GeoFailureCallback(int code, string message, string fingerprint);

        [DllImport("__Internal")]
        private static extern void _SeonGetFingerprint(
            FingerprintSuccessCallback onSuccess,
            FingerprintErrorCallback onError,
            GeoFailureCallback onGeoFailure);

        [DllImport("__Internal")]
        private static extern int _SeonStartBehaviourMonitoring();

        [DllImport("__Internal")]
        private static extern void _SeonStopBehaviourMonitoring(
            FingerprintSuccessCallback onSuccess,
            FingerprintErrorCallback onError,
            GeoFailureCallback onGeoFailure);

        private static readonly object CallbackLock = new object();

        private static Action<SeonFingerprintResult> _currentFingerprintCallback;
        private static Action<SeonFingerprintResult> _currentBehaviourCallback;

        [MonoPInvokeCallback(typeof(FingerprintSuccessCallback))]
        private static void OnFingerprintSuccess(string fingerprint) {
            var callback = TakeFingerprintCallback();
            if (callback != null) {
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(new SeonFingerprintResult(fingerprint)));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintErrorCallback))]
        private static void OnFingerprintError(int code, string message) {
            var callback = TakeFingerprintCallback();
            if (callback != null) {
                var error = new SeonError { Code = code, Message = message };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(new SeonFingerprintResult(null, error)));
            }
        }

        [MonoPInvokeCallback(typeof(GeoFailureCallback))]
        private static void OnFingerprintGeoFailure(int code, string message, string fingerprint) {
            var callback = TakeFingerprintCallback();
            if (callback != null) {
                var warning = new SeonError {
                    Code = code,
                    Message = message,
                    GeolocationStatusCode = code
                };
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    callback(new SeonFingerprintResult(fingerprint, warning)));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintSuccessCallback))]
        private static void OnBehaviourSuccess(string fingerprint) {
            var callback = TakeBehaviourCallback();
            if (callback != null) {
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(new SeonFingerprintResult(fingerprint)));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintErrorCallback))]
        private static void OnBehaviourError(int code, string message) {
            var callback = TakeBehaviourCallback();
            if (callback != null) {
                var error = new SeonError { Code = code, Message = message };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(new SeonFingerprintResult(null, error)));
            }
        }

        [MonoPInvokeCallback(typeof(GeoFailureCallback))]
        private static void OnBehaviourGeoFailure(int code, string message, string fingerprint) {
            var callback = TakeBehaviourCallback();
            if (callback != null) {
                var warning = new SeonError {
                    Code = code,
                    Message = message,
                    GeolocationStatusCode = code
                };
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    callback(new SeonFingerprintResult(fingerprint, warning)));
            }
        }

        public void SetSessionId(string sessionId) {
            _SeonSetSessionId(sessionId);
        }

        public string GetSessionId() {
            IntPtr ptr = _SeonGetSessionId();
            if (ptr == IntPtr.Zero) {
                return null;
            }
            string result = Marshal.PtrToStringAnsi(ptr);
            _SeonFreeString(ptr);
            return result;
        }

        public void SetLoggingEnabled(bool enabled) {
            _SeonSetLoggingEnabled(enabled);
        }

        public void SetDnsTimeout(int timeoutMs) {
            // Android-only; no-op on iOS.
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config) {
            _SeonSetGeolocationConfig(
                config.Enabled,
                config.PrefetchEnabled,
                config.MaxCacheAgeSec,
                config.ServiceTimeoutMs);
        }

        public void GetFingerprint(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!TrySetFingerprintCallback(onFinished)) {
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "A fingerprint request is already in progress."
                }));
                return;
            }

            SeonMainThreadDispatcher.EnsureInitialized();
            try {
                _SeonGetFingerprint(OnFingerprintSuccess, OnFingerprintError, OnFingerprintGeoFailure);
            }
            catch (Exception ex) {
                TakeFingerprintCallback();
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "Failed to start fingerprint request: " + ex.Message
                }));
            }
        }

        public void StartBehaviourMonitoring() {
            int result = _SeonStartBehaviourMonitoring();
            if (result != 0) {
                throw new SeonException(new SeonError {
                    Code = result,
                    Message = "Failed to start behaviour monitoring."
                });
            }
        }

        public void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                throw new ArgumentNullException(nameof(onFinished));
            }

            if (!TrySetBehaviourCallback(onFinished)) {
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "A stop behaviour monitoring request is already in progress."
                }));
                return;
            }

            SeonMainThreadDispatcher.EnsureInitialized();
            try {
                _SeonStopBehaviourMonitoring(OnBehaviourSuccess, OnBehaviourError, OnBehaviourGeoFailure);
            }
            catch (Exception ex) {
                TakeBehaviourCallback();
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "Failed to stop behaviour monitoring: " + ex.Message
                }));
            }
        }

        private static bool TrySetFingerprintCallback(Action<SeonFingerprintResult> callback) {
            lock (CallbackLock) {
                if (_currentFingerprintCallback != null) {
                    return false;
                }

                _currentFingerprintCallback = callback;
                return true;
            }
        }

        private static bool TrySetBehaviourCallback(Action<SeonFingerprintResult> callback) {
            lock (CallbackLock) {
                if (_currentBehaviourCallback != null) {
                    return false;
                }

                _currentBehaviourCallback = callback;
                return true;
            }
        }

        private static Action<SeonFingerprintResult> TakeFingerprintCallback() {
            lock (CallbackLock) {
                var callback = _currentFingerprintCallback;
                _currentFingerprintCallback = null;
                return callback;
            }
        }

        private static Action<SeonFingerprintResult> TakeBehaviourCallback() {
            lock (CallbackLock) {
                var callback = _currentBehaviourCallback;
                _currentBehaviourCallback = null;
                return callback;
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
