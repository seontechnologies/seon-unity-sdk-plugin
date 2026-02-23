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
            string geo = "";
            if (GeolocationStatusCode.HasValue) {
                geo = ", GeoStatus=" + GeolocationStatusCode.Value;
            }
            return "SeonError(Code=" + Code + ", Message=\"" + Message + "\"" + geo + ")";
        }
    }

    /// <summary>
    /// Exception thrown by the SEON SDK for synchronous errors
    /// (e.g., calling StopBehaviourMonitoring without StartBehaviourMonitoring).
    /// </summary>
    public class SeonException : Exception
    {
        private readonly SeonError _error;

        /// <summary>
        /// The underlying SEON error.
        /// </summary>
        public SeonError Error
        {
            get { return _error; }
        }

        public SeonException(SeonError error) : base(GetErrorMessage(error))
        {
            if (error != null) {
                _error = error;
            } else {
                SeonError fallback = new SeonError();
                fallback.Code = -1;
                fallback.Message = "Unknown SEON error";
                _error = fallback;
            }
        }

        public SeonException(string message) : base(message)
        {
            SeonError error = new SeonError();
            error.Code = -1;
            error.Message = message;
            _error = error;
        }

        private static string GetErrorMessage(SeonError error)
        {
            if (error != null) {
                return error.Message;
            }
            return "Unknown SEON error";
        }
    }
}
