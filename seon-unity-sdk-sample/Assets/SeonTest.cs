using UnityEngine;
using Seon;
public class SeonTest : MonoBehaviour
{
    void Start()
    {
        SeonSDK.SetSessionId("unity-simulator-test-" + System.Guid.NewGuid());
        SeonSDK.SetLoggingEnabled(true);
        SeonSDK.GetFingerprint(
            onSuccess: fp => Debug.Log($"✅ REAL Fingerprint: " + fp),
            onError: err => Debug.LogError($"❌ Error [{err.Code}]: {err.Message}")
        );
    }
}
