# SEON Device Intelligence SDK for Unity

Unity plugin wrapping [SEON](https://seon.io)'s native iOS and Android Device Intelligence SDKs into a unified C# API.

## Requirements

| Platform    | Minimum                                                                                              |
| ----------- | ---------------------------------------------------------------------------------------------------- |
| **Unity**   | 2022.3 LTS (Unity 6 LTS recommended)                                                                 |
| **iOS**     | 15.0+                                                                                                |
| **Android** | API 21+ (Android 5.0)                                                                                |
| **EDM4U**   | [External Dependency Manager](https://github.com/googlesamples/unity-jar-resolver) must be installed |

## Quick Start

### 1. Install the Plugin

Add the following to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "io.seon.unitysdk": "https://github.com/seontechnologies/seon-unity-sdk.git",
    "com.google.external-dependency-manager": "1.2.187"
  },
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.google.external-dependency-manager"]
    }
  ]
}
```

> **Note:** Merge the `dependencies` and `scopedRegistries` entries into your existing `manifest.json` — don't replace the entire file. The scoped registry entry tells Unity where to find EDM4U, which is required for resolving the native iOS (CocoaPods) and Android (Gradle/Maven) SEON SDK dependencies.

**Alternative — `.unitypackage`:**
Download from [Releases](https://github.com/seontechnologies/seon-unity-sdk/releases) and import. You'll also need to install [EDM4U](https://github.com/googlesamples/unity-jar-resolver) manually.

### 3. Integrate

```csharp
using Seon;
using UnityEngine;

public class FraudCheck : MonoBehaviour
{
    void Start()
    {
        SeonSDK.Init("my-session-id");
        SeonSDK.SetLoggingEnabled(true);

        SeonSDK.GetFingerprint(
            onSuccess: fingerprint =>
            {
                Debug.Log($"Fingerprint: {fingerprint}");
                // Send this as the 'session' property of your Fraud API request
            },
            onError: error =>
            {
                Debug.LogError($"Error [{error.Code}]: {error.Message}");
            }
        );
    }
}
```

## API Reference

### `SeonSDK` (Static Class)

| Method                                                       | Description                                              |
| ------------------------------------------------------------ | -------------------------------------------------------- |
| `Init(string sessionId)`                                     | Initialize SDK. **Required before any other call.**      |
| `SetSessionId(string sessionId)`                             | Update session ID after init.                            |
| `SetLoggingEnabled(bool enabled)`                            | Toggle diagnostic logging.                               |
| `SetDnsTimeout(int timeoutMs)`                               | DNS timeout in ms. Android only. Pass `0` to skip DNS.   |
| `SetGeolocationConfig(SeonGeolocationConfig)`                | Configure geolocation. Call before `GetFingerprint`.     |
| `GetFingerprint(Action<string>, Action<SeonError>)`          | Async device fingerprint (base64 string).                |
| `StartBehaviourMonitoring()`                                 | Start monitoring sensor data for suspicious activity.    |
| `StopBehaviourMonitoring(Action<string>, Action<SeonError>)` | Stop monitoring, returns enriched fingerprint.           |
| `Cleanup()`                                                  | Reset SDK state. `Init` must be called again after this. |

### `SeonGeolocationConfig`

| Property           | Type   | Default | Description                       |
| ------------------ | ------ | ------- | --------------------------------- |
| `Enabled`          | `bool` | `false` | Enable geolocation collection.    |
| `PrefetchEnabled`  | `bool` | `false` | Pre-fetch location on init.       |
| `MaxCacheAgeSec`   | `int`  | `600`   | Max age of cached location (sec). |
| `ServiceTimeoutMs` | `int`  | `3000`  | Location service timeout (ms).    |

### `SeonError`

| Property                | Type     | Description                                       |
| ----------------------- | -------- | ------------------------------------------------- |
| `Code`                  | `int`    | Platform-specific error code.                     |
| `Message`               | `string` | Human-readable description.                       |
| `GeolocationStatusCode` | `int?`   | Non-null if geo failed but fingerprint succeeded. |

## Behaviour Monitoring

```csharp
SeonSDK.Init("session-id");

// Start monitoring at the beginning of a user journey
SeonSDK.StartBehaviourMonitoring();

// ... user interacts with the app for a few seconds ...

// Stop and get enriched fingerprint
SeonSDK.StopBehaviourMonitoring(
    onSuccess: enrichedFingerprint => { /* includes behaviour signals */ },
    onError: error => { /* handle error */ }
);
```

> **Note:** The behaviour analysis needs at least a few seconds of data collection to be effective. Detections include `possible_automation`, `possible_device_farm`, `possible_vishing`, and `possible_cloud_device`.

## Geolocation

> **Important:** Your app is responsible for requesting location permissions from the user. The SDK does not request permissions.

```csharp
// Request location permissions from the user first (platform-specific)

SeonSDK.SetGeolocationConfig(new SeonGeolocationConfig
{
    Enabled = true,
    PrefetchEnabled = true,
    ServiceTimeoutMs = 5000
});

SeonSDK.GetFingerprint(
    onSuccess: fp => { /* fingerprint enriched with location data */ },
    onError: err => { /* handle error */ }
);
```

## iOS Build Configuration

The SEON Build Config controls Xcode post-build modifications. A default config asset is auto-created when needed. Open it via **Window → SEON → Build Config**.

Options you can enable:

- Add `CoreLocation.framework`
- Set `NSLocationWhenInUseUsageDescription` in Info.plist

By default, post-build processing is **enabled** with geolocation features **disabled** — no Xcode modifications are made unless you explicitly enable them.

You can also configure these manually in Xcode.

## Android ProGuard / R8

ProGuard rules are bundled at `Runtime/Plugins/Android/proguard-seon.pro`. When code minification is enabled, add this file to your ProGuard configuration or ensure it's picked up automatically.

## Editor Support

The SDK returns mock fingerprints in the Unity Editor so you can test your integration flow without deploying to a device.

## License

MIT
