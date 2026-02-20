using System;

namespace Seon
{
    /// <summary>
    /// Main entry point for the SEON Device Intelligence SDK.
    /// Provides a unified C# API wrapping native iOS and Android implementations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call <see cref="Init"/> before any other method. The SDK delegates to
    /// platform-specific implementations selected at compile time:
    /// <list type="bullet">
    ///   <item><description>iOS: Obj-C bridge via DllImport</description></item>
    ///   <item><description>Android: JNI bridge via AndroidJavaObject</description></item>
    ///   <item><description>Editor: Mock stub returning dummy data</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// SeonSDK.Init("my-session-id");
    /// SeonSDK.SetLoggingEnabled(true);
    /// SeonSDK.GetFingerprint(
    ///     onSuccess: fp => Debug.Log(fp),
    ///     onError: err => Debug.LogError(err.Message)
    /// );
    /// </code>
    /// </example>
    public static class SeonSDK
    {
        private static ISeonPlatform _platform;
        private static bool _initialized;

        static SeonSDK()
        {
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
        /// Initialize the SDK with a session identifier.
        /// Must be called once before any other SDK method.
        /// </summary>
        /// <param name="sessionId">
        /// A unique identifier for this session. This value is included in the
        /// generated fingerprint and is used to correlate with Fraud API requests.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="sessionId"/> is null or empty.
        /// </exception>
        public static void Init(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentNullException(nameof(sessionId), "Session ID must not be null or empty.");

            _platform.Init(sessionId);
            _initialized = true;
        }

        /// <summary>
        /// Update the session ID after initialization.
        /// </summary>
        /// <param name="sessionId">New session identifier.</param>
        /// <exception cref="InvalidOperationException">Thrown if SDK is not initialized.</exception>
        public static void SetSessionId(string sessionId)
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(sessionId))
                throw new ArgumentNullException(nameof(sessionId));

            _platform.SetSessionId(sessionId);
        }

        /// <summary>
        /// Enable or disable SDK diagnostic logging.
        /// </summary>
        /// <param name="enabled">True to enable logging, false to disable.</param>
        public static void SetLoggingEnabled(bool enabled)
        {
            EnsureInitialized();
            _platform.SetLoggingEnabled(enabled);
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
        public static void SetDnsTimeout(int timeoutMs)
        {
            EnsureInitialized();
            _platform.SetDnsTimeout(timeoutMs);
        }

        /// <summary>
        /// Configure geolocation collection.
        /// Must be called before <see cref="GetFingerprint"/> or
        /// <see cref="StopBehaviourMonitoring"/> for location data to be included.
        /// </summary>
        /// <param name="config">Geolocation configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown if config is null.</exception>
        public static void SetGeolocationConfig(SeonGeolocationConfig config)
        {
            EnsureInitialized();
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _platform.SetGeolocationConfig(config);
        }

        /// <summary>
        /// Asynchronously compute the device fingerprint.
        /// </summary>
        /// <param name="onSuccess">
        /// Invoked on the Unity main thread with the base64-encoded fingerprint string.
        /// Use this value as the <c>session</c> property of your SEON Fraud API request.
        /// </param>
        /// <param name="onError">
        /// Invoked on the Unity main thread if fingerprinting fails.
        /// </param>
        /// <exception cref="InvalidOperationException">Thrown if SDK is not initialized.</exception>
        public static void GetFingerprint(Action<string> onSuccess, Action<SeonError> onError)
        {
            EnsureInitialized();
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            _platform.GetFingerprint(onSuccess, onError);
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
        /// <exception cref="InvalidOperationException">Thrown if SDK is not initialized.</exception>
        /// <exception cref="SeonException">Thrown if monitoring cannot be started.</exception>
        public static void StartBehaviourMonitoring()
        {
            EnsureInitialized();
            _platform.StartBehaviourMonitoring();
        }

        /// <summary>
        /// Stop behaviour monitoring and retrieve the enriched fingerprint.
        /// </summary>
        /// <param name="onSuccess">
        /// Invoked with the base64 fingerprint string that includes both device
        /// fingerprint data and behaviour analysis results.
        /// </param>
        /// <param name="onError">
        /// Invoked if stopping monitoring or fingerprint generation fails.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if SDK is not initialized or <see cref="StartBehaviourMonitoring"/>
        /// was not called first.
        /// </exception>
        public static void StopBehaviourMonitoring(Action<string> onSuccess, Action<SeonError> onError)
        {
            EnsureInitialized();
            if (onSuccess == null)
                throw new ArgumentNullException(nameof(onSuccess));
            if (onError == null)
                throw new ArgumentNullException(nameof(onError));

            _platform.StopBehaviourMonitoring(onSuccess, onError);
        }

        /// <summary>
        /// Reset the SDK state. After calling this, <see cref="Init"/> must be called again.
        /// Useful for session rotation.
        /// </summary>
        public static void Cleanup()
        {
            if (_initialized)
            {
                _platform.Cleanup();
                _initialized = false;
            }
        }

        // ── Internal ───────────────────────────────────────────────────

        /// <summary>
        /// Replace the platform implementation. For testing only.
        /// </summary>
        internal static void SetPlatform(ISeonPlatform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        private static void EnsureInitialized()
        {
            if (!_initialized)
                throw new InvalidOperationException(
                    "SeonSDK.Init() must be called before using any other SDK method.");
        }
    }
}
