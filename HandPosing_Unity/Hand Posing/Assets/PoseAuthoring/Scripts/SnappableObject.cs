using PoseAuthoring.PoseRecording;
using PoseAuthoring.PoseVolumes;
using System.Collections.Generic;
using UnityEngine;

namespace PoseAuthoring
{
    public class SnappableObject : MonoBehaviour
    {
        [SerializeField]
        private HandPosesCollection posesCollection;

        [Space]
        [InspectorButton("SaveToAsset")]
        public string StorePoses;
        [InspectorButton("LoadFromAsset")]
        public string LoadPoses;

        private List<SnapPose> snapPoses = new List<SnapPose>();

        public SnapPose FindBestSnapPose(HandPose userPose, out ScoredHandPose bestHandPose)
        {
            SnapPose bestSnap = null;
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

        public SnapPose AddPose(HandPuppet puppet)
        {
            HandPose pose = puppet.TrackedPose(this.transform, true);
            return AddPose(pose);
        }

        public SnapPose AddPose(HandPose pose)
        {
            GameObject go = new GameObject("Snap Point");
            go.transform.SetParent(this.transform, false);
            SnapPose record = go.AddComponent<SnapPose>();
            record.LoadPose(pose, this.transform);
            this.snapPoses.Add(record);
            return record;
        }

        public void LoadFromAsset()
        {
            if(posesCollection != null)
            {
                foreach (var handPose in posesCollection.Poses)
                {
                    AddPose(handPose);
                }
            }
        }

        public void SaveToAsset()
        {
            List<HandPose> savedPoses = new List<HandPose>();
            foreach (var snap in this.GetComponentsInChildren<SnapPose>())
            {
                //ghost.RefreshPose(this.transform);
                savedPoses.Add(snap.SavePose());
            }
            posesCollection.StorePoses(savedPoses);
        }
    }
}