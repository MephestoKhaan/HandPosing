using UnityEngine;
using PoseAuthoring.Adapters;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private GrabNotifier snapNotifier;

        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;
        
        private void Update()
        {
            if(Input.GetKeyDown(recordKey))
            {
                RecordPose();
            }
        }

        public void RecordPose()
        {
            SnappableObject snappable = snapNotifier.FindClosestSnappable();
            snappable?.AddSnapPoint(puppetHand);
        }
    }
}