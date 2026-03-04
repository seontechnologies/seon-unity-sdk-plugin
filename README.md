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
    "io.seon.unitysdk": "https://github.com/seontechnologies/seon-unity-sdk-plugin.git?path=io.seon.unitysdk",
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

> **Unity 6+:** You may see a "Missing Signature" popup — this is normal for third-party packages. Click **Close** to dismiss.

**Alternative — `.unitypackage`:**
Download from [Releases](https://github.com/seontechnologies/seon-unity-sdk-plugin/releases) and import. You'll also need to install [EDM4U](https://github.com/googlesamples/unity-jar-resolver) manually.

### 2. Integrate

```csharp
using Seon;
using UnityEngine;

public class FraudCheck : MonoBehaviour
{
    void Start()
    {
        SeonSDK.SetSessionId("my-session-id");
        // Optional: Set logging enabled for debugging purposes
        // SeonSDK.SetLoggingEnabled(true);

        SeonSDK.GetFingerprint(result =>
        {
            if (!result.IsSuccess)
            {
                Debug.LogError($"Error [{result.Error.Code}]: {result.Error.Message}");
                if (result.Error.GeolocationStatusCode.HasValue)
                {
                    Debug.LogWarning($"Geo status: {result.Error.GeolocationStatusCode}");
                }
                return;
            }

            Debug.Log($"Fingerprint: {result.Fingerprint}");
            // Send this as the 'session' property of your Fraud API request

            // Note: Even if IsSuccess is true, Geolocation might have thrown a warning if you have enabled it via SeonGeolocationConfig
            if (result.Error != null)
            {
                Debug.LogWarning($"Geolocation warning [{result.Error.GeolocationStatusCode}]: {result.Error.Message}");
            }
        });
    }
}
```

## API Reference

### `SeonSDK` (Static Class)

| Method                                                   | Description                                                   |
| -------------------------------------------------------- | ------------------------------------------------------------- |
| `SetSessionId(string sessionId)`                         | Set session identifier. **Required before `GetFingerprint`.** |
| `GetSessionId()`                                         | Retrieve the current global session identifier.               |
| `SetLoggingEnabled(bool enabled)`                        | Toggle diagnostic logging.                                    |
| `SetDnsTimeout(int timeoutMs)`                           | DNS resolution timeout in ms. Android only.                   |
| `SetGeolocationConfig(SeonGeolocationConfig)`            | Configure geolocation. Call before `GetFingerprint`.          |
| `GetFingerprint(Action<SeonFingerprintResult>)`          | Async fingerprint result (success / failure / partial).       |
| `StartBehaviourMonitoring()`                             | Start monitoring sensor data. Returns `SeonError` if failed.  |
| `StopBehaviourMonitoring(Action<SeonFingerprintResult>)` | Async behaviour fingerprint result (typed).                   |

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

### `SeonFingerprintResult`

| Property      | Type        | Description                                                                  |
| ------------- | ----------- | ---------------------------------------------------------------------------- |
| `Fingerprint` | `string`    | Base64 fingerprint. Set for full and partial success.                        |
| `Error`       | `SeonError` | Contains failure details, or geolocation warning details on partial success. |
| `IsSuccess`   | `bool`      | True when `Fingerprint` is present.                                          |

## Behaviour Monitoring

```csharp
SeonSDK.SetSessionId("session-id");

// Start monitoring at the beginning of a user journey
var startError = SeonSDK.StartBehaviourMonitoring();
if (startError != null)
{
    Debug.LogError($"Start monitoring failed [{startError.Code}]: {startError.Message}");
    return;
}

// Stop and get enriched fingerprint
SeonSDK.StopBehaviourMonitoring(result =>
{
    if (!result.IsSuccess)
    {
        Debug.LogError($"Behaviour error [{result.Error.Code}]: {result.Error.Message}");
        return;
    }

    var enrichedFingerprint = result.Fingerprint;
    // includes behaviour signals

    if (result.Error != null)
    {
        // geolocation warning details (e.g. user denied permissions mid-session)
    }
});
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

SeonSDK.GetFingerprint(result =>
{
    if (!result.IsSuccess)
    {
        // handle error
        return;
    }

    var fp = result.Fingerprint; // fingerprint enriched with location data
    if (result.Error != null)
    {
        // geolocation warning is available at result.Error.GeolocationStatusCode
    }
});
```

## iOS Build Configuration

The SEON Build Config controls Xcode post-build modifications. A default config asset is auto-created when needed. Open it via **Window → SEON → Build Config**.

The SeonSDK xcframework is **always embedded** (required for runtime linking). The config controls optional geolocation-related modifications:

- Add `CoreLocation.framework`
- Set `NSLocationWhenInUseUsageDescription` in Info.plist

By default, these geolocation features are **disabled** — enable them in the config if your integration uses geolocation.

You can also configure these manually in Xcode.

## Editor Support

The SDK returns mock fingerprints in the Unity Editor so you can test your integration flow without deploying to a device.

## Sending the Fingerprint to SEON

The fingerprint returned by `GetFingerprint` should be sent **from your backend server** to SEON's Fraud API as the `session` property. Never call the Fraud API directly from the client — your API key must remain secret.

For full integration details, see the official SEON documentation:

- [Device Intelligence Integration Guide](https://docs.seon.io/integration/device-intelligence) — end-to-end walkthrough of collecting and submitting fingerprints
- [Fraud API Reference](https://docs.seon.io/api-reference/fraud-api) — request/response format, fields, and scoring

## Native SDK Documentation

This Unity SDK is a wrapper around the native SEON mobile SDKs. For extensive details on specific error codes, geolocation status codes, and edge cases, please refer to the official SEON native documentation:

- [SEON iOS SDK Documentation](https://docs.seon.io/api-reference/device-fingerprinting/ios-sdk)
- [SEON Android SDK Documentation](https://docs.seon.io/api-reference/device-fingerprinting/android-sdk)
