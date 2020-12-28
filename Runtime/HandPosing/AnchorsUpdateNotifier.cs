using UnityEngine;

namespace HandPosing
{
    /// <summary>
    /// Utility to manually trigger the update of the hand puppet system.
    /// Since the puppet must run after the hands have been moved natively, implement this to enforce the order.
    /// </summary>
    public class AnchorsUpdateNotifier : MonoBehaviour
    {
        /// <summary>
        /// Event called every time the hands are updated.
        /// Could be triggered multiple times per frame.
        /// </summary>
        public System.Action OnAnchorsEveryUpdate;
        /// <summary>
        /// Event called the first time the hands are updated in a frame.
        /// </summary>
        public System.Action OnAnchorsFirstUpdate;

        protected virtual void Update()
        {
            OnAnchorsFirstUpdate?.Invoke();
            OnAnchorsEveryUpdate?.Invoke();
        }
    }

}