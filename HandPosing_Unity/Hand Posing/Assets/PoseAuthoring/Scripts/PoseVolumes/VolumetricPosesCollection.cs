using System;
using System.Collections.Generic;

using UnityEngine;

namespace PoseAuthoring.PoseVolumes
{
    [CreateAssetMenu(menuName = "PoseAuthoring/Volumetric Poses Collection")]
    public class VolumetricPosesCollection : ScriptableObject
    {
        [SerializeField]
        private List<VolumetricPose> _poses;

        public List<VolumetricPose> Poses
        {
            get
            {
                return _poses;
            }
        }

        public void StorePoses(List<VolumetricPose> poses)
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