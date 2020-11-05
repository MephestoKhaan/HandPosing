using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring.PoseVolumes
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Hand Poses Collection")]
    public class HandPosesCollection : ScriptableObject
    {
        [SerializeField]
        private List<HandPose> _poses;

        public List<HandPose> Poses
        {
            get
            {
                return _poses;
            }
        }

        public void StorePoses(List<HandPose> poses)
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