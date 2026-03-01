#import <SeonSDK/SEONFingerprint.h>

// ──────────────────────────────────────────────────────────────
// Bridge between C# and the SEON iOS SDK
// Exposes C functions with C linkage that can be called from C# via DllImport.
// ──────────────────────────────────────────────────────────────

// Callback typedefs matching the C# delegates
typedef void (*FingerprintSuccessCallback)(const char *fingerprint);
typedef void (*FingerprintErrorCallback)(int code, const char *message);
typedef void (*GeoFailureCallback)(int code, const char *message,
                                   const char *fingerprint);

extern "C" {

// Config

void _SeonSetSessionId(const char *sessionId) {
  [[SEONFingerprint sharedManager]
      setSessionId:[NSString stringWithUTF8String:sessionId]];
}

const char *_SeonGetSessionId() {
  NSString *sessionId = [[SEONFingerprint sharedManager] sessionId];
  if (sessionId == nil) {
    return NULL;
  }
  return strdup([sessionId UTF8String]);
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
}
