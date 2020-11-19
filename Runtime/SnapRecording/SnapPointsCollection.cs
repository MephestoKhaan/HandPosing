using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Snap Points Collection")]
    public class SnapPointsCollection : ScriptableObject
    {
        [SerializeField]
        private List<SnapPointData> _snapPoints;

        public List<SnapPointData> SnapPoints
        {
            get
            {
                return _snapPoints;
            }
        }

        public void StorePoses(List<SnapPointData> snapPoints)
        {
            _snapPoints = snapPoints;
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}