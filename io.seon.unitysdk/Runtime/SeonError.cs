using System;

namespace Seon
{
    /// <summary>
    /// Represents an error returned by the SEON SDK.
    /// </summary>
    public class SeonError
    {
        /// <summary>
        /// Platform-specific error code.
        /// </summary>
        public int Code { get; set; }

        /// <summary>
        /// Human-readable error description.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Non-null when geolocation collection failed but the fingerprint
        /// was still generated successfully. Contains the geolocation-specific
        /// status code.
        /// </summary>
        /// <remarks>
        /// iOS codes: 1008-1013 (see SEON iOS SDK docs).
        /// Android codes: -1 to 6 (see SEON Android SDK docs).
        /// </remarks>
        public int? GeolocationStatusCode { get; set; }

        public override string ToString()
        {
            var geo = GeolocationStatusCode.HasValue
                ? $", GeoStatus={GeolocationStatusCode.Value}"
                : "";
            return $"SeonError(Code={Code}, Message=\"{Message}\"{geo})";
        }
    }

    /// <summary>
    /// Exception thrown by the SEON SDK for synchronous errors
    /// (e.g., calling StopBehaviourMonitoring without StartBehaviourMonitoring).
    /// </summary>
    public class SeonException : Exception
    {
        /// <summary>
        /// The underlying SEON error.
        /// </summary>
        public SeonError Error { get; }

        public SeonException(SeonError error) : base(error?.Message ?? "Unknown SEON error")
        {
            Error = error ?? new SeonError { Code = -1, Message = "Unknown SEON error" };
        }

        public SeonException(string message) : base(message)
        {
            Error = new SeonError { Code = -1, Message = message };
        }
    }
}
