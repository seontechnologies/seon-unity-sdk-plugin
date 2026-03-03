namespace Seon
{
    /// <summary>
    /// Represents the outcome of a fingerprinting operation.
    /// </summary>
    public class SeonFingerprintResult
    {
        /// <summary>
        /// Base64-encoded fingerprint. Present for success and partial success.
        /// </summary>
        public string Fingerprint { get; }

        /// <summary>
        /// Optional error details. For geolocation partial success this contains
        /// geolocation warning metadata with <see cref="SeonError.GeolocationStatusCode"/>
        /// while <see cref="Fingerprint"/> is still present.
        /// </summary>
        public SeonError Error { get; }

        /// <summary>
        /// True when a fingerprint was generated.
        /// </summary>
        public bool IsSuccess => !string.IsNullOrEmpty(Fingerprint);

        public SeonFingerprintResult(string fingerprint, SeonError error = null)
        {
            Fingerprint = fingerprint;
            Error = error;
        }
    }
}
