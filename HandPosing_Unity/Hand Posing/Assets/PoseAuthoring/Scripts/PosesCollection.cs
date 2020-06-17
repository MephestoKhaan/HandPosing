using OVRSimpleJSON;
using System.Collections.Generic;

using UnityEngine;

namespace PoseAuthoring
{
    [CreateAssetMenu(menuName = "PoseAuthoring/PosesCollection")]
    public class PosesCollection : ScriptableObject
    {
        [SerializeField]
        private List<HandPose> _snapPoses;

        public List<HandPose> SnapPoses
        {
            get
            {
                return _snapPoses;
            }
        }

        public void StorePoses(List<HandPose> poses)
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