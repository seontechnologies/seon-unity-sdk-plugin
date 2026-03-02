using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Seon {
    /// <summary>
    /// Dispatches actions to the Unity main thread.
    /// Native callbacks from iOS/Android arrive on background threads —
    /// this component ensures they execute on Unity's main thread.
    /// </summary>
    /// <remarks>
    /// Automatically created as a DontDestroyOnLoad GameObject when first accessed.
    /// </remarks>
    internal class SeonMainThreadDispatcher : MonoBehaviour {
        private static SeonMainThreadDispatcher _instance;
        private static int _mainThreadId;
        private readonly Queue<Action> _actionQueue = new Queue<Action>();

        /// <summary>
        /// Auto-create on the main thread at app startup, before any scene loads.
        /// This guarantees the dispatcher exists before any native callback could
        /// try to access it from a background thread.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInit() {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            if (_instance == null) {
                CreateDispatcher();
            }
        }

        internal static SeonMainThreadDispatcher Instance {
            get {
                // should never happen — AutoInit creates this at startup.
                EnsureInitialized();
                return _instance;
            }
        }

        /// <summary>
        /// Ensure the dispatcher GameObject exists. Call this before any
        /// async operation that will deliver callbacks on a background thread.
        /// </summary>
        internal static void EnsureInitialized() {
            if (_instance != null) {
                return;
            }

            // Creating Unity objects is only allowed on the main thread.
            // AutoInit (RuntimeInitializeOnLoadMethod) should have already
            // created the instance. If we get here from a background thread,
            // something went very wrong — log and bail rather than crash.
            if (!IsMainThread()) {
                Debug.LogError("[SeonSDK] EnsureInitialized called from a background thread " +
                    "before the dispatcher was created. Callbacks will not be delivered.");
                return;
            }

            Debug.LogWarning("[SeonSDK] Dispatcher was null — recreating. " +
                "This should not happen in normal operation.");
            CreateDispatcher();
        }

        private static bool IsMainThread() {
            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        private static void CreateDispatcher() {
            GameObject go = new GameObject("[SeonMainThreadDispatcher]");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SeonMainThreadDispatcher>();
        }

        /// <summary>
        /// Enqueue an action to be executed on the next Update cycle.
        /// </summary>
        internal void Enqueue(Action action) {
            if (action == null) {
                return;
            }
            lock (_actionQueue) {
                _actionQueue.Enqueue(action);
            }
        }

        private void Update() {
            // Drain queue under lock, then execute outside lock
            // to avoid blocking producers during callback execution.
            List<Action> pending = null;
            lock (_actionQueue) {
                if (_actionQueue.Count > 0) {
                    pending = new List<Action>(_actionQueue);
                    _actionQueue.Clear();
                }
            }
            if (pending != null) {
                foreach (var action in pending) {
                    try {
                        action.Invoke();
                    }
                    catch (Exception ex) {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private void OnDestroy() {
            _instance = null;
        }
    }
}
