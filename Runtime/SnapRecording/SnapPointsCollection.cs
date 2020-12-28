using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.SnapRecording
{
    /// <summary>
    /// A collection of SnapPoints Data, to be used to store the information of a Snappable
    /// so it survives Play-Mode Edit-Mode cycles. 
    /// 
    /// Use this to store information once in Play-Mode (where Hand-tracking can be used) 
    /// and then restore it forever at Edit-time.
    /// </summary>
    [CreateAssetMenu(menuName = "PoseAuthoring/Snap Points Collection")]
    public class SnapPointsCollection : ScriptableObject
    {
        /// <summary>
        /// The data-only version of the SnapPoints to be restored.
        /// Do not modify this manually here unless you are sure of what you are doing, instead
        /// reload it at Edit-Mode and use the provided tools at the SnapPoint.
        /// </summary>
        [SerializeField]
        [Tooltip("Do not modify this manually unless you are sure! Instead load the SnapPoints at the Snappable and use the tools provided.")]
        private List<SnapPointData> _snapPoints;

        /// <summary>
        /// General getter for the data-only version of the SnapPoints to be restored.
        /// </summary>
        public List<SnapPointData> SnapPoints
        {
            get
            {
                return _snapPoints;
            }
        }

        /// <summary>
        /// Register all the data into the Asset Database so it survives the Play-Mode shutdown.
        /// </summary>
        /// <param name="snapPoints"></param>
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