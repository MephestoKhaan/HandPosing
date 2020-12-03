using UnityEngine;

namespace HandPosing.SnapRecording
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

        public Quaternion MirrorRotationOffset(Handeness mirrorHand)
        {
            Transform rightGrip = rightHand.GetComponent<HandPuppet>().Grip;
            Transform leftGrip = leftHand.GetComponent<HandPuppet>().Grip;

            if(mirrorHand == Handeness.Left)
            {
                return (leftGrip.rotation) * Quaternion.Inverse(rightGrip.rotation);
            }
            else
            {
                return (rightGrip.rotation) * Quaternion.Inverse(leftGrip.rotation);
            }
        }
    }
}