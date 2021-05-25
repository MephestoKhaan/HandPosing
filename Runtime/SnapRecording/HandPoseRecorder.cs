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

#if UNITY_INPUTSYSTEM
        [SerializeField]
        private string recordKey = "space";
#else
        [SerializeField]
        private KeyCode recordKeyCode = KeyCode.Space;
#endif

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


#if UNITY_INPUTSYSTEM
        private void Start()
        {
            UnityEngine.InputSystem.InputAction recordAction = new UnityEngine.InputSystem.InputAction("onRecordPose", binding: $"<Keyboard>/{recordKey}");
            recordAction.Enable();
            recordAction.performed += ctx => RecordPose();
        }
#else
        private void Update()
        {
            if(Input.GetKeyDown(recordKeyCode))
            {
                RecordPose();
            }
        }
#endif




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