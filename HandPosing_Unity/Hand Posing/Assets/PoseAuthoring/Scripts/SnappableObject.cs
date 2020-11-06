using PoseAuthoring.PoseRecording;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class SnappableObject : MonoBehaviour
    {
        [SerializeField]
        private HandPosesCollection posesCollection;
        [SerializeField]
        private HandGhostProvider ghostProvider;

        [Space]
        [InspectorButton("SaveToAsset")]
        public string StorePoses;
        [InspectorButton("LoadFromAsset")]
        public string LoadPoses;

        [SerializeField]
        private List<SnapPoint> snapPoses = new List<SnapPoint>();

        public SnapPoint FindBestSnapPose(HandPose userPose, out ScoredHandPose bestHandPose)
        {
            SnapPoint bestSnap = null;
            bestHandPose = ScoredHandPose.Null();
            foreach (var snapPose in this.snapPoses)
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

        public SnapPoint AddSnapPoint(HandPuppet puppet)
        {
            HandPose pose = puppet.TrackedPose(this.transform, true);
            SnapPoint record = GenerateSnapPoint();
            record.SetPose(pose, this.transform);
            record.LoadGhost(ghostProvider);
            return record;
        }

        private SnapPoint LoadSnapPoint(SnapPointData data)
        {
            SnapPoint record = GenerateSnapPoint();
            record.LoadData(data, this.transform);
            record.LoadGhost(ghostProvider);
            return record;
        }

        private SnapPoint GenerateSnapPoint()
        {
            GameObject go = new GameObject("Snap Point");
            go.transform.SetParent(this.transform, false);
            SnapPoint record = go.AddComponent<SnapPoint>();
            this.snapPoses.Add(record);
            return record;
        }

#if UNITY_EDITOR
        private void LoadFromAsset()
        {
            if(posesCollection != null)
            {
                foreach (var handPose in posesCollection.Poses)
                {
                    LoadSnapPoint(handPose);
                }
            }
        }

        private void SaveToAsset()
        {
            List<SnapPointData> savedPoses = new List<SnapPointData>();
            foreach (var snap in this.GetComponentsInChildren<SnapPoint>())
            {
                savedPoses.Add(snap.SaveData());
            }
            posesCollection.StorePoses(savedPoses);
        }
#endif
    }
}