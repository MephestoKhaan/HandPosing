using UnityEngine;

namespace PoseAuthoring
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Provider")]
    public class HandProvider : ScriptableObject
    {
        public HandPuppet rightHand;
        public HandPuppet leftHand;
    }
}