using HandPosing.SnapRecording;
using System.Collections.Generic;
using UnityEngine;

namespace HandPosing.Interaction
{
    public class Snappable : MonoBehaviour
    {
        [SerializeField]
        private SnapPointsCollection posesCollection;
        [SerializeField]
        private HandGhostProvider ghostProvider;

        [Space]
        [InspectorButton("SaveToAsset")]
        public string StorePoses;
        [InspectorButton("LoadFromAsset")]
        public string LoadPoses;

        [Space]
        [SerializeField]
        private List<SnapPoint> snapPoints = new List<SnapPoint>();
        [InspectorButton("RemoveSnaps")]
        public string ClearSnapPoints;

        public SnapPoint FindBestSnapPose(HandPose userPose, out ScoredHandPose bestHandPose)
        {
            SnapPoint bestSnap = null;
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

        public SnapPoint AddSnapPoint(HandPuppet puppet)
        {
            HandPose pose = puppet.TrackedPose(this.transform, true);
            SnapPoint snapPoint = GenerateSnapPoint();
            snapPoint.SetPose(pose, this.transform);
            snapPoint.LoadGhost(ghostProvider);

            return snapPoint;
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
            SnapPoint record = SnapPoint.Create(this.transform);
            this.snapPoints.Add(record);
            return record;
        }

#if UNITY_EDITOR
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

        private void SaveToAsset()
        {
            List<SnapPointData> savedPoses = new List<SnapPointData>();
            foreach (var snap in this.GetComponentsInChildren<SnapPoint>())
            {
                savedPoses.Add(snap.SaveData());
            }
            this.posesCollection.StorePoses(savedPoses);
        }

        private void RemoveSnaps()
        {
            if (this.snapPoints != null)
            {
                foreach (var snapPoint in this.snapPoints)
                {
                    if(snapPoint != null)
                    {
                        DestroyImmediate(snapPoint.gameObject);
                    }
                }
                this.snapPoints.Clear();
            }
        }
#endif
    }
}