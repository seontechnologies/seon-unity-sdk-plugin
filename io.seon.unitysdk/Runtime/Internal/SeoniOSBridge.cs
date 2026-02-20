using System;
using System.Runtime.InteropServices;
using UnityEngine;
using AOT;

namespace Seon
{
    /// <summary>
    /// iOS platform bridge using DllImport to call extern "C" functions
    /// defined in SeonUnityBridge.mm, which wraps SEONFingerprint.
    /// </summary>
    internal class SeoniOSBridge : ISeonPlatform
    {
#if UNITY_IOS && !UNITY_EDITOR

        // ── Native function declarations ────────────────────────────────

        [DllImport("__Internal")]
        private static extern void _SeonInit(string sessionId);

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
        private delegate void FingerprintGeoErrorCallback(int code, string message, int geoStatusCode);

        [DllImport("__Internal")]
        private static extern void _SeonGetFingerprint(
            FingerprintSuccessCallback onSuccess,
            FingerprintErrorCallback onError);

        [DllImport("__Internal")]
        private static extern int _SeonStartBehaviourMonitoring();

        [DllImport("__Internal")]
        private static extern void _SeonStopBehaviourMonitoring(
            FingerprintSuccessCallback onSuccess,
            FingerprintErrorCallback onError);

        [DllImport("__Internal")]
        private static extern void _SeonCleanup();

        // ── Stored callbacks (prevent GC collection) ────────────────────

        private static Action<string> _currentSuccessCallback;
        private static Action<SeonError> _currentErrorCallback;
        private static Action<string> _currentBehaviourSuccessCallback;
        private static Action<SeonError> _currentBehaviourErrorCallback;

        // ── MonoPInvokeCallback handlers ────────────────────────────────

        [MonoPInvokeCallback(typeof(FingerprintSuccessCallback))]
        private static void OnFingerprintSuccess(string fingerprint)
        {
            var callback = _currentSuccessCallback;
            _currentSuccessCallback = null;
            if (callback != null)
            {
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(fingerprint));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintErrorCallback))]
        private static void OnFingerprintError(int code, string message)
        {
            var callback = _currentErrorCallback;
            _currentErrorCallback = null;
            if (callback != null)
            {
                var error = new SeonError { Code = code, Message = message };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(error));
            }
        }

        [MonoPInvokeCallback(typeof(FingerprintSuccessCallback))]
        private static void OnBehaviourSuccess(string fingerprint)
        {
            var callback = _currentBehaviourSuccessCallback;
            _currentBehaviourSuccessCallback = null;
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
            if (callback != null)
            {
                var error = new SeonError { Code = code, Message = message };
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback(error));
            }
        }

        // ── ISeonPlatform implementation ────────────────────────────────

        public void Init(string sessionId)
        {
            // Ensure dispatcher exists before any async work
            _ = SeonMainThreadDispatcher.Instance;
            _SeonInit(sessionId);
        }

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
            // DNS timeout is Android-only; no-op on iOS
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
            _currentSuccessCallback = onSuccess;
            _currentErrorCallback = onError;
            _SeonGetFingerprint(OnFingerprintSuccess, OnFingerprintError);
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
            _currentBehaviourSuccessCallback = onSuccess;
            _currentBehaviourErrorCallback = onError;
            _SeonStopBehaviourMonitoring(OnBehaviourSuccess, OnBehaviourError);
        }

        public void Cleanup()
        {
            _currentSuccessCallback = null;
            _currentErrorCallback = null;
            _currentBehaviourSuccessCallback = null;
            _currentBehaviourErrorCallback = null;
            _SeonCleanup();
        }

#else
        // Stubs for non-iOS platforms (should never be reached due to compile-time selection)
        public void Init(string sessionId) => throw new PlatformNotSupportedException();
        public void SetSessionId(string sessionId) => throw new PlatformNotSupportedException();
        public void SetLoggingEnabled(bool enabled) => throw new PlatformNotSupportedException();
        public void SetDnsTimeout(int timeoutMs) => throw new PlatformNotSupportedException();
        public void SetGeolocationConfig(SeonGeolocationConfig config) => throw new PlatformNotSupportedException();
        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError) => throw new PlatformNotSupportedException();
        public void StartBehaviourMonitoring() => throw new PlatformNotSupportedException();
        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError) => throw new PlatformNotSupportedException();
        public void Cleanup() { }
#endif
    }
}
