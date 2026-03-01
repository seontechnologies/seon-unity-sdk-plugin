using System;
using UnityEngine;

namespace Seon {
    /// <summary>
    /// Main entry point for the SEON Device Intelligence SDK.
    /// Provides a unified C# API wrapping native iOS and Android implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="SetSessionId"/> before <see cref="GetFingerprint"/>.
    /// The SDK delegates to platform-specific implementations selected at compile time:
    /// <list type="bullet">
    ///   <item><description>iOS: Obj-C bridge via DllImport</description></item>
    ///   <item><description>Android: JNI bridge via AndroidJavaObject</description></item>
    ///   <item><description>Editor: Mock stub returning dummy data</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// All public methods are defensively guarded — invalid arguments are logged
    /// via <c>Debug.LogError</c> and the call is silently ignored. The SDK will
    /// never throw an exception that could crash the host application.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// SeonSDK.SetSessionId("my-session-id");
    /// SeonSDK.SetLoggingEnabled(true);
    /// SeonSDK.GetFingerprint(result =>
    /// {
    ///     if (!result.IsSuccess) {
    ///         Debug.LogError(result.Error.Message);
    ///         return;
    ///     }
    ///
    ///     Debug.Log(result.Fingerprint);
    /// });
    /// </code>
    /// </example>
    public static class SeonSDK {
        private const string Tag = "[SeonSDK]";

        private static ISeonPlatform _platform;
        private static string _sessionId;

        static SeonSDK() {
#if UNITY_EDITOR
            _platform = new SeonEditorStub();
#elif UNITY_IOS
            _platform = new SeoniOSBridge();
#elif UNITY_ANDROID
            _platform = new SeonAndroidBridge();
#else
            _platform = new SeonEditorStub();
#endif
        }

        /// <summary>
        /// Set the session identifier. Must be called before <see cref="GetFingerprint"/>.
        /// </summary>
        /// <param name="sessionId">
        /// A unique identifier for this session. This value is included in the
        /// generated fingerprint and is used to correlate with Fraud API requests.
        /// </param>
        public static void SetSessionId(string sessionId) {
            if (string.IsNullOrWhiteSpace(sessionId)) {
                Debug.LogError(Tag + " SetSessionId: sessionId must not be null, empty, or whitespace.");
                return;
            }

            _sessionId = sessionId;
            try {
                _platform.SetSessionId(sessionId);
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " SetSessionId failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Get the current session identifier, or <c>null</c> if not yet set.
        /// </summary>
        public static string GetSessionId() {
            try {
                return _platform.GetSessionId();
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " GetSessionId failed: " + ex.Message);
                return _sessionId;
            }
        }

        /// <summary>
        /// Enable or disable SDK diagnostic logging.
        /// </summary>
        /// <param name="enabled">True to enable logging, false to disable.</param>
        public static void SetLoggingEnabled(bool enabled) {
            try {
                _platform.SetLoggingEnabled(enabled);
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " SetLoggingEnabled failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Set the DNS timeout for the SDK's internal network call.
        /// Android only — ignored on iOS.
        /// </summary>
        /// <param name="timeoutMs">
        /// Timeout in milliseconds. Pass 0 to skip the DNS lookup entirely,
        /// which means <c>device_ip_*</c> and <c>dns_ip_*</c> fields won't be
        /// populated in the Fraud API response.
        /// </param>
        public static void SetDnsTimeout(int timeoutMs) {
            if (timeoutMs < 0) {
                Debug.LogError(Tag + " SetDnsTimeout: timeoutMs must not be negative.");
                return;
            }

            try {
                _platform.SetDnsTimeout(timeoutMs);
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " SetDnsTimeout failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Configure geolocation collection.
        /// Must be called before <see cref="GetFingerprint"/> or
        /// <see cref="StopBehaviourMonitoring"/> for location data to be included.
        /// </summary>
        /// <param name="config">Geolocation configuration.</param>
        public static void SetGeolocationConfig(SeonGeolocationConfig config) {
            if (config == null) {
                Debug.LogError(Tag + " SetGeolocationConfig: config must not be null.");
                return;
            }

            try {
                _platform.SetGeolocationConfig(config);
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " SetGeolocationConfig failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Asynchronously compute the device fingerprint.
        /// </summary>
        /// <param name="onFinished">
        /// Invoked on the Unity main thread with a typed result object.
        /// This callback can represent success, failure, or partial success
        /// (fingerprint generated but geolocation failed).
        /// </param>
        public static void GetFingerprint(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                Debug.LogError(Tag + " GetFingerprint: onFinished callback must not be null.");
                return;
            }

            if (string.IsNullOrEmpty(_sessionId)) {
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "Call SeonSDK.SetSessionId() before using GetFingerprint."
                }));
                return;
            }

            try {
                _platform.GetFingerprint(onFinished);
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " GetFingerprint failed: " + ex.Message);
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "Internal SDK error: " + ex.Message
                }));
            }
        }

        /// <summary>
        /// Start monitoring device sensor data for suspicious behaviour.
        /// Call this at the beginning of a user journey you want to monitor.
        /// </summary>
        /// <remarks>
        /// Behaviour monitoring collects sensor data (accelerometer, gyroscope, etc.)
        /// to detect automation, device farms, and vishing activity.
        /// The analysis needs at least a few seconds of data collection to be effective.
        /// </remarks>
        /// <returns>
        /// <c>null</c> on success, or a <see cref="SeonError"/> describing the failure.
        /// </returns>
        public static SeonError StartBehaviourMonitoring() {
            if (string.IsNullOrEmpty(_sessionId)) {
                var error = new SeonError {
                    Code = -1,
                    Message = "Call SeonSDK.SetSessionId() before using StartBehaviourMonitoring."
                };
                Debug.LogError(Tag + " " + error.Message);
                return error;
            }

            try {
                _platform.StartBehaviourMonitoring();
                return null;
            }
            catch (Exception ex) {
                var error = new SeonError {
                    Code = -1,
                    Message = "Failed to start behaviour monitoring: " + ex.Message
                };
                Debug.LogError(Tag + " " + error.Message);
                return error;
            }
        }

        /// <summary>
        /// Stop behaviour monitoring and retrieve the enriched fingerprint.
        /// </summary>
        /// <param name="onFinished">
        /// Invoked on the Unity main thread with a typed result object.
        /// This callback can represent success, failure, or partial success
        /// (fingerprint generated but geolocation failed).
        /// </param>
        public static void StopBehaviourMonitoring(Action<SeonFingerprintResult> onFinished) {
            if (onFinished == null) {
                Debug.LogError(Tag + " StopBehaviourMonitoring: onFinished callback must not be null.");
                return;
            }

            if (string.IsNullOrEmpty(_sessionId)) {
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "Call SeonSDK.SetSessionId() before using StopBehaviourMonitoring."
                }));
                return;
            }

            try {
                _platform.StopBehaviourMonitoring(onFinished);
            }
            catch (Exception ex) {
                Debug.LogError(Tag + " StopBehaviourMonitoring failed: " + ex.Message);
                onFinished(new SeonFingerprintResult(null, new SeonError {
                    Code = -1,
                    Message = "Internal SDK error: " + ex.Message
                }));
            }
        }

        // ── Internal ───────────────────────────────────────────────────

        /// <summary>
        /// Replace the platform implementation. For testing only.
        /// </summary>
        internal static void SetPlatform(ISeonPlatform platform) {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }
    }
}
