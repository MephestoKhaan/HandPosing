using UnityEngine;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// Holds references to the prefabs for Ghost-Hands, so they can be instantiated
    /// in runtime to represent static poses.
    /// </summary>
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Ghost Provider")]
    public class HandGhostProvider : ScriptableObject
    {
        /// <summary>
        /// The prototype for the left hand ghost.
        /// </summary>
        [SerializeField]
        private HandGhost leftHand;
        /// <summary>
        /// The prototype for the right hand ghost.
        /// </summary>
        [SerializeField]
        private HandGhost rightHand;

        /// <summary>
        /// Helper method to obtain the prototypes
        /// The result is to be instaned, not used directly.
        /// </summary>
        /// <param name="handeness">The desired handeness of the ghost prefab</param>
        /// <returns>A Ghost prefab</returns>
        public HandGhost GetHand(Handeness handeness)
        {
            return handeness == Handeness.Right ? rightHand : leftHand;
        }
    }
}