using UnityEngine;

namespace PoseAuthoring.Adapters
{
    public class AnchorsUpdateNotifier : MonoBehaviour
    {
        public System.Action OnAnchorsUpdated;

        protected virtual void Update()
        {
            OnAnchorsUpdated?.Invoke();
        }
    }

}