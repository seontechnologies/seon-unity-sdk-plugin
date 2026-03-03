using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Seon.Tests.Runtime
{
    /// <summary>
    /// PlayMode integration tests for SEON SDK.
    /// These tests validate actual SDK behavior on device or simulator.
    /// When running in the Editor, they exercise the editor stub.
    /// </summary>
    [TestFixture]
    public class SeonSDKPlayModeTests
    {
        [UnityTest]
        public IEnumerator SetSessionId_AndGetFingerprint_ReturnsNonEmptyString()
        {
            SeonSDK.SetSessionId("playmode-test-" + System.Guid.NewGuid());
            SeonSDK.SetLoggingEnabled(true);

            SeonFingerprintResult result = null;
            bool completed = false;

            SeonSDK.GetFingerprint(r =>
            {
                result = r;
                completed = true;
            });

            // Wait for callback (timeout after 10 seconds)
            float timeout = 10f;
            while (!completed && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(completed, "GetFingerprint callback was not invoked within timeout.");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess, result.Error != null ? $"GetFingerprint warning/error: [{result.Error.Code}] {result.Error.Message}" : "");
            Assert.IsNotNull(result.Fingerprint);
            Assert.IsNotEmpty(result.Fingerprint);

            Debug.Log($"[SeonSDK PlayMode Test] Fingerprint received ({result.Fingerprint.Length} chars)");
        }

        [UnityTest]
        public IEnumerator BehaviourMonitoring_StartWaitStop_ReturnsFingerprint()
        {
            SeonSDK.SetSessionId("playmode-behaviour-" + System.Guid.NewGuid());
            SeonSDK.SetLoggingEnabled(true);

            var startError = SeonSDK.StartBehaviourMonitoring();
            Assert.IsNull(startError, "StartBehaviourMonitoring should return null on success.");

            // Let the SDK collect some sensor data
            yield return new WaitForSeconds(2f);

            SeonFingerprintResult result = null;
            bool completed = false;

            SeonSDK.StopBehaviourMonitoring(r =>
            {
                result = r;
                completed = true;
            });

            float timeout = 10f;
            while (!completed && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(completed, "StopBehaviourMonitoring callback was not invoked within timeout.");
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess, result.Error != null ? $"StopBehaviourMonitoring warning/error: [{result.Error.Code}] {result.Error.Message}" : "");
            Assert.IsNotNull(result.Fingerprint);
            Assert.IsNotEmpty(result.Fingerprint);

            Debug.Log($"[SeonSDK PlayMode Test] Behaviour fingerprint received ({result.Fingerprint.Length} chars)");
        }

        [UnityTest]
        public IEnumerator GetFingerprint_WithGeolocation_ReturnsFingerprint()
        {
            SeonSDK.SetSessionId("playmode-geo-" + System.Guid.NewGuid());
            SeonSDK.SetLoggingEnabled(true);
            SeonSDK.SetGeolocationConfig(new SeonGeolocationConfig
            {
                Enabled = true,
                PrefetchEnabled = true,
                MaxCacheAgeSec = 300,
                ServiceTimeoutMs = 5000
            });

            SeonFingerprintResult result = null;
            bool completed = false;

            SeonSDK.GetFingerprint(r =>
            {
                result = r;
                completed = true;
            });

            float timeout = 15f;
            while (!completed && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(completed, "GetFingerprint with geo callback was not invoked within timeout.");
            // Note: geolocation may fail (permissions) but fingerprint should still succeed
            Assert.IsNotNull(result);
            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Fingerprint);
            Assert.IsNotEmpty(result.Fingerprint);

            Debug.Log($"[SeonSDK PlayMode Test] Geo fingerprint received ({result.Fingerprint.Length} chars)");
        }

        [UnityTest]
        public IEnumerator StopBehaviourMonitoring_WithoutStart_ReturnsError()
        {
            SeonSDK.SetSessionId("playmode-error-" + System.Guid.NewGuid());

            SeonFingerprintResult result = null;
            bool completed = false;

            SeonSDK.StopBehaviourMonitoring(r =>
            {
                result = r;
                completed = true;
            });

            float timeout = 5f;
            while (!completed && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(completed, "StopBehaviourMonitoring callback was not invoked.");
            Assert.IsNotNull(result, "Expected result object when StopBehaviourMonitoring called without Start.");
            Assert.IsFalse(result.IsSuccess, "Expected failure when StopBehaviourMonitoring called without Start.");
            Assert.IsNotNull(result.Error, "Expected non-null error for failure result.");

            Debug.Log($"[SeonSDK PlayMode Test] Expected error received: {result.Error}");
        }
    }
}
