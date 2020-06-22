using OVRSimpleJSON;
using System.Collections.Generic;

using UnityEngine;

namespace PoseAuthoring
{
    [CreateAssetMenu(menuName = "PoseAuthoring/PosesCollection")]
    public class PosesCollection : ScriptableObject
    {
        [SerializeField]
        private List<HandSnapPose> _snapPoses;

        public List<HandSnapPose> SnapPoses
        {
            get
            {
                return _snapPoses;
            }
        }

        public void StorePoses(List<HandSnapPose> poses)
        {
            _snapPoses = poses;
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.Refresh();
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
#endif
        }
    }
}