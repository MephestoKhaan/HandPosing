using UnityEngine;

namespace HandPosing
{
    public class AnchorsUpdateNotifier : MonoBehaviour
    {
        public System.Action OnAnchorsEveryUpdate;
        public System.Action OnAnchorsFirstUpdate;

        protected virtual void Update()
        {
            OnAnchorsFirstUpdate?.Invoke();
            OnAnchorsEveryUpdate?.Invoke();
        }
    }

}