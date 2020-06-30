using UnityEngine;
using static PoseAuthoring.HandSnapPose;

namespace PoseAuthoring
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Provider")]
    public class HandProvider : ScriptableObject
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