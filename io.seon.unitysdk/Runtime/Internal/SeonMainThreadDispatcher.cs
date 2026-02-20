using System;
using System.Collections.Generic;
using UnityEngine;

namespace Seon
{
    /// <summary>
    /// Dispatches actions to the Unity main thread.
    /// Native callbacks from iOS/Android arrive on background threads —
    /// this component ensures they execute on Unity's main thread.
    /// </summary>
    /// <remarks>
    /// Automatically created as a DontDestroyOnLoad GameObject when first accessed.
    /// </remarks>
    internal class SeonMainThreadDispatcher : MonoBehaviour
    {
        private static SeonMainThreadDispatcher _instance;
        private static readonly object Lock = new object();
        private readonly Queue<Action> _actionQueue = new Queue<Action>();

        internal static SeonMainThreadDispatcher Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (Lock)
                    {
                        if (_instance == null)
                        {
                            var go = new GameObject("[SeonMainThreadDispatcher]");
                            go.hideFlags = HideFlags.HideAndDontSave;
                            DontDestroyOnLoad(go);
                            _instance = go.AddComponent<SeonMainThreadDispatcher>();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Enqueue an action to be executed on the next Update cycle.
        /// </summary>
        internal void Enqueue(Action action)
        {
            if (action == null) return;
            lock (_actionQueue)
            {
                _actionQueue.Enqueue(action);
            }
        }

        private void Update()
        {
            lock (_actionQueue)
            {
                while (_actionQueue.Count > 0)
                {
                    var action = _actionQueue.Dequeue();
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            _instance = null;
        }
    }
}
