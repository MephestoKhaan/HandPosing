using UnityEngine;

namespace PoseAuthoring
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Provider")]
    public class HandProvider : ScriptableObject
    {
        [SerializeField]
        private HandGhost rightHand;
        [SerializeField]
        private HandGhost leftHand;

        public HandGhost GetHand(bool isRightHand)
        {
            return isRightHand ? rightHand : leftHand;
        }
    }
}