using UnityEngine;
using HandPosing.Interaction;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// Extract the current pose of the user hand and generates a valid snap point in the
    /// nearest snappable.
    /// Typically be used in play-mode.
    /// </summary>
    public class HandPoseRecorder : MonoBehaviour
    {
        /// <summary>
        /// The user-puppeted hand.
        /// Used to extract the pose from.
        /// </summary>
        [SerializeField]
        [Tooltip("The user-puppeted hand.")]
        private HandPuppet puppetHand;
        /// <summary>
        /// Component implementing IGrabNotifier, to get the nearest object that can be grabbed
        /// </summary>
        [SerializeField]
        [Tooltip("Must implement IGrabNotifier")]
        private Component grabber;
        /// <summary>
        /// Key to trigger the recording event.
        /// It is recommended to be a big key since the recorder might be wearing a headset.
        /// </summary>
        [SerializeField]
        private KeyCode recordKey = KeyCode.Space;
        /// <summary>
        /// Create an Inspector button for manually triggering the pose recorer.
        /// </summary>
        [InspectorButton("RecordPose")]
        public string record;


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

        /// <summary>
        /// Finds the nearest object that can be snapped to and adds a new snap point to 
        /// it with the user hand representation.
        /// </summary>
        public void RecordPose()
        {
            Snappable snappable = _grabNotifier.FindClosestSnappable();
            snappable?.AddSnapPoint(puppetHand);
        }
    }
}