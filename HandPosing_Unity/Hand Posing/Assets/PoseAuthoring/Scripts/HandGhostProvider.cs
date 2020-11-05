using System.Linq;
using UnityEngine;
using static PoseAuthoring.HandPose;

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

        #region singleton
        static HandGhostProvider _instance = null;
        public static HandGhostProvider Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.FindObjectsOfTypeAll<HandGhostProvider>().FirstOrDefault();
                }
                return _instance;
            }
        }
        #endregion
    }
}