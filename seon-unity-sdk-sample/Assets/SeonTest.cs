using UnityEngine;
using Seon;
public class SeonTest : MonoBehaviour
{
    void Start()
    {
        SeonSDK.SetSessionId("unity-simulator-test-" + System.Guid.NewGuid());
        SeonSDK.SetLoggingEnabled(true);
        SeonSDK.SetDnsTimeout(0);
        SeonGeolocationConfig config = new SeonGeolocationConfig(enabled: true, prefetchEnabled: true, maxCacheAgeSec: 300, serviceTimeoutMs: 5000);
        SeonSDK.SetGeolocationConfig(config);
        SeonSDK.GetFingerprint((result) =>
        {
            if (result != null)
            {
                if (result.Error != null)
                {
                    Debug.LogError("Error getting fingerprint: " + result.Error);
                    Debug.LogError("Geolocation status code: " + result.Error.GeolocationStatusCode);
                }
                if (result.Fingerprint != null)
                {
                    Debug.Log("Fingerprint: " + result.Fingerprint);
                }
            }

        });
    }
}
