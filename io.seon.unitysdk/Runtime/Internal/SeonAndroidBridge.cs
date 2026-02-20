using System;
using UnityEngine;

namespace Seon
{
    /// <summary>
    /// Android platform bridge using Unity's AndroidJavaObject JNI wrapper.
    /// Calls into the SEON Android SDK (io.seon.androidsdk) resolved by EDM4U.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The native SEON SDK dispatches all work to its own internal ExecutorService
    /// worker threads. C# calls Java methods from Unity's main thread, the SDK
    /// internally schedules them, and callbacks arrive on executor threads.
    /// This bridge marshals all callbacks back to Unity's main thread via
    /// <see cref="SeonMainThreadDispatcher"/>.
    /// </para>
    /// </remarks>
    internal class SeonAndroidBridge : ISeonPlatform
    {
#if UNITY_ANDROID && !UNITY_EDITOR

        private AndroidJavaObject _seon;
        private bool _behaviourMonitoringActive;

        // ── ISeonPlatform implementation ────────────────────────────────

        public void Init(string sessionId)
        {
            // Ensure dispatcher exists before any async work
            _ = SeonMainThreadDispatcher.Instance;

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
            {
                using (var builder = new AndroidJavaObject("io.seon.androidsdk.service.SeonBuilder"))
                {
                    builder.Call<AndroidJavaObject>("withContext", context);
                    builder.Call<AndroidJavaObject>("withSessionId", sessionId);
                    _seon = builder.Call<AndroidJavaObject>("build");
                }
            }
        }

        public void SetSessionId(string sessionId)
        {
            // Android SDK doesn't have a separate setSessionId after build.
            // To change session, we need to re-initialize.
            // Store for potential re-init, but for now re-build.
            Cleanup();
            Init(sessionId);
        }

        public void SetLoggingEnabled(bool enabled)
        {
            EnsureSeon();
            _seon.Call("setLoggingEnabled", enabled);
        }

        public void SetDnsTimeout(int timeoutMs)
        {
            // DNS timeout must be set during build via SeonBuilder.withDnsTimeout().
            // After build, it cannot be changed. Log a warning.
            Debug.LogWarning(
                "[SeonSDK] SetDnsTimeout must be called before Init on Android. " +
                "Currently this requires re-initialization. Consider calling Init with " +
                "a new session to apply DNS timeout changes.");
        }

        public void SetGeolocationConfig(SeonGeolocationConfig config)
        {
            EnsureSeon();

            using (var configBuilder = new AndroidJavaObject("io.seon.androidsdk.dto.SeonGeolocationConfigBuilder"))
            {
                configBuilder.Call<AndroidJavaObject>("withPrefetchEnabled", config.PrefetchEnabled);
                configBuilder.Call<AndroidJavaObject>(
                    "withGeolocationServiceTimeoutMs", config.ServiceTimeoutMs);
                configBuilder.Call<AndroidJavaObject>(
                    "withMaxGeoLocationCacheAgeSec", config.MaxCacheAgeSec);

                using (var geoConfig = configBuilder.Call<AndroidJavaObject>("build"))
                {
                    _seon.Call("setGeolocationEnabled", config.Enabled);
                    _seon.Call("setGeoLocationConfig", geoConfig);
                }
            }
        }

        public void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError)
        {
            EnsureSeon();

            try
            {
                var callback = new SeonCallbackProxy(onSuccess, onError);
                _seon.Call("getFingerprintBase64", callback);
            }
            catch (AndroidJavaException ex)
            {
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onError(new SeonError
                    {
                        Code = -1,
                        Message = $"Android SDK error: {ex.Message}"
                    }));
            }
        }

        public void StartBehaviourMonitoring()
        {
            EnsureSeon();

            try
            {
                _seon.Call("startBehaviourMonitoring");
                _behaviourMonitoringActive = true;
            }
            catch (AndroidJavaException ex)
            {
                throw new SeonException(new SeonError
                {
                    Code = -1,
                    Message = $"Failed to start behaviour monitoring: {ex.Message}"
                });
            }
        }

        public void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError)
        {
            EnsureSeon();

            if (!_behaviourMonitoringActive)
            {
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onError(new SeonError
                    {
                        Code = -1,
                        Message = "StartBehaviourMonitoring was not called before StopBehaviourMonitoring."
                    }));
                return;
            }

            try
            {
                var callback = new SeonCallbackProxy(onSuccess, onError);
                _seon.Call("stopBehaviourMonitoring", callback);
                _behaviourMonitoringActive = false;
            }
            catch (AndroidJavaException ex)
            {
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                    onError(new SeonError
                    {
                        Code = -1,
                        Message = $"Failed to stop behaviour monitoring: {ex.Message}"
                    }));
            }
        }

        public void Cleanup()
        {
            _behaviourMonitoringActive = false;
            if (_seon != null)
            {
                _seon.Dispose();
                _seon = null;
            }
        }

        private void EnsureSeon()
        {
            if (_seon == null)
                throw new InvalidOperationException("SEON Android SDK not initialized. Call Init() first.");
        }

        // ── Android callback proxies ────────────────────────────────────

        /// <summary>
        /// AndroidJavaProxy implementing the SeonCallback Java interface.
        /// Callbacks arrive on the SDK's ExecutorService thread and are
        /// marshalled to Unity's main thread.
        /// </summary>
        private class SeonCallbackProxy : AndroidJavaProxy
        {
            private readonly Action<string> _onSuccess;
            private readonly Action<SeonError> _onError;

            public SeonCallbackProxy(Action<string> onSuccess, Action<SeonError> onError)
                : base("io.seon.androidsdk.service.SeonCallback")
            {
                _onSuccess = onSuccess;
                _onError = onError;
            }

            /// <summary>
            /// Called by the native SDK on its ExecutorService thread when
            /// fingerprinting completes successfully.
            /// </summary>
            // ReSharper disable once UnusedMember.Local — called via JNI reflection
            void onComplete(string response)
            {
                var callback = _onSuccess;
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback?.Invoke(response));
            }
        }

        /// <summary>
        /// AndroidJavaProxy implementing SeonCallbackWithGeo for geolocation-aware calls.
        /// </summary>
        private class SeonCallbackWithGeoProxy : AndroidJavaProxy
        {
            private readonly Action<string> _onSuccess;
            private readonly Action<SeonError> _onError;

            public SeonCallbackWithGeoProxy(Action<string> onSuccess, Action<SeonError> onError)
                : base("io.seon.androidsdk.service.SeonCallbackWithGeo")
            {
                _onSuccess = onSuccess;
                _onError = onError;
            }

            // ReSharper disable once UnusedMember.Local
            void onComplete(string response)
            {
                var callback = _onSuccess;
                SeonMainThreadDispatcher.Instance.Enqueue(() => callback?.Invoke(response));
            }

            // ReSharper disable once UnusedMember.Local
            void onCompleteWithGeoFailure(string response, int geoStatusCode)
            {
                // Fingerprint succeeded but geo failed — still call onSuccess with the fingerprint,
                // but log the geo failure
                var callback = _onSuccess;
                SeonMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    Debug.LogWarning(
                        $"[SeonSDK] Geolocation collection failed (status={geoStatusCode}) " +
                        "but fingerprint was generated successfully.");
                    callback?.Invoke(response);
                });
            }
        }

#else
        // Stubs for non-Android platforms
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
