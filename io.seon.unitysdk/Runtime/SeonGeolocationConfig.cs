namespace Seon
{
    /// <summary>
    /// Configuration for geolocation data collection.
    /// Pass an instance to <see cref="SeonSDK.SetGeolocationConfig"/> before calling
    /// <see cref="SeonSDK.GetFingerprint"/> or <see cref="SeonSDK.StopBehaviourMonitoring"/>.
    /// </summary>
    public class SeonGeolocationConfig
    {
        /// <summary>
        /// Enable or disable geolocation collection. Default: <c>false</c>.
        /// </summary>
        /// <remarks>
        /// The integrating app is responsible for requesting location permissions
        /// from the user before enabling this feature.
        /// </remarks>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Pre-fetch a valid location immediately when the SDK is initialized.
        /// Default: <c>false</c>.
        /// </summary>
        /// <remarks>
        /// When enabled, the SDK starts resolving the device location as soon as
        /// the Seon object is created, improving response time for subsequent
        /// fingerprint calls.
        /// </remarks>
        public bool PrefetchEnabled { get; set; } = false;

        /// <summary>
        /// Maximum allowed age of a cached location in seconds.
        /// Default: <c>600</c>.
        /// </summary>
        public int MaxCacheAgeSec { get; set; } = 600;

        /// <summary>
        /// Maximum time in milliseconds to wait for a valid location.
        /// Default: <c>3000</c>.
        /// </summary>
        public int ServiceTimeoutMs { get; set; } = 3000;
    }
}
