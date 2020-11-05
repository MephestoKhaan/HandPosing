using UnityEngine;

using Grabber = Interaction.Grabber;
using Grabbable = Interaction.Grabbable;

namespace PoseAuthoring
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private Grabber grabber;

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
            Grabbable grabbable = grabber.FindClosestGrabbable().Item1;
            grabbable?.Snappable?.AddPose(puppetHand);
        }
    }
}