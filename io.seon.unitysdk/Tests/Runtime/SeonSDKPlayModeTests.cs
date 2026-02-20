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
        [SetUp]
        public void SetUp()
        {
            // Each test starts with a clean state
            SeonSDK.Cleanup();
        }

        [TearDown]
        public void TearDown()
        {
            SeonSDK.Cleanup();
        }

        [UnityTest]
        public IEnumerator Init_AndGetFingerprint_ReturnsNonEmptyString()
        {
            SeonSDK.Init("playmode-test-" + System.Guid.NewGuid());
            SeonSDK.SetLoggingEnabled(true);

            string fingerprint = null;
            SeonError error = null;
            bool completed = false;

            SeonSDK.GetFingerprint(
                onSuccess: fp =>
                {
                    fingerprint = fp;
                    completed = true;
                },
                onError: err =>
                {
                    error = err;
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
            Assert.IsNull(error, error != null ? $"GetFingerprint failed: [{error.Code}] {error.Message}" : "");
            Assert.IsNotNull(fingerprint);
            Assert.IsNotEmpty(fingerprint);

            Debug.Log($"[SeonSDK PlayMode Test] Fingerprint received ({fingerprint.Length} chars)");
        }

        [UnityTest]
        public IEnumerator BehaviourMonitoring_StartWaitStop_ReturnsFingerprint()
        {
            SeonSDK.Init("playmode-behaviour-" + System.Guid.NewGuid());
            SeonSDK.SetLoggingEnabled(true);

            SeonSDK.StartBehaviourMonitoring();

            // Let the SDK collect some sensor data
            yield return new WaitForSeconds(2f);

            string fingerprint = null;
            SeonError error = null;
            bool completed = false;

            SeonSDK.StopBehaviourMonitoring(
                onSuccess: fp =>
                {
                    fingerprint = fp;
                    completed = true;
                },
                onError: err =>
                {
                    error = err;
                    completed = true;
                });

            float timeout = 10f;
            while (!completed && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(completed, "StopBehaviourMonitoring callback was not invoked within timeout.");
            Assert.IsNull(error, error != null ? $"StopBehaviourMonitoring failed: [{error.Code}] {error.Message}" : "");
            Assert.IsNotNull(fingerprint);
            Assert.IsNotEmpty(fingerprint);

            Debug.Log($"[SeonSDK PlayMode Test] Behaviour fingerprint received ({fingerprint.Length} chars)");
        }

        [UnityTest]
        public IEnumerator GetFingerprint_WithGeolocation_ReturnsFingerprint()
        {
            SeonSDK.Init("playmode-geo-" + System.Guid.NewGuid());
            SeonSDK.SetLoggingEnabled(true);
            SeonSDK.SetGeolocationConfig(new SeonGeolocationConfig
            {
                Enabled = true,
                PrefetchEnabled = true,
                MaxCacheAgeSec = 300,
                ServiceTimeoutMs = 5000
            });

            string fingerprint = null;
            SeonError error = null;
            bool completed = false;

            SeonSDK.GetFingerprint(
                onSuccess: fp =>
                {
                    fingerprint = fp;
                    completed = true;
                },
                onError: err =>
                {
                    error = err;
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
            Assert.IsNotNull(fingerprint);
            Assert.IsNotEmpty(fingerprint);

            Debug.Log($"[SeonSDK PlayMode Test] Geo fingerprint received ({fingerprint.Length} chars)");
        }

        [UnityTest]
        public IEnumerator StopBehaviourMonitoring_WithoutStart_ReturnsError()
        {
            SeonSDK.Init("playmode-error-" + System.Guid.NewGuid());

            SeonError error = null;
            bool completed = false;

            SeonSDK.StopBehaviourMonitoring(
                onSuccess: _ =>
                {
                    completed = true;
                },
                onError: err =>
                {
                    error = err;
                    completed = true;
                });

            float timeout = 5f;
            while (!completed && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(completed, "StopBehaviourMonitoring callback was not invoked.");
            Assert.IsNotNull(error, "Expected error when StopBehaviourMonitoring called without Start.");

            Debug.Log($"[SeonSDK PlayMode Test] Expected error received: {error}");
        }
    }
}
