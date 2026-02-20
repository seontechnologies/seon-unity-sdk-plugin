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

// Helper: convert NSString to a heap-allocated C string that C# can read.
// Unity will handle freeing the memory.
static char *MakeStringCopy(NSString *str) {
  if (str == nil) {
    return NULL;
  }
  const char *utf8 = [str UTF8String];
  if (utf8 == NULL) {
    return NULL;
  }
  char *copy = (char *)malloc(strlen(utf8) + 1);
  strcpy(copy, utf8);
  return copy;
}

extern "C" {

// ── Initialization ──────────────────────────────────────────

void _SeonInit(const char *sessionId) {
  NSString *sid = [NSString stringWithUTF8String:sessionId];
  [[SEONFingerprint sharedManager] setSessionId:sid];
}

void _SeonSetSessionId(const char *sessionId) {
  NSString *sid = [NSString stringWithUTF8String:sessionId];
  [[SEONFingerprint sharedManager] setSessionId:sid];
}

// ── Configuration ───────────────────────────────────────────

void _SeonSetLoggingEnabled(bool enabled) {
  [[SEONFingerprint sharedManager] setLoggingEnabled:enabled];
}

void _SeonSetGeolocationConfig(bool enabled, bool prefetchEnabled,
                               int maxCacheAgeSec, int serviceTimeoutMs) {
  SEONGeolocationConfig *config = [[SEONGeolocationConfig alloc] init];
  config.geolocationEnabled = enabled;
  config.prefetchEnabled = prefetchEnabled;
  config.maxGeoLocationCacheAgeSec = maxCacheAgeSec;
  config.geolocationServiceTimeoutMs = serviceTimeoutMs;
  [[SEONFingerprint sharedManager] setGeolocationConfig:config];
}

// ── Fingerprinting ──────────────────────────────────────────

void _SeonGetFingerprint(FingerprintSuccessCallback onSuccess,
                         FingerprintErrorCallback onError) {
  [[SEONFingerprint sharedManager] getFingerprintBase64:^(NSString *fingerprint,
                                                          NSError *error) {
    if (error != nil) {
      if (onError) {
        onError((int)error.code, MakeStringCopy(error.localizedDescription));
      }
    } else {
      if (onSuccess) {
        onSuccess(MakeStringCopy(fingerprint));
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
                                  FingerprintErrorCallback onError) {
  [[SEONFingerprint sharedManager] stopBehaviourMonitoring:^(
                                       NSString *fingerprint, NSError *error) {
    if (error != nil) {
      if (onError) {
        onError((int)error.code, MakeStringCopy(error.localizedDescription));
      }
    } else {
      if (onSuccess) {
        onSuccess(MakeStringCopy(fingerprint));
      }
    }
  }];
}

// ── Cleanup ─────────────────────────────────────────────────

void _SeonCleanup() {
  // SEONFingerprint is a singleton; reset session ID
  [[SEONFingerprint sharedManager] setSessionId:@""];
}

} // extern "C"
