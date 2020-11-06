using UnityEngine;

namespace PoseAuthoring
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Ghost Provider")]
    public class HandGhostProvider : ScriptableObject
    {
        [SerializeField]
        private HandGhost leftHand;
        [SerializeField]
        private HandGhost rightHand;

        public HandGhost GetHand(Handeness handeness)
        {
            return handeness == Handeness.Right ? rightHand : leftHand;
        }
    }
}