using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;

namespace Seon {
    /// <summary>
    /// iOS platform bridge to call extern "C" functions
    /// defined in SeonUnityBridge.mm
    /// </summary>
    internal class SeoniOSBridge : ISeonPlatform {
#if UNITY_IOS

        [DllImport("__Internal")]
        private static extern void _SeonSetSessionId(string sessionId);

        [DllImport("__Internal")]
        private static extern void _SeonSetLoggingEnabled(bool enabled);

        [DllImport("__Internal")]
        private static extern void _SeonSetGeolocationConfig(
            bool enabled,
            bool prefetchEnabled,
            int maxCacheAgeSec,
            int serviceTimeoutMs);

        // Callback delegate types
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

        // callbacks

        private static Action<string> _currentSuccessCallback;
        private static Action<SeonError> _currentErrorCallback;
        private static Action<string> _currentBehaviourSuccessCallback;
        private static Action<SeonError> _currentBehaviourErrorCallback;

        // callback handlers must be static and decorated with MonoPInvokeCallback to be callable from native code

        [MonoPInvokeCallback(typeof(FingerprintSuccessCallback))]
        private static void OnFingerprintSuccess(string fingerprint){
            var callback = _currentSuccessCallback;
            _currentSuccessCallback = null;
            _currentErrorCallback = null;
            if (callback != null){
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(fingerprint));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintErrorCallback))]
        private static void OnFingerprintError(int code, string message){
            var callback = _currentErrorCallback;
            _currentErrorCallback = null;
            _currentSuccessCallback = null;
            if (callback != null){
                var error = new SeonError { Code = code, Message = message };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(error));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintSuccessCallback))]
        private static void OnBehaviourSuccess(string fingerprint)
        {
            var callback = _currentBehaviourSuccessCallback;
            _currentBehaviourSuccessCallback = null;
            _currentBehaviourErrorCallback = null;
            if (callback != null)
            {
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(fingerprint));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintErrorCallback))]
        private static void OnBehaviourError(int code, string message)
        {
            var callback = _currentBehaviourErrorCallback;
            _currentBehaviourErrorCallback = null;
            _currentBehaviourSuccessCallback = null;
            if (callback != null)
            {
                var error = new SeonError { Code = code, Message = message };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(error));
            }
        }

        [MonoPInvokeCallback(typeof(GeoFailureCallback))]
        private static void OnFingerprintGeoFailure(int code, string message, string fingerprint)
        {
            var callback = _currentErrorCallback;
            _currentErrorCallback = null;
            _currentSuccessCallback = null;
            if (callback != null)
            {
                var error = new SeonError
                {
                    Code = code,
                    Message = fingerprint,
                    GeolocationStatusCode = code
                };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(error));
            }
        }

        [MonoPInvokeCallback(typeof(GeoFailureCallback))]
        private static void OnBehaviourGeoFailure(int code, string message, string fingerprint)
        {
            var callback = _currentBehaviourErrorCallback;
            _currentBehaviourErrorCallback = null;
            _currentBehaviourSuccessCallback = null;
            if (callback != null)
            {
                var error = new SeonError
                {
                    Code = code,
                    Message = fingerprint,
                    GeolocationStatusCode = code
                };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(error));
            }
        }
        // interface implementations
        public void SetSessionId(string sessionId)
        {
            _SeonSetSessionId(sessionId);
        }

        public void SetLoggingEnabled(bool enabled)
        {
            _SeonSetLoggingEnabled(enabled);
        }

        public void SetDnsTimeout(int timeoutMs)
        {
            // DNS timeout is Android-only, no-op
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config)
        {
            _SeonSetGeolocationConfig(
                config.Enabled,
                config.PrefetchEnabled,
                config.MaxCacheAgeSec,
                config.ServiceTimeoutMs);
        }

        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError)
        {
            if (_currentSuccessCallback != null)
            {
                onError(new SeonError
                {
                    Code = -1,
                    Message = "A fingerprint request is already in progress."
                });
                return;
            }

            SeonMainThreadDispatcher.EnsureInitialized();

            _currentSuccessCallback = onSuccess;
            _currentErrorCallback = onError;
            _SeonGetFingerprint(OnFingerprintSuccess, OnFingerprintError, OnFingerprintGeoFailure);
        }

        public void StartBehaviourMonitoring()
        {
            int result = _SeonStartBehaviourMonitoring();
            if (result != 0)
            {
                throw new SeonException(new SeonError
                {
                    Code = result,
                    Message = "Failed to start behaviour monitoring."
                });
            }
        }

        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError)
        {
            if (_currentBehaviourSuccessCallback != null)
            {
                onError(new SeonError
                {
                    Code = -1,
                    Message = "A stop behaviour monitoring request is already in progress."
                });
                return;
            }

            SeonMainThreadDispatcher.EnsureInitialized();

            _currentBehaviourSuccessCallback = onSuccess;
            _currentBehaviourErrorCallback = onError;
            _SeonStopBehaviourMonitoring(OnBehaviourSuccess, OnBehaviourError, OnBehaviourGeoFailure);
        }

#else
        // Stubs, should never be reached due to runtime platform check
        public void SetSessionId(string sessionId) {
            throw new PlatformNotSupportedException();
        }
        public void SetLoggingEnabled(bool enabled) {
            throw new PlatformNotSupportedException();
        }
        public void SetDnsTimeout(int timeoutMs) {
            throw new PlatformNotSupportedException();
        }
        public void SetGeolocationConfig(SeonGeolocationConfig config) {
            throw new PlatformNotSupportedException();
        }
        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError) {
            throw new PlatformNotSupportedException();
        }
        public void StartBehaviourMonitoring() {
            throw new PlatformNotSupportedException();
        }
        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError) {
            throw new PlatformNotSupportedException();
        }
#endif
    }
}
