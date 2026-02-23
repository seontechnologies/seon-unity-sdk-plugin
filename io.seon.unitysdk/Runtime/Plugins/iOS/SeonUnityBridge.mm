#import <SeonSDK/SEONFingerprint.h>

// ──────────────────────────────────────────────────────────────
// SeonUnityBridge.mm
// Objective-C++ bridge between Unity C# (via DllImport) and
// the SEON iOS SDK (SEONFingerprint). All functions are
// extern "C" to prevent name mangling.
// ──────────────────────────────────────────────────────────────

// Callback typedefs matching the C# delegates
typedef void (*FingerprintSuccessCallback)(const char *fingerprint);
typedef void (*FingerprintErrorCallback)(int code, const char *message);
typedef void (*GeoFailureCallback)(int code, const char *message,
                                   const char *fingerprint);

extern "C" {

// Config

void _SeonSetSessionId(const char *sessionId) {
  NSString *sid = [NSString stringWithUTF8String:sessionId];
  [[SEONFingerprint sharedManager] setSessionId:sid];
}

void _SeonSetLoggingEnabled(bool enabled) {
  [[SEONFingerprint sharedManager] setLoggingEnabled:enabled];
}

void _SeonSetGeolocationConfig(bool enabled, bool prefetchEnabled,
                               int maxCacheAgeSec, int serviceTimeoutMs) {
  SEONGeolocationConfig *geoConfig = [[SEONGeolocationConfig alloc] init];
  geoConfig.geolocationEnabled = enabled;
  geoConfig.prefetchEnabled = prefetchEnabled;
  geoConfig.maxGeoLocationCacheAgeSec = maxCacheAgeSec;
  geoConfig.geolocationServiceTimeoutMs = serviceTimeoutMs;
  [[SEONFingerprint sharedManager] setGeolocationConfig:geoConfig];
}

// Fingerprinting

void _SeonGetFingerprint(FingerprintSuccessCallback onSuccess,
                         FingerprintErrorCallback onError,
                         GeoFailureCallback onGeoFailure) {
  [[SEONFingerprint sharedManager] getFingerprintBase64:^(NSString *fingerprint,
                                                          NSError *error) {
    if (error != nil && fingerprint != nil) {
      // Geo failure: fingerprint succeeded but geolocation failed
      if (onGeoFailure) {
        onGeoFailure((int)error.code, [error.localizedDescription UTF8String],
                     [fingerprint UTF8String]);
      }
    } else if (error != nil) {
      // Real error: no fingerprint
      if (onError) {
        onError((int)error.code, [error.localizedDescription UTF8String]);
      }
    } else {
      // Full success
      if (onSuccess) {
        onSuccess([fingerprint UTF8String]);
      }
    }
  }];
}

// ── Behaviour Monitoring ────────────────────────────────────

int _SeonStartBehaviourMonitoring() {
  NSError *error = nil;
  error = [[SEONFingerprint sharedManager] startBehaviourMonitoring];
  return (error != nil) ? (int)error.code : 0;
}

void _SeonStopBehaviourMonitoring(FingerprintSuccessCallback onSuccess,
                                  FingerprintErrorCallback onError,
                                  GeoFailureCallback onGeoFailure) {
  [[SEONFingerprint sharedManager] stopBehaviourMonitoring:^(
                                       NSString *fingerprint, NSError *error) {
    if (error != nil && fingerprint != nil) {
      if (onGeoFailure) {
        onGeoFailure((int)error.code, [error.localizedDescription UTF8String],
                     [fingerprint UTF8String]);
      }
    } else if (error != nil) {
      if (onError) {
        onError((int)error.code, [error.localizedDescription UTF8String]);
      }
    } else {
      if (onSuccess) {
        onSuccess([fingerprint UTF8String]);
      }
    }
  }];
}

} // extern "C"
