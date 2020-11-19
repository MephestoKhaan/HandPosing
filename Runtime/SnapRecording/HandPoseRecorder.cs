using UnityEngine;
using HandPosing.Interaction;

namespace HandPosing.SnapRecording
{
    public class HandPoseRecorder : MonoBehaviour
    {
        [SerializeField]
        private HandPuppet puppetHand;
        [SerializeField]
        private Component grabber;

        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;

        private IGrabNotifier _grabNotifier;

        private void Reset()
        {
            puppetHand = this.GetComponent<HandPuppet>();
            grabber = this.GetComponent<IGrabNotifier>() as Component;
        }

        private void Awake()
        {
            _grabNotifier = grabber as IGrabNotifier;
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
            Snappable snappable = _grabNotifier.FindClosestSnappable();
            snappable?.AddSnapPoint(puppetHand);
        }
    }
}