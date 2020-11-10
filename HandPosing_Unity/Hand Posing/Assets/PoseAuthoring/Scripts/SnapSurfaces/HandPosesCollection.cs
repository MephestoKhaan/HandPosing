using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring.PoseRecording
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Poses Collection")]
    public class HandPosesCollection : ScriptableObject
    {
        [SerializeField]
        private List<SnapPointData> _poses;

        public List<SnapPointData> Poses
        {
            get
            {
                return _poses;
            }
        }

        public void StorePoses(List<SnapPointData> poses)
        {
            _poses = poses;
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}