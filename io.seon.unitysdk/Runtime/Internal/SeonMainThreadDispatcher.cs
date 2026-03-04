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
            DrainPendingQueue();
        }

        // Static fallback queue for actions enqueued before the dispatcher
        // GameObject exists (e.g. off-main-thread before AutoInit).
        private static readonly Queue<Action> _pendingQueue = new Queue<Action>();
        private static readonly object _pendingLock = new object();

        /// <summary>
        /// Enqueue an action to be executed on the next Update cycle.
        /// Safe to call from any thread, even before the dispatcher is created.
        /// </summary>
        internal static void Enqueue(Action action) {
            if (action == null) {
                return;
            }

            var instance = _instance;
            if (instance != null) {
                lock (instance._actionQueue) {
                    instance._actionQueue.Enqueue(action);
                }
                return;
            }

            // _instance is null — either pre-AutoInit or destroyed mid-play.
            // If we're on the main thread, re-create and drain pending queue.
            if (_mainThreadId != 0 && Thread.CurrentThread.ManagedThreadId == _mainThreadId) {
                CreateDispatcher();
                DrainPendingQueue();
                lock (_instance._actionQueue) {
                    _instance._actionQueue.Enqueue(action);
                }
            }
            else {
                // Off main thread or pre-AutoInit — buffer until ready.
                lock (_pendingLock) {
                    _pendingQueue.Enqueue(action);
                }
            }
        }

        private static void CreateDispatcher() {
            GameObject go = new GameObject("[SeonMainThreadDispatcher]");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SeonMainThreadDispatcher>();
        }

        /// <summary>
        /// Drain any buffered pending actions into the instance's action queue.
        /// Must be called from the main thread after _instance is set.
        /// </summary>
        private static void DrainPendingQueue() {
            lock (_pendingLock) {
                if (_pendingQueue.Count > 0) {
                    lock (_instance._actionQueue) {
                        while (_pendingQueue.Count > 0) {
                            _instance._actionQueue.Enqueue(_pendingQueue.Dequeue());
                        }
                    }
                }
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
