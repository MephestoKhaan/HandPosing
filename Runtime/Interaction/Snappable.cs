using HandPosing.SnapRecording;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HandPosing.Interaction
{
    /// <summary>
    /// Snappables are objects the hands can snap to using a Snapper. Internally it has a list
    /// of Snap Points that specify the positions and rotation of bones to gently held the object.
    /// Snap Points can be generated during Play-Mode using Hand-Tracking, but in order for the data
    /// to survive once Play-Mode is left, it is important to store it into a SnapPointsCollection.
    /// Then, the SnapPoints can be re-created forever in Edit-Mode from said collection and be modified.
    /// At that point the SnapPointsCollection is no longer needed.
    /// </summary>
    public class Snappable : MonoBehaviour
    {
        /// <summary>
        /// This ScriptableObject stores the SnapPoints generated at Play-Mode so it survives
        /// the Play-Edit cycle. 
        /// Create a collection and assign it even in Play Mode and make sure to store here the
        /// snap points, then restore it in Edit-Mode to be serialized.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory. Collection for storing generated Snap Points during Play-Mode, so they can be restored in Edit-Mode")]
        private SnapPointsCollection posesCollection;
        /// <summary>
        /// Gives references to the hand prototipes uses to represent the snap points. These are the
        /// static hands placed around the Snappable to visualize the different holding hand-poses.
        /// 
        /// Not mandatory.
        /// </summary>
        [SerializeField]
        [Tooltip("Not mandatory. Prototypes of the static hands (ghosts) that visualize holding poses")]
        private HandGhostProvider ghostProvider;

        [Space]
        /// <summary>
        /// Creates an Inspector button to store the current SnapPoints to the posesCollection. 
        /// Use it during Play-Mode.
        /// </summary>
        [InspectorButton("SaveToAsset")]
        public string StorePoses;
        /// <summary>
        /// Creates an Inspector button that restores the saved SnapPoints inn posesCollection. 
        /// Use it in Edit-Mode.
        /// </summary>
        [InspectorButton("LoadFromAsset")]
        public string LoadPoses;

        [Space]
        /// <summary>
        /// List of valid snap points in which the object can be held.
        /// </summary>
        [SerializeField]
        private List<BaseSnapPoint> snapPoints = new List<BaseSnapPoint>();

        /// <summary>
        /// Creates an Inspector button to remove the snapPoints collection, destroying its
        /// associated gameObjects.
        /// </summary>
        [InspectorButton("RemoveSnaps")]
        public string ClearSnapPoints;

        /// <summary>
        /// Find the Snap Point on this Snappabe with the best score to the given hand pose.
        /// </summary>
        /// <param name="userPose">The hand pose to be compared with this snappable.</param>
        /// <param name="bestHandPose">The most similar Hand Pose that snaps to the object, with its score.</param>
        /// <returns>The SnapPoint that is closer to the hand</returns>
        public BaseSnapPoint FindBestSnapPose(HandPose userPose, out ScoredHandPose bestHandPose)
        {
            BaseSnapPoint bestSnap = null;
            bestHandPose = ScoredHandPose.Null();
            foreach (var snapPose in this.snapPoints)
            {
                ScoredHandPose pose = snapPose.CalculateBestPose(userPose);
                if (pose.Score > bestHandPose.Score)
                {
                    bestSnap = snapPose;
                    bestHandPose = pose;
                }
            }
            return bestSnap;
        }


        public void LerpGripOffset(Pose pose, float weight, Transform handGrip)
        {
            Pose fromGrip = this.transform.GlobalPose(pose);
            Pose toGrip = handGrip.GetPose();
            Pose targetGrip = PoseUtils.Lerp(fromGrip, toGrip, weight);

            Pose inverseGrip = this.transform.RelativeOffset(handGrip);
            Pose targetPose = PoseUtils.Multiply(targetGrip, inverseGrip);
            this.transform.SetPose(targetPose);

        }

        /// <summary>
        /// Creates a new SnapPoint at the exact pose of a given hand. 
        /// Mostly used with Hand-Tracking at Play-Mode
        /// </summary>
        /// <param name="puppet">The user controlled hand.</param>
        /// <returns>The generated SnapPoint.</returns>
        public SnapPoint AddSnapPoint(HandPuppet puppet)
        {
            HandPose rawPose = puppet.TrackedPose(this.transform, true);
            SnapPoint snapPoint = GenerateSnapPoint();
            snapPoint.SetPose(rawPose, this.transform);
            snapPoint.LoadGhost(ghostProvider);

            return snapPoint;
        }

        /// <summary>
        /// Creates a new SnapPoint from the stored data.
        /// Mostly used to restore a SnapPoint that was stored during Play-Mode.
        /// </summary>
        /// <param name="data">The data of the SnapPoint.</param>
        /// <returns>The geerated SnapPoint.</returns>
        private SnapPoint LoadSnapPoint(SnapPointData data)
        {
            SnapPoint record = GenerateSnapPoint();
            record.LoadData(data, this.transform);
            record.LoadGhost(ghostProvider);
            return record;
        }

        /// <summary>
        /// Creates a default SnapPoint for this Snappable.
        /// </summary>
        /// <returns>An non-populated SnapPoint.</returns>
        private SnapPoint GenerateSnapPoint()
        {
            SnapPoint record = SnapPoint.Create(this.transform);
            this.snapPoints.Add(record);
            return record;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Load the SnapPoints from a Collection.
        /// 
        /// This method is called from a button in the Inspector and will load the posesCollection.
        /// </summary>
        private void LoadFromAsset()
        {
            if(this.posesCollection != null)
            {
                foreach (var handPose in this.posesCollection.SnapPoints)
                {
                    LoadSnapPoint(handPose);
                }
            }
        }

        /// <summary>
        /// Stores the Snappable SnapPoints to a SerializedObject (the empty object must be
        /// provided in the inspector or one will be auto-generated). First it translates the SnapPoints to a serializable
        /// form (SnapPointData).
        /// 
        /// This method is called from a button in the Inspector.
        /// </summary>
        private void SaveToAsset()
        {
            List<SnapPointData> savedPoses = new List<SnapPointData>();
            foreach (var snap in this.GetComponentsInChildren<SnapPoint>())
            {
                savedPoses.Add(snap.SaveData());
            }
            if(this.posesCollection == null)
            {
                GenerateAsset();
            }
            this.posesCollection?.StorePoses(savedPoses);
        }

        private void GenerateAsset()
        {
#if UNITY_EDITOR
            this.posesCollection = ScriptableObject.CreateInstance<SnapPointsCollection>();
            string parentDir = "Assets/SnapPointsCollection";
            if (!System.IO.Directory.Exists(parentDir))
            {
                System.IO.Directory.CreateDirectory(parentDir);
            }
            AssetDatabase.CreateAsset(this.posesCollection, $"{parentDir}/{this.name}_PoseCollection.asset");
            AssetDatabase.SaveAssets();
#endif
        }

        private void RemoveSnaps()
        {
            if (this.snapPoints != null)
            {
                foreach (var snapPoint in this.snapPoints)
                {
                    if(snapPoint != null)
                    {
                        snapPoint.DestroyImmediate();
                    }
                }
                this.snapPoints.Clear();
            }
        }
#endif
    }
}