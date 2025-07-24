using System;
using System.Collections.Generic;
using UnityEngine;

namespace UABS.Assets.Script.Misc.Threads
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _queue = new();

        public static void Enqueue(Action action)
        {
            lock (_queue)
            {
                _queue.Enqueue(action);
            }
        }

        void Update()
        {
            while (_queue.Count > 0)
            {
                Action action;
                lock (_queue)
                {
                    action = _queue.Dequeue();
                }
                action?.Invoke();
            }
        }
    }
}