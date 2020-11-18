using UnityEngine;
using PoseAuthoring.Interaction;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private IGrabNotifier snapNotifier;

        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;

        private void Reset()
        {
            puppetHand = this.GetComponent<HandPuppet>();
        }

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